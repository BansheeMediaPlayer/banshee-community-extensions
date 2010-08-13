//
// AppIndicatorService.cs
//
// Authors:
//   Sense Hofstede <qense@ubuntu.com>
//   Chow Loong Jin <hyperair@ubuntu.com>
//   Aaron Bockover <abockover@novell.com>
//   Sebastian Dröge <slomo@circular-chaos.org>
//   Alexander Hixon <hixon.alexander@mediati.org>
//
// Copyright (C) 2010 Sense Hofstede
// Copyright (C) 2010 Chow Loong Jin
// Copyright (C) 2005-2008 Novell, Inc.
// Copyright (C) 2006-2007 Sebastian Dröge
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

using AppIndicator;
using Gtk;
using Notifications;

using Hyena;
using Banshee.Base;
using Banshee.Gui;
using Banshee.ServiceStack;
using Banshee.Collection;
using Banshee.Collection.Gui;
using Banshee.Configuration;
using Banshee.IO;
using Banshee.MediaEngine;

namespace Banshee.AppIndicator
{
    public class AppIndicatorService : IExtensionService
    {
        private BansheeActionGroup actions;
        private bool? actions_supported;
        private ArtworkManager artwork_manager_service;
        private Notification current_nf;
        private TrackInfo current_track;
        private bool disposed;
        private GtkElementsService elements_service;
        private ApplicationIndicator indicator;
        private InterfaceActionService interface_action_service;
        private string notify_last_artist;
        private string notify_last_title;
        private bool show_notifications;
        private int ui_manager_id;

        private const int icon_size = 42;

        public AppIndicatorService ()
        {
        }

        void IExtensionService.Initialize ()
        {
            elements_service = ServiceManager.Get<GtkElementsService> ();
            interface_action_service = ServiceManager.Get<InterfaceActionService> ();

            var notif_addin = AddinManager.Registry.GetAddin("Banshee.NotificationArea");

            if (notif_addin != null && notif_addin.Enabled) {
                Log.Debug("NotificationArea conflicts with ApplicationIndicator, disabling NotificationArea");
                notif_addin.Enabled = false;
            }

            AddinManager.AddinLoaded += OnAddinLoaded;

            if (!ServiceStartup ()) {
                ServiceManager.ServiceStarted += OnServiceStarted;
            }
        }

        void OnAddinLoaded (object sender, AddinEventArgs args)
        {
            if (args.AddinId == "Banshee.NotificationArea") {
                Log.Debug("ApplicationIndicator conflicts with NotificationArea, disabling ApplicationIndicator");
                AddinManager.Registry.GetAddin("Banshee.AppIndicator").Enabled = false;
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

            Initialize ();

            ServiceManager.ServiceStarted -= OnServiceStarted;

            return true;
        }

        private void Initialize ()
        {
            interface_action_service.GlobalActions.Add (new ActionEntry [] {
                new ActionEntry ("CloseAction", Stock.Close,
                    AddinManager.CurrentLocalizer.GetString ("_Close"), "<Control>W",
                    AddinManager.CurrentLocalizer.GetString ("Close"), CloseWindow)
            });

            actions = new BansheeActionGroup (interface_action_service, "AppIndicator");
            actions.Add (new ToggleActionEntry [] {
                new ToggleActionEntry ("ShowHideAction", null,
                    AddinManager.CurrentLocalizer.GetString ("_Show Banshee"), null,
                    AddinManager.CurrentLocalizer.GetString ("Show the Banshee main window"), ToggleShowHide, PrimaryWindowVisible)
            });

            interface_action_service.AddActionGroup (actions);
            ui_manager_id = (int)interface_action_service.UIManager.AddUiFromResource ("AppIndicatorMenu.xml");

            ServiceManager.PlayerEngine.ConnectEvent (OnPlayerEvent,
               PlayerEvent.StartOfStream |
               PlayerEvent.EndOfStream |
               PlayerEvent.TrackInfoUpdated |
               PlayerEvent.StateChange);

            artwork_manager_service = ServiceManager.Get<ArtworkManager> ();
            artwork_manager_service.AddCachedSize (icon_size);

            // Forcefully load this
            show_notifications = ShowNotifications;

            DrawAppIndicator ();
        }

        public void Dispose ()
        {
            if (disposed) {
                return;
            }

            if (current_nf != null) {
                try {
                    current_nf.Close ();
                } catch {}
            }

            // Hide the AppIndicator before disposing
            indicator.Status = Status.Passive;
            indicator.Dispose();
            indicator = null;

            ServiceManager.PlayerEngine.DisconnectEvent (OnPlayerEvent);

            elements_service.PrimaryWindowClose = null;

            Gtk.Action close_action = interface_action_service.GlobalActions["CloseAction"];
            if (close_action != null) {
                interface_action_service.GlobalActions.Remove (close_action);
            }

            if (ui_manager_id >= 0) {
                interface_action_service.RemoveActionGroup ("AppIndicator");
                interface_action_service.UIManager.RemoveUi ((uint)ui_manager_id);
                ui_manager_id = -1;
            }

            elements_service = null;
            interface_action_service = null;

            AddinManager.AddinLoaded -= OnAddinLoaded;

            disposed = true;
        }

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

        private void OnPrimaryWindowMapped (object o, MapEventArgs args)
        {
            ToggleAction showhideaction = (ToggleAction) actions["ShowHideAction"];
            if (showhideaction.Active == false) {
                showhideaction.Active = true;
            }
        }

        private void CloseWindow (object o, EventArgs args)
        {
            try {
                if (NotifyOnCloseSchema.Get ()) {
                    Notification nf = new Notification (
                        AddinManager.CurrentLocalizer.GetString ("Still Running"),
                        AddinManager.CurrentLocalizer.GetString (
                            "Banshee was closed to the notification area. " +
                            "Use the <i>Quit</i> option to end your session."),
                            "media-player-banshee");
                    nf.Urgency = Urgency.Low;
                    nf.Show ();

                    NotifyOnCloseSchema.Set (false);
                }
            } catch (Exception e) {
                Hyena.Log.Warning ("Error while trying to notify of window close.", e.Message, false);
            }

            PrimaryWindowVisible = false;
        }

        private bool DrawAppIndicator ()
        {
            try {
                indicator = new ApplicationIndicator ("banshee",
                                                      (IconThemeUtils.HasIcon ("banshee-panel")) ?
                                                      "banshee-panel" :
                                                      Banshee.ServiceStack.Application.IconName,
                                                      Category.ApplicationStatus);

                // Load the menu
                Menu menu = (Menu) interface_action_service.UIManager.GetWidget("/AppIndicatorTrayMenu");
                menu.Show ();

                indicator.Menu = menu;

                // Show the tray icon
                indicator.Status = Status.Active;

                if (!QuitOnCloseSchema.Get ()) {
                    RegisterCloseHandler ();
                }
            } catch (Exception e) {
                Hyena.Log.Warning ("Error while trying to create the Application Indicator.", e.Message, false);
                return false;
            }

            return true;
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

        private void ToggleShowHide (object o, EventArgs args)
        {
            PrimaryWindowVisible = ((ToggleAction)actions["ShowHideAction"]).Active;
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

            if (!show_notifications) {
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
                Hyena.Log.Warning ("Cannot show notification", e.Message, false);
            }
        }

        private void RegisterCloseHandler ()
        {
            if (elements_service.PrimaryWindowClose == null) {
                elements_service.PrimaryWindowClose = OnPrimaryWindowClose;
            }
            elements_service.PrimaryWindow.MapEvent += OnPrimaryWindowMapped;
        }

        private void UnregisterCloseHandler ()
        {
            if (elements_service.PrimaryWindowClose != null) {
                elements_service.PrimaryWindowClose = null;
            }
            elements_service.PrimaryWindow.MapEvent -= OnPrimaryWindowMapped;
        }

        public bool PrimaryWindowVisible {
            get {
                return elements_service.PrimaryWindow.Visible;
            }

            set {
                elements_service.PrimaryWindow.SetVisible(value);
                ((ToggleAction)actions["ShowHideAction"]).Active = value;
            }
        }

        public bool ShowNotifications {
            get {
                show_notifications = ShowNotificationsSchema.Get ();
                return show_notifications;
            }

            set {
                ShowNotificationsSchema.Set (value);
                show_notifications = value;
            }
        }

        public bool QuitOnClose {
            get {
                return QuitOnCloseSchema.Get ();
            }

            set {
                QuitOnCloseSchema.Set (value);
                if (value) {
                    UnregisterCloseHandler ();
                } else {
                    RegisterCloseHandler ();
                }
            }
        }

        string IService.ServiceName {
            get { return "AppIndicatorService"; }
        }

        public static readonly SchemaEntry<bool> EnabledSchema = new SchemaEntry<bool> (
            "plugins.app_indicator", "enabled",
            true,
            "Plugin enabled",
            "Notification area plugin enabled"
        );

        public static readonly SchemaEntry<bool> ShowNotificationsSchema = new SchemaEntry<bool> (
            "plugins.app_indicator", "show_notifications",
            true,
            "Show notifications",
            "Show information notifications when item starts playing"
        );

        public static readonly SchemaEntry<bool> NotifyOnCloseSchema = new SchemaEntry<bool> (
            "plugins.app_indicator", "notify_on_close",
            true,
            "Show a notification when closing main window",
            "When the main window is closed, show a notification stating this has happened."
        );

        public static readonly SchemaEntry<bool> QuitOnCloseSchema = new SchemaEntry<bool> (
            "plugins.app_indicator", "quit_on_close",
            false,
            "Quit on close",
            "Quit instead of hide to notification area on close"
        );


    }
}
