//
// SoundMenuService.cs
//
// Authors:
//   Bertrand Lorentz <bertrand.lorentz@gmail.com>
//
// Copyright (C) 2010 Bertrand Lorentz
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;

using Mono.Addins;
using Mono.Unix;
using Gtk;
using Notifications;

using Hyena;
using Banshee.Base;
using Banshee.Collection;
using Banshee.Collection.Gui;
using Banshee.Configuration;
using Banshee.Gui;
using Banshee.IO;
using Banshee.MediaEngine;
using Banshee.ServiceStack;
using Banshee.Preferences;

using Indicate;

namespace Banshee.SoundMenu
{
    public class SoundMenuService : IExtensionService
    {
        private bool? actions_supported;
        private ArtworkManager artwork_manager_service;
        private Notification current_nf;
        private TrackInfo current_track;
        private GtkElementsService elements_service;
        private InterfaceActionService interface_action_service;
        private string notify_last_artist;
        private string notify_last_title;
        private Server server;

        private const int icon_size = 42;

        public SoundMenuService ()
        {
        }

        void IExtensionService.Initialize ()
        {
            elements_service = ServiceManager.Get<GtkElementsService> ();
            interface_action_service = ServiceManager.Get<InterfaceActionService> ();

            var notif_addin = AddinManager.Registry.GetAddin("Banshee.NotificationArea");

            if (notif_addin != null && notif_addin.Enabled) {
                Log.Debug("NotificationArea conflicts with SoundMenu, disabling NotificationArea");
                notif_addin.Enabled = false;
            }

            AddinManager.AddinLoaded += OnAddinLoaded;

            if (!ServiceStartup ()) {
                ServiceManager.ServiceStarted += OnServiceStarted;
            }
        }

        private void OnServiceStarted (ServiceStartedArgs args)
        {
            if (args.Service is Banshee.Gui.InterfaceActionService) {
                interface_action_service = (InterfaceActionService)args.Service;
            } else if (args.Service is GtkElementsService) {
                elements_service = (GtkElementsService)args.Service;
            }

            ServiceStartup ();
        }

        private bool ServiceStartup ()
        {
            if (elements_service == null || interface_action_service == null) {
                return false;
            }

            interface_action_service.GlobalActions.Add (new ActionEntry [] {
                new ActionEntry ("CloseAction", Stock.Close,
                    AddinManager.CurrentLocalizer.GetString ("_Close"), "<Control>W",
                    AddinManager.CurrentLocalizer.GetString ("Close"), CloseWindow)
            });


            InstallPreferences ();
            server = Server.RefDefault ();
            if (Enabled) {
                Register ();
            }

            ServiceManager.PlayerEngine.ConnectEvent (OnPlayerEvent,
               PlayerEvent.StartOfStream |
               PlayerEvent.EndOfStream |
               PlayerEvent.TrackInfoUpdated |
               PlayerEvent.StateChange);

            artwork_manager_service = ServiceManager.Get<ArtworkManager> ();
            artwork_manager_service.AddCachedSize (icon_size);

            RegisterCloseHandler ();

            ServiceManager.ServiceStarted -= OnServiceStarted;

            return true;
        }

        public void Dispose ()
        {
            if (current_nf != null) {
                try {
                    current_nf.Close ();
                } catch {}
            }

            UninstallPreferences ();

            ServiceManager.PlayerEngine.DisconnectEvent (OnPlayerEvent);

            elements_service.PrimaryWindowClose = null;

            Gtk.Action close_action = interface_action_service.GlobalActions["CloseAction"];
            if (close_action != null) {
                interface_action_service.GlobalActions.Remove (close_action);
            }

            AddinManager.AddinLoaded -= OnAddinLoaded;

            elements_service = null;
            interface_action_service = null;
        }

        void OnAddinLoaded (object sender, AddinEventArgs args)
        {
            if (args.AddinId == "Banshee.NotificationArea") {
                Log.Debug("SoundMenu conflicts with NotificationArea, disabling SoundMenu");
                AddinManager.Registry.GetAddin("Banshee.SoundMenu").Enabled = false;
            }
        }

        public void Register ()
        {
            Log.Debug ("Registering with sound indicator");
            server.SetType ("music.banshee");
            string desktop_file = Paths.Combine (Paths.InstalledApplicationDataRoot,
                                                 "applications", "banshee-1.desktop");
            server.DesktopFile (desktop_file);
            server.Show ();
        }

        public void Unregister ()
        {
            server.Hide ();
        }

#region Notifications
        private bool ActionsSupported {
            get {
                if (!actions_supported.HasValue) {
                    actions_supported = Notifications.Global.Capabilities != null &&
                        Array.IndexOf (Notifications.Global.Capabilities, "actions") > -1;
                }

                return actions_supported.Value;
            }
        }

        private bool OnPrimaryWindowClose ()
        {
            CloseWindow (null, null);
            return true;
        }

        private void CloseWindow (object o, EventArgs args)
        {
            try {
                if (NotifyOnCloseSchema.Get ()) {
                    Notification nf = new Notification (
                        AddinManager.CurrentLocalizer.GetString ("Still Running"),
                        AddinManager.CurrentLocalizer.GetString (
                            "Banshee was closed to the sound menu. " +
                            "Use the <i>Quit</i> option to end your session."),
                            "media-player-banshee");
                    nf.Urgency = Urgency.Low;
                    nf.Show ();

                    NotifyOnCloseSchema.Set (false);
                }
            } catch (Exception e) {
                Log.Warning ("Error while trying to notify of window close.", e.Message, false);
            }

            elements_service.PrimaryWindow.Visible = false;
        }

        private void OnPlayerEvent (PlayerEventArgs args)
        {
            switch (args.Event) {
                case PlayerEvent.StartOfStream:
                case PlayerEvent.TrackInfoUpdated:
                    current_track = ServiceManager.PlayerEngine.CurrentTrack;
                    ShowTrackNotification ();
                    break;
                case PlayerEvent.EndOfStream:
                    current_track = null;
                    break;
            }
        }

        private void OnSongSkipped (object o, ActionArgs args)
        {
            if (args.Action == "skip-song") {
                ServiceManager.PlaybackController.Next ();
            }
        }

        private string GetByFrom (string artist, string display_artist, string album, string display_album)
        {
            bool has_artist = !String.IsNullOrEmpty (artist);
            bool has_album = !String.IsNullOrEmpty (album);

            string markup = null;
            if (has_artist && has_album) {
                // Translators: {0} and {1} are Artist Name and
                // Album Title, respectively;
                // e.g. 'by Parkway Drive from Killing with a Smile'
                markup = String.Format (AddinManager.CurrentLocalizer.GetString ("by '{0}' from '{1}'"),
                                        display_artist, display_album);
            } else if (has_album) {
                // Translators: {0} is for Album Title;
                // e.g. 'from Killing with a Smile'
                markup = String.Format (AddinManager.CurrentLocalizer.GetString ("from '{0}'"),
                                        display_album);
            } else {
                // Translators: {0} is for Artist Name;
                // e.g. 'by Parkway Drive'
                markup = String.Format(AddinManager.CurrentLocalizer.GetString ("by '{0}'"),
                                       display_artist);
            }
            return markup;
        }

        private void ShowTrackNotification ()
        {
            // This has to happen before the next if, otherwise the last_* members aren't set correctly.
            if (current_track == null || (notify_last_title == current_track.DisplayTrackTitle
                && notify_last_artist == current_track.DisplayArtistName)) {
                return;
            }

            notify_last_title = current_track.DisplayTrackTitle;
            notify_last_artist = current_track.DisplayArtistName;

            if (!ShowNotificationsSchema.Get ()) {
                return;
            }

            foreach (var window in elements_service.ContentWindows) {
                if (window.HasToplevelFocus) {
                    return;
                }
            }

            bool is_notification_daemon = false;
            try {
                var name = Notifications.Global.ServerInformation.Name;
                is_notification_daemon = name == "notification-daemon" || name == "Notification Daemon";
            } catch {
                // This will be reached if no notification daemon is running
                return;
            }

            string message = GetByFrom (
                current_track.ArtistName, current_track.DisplayArtistName,
                current_track.AlbumTitle, current_track.DisplayAlbumTitle);

            string image = null;

            image = is_notification_daemon
                ? CoverArtSpec.GetPathForSize (current_track.ArtworkId, icon_size)
                : CoverArtSpec.GetPath (current_track.ArtworkId);

            if (!File.Exists (new SafeUri(image))) {
                if (artwork_manager_service != null) {
                    // artwork does not exist, try looking up the pixbuf to trigger scaling or conversion
                    Gdk.Pixbuf tmp_pixbuf = is_notification_daemon
                        ? artwork_manager_service.LookupScalePixbuf (current_track.ArtworkId, icon_size)
                        : artwork_manager_service.LookupPixbuf (current_track.ArtworkId);

                    if (tmp_pixbuf == null) {
                        image = "audio-x-generic";
                    } else {
                        tmp_pixbuf.Dispose ();
                    }
                }
            }
            try {
                if (current_nf == null) {
                    current_nf = new Notification (current_track.DisplayTrackTitle, message, image);
                } else {
                    current_nf.Summary = current_track.DisplayTrackTitle;
                    current_nf.Body = message;
                    current_nf.IconName = image;
                }

                current_nf.Urgency = Urgency.Low;
                current_nf.Timeout = 4500;

                if (!current_track.IsLive && ActionsSupported && interface_action_service.PlaybackActions["NextAction"].Sensitive) {
                    current_nf.AddAction ("skip-song", AddinManager.CurrentLocalizer.GetString ("Skip this item"), OnSongSkipped);
                }
                current_nf.Show ();

            } catch (Exception e) {
                Log.Warning ("Cannot show notification", e.Message, false);
            }
        }

        private void RegisterCloseHandler ()
        {
            if (elements_service.PrimaryWindowClose == null) {
                elements_service.PrimaryWindowClose = OnPrimaryWindowClose;
            }
        }

        private void UnregisterCloseHandler ()
        {
            if (elements_service.PrimaryWindowClose != null) {
                elements_service.PrimaryWindowClose = null;
            }
        }
#endregion

#region Preferences
        private PreferenceBase enabled_pref;

        private void InstallPreferences ()
        {
            PreferenceService service = ServiceManager.Get<PreferenceService> ();
            if (service == null) {
                return;
            }

            enabled_pref = service["general"]["misc"].Add (
                new SchemaPreference<bool> (EnabledSchema,
                    Catalog.GetString ("_Show Banshee in the sound menu"),
                    Catalog.GetString ("Control Banshee through the sound menu"),
                    delegate { Enabled = EnabledSchema.Get (); })
            );
        }

        private void UninstallPreferences ()
        {
            PreferenceService service = ServiceManager.Get<PreferenceService> ();
            if (service == null) {
                return;
            }

            service["general"]["misc"].Remove (enabled_pref);
        }

        public bool Enabled {
            get { return EnabledSchema.Get (); }
            set {
                EnabledSchema.Set (value);
                if (value) {
                    Register ();
                    RegisterCloseHandler ();
                } else {
                    Unregister ();
                    UnregisterCloseHandler ();
                }
            }
        }

        private static readonly SchemaEntry<bool> EnabledSchema = new SchemaEntry<bool> (
            "plugins.soundmenu", "enabled",
            true,
            "Show Banshee in the sound menu",
            "Show Banshee in the sound menu"
        );

        public static readonly SchemaEntry<bool> ShowNotificationsSchema = new SchemaEntry<bool> (
            "plugins.soundmenu", "show_notifications",
            true,
            "Show notifications",
            "Show information notifications when item starts playing"
        );

        public static readonly SchemaEntry<bool> NotifyOnCloseSchema = new SchemaEntry<bool> (
            "plugins.soundmenu", "notify_on_close",
            true,
            "Show a notification when closing main window",
            "When the main window is closed, show a notification stating this has happened."
        );
#endregion

        string IService.ServiceName {
            get { return "SoundMenuService"; }
        }
    }
}