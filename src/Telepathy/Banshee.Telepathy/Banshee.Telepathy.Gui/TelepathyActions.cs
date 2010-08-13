//
// TelepathyActions.cs
//
// Authors:
//   Neil Loknath <neil.loknath@gmail.com>
//
// Copyright (C) 2009 Neil Loknath
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

using Gtk;
using Mono.Addins;

using Banshee.Collection;
using Banshee.Collection.Database;
using Banshee.Gui;
using Banshee.MediaEngine;
using Banshee.ServiceStack;
using Banshee.Sources;
using Banshee.Telepathy.Data;
using Banshee.Telepathy.DBus;

using Banshee.Telepathy.API;
using Banshee.Telepathy.API.DBus;
using Banshee.Telepathy.API.Dispatchables;

using Hyena;

namespace Banshee.Telepathy.Gui
{
    public class TelepathyActions : BansheeActionGroup
    {
        private uint actions_id;
        private ContactContainerSource container;
        private Announcer announcer = null;

        public TelepathyActions (ContactContainerSource container) : base (ServiceManager.Get<InterfaceActionService> (), "telepathy-container")
        {
            this.container = container;

            Add (new ActionEntry [] {
                new ActionEntry ("DownloadTrackAction", null,
                    AddinManager.CurrentLocalizer.GetString ("Download Track(s)"), null,
                    AddinManager.CurrentLocalizer.GetString ("Download selected tracks to your computer"),
                    OnDownloadTrack)
            });

            Add (new ActionEntry [] {
                new ActionEntry ("CancelDownloadTrackAction", null,
                    AddinManager.CurrentLocalizer.GetString ("Cancel Download(s)"), null,
                    AddinManager.CurrentLocalizer.GetString ("Cancel download of selected tracks to your computer"),
                    OnCancelDownloadTrack)
            });

            Add (new ActionEntry [] {
                new ActionEntry ("CancelBrowseRequest", null,
                    AddinManager.CurrentLocalizer.GetString ("Cancel Browse Request"), "c",
                    AddinManager.CurrentLocalizer.GetString ("Cancel pending request to browse a contact's library"),
                    OnCancelBrowseRequest)
            });

            Add (new ToggleActionEntry [] {
                new ToggleActionEntry ("AllowDownloadsAction", null,
                    AddinManager.CurrentLocalizer.GetString ("Allow Downloads"), null,
                    AddinManager.CurrentLocalizer.GetString ("Allow file downloads when sharing libraries"),
                    OnAllowDownloads, ContactContainerSource.AllowDownloadsSchema.Get ())
            });

            Add (new ToggleActionEntry [] {
                new ToggleActionEntry ("AllowStreamingAction", null,
                    AddinManager.CurrentLocalizer.GetString ("Allow Streaming"), null,
                    AddinManager.CurrentLocalizer.GetString ("Allow streaming when sharing libraries"),
                    OnAllowStreaming, ContactContainerSource.AllowStreamingSchema.Get ())
            });

            Add (new ToggleActionEntry [] {
                new ToggleActionEntry ("ShareCurrentlyPlayingAction", null,
                    AddinManager.CurrentLocalizer.GetString ("Share Currently Playing"), null,
                    AddinManager.CurrentLocalizer.GetString ("Set Empathy presence message to what you're currently playing"),
                    OnShareCurrentlyPlaying, ContactContainerSource.ShareCurrentlyPlayingSchema.Get ())
            });

            actions_id = Actions.UIManager.AddUiFromResource ("GlobalUI.xml");
            Actions.AddActionGroup (this);

            ServiceManager.PlayerEngine.ConnectEvent (OnPlayerEvent,
                PlayerEvent.StartOfStream | PlayerEvent.StateChange);

            //OnUpdated (null, null);

            //Register ();

            Actions.TrackActions.PostActivate += OnTrackActionsActiviated;
        }

        public Source Parent {
            get { return container; }
        }

        public override void Dispose ()
        {
            ServiceManager.PlayerEngine.DisconnectEvent (OnPlayerEvent);
            Actions.UIManager.RemoveUi (actions_id);
            Actions.RemoveActionGroup (this);

            if (announcer != null) {
                announcer.Dispose ();
            }

            base.Dispose ();
        }

        private void AnnounceTrack (TrackInfo track)
        {
            if (announcer == null) {
                announcer = new Announcer (container.TelepathyService.ConnectionLocator);
            }

            if (announcer != null && ContactContainerSource.ShareCurrentlyPlayingSchema.Get ()) {
                if (track != null) {
                    announcer.Announce (String.Format (AddinManager.CurrentLocalizer.GetString ("Currently playing {0} by {1} from {2}"),
                                                track.TrackTitle, track.ArtistName, track.AlbumTitle));
                }
            }
        }

        private void OnAllowDownloads (object o, EventArgs args)
        {
            ToggleAction action = this["AllowDownloadsAction"] as Gtk.ToggleAction;
            container.UpdateDownloadingAllowed (action.Active);
        }

        private void OnAllowStreaming (object o, EventArgs args)
        {
            ToggleAction action = this["AllowStreamingAction"] as Gtk.ToggleAction;
            container.UpdateStreamingAllowed (action.Active);
        }

        private void OnShareCurrentlyPlaying (object o, EventArgs args)
        {
            ToggleAction action = this["ShareCurrentlyPlayingAction"] as Gtk.ToggleAction;
            ContactContainerSource.ShareCurrentlyPlayingSchema.Set (action.Active);

            if (announcer != null && !ContactContainerSource.ShareCurrentlyPlayingSchema.Get ()) {
                announcer.Announce (String.Empty);
            }
            else {
                AnnounceTrack (ServiceManager.PlayerEngine.CurrentTrack);
            }
        }

        private void OnDownloadTrack (object o, EventArgs args)
        {
            IContactSource source = ServiceManager.SourceManager.ActiveSource as IContactSource;
            if (source == null || !source.IsDownloadingAllowed) {
                return;
            }

            foreach (DatabaseTrackInfo track in source.DatabaseTrackModel.SelectedItems) {
                TelepathyDownloadKey key = new TelepathyDownloadKey (ContactTrackInfo.From (track));
                TelepathyDownload download = TelepathyService.DownloadManager.DownloadManager.Get (key);
                if (download == null) {
                    TelepathyService.DownloadManager.DownloadManager.Queue (
                        new TelepathyDownload (key)
                    );
                }
            }
        }

        private void OnCancelDownloadTrack (object o, EventArgs args)
        {
            DatabaseSource source = ServiceManager.SourceManager.ActiveSource as DatabaseSource;
            if (source == null) {
                return;
            }

            foreach (DatabaseTrackInfo track in source.DatabaseTrackModel.SelectedItems) {
                ContactTrackInfo.From (track).CancelTransfer ();
            }
        }

        private void OnCancelBrowseRequest (object o, EventArgs args)
        {
            // FIXME https://bugs.freedesktop.org/show_bug.cgi?id=22337
            ContactSource source = ServiceManager.SourceManager.ActiveSource as ContactSource;
            if (source != null) {
                Contact contact = source.Contact;
                DBusActivity activity = contact.DispatchManager.Get <DBusActivity> (contact, MetadataProviderService.BusName);

                if (activity != null) {
                    if (activity.State == ActivityState.RemotePending) {
                        activity.Close ();
                    }
                }
            }
        }

        private void OnTrackActionsActiviated (object o, EventArgs args)
        {
            IContactSource source = ServiceManager.SourceManager.ActiveSource as IContactSource;
            if (source == null) {
                return;
            }

            Contact contact = source.Contact;
            if (contact == null) {
                return;
            }

            if (source.IsDownloadingAllowed) {
                this["DownloadTrackAction"].Sensitive = true;
            }
            else {
                this["DownloadTrackAction"].Sensitive = false;
            }
        }

        private void OnPlayerEvent (PlayerEventArgs args)
        {
            if (ContactContainerSource.ShareCurrentlyPlayingSchema.Get ()) {
                switch (args.Event) {
                    case PlayerEvent.StartOfStream:
                        AnnounceTrack (ServiceManager.PlayerEngine.CurrentTrack);
                        break;
                    case PlayerEvent.StateChange:
                        PlayerEventStateChangeArgs state = args as PlayerEventStateChangeArgs;
                        if (state != null) {
                            switch (state.Current) {
                                case PlayerState.Paused:
                                    announcer.Announce (String.Empty);
                                    break;
                                case PlayerState.Playing:
                                    AnnounceTrack (ServiceManager.PlayerEngine.CurrentTrack);
                                    break;
                            }
                        }
                        break;
                }
            }
        }
    }
}
