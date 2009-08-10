//
// ContactSource.cs
//
// Author:
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

//using Gtk;
using System;
using System.Collections.Generic;
using Mono.Unix;

using Hyena;
using Hyena.Data.Sqlite;

using Banshee.Base;
using Banshee.Collection;
using Banshee.Collection.Database;
using Banshee.Gui.Dialogs;
using Banshee.Library;
using Banshee.Sources;
using Banshee.ServiceStack;

using Banshee.Telepathy.API;
using Banshee.Telepathy.API.Dispatchables;

using Banshee.Telepathy.Gui;
using Banshee.Telepathy.DBus;
using Banshee.Telepathy.Net;

using NDesk.DBus;

namespace Banshee.Telepathy.Data
{
    public enum ContactSourceState {
        Unloaded,
        LoadingMetadata,
        LoadedMetadata,
        LoadingPlaylists,
        Loaded
    };
    
    public class ContactSource : PrimarySource, IContactSource
    {
        private Contact contact;
        private readonly DownloadMonitor download_monitor = new DownloadMonitor ();
        private ContactRequestDialog dialog;
        

        private static readonly string tmp_download_path = Paths.Combine (TelepathyService.CacheDirectory, "partial-downloads");
        public static string TempDownloadDirectory {
            get { return tmp_download_path; }
        }
        
        private HyenaSqliteCommand purge_source_command = new HyenaSqliteCommand (@"
            DELETE FROM CorePrimarySources WHERE PrimarySourceId = ?
        ");
                
        public ContactSource (Contact contact) : base (Catalog.GetString ("Contact"), String.Format ("{0} ({1})", contact.Name, contact.Status.ToString ()),
                                                    contact.ToString (), 300)
        {
            this.contact = contact;
            contact.ContactUpdated += OnContactUpdated;
            Log.DebugFormat ("ContactSource created for {0}", contact.Name);
            
            //Properties.SetString ("UnmapSourceActionLabel", Catalog.GetString ("Disconnect"));
            //Properties.SetString ("UnmapSourceActionIconName", "gtk-disconnect");
            Properties.SetString ("Icon.Name", "stock_person");
            Properties.SetString ("ActiveSourceUIResource", "ActiveSourceUI.xml");
            Properties.Set<bool> ("ActiveSourceUIResourcePropagate", true);
            Properties.SetString ("GtkActionPath", "/ContactSourcePopup");

            Properties.SetString ("TrackView.ColumnControllerXml", @"
                    <column-controller>
                      <add-all-defaults />
                      <column modify-default=""IndicatorColumn"">
                          <renderer type=""Banshee.Telepathy.Gui.ColumnCellContactStatusIndicator"" />
                      </column>
                    </column-controller>
                "
            );
            
            SupportsPlaylists = false;
            SavedCount = 0;
            //UpdateIcon ();

            ContactSourceInitialize ();
            AfterInitialized ();
        }

        private ContactSourceState state = ContactSourceState.Unloaded;
        public ContactSourceState State {
            get { return state; }
        }
            
        public Contact Contact {
            get { return contact; }
        }
        
        public string AccountId {
            get { return contact.AccountId; }
        }

        public string ContactName {
            get { return contact.Name; }
        }

        public string ContactStatus {
            get { return contact.Status.ToString (); }
        }
        
        public override bool CanRemoveTracks {
            get { return false; }
        }

        public override bool CanDeleteTracks {
            get { return false; }
        }

        public override bool ConfirmRemoveTracks {
            get { return false; }
        }

        public override bool HasEditableTrackProperties {
            get { return false; }
        }
        
        private bool can_activate = true;
        public override bool CanActivate {
            get { return can_activate; }
        }

        private bool is_temporary = true;
        public bool IsTemporary {
            get { return is_temporary; }
            //set { is_temporary = value; }
        }

        protected override void Initialize ()
        { 
            base.Initialize ();
            ContactSourceInitialize ();
        }

        private void PurgeSelf ()
        {
            ServiceManager.DbConnection.Execute (purge_source_command, DbId);
        }

        public void CleanUpData ()
        {
            PurgeTracks ();

            List<Source> children = new List<Source> (Children);
            foreach (Source child in children) {
                if (child is Banshee.Sources.IUnmapableSource) {
                    (child as Banshee.Sources.IUnmapableSource).Unmap ();
                }
            }
            
            ClearChildSources ();
        }
            
        public override void Dispose ()
        {
            can_activate = false;
            
            DBusActivity.ResponseRequired -= OnActivityResponseRequired;
            DBusActivity.Ready -= OnActivityReady;
            DBusActivity.Closed -= OnActivityClosed;

            CleanUpData ();

            DispatchManager dm  = contact.DispatchManager;
            dm.RemoveAll (contact);
            
            if (is_temporary) {
                PurgeSelf ();
            }
            
            base.Dispose ();
        }
        
        private void ContactSourceInitialize ()
        {
            // let's listen for requests from our contacts
            DBusActivity.ResponseRequired += OnActivityResponseRequired;
            DBusActivity.Ready += OnActivityReady;
            DBusActivity.Closed += OnActivityClosed;

            download_monitor.AllFinished += OnAllDownloadsFinished;

            TrackExternalObjectHandler = GetContactTrackInfoObject;
        }

        public override void Activate ()
        {
            Log.DebugFormat ("{0} selected", contact.Name);

            if (contact.DispatchManager.Exists <DBusActivity> (contact, MetadataProviderService.BusName)) {
                LoadData ();
            }
            else {
                if (contact.HasService (MetadataProviderService.BusName)) {
                    SetStatus (Catalog.GetString ("Waiting for response from contact"), false);
                    
                    IDictionary <string, object> properties = new Dictionary <string, object> ();
                    properties.Add ("ServiceName", MetadataProviderService.BusName);
                    
                    DispatchManager dm = contact.Connection.DispatchManager;
                    dm.Request <DBusActivity> (contact, properties);
                }
                else {
                    SetStatus (Catalog.GetString ("Contact does not support Telepathy extension"), true);
                }
            }

            base.Activate ();
        }

        private object GetContactTrackInfoObject (DatabaseTrackInfo track)
        {
            return new ContactTrackInfo (track, this);
        }

        private void RequestStreamTube ()
        {
            if (!Contact.DispatchManager.Exists <StreamActivityListener> (Contact, StreamingServer.ServiceName)) {
                IDictionary <string, object> properties = new Dictionary <string, object> ();
                properties.Add ("Service", StreamingServer.ServiceName);
                Contact.DispatchManager.Request <StreamActivityListener> (Contact, properties);
            }
        }
        
#region Activity Interaction
        
        private void RegisterActivityServices (DBusActivity activity)
        {
            RegisterActivityServices (activity, true);
        }

        private void RegisterActivityServices (DBusActivity activity, bool permission)
        {
            IMetadataProviderService provider_service = new MetadataProviderService (activity, permission);
            activity.RegisterDBusObject (provider_service, MetadataProviderService.ObjectPath);
        }

        private void LoadData ()
        {
            DBusActivity activity = contact.DispatchManager.Get <DBusActivity> (contact, MetadataProviderService.BusName);
            LoadData (activity);
        }
            
        private void LoadData (DBusActivity activity)
        {
            if (activity.State != ActivityState.Connected || state != ContactSourceState.Unloaded) {
                return;
            }
        
            IMetadataProviderService service = activity.GetDBusObject <IMetadataProviderService> (MetadataProviderService.BusName, MetadataProviderService.ObjectPath);
           
            if (service.PermissionGranted ()) {

                // clean up any residual tracks
                download_monitor.Reset ();
                SetStatus (Catalog.GetString ("Loading..."), false);
                state = ContactSourceState.LoadingMetadata;
                CleanUpData ();

                string metadata_path = service.CreateMetadataProvider (LibraryType.Music).ToString ();
                Log.DebugFormat ("Metadata path {0}", metadata_path);
                
                IMetadataProvider library_provider = activity.GetDBusObject <IMetadataProvider>
                    (MetadataProvider.BusName, metadata_path);

                download_monitor.Add (metadata_path, new Download ());
                
                library_provider.ChunkReady += OnLibraryChunkReady;
                library_provider.GetChunks (400);

                download_monitor.Start ();
            }
            else {
                SetStatus (Catalog.GetString ("Waiting for response from contact"), false);
                
                service.PermissionResponse += OnPermissionResponse;
                service.RequestPermission ();
            }
        }

        private void LoadPlaylists ()
        {
            DBusActivity activity = contact.DispatchManager.Get <DBusActivity> (contact, MetadataProviderService.BusName);
            LoadPlaylists (activity);
        }
        
        private void LoadPlaylists (DBusActivity activity)
        {
            if (activity.State != ActivityState.Connected || state != ContactSourceState.LoadedMetadata) {
                return;
            }
        
            IMetadataProviderService service = activity.GetDBusObject <IMetadataProviderService> (MetadataProviderService.BusName, MetadataProviderService.ObjectPath);
            
            int [] playlist_ids = service.GetPlaylistIds (LibraryType.Music);

            download_monitor.Reset ();
            
            if (playlist_ids.Length == 0) {
                state = ContactSourceState.Loaded;
                HideStatus ();
            } 
            else {
                foreach (int id in playlist_ids) {
                    string playlist_path = service.CreatePlaylistProvider (id).ToString ();
                    Log.DebugFormat ("Playlist path {0}", playlist_path);
                    
                    IPlaylistProvider playlist_provider = activity.GetDBusObject <IPlaylistProvider>
                    (PlaylistProvider.BusName, playlist_path);

                    download_monitor.Add (playlist_path, new Download ());
                    download_monitor.AssociateObject (playlist_path, new ContactPlaylistSource (playlist_provider.GetName (), this));
                    
                    playlist_provider.ChunkReady += OnPlaylistChunkReady;
                    playlist_provider.GetChunks (400);
                }

                download_monitor.Start ();
            }
        }
        
        private void ResetStatus ()
        {
            state = ContactSourceState.Unloaded;
            download_monitor.Reset ();
            HideStatus ();
        }

#endregion

#region Contact Events
        
        private void OnContactUpdated (object sender, ContactStatusEventArgs args)
        {
            this.Name = String.Format ("{0} ({1})", ContactName, ContactStatus);
        }

#endregion        
        
#region Activity Events

        private void OnAllDownloadsFinished (object sender, EventArgs args)
        {
            if (state == ContactSourceState.LoadingMetadata) {
                state = ContactSourceState.LoadedMetadata;

                SetStatus (Catalog.GetString ("Loading playlists"), false);
                
                //FIXME delay required to let tracks save to database
                //after download
                System.Timers.Timer timer = new System.Timers.Timer (1000);
                timer.Elapsed += delegate {
                    LoadPlaylists ();
                    timer.Stop ();
                };
                timer.AutoReset = false;
                timer.Start ();
            }
            else {
                state = ContactSourceState.Loaded;
            }
        }
        
        private void OnActivityReady (object sender, EventArgs args)
        {
            DBusActivity activity = sender as DBusActivity;
            if (activity == null || !activity.Service.Equals (MetadataProviderService.BusName)) {
                return;
            }
            
            Log.DebugFormat ("ContactSource OnReady for {0}", contact.Name);
            
            if (contact.Equals (activity.Contact)) {
                // TODO decide if this is the right place for this
                RequestStreamTube ();
                
                if (activity.InitiatorHandle != contact.Connection.SelfHandle) {
                    RegisterActivityServices (activity);
                }
                else {
                    RegisterActivityServices (activity, false);
                    LoadData (activity);
                }
            }
        }

        private void OnActivityClosed (object sender, EventArgs args)
        {
            DBusActivity activity = sender as DBusActivity;
            if (activity == null || !activity.Service.Equals (MetadataProviderService.BusName)) {
                return;
            }

            if (contact.Equals (activity.Contact)) {
                if (activity.InitiatorHandle != contact.Connection.SelfHandle) {
                    if (dialog != null) {
                        dialog.Destroy ();
                        dialog = null;
                    }
                }
            }

            ResetStatus ();
        }
        
        private void OnActivityResponseRequired (object sender, EventArgs args)
        {
            DBusActivity activity = sender as DBusActivity;
            if (activity == null || !activity.Service.Equals (MetadataProviderService.BusName)) {
                return;
            }
            
            Log.DebugFormat ("OnActivityResponseRequired from {0} for {1}", activity.Contact.Handle, contact.Name);
                             
            if (contact.Equals (activity.Contact) &&
                activity.InitiatorHandle != contact.Connection.SelfHandle) {
                Log.DebugFormat ("{0} handle {1} accepting tube from ContactSource", contact.Name, contact.Handle);
                
                dialog = new ContactRequestDialog ("Contact Request", 
                                                                    String.Format ("{0} would like to browse your music library.",
                                                                    contact.Name));
                dialog.ShowAll ();
                dialog.Response += delegate (object o, Gtk.ResponseArgs e) {
                    try {
                        if (e.ResponseId == Gtk.ResponseType.Accept) {               
                            activity.Accept ();
                        }
                        else if (e.ResponseId == Gtk.ResponseType.Reject) {
                            activity.Reject ();
                        }
                    }
                    catch (Exception ex) {
                        Log.Exception (ex);
                    }

                    if (dialog !=  null) {
                        dialog.Destroy ();
                        dialog = null;
                    }
                };
                
            }
        }
        
#endregion
        
#region MetadataServiceProvider Events
        
        private void OnPermissionResponse (bool granted)
        {
            if (granted) {
                Log.Debug ("Permission granted");
                LoadData ();
            }
            else {
                Log.Debug ("Permission denied");
                ResetStatus ();
            }
        }

        private void OnLibraryChunkReady (string object_path, IDictionary<string, object>[] chunk, 
                           long timestamp, int seq_num, int total)
        {
            Log.DebugFormat ("Library Chunk Ready timestamp {0} seq {1} tracks {2} path {3}",
                       timestamp, seq_num, chunk.Length, object_path);
            
            ContactTrackInfo contact_track = null;

            Download current_download = download_monitor.Get (object_path);

            if (!current_download.IsStarted) {
                Log.Debug ("Initializing download");
                current_download.Timestamp = timestamp;
                current_download.TotalExpected = total;
            }

            current_download.UpdateDownload (timestamp, seq_num, chunk.Length);

            string message = String.Format ("Loading {0} of {1}", current_download.TotalDownloaded, total);
            SetStatus (Catalog.GetString (message), false);
            
            ThreadAssist.Spawn (delegate {
                HyenaSqliteConnection conn = ServiceManager.DbConnection;
                conn.BeginTransaction ();
                
                for (int i = 0; i < chunk.Length; i++) {
                    IDictionary <string, object> track = chunk[i];
                    contact_track = new ContactTrackInfo (track, this);

                    // notify once per chunk
                    if (i == chunk.Length - 1) {
                        conn.CommitTransaction ();
                        contact_track.Save (true);
                    } else {
                        contact_track.Save (false);
                    }
                    
                }
            });
            
            if (current_download.IsFinished && !current_download.Processed) {
                current_download.Processed = true;
                Log.DebugFormat ("Download complete for {0}", object_path);
            }
        }

        private void OnPlaylistChunkReady (string object_path, IDictionary<string, object>[] chunk, 
                           long timestamp, int seq_num, int total)
        {
            Log.DebugFormat ("Playlist Chunk Ready timestamp {0} seq {1} tracks {2} path {3}",
                       timestamp, seq_num, chunk.Length, object_path);

            Download current_download = download_monitor.Get (object_path);
                        
            if (!current_download.IsStarted) {
                Log.Debug ("Initializing download");
                current_download.Timestamp = timestamp;
                current_download.TotalExpected = total;
            }

            current_download.UpdateDownload (timestamp, seq_num, chunk.Length);
            
            string message = String.Format ("Loading {0} of {1}", current_download.TotalDownloaded, total);
            SetStatus (Catalog.GetString (message), false);
            
            ThreadAssist.Spawn (delegate {
                ContactPlaylistSource source = download_monitor.GetAssociatedObject (current_download) as ContactPlaylistSource;
                
                if (source != null) {
                    source.AddTracks (chunk);
                }

                ThreadAssist.ProxyToMain ( delegate {
                    if (current_download.IsFinished && !current_download.Processed) {
                        current_download.Processed = true;
                        Log.DebugFormat ("Download complete for {0}", object_path);
                        AddChildSource (source);
                        HideStatus ();
                    }
                });
            });
            
            
        }

#endregion
        
    }
}
