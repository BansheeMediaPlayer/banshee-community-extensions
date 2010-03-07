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

using System;
using System.Collections.Generic;
using Mono.Unix;

using Hyena.Data.Sqlite;
using Hyena;

using Banshee.Base;
using Banshee.Collection;
using Banshee.Collection.Database;
using Banshee.Gui.Dialogs;
using Banshee.Library;
using Banshee.Sources;
using Banshee.ServiceStack;
using Banshee.Telepathy.Gui;
using Banshee.Telepathy.DBus;
using Banshee.Telepathy.Net;

using Banshee.Telepathy.API;
using Banshee.Telepathy.API.Data;
using Banshee.Telepathy.API.Dispatchables;

namespace Banshee.Telepathy.Data
{
    public enum ContactSourceState {
        Unloaded,
        Waiting,
        PermissionGranted,
        PermissionNotGranted,
        LoadingMetadata,
        LoadedMetadata,
        LoadingPlaylists,
        Loaded
    };
    
    public class ContactSource : PrimarySource, IContactSource
    {
        private const int chunk_length = 250;
		private readonly TubeManager tube_manager;
		private readonly IDictionary<LibraryDownload, ContactPlaylistSource> playlist_map = new Dictionary<LibraryDownload, ContactPlaylistSource> ();
//        private ContactRequestDialog dialog;
        
//        private delegate bool GetBoolPropertyCaller ();
//        private GetBoolPropertyCaller permission_caller;
//        private GetBoolPropertyCaller downloading_caller;
        
        private static readonly string tmp_download_path = Paths.Combine (TelepathyService.CacheDirectory, "partial-downloads");
        public static string TempDownloadDirectory {
            get { return tmp_download_path; }
        }
        
        private HyenaSqliteCommand purge_source_command = new HyenaSqliteCommand (@"
            DELETE FROM CorePrimarySources WHERE PrimarySourceId = ?
        ");
        
		private SourceMessage response_message;
		private bool getting_response = false;
		
        public ContactSource (Contact contact) : base (Catalog.GetString ("Contact"), 
                                                       String.Format ("{0} ({1})", 
                                                                      contact != null ? contact.Name : String.Empty, 
                                                                      contact != null ? contact.Status.ToString () : String.Empty),
                                                       contact !=null ? contact.ToString () : String.Empty, 
                                                       300)
        {
            Contact = contact;
            Contact.ContactUpdated += OnContactUpdated;
            Hyena.Log.DebugFormat ("ContactSource created for {0}", Contact.Name);
            
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
            
            if (SavedCount > 0) {
                CleanUpData ();
            }
            SavedCount = 0;
			
			tube_manager = new TubeManager (contact);
			
            ContactSourceInitialize ();
            AfterInitialized ();
        }

        private ContactSourceState state = ContactSourceState.Unloaded;
        public ContactSourceState State {
            get { return state; }
        }
            
        private Contact contact;
        public Contact Contact {
            get { return contact; }
            protected set {
                if (value == null) {
                   throw new ArgumentNullException ("contact");
                }
                contact = value;
            }
        }
        
        public string AccountId {
            get {
                if (Contact != null) {
                    return Contact.AccountId;
                }
                return String.Empty;
            }
        }

        public string ContactName {
            get { 
                if (Contact != null) {
                    return Contact.Name; 
                }
                return String.Empty;
            }
        }

        public string ContactStatus {
            get {
                if (Contact != null) {
                    return Contact.Status.ToString (); 
                }
                return String.Empty;
            }
        }

        private DBusActivity current_activity = null;
        public DBusActivity CurrentActivity {
            get { return current_activity; }
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
        }

        public bool IsDownloadingAllowed {
            get { 
				if (tube_manager != null) {
					return tube_manager.IsDownloadingAllowed; 
				} else {
					return false;
				}
			}
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
            
            if (tube_manager != null) {
                tube_manager.Dispose ();
            }
            
            //UnregisterHandlers ();
            CleanUpData ();

            if (is_temporary) {
                PurgeSelf ();
            }
            
            base.Dispose ();
        }
        
        private void ContactSourceInitialize ()
        {
			tube_manager.StateChanged += OnTubeManagerStateChanged;
			tube_manager.ResponseRequired += OnTubeManagerResponseRequired;
			tube_manager.TracksDownloaded += OnTubeManagerTracksDownloaded;
			tube_manager.PlaylistTracksDownloaded += OnTubeManagerPlaylistTracksDownloaded;
			tube_manager.Closed += OnTubeManagerClosed;
			tube_manager.Error += OnTubeManagerError;
			
            TrackExternalObjectHandler = GetContactTrackInfoObject;
        }
        
        public override void Activate ()
        {
            if (Contact == null) {
                Hyena.Log.Error ("ContactSource.Activate found contact is null.");
                return;
            }
            
			if (getting_response) {
				return;
			}
			
            Hyena.Log.DebugFormat ("{0} selected", Contact.Name);

			tube_manager.Browse ();
            base.Activate ();
        }

        private object GetContactTrackInfoObject (DatabaseTrackInfo track)
        {
            return new ContactTrackInfo (track, this);
        }

        internal new void InvalidateCaches ()
        {
            ThreadAssist.SpawnFromMain (delegate {
                base.InvalidateCaches ();    
            });
        }
        
		private void ResetResponseMessage ()
		{
			getting_response = false;
			
			if (response_message != null) {
				RemoveMessage (response_message);
			}
		}
		
		private void ShowResponseMessage ()
		{
			getting_response = true;
			
			if (response_message == null) {
				response_message = new SourceMessage (this);
				response_message.CanClose = false;
            	response_message.IsSpinning = false;
            	response_message.SetIconName (null);
            	response_message.IsHidden = false;
			}
			
			PushMessage (response_message);
			response_message.FreezeNotify ();
			response_message.ClearActions ();
			
			string status_name = String.Format ("<i>{0}</i>", GLib.Markup.EscapeText (Name));
			string message = String.Format (Catalog.GetString ("{0} is requesting to browse your library"), Contact.Name);
			response_message.Text = String.Format (GLib.Markup.EscapeText (message), status_name);
            
            response_message.AddAction (new MessageAction (Catalog.GetString ("Accept"),
                delegate { 
					tube_manager.AcceptBrowseRequest ();
					ResetResponseMessage ();
				}));
            response_message.AddAction (new MessageAction (Catalog.GetString ("Reject"),
                delegate { 
					tube_manager.RejectBrowseRequest ();
					ResetResponseMessage ();
				}));

            response_message.ThawNotify ();
			TelepathyNotification.Create ().Show (Contact.Name, 
                    	Catalog.GetString ("is requesting to browse your Banshee library"));
			
			// show notify bubble every 30 seconds
			System.Timers.Timer notify_timer = new System.Timers.Timer (30000);
			notify_timer.Elapsed += (o, a) => {
                if (!getting_response) {
               		notify_timer.Stop ();
				} else {
					TelepathyNotification.Create ().Show (Contact.Name, 
                    	Catalog.GetString ("is requesting to browse your Banshee library"));
				}
            };
            notify_timer.AutoReset = true;
            notify_timer.Start ();
			
			// pulse source every 5 seconds
			NotifyUser ();
			System.Timers.Timer timer = new System.Timers.Timer (5000);
            timer.Elapsed += (o, a) => {
                if (!getting_response) {
               		timer.Stop ();
					notify_timer.Stop ();
				} else {
					NotifyUser ();
				}
            };
            timer.AutoReset = true;
            timer.Start ();
		}
		
#region Contact Events
        
        private void OnContactUpdated (object sender, ContactStatusEventArgs args)
        {
            this.Name = String.Format ("{0} ({1})", ContactName, ContactStatus);
        }

#endregion        
        
		private void OnTubeManagerStateChanged (object sender, EventArgs args)
        {
			TubeManagerStateChangedEventArgs state_args = args as TubeManagerStateChangedEventArgs;
			Hyena.Log.DebugFormat ("OnTubeManagerStateChanged: {0}", state_args.state.ToString ());
			
			switch (state_args.state) {
			case TubeManager.State.Unloaded:
				HideStatus ();
				break;
			case TubeManager.State.Waiting:
				SetStatus (Catalog.GetString ("Waiting for response from contact..."), false);
				break;
			//case TubeManager.State.PermissionNotGranted:
			//case TubeManager.State.PermissionGranted:
			case TubeManager.State.LoadingMetadata:
				if (Count > 0) {
					CleanUpData ();
				}
				SetStatus (Catalog.GetString ("Loading..."), false);
				break;
			case TubeManager.State.LoadedMetadata:
				SetStatus (Catalog.GetString ("All tracks downloaded. Loading..."), false);
				break;
			case TubeManager.State.LoadingPlaylists:
				SetStatus (Catalog.GetString ("Loading playlists..."), false);
				break;
			//case TubeManager.State.Loaded:
			}
		}
		
		private void OnTubeManagerError (object sender, EventArgs args)
		{
			TubeManagerErrorEventArgs error_args = args as TubeManagerErrorEventArgs;
			
			switch (error_args.error) {
			case TubeManager.ErrorReason.ClosedBeforeDownloaded:
				SetStatus (Catalog.GetString ("A problem occured while downloading this contact's library"), true);
				break;
			case TubeManager.ErrorReason.ErrorDuringLoad:
				SetStatus (Catalog.GetString ("An error occurred while loading data"), true);
				break;
			case TubeManager.ErrorReason.ErrorDuringPlaylistLoad:
				SetStatus (Catalog.GetString ("An error occurred while loading playlists"), true);
				break;
			}
		}
		
        private void OnTubeManagerClosed (object sender, EventArgs args)
        {
            TubeManager manager = sender as TubeManager;
            
            if (manager.CurrentActivity.InitiatorHandle != Contact.Connection.SelfHandle) {
//                if (dialog != null) {
//                    dialog.Destroy ();
//                    dialog = null;
//                }
				if (getting_response) {
					ResetResponseMessage ();
				}
            } else {
                
                TelepathyNotification.Create ().Show (Contact.Name, 
                    Catalog.GetString ("is no longer sharing their Banshee library with you"));
            }
        }
        
		private long CalculateLoadingTracks (int track_count, long expected)
		{
			long loading = Count + track_count;
			if (loading > expected) {
				loading = expected;
			}
		
			return loading;
		}
		
		private void OnTubeManagerPlaylistTracksDownloaded (object sender, EventArgs args)
		{
			DownloadedTracksEventArgs track_args = args as DownloadedTracksEventArgs;
			IDictionary <string, object> [] chunk = track_args.tracks as IDictionary <string, object> [];
            if (chunk == null) {
                return;
            }

            LibraryDownload d = track_args.download as LibraryDownload;

			ThreadAssist.ProxyToMain (delegate {
				SetStatus (String.Format (Catalog.GetString ("Loading {0} of {1}"), 
					CalculateLoadingTracks (chunk.Length, d.TotalExpected), 
			    	d.TotalExpected), false);
			});
            
			ContactPlaylistSource source = null;
			if (playlist_map.ContainsKey (d)) {	
				source = playlist_map[d];
			} else {
				source = new ContactPlaylistSource (track_args.name, this);
				playlist_map.Add (d, source);
            } 

			source.AddTracks (chunk);
			
            ThreadAssist.ProxyToMain (delegate {
                if (d != null && d.IsFinished) {
                    Hyena.Log.DebugFormat ("Download complete for {0}", source.Name);
                    AddChildSource (source);
					playlist_map.Remove (d);
                    HideStatus ();
                }
            });			
		}
		
		private void OnTubeManagerTracksDownloaded (object sender, EventArgs args)
		{
			DownloadedTracksEventArgs track_args = args as DownloadedTracksEventArgs;
			
			IDictionary <string, object> [] chunk = track_args.tracks as IDictionary <string, object> [];
            if (chunk == null) {
                return;
            }
            
			LibraryDownload d = track_args.download as LibraryDownload;
		
			ThreadAssist.ProxyToMain (delegate {
				SetStatus (String.Format (Catalog.GetString ("Loading {0} of {1}"), 
					CalculateLoadingTracks (chunk.Length, d.TotalExpected), 
			    	d.TotalExpected), false);
			});
			
            HyenaSqliteConnection conn = ServiceManager.DbConnection;
            conn.BeginTransaction ();
            
            for (int i = 0; i < chunk.Length; i++) {
                IDictionary <string, object> track = chunk[i];
                ContactTrackInfo contact_track = new ContactTrackInfo (track, this);

                // notify once per chunk
                if (i == chunk.Length - 1) {
                    conn.CommitTransaction ();
                    contact_track.Save (true);
                } else {
                    contact_track.Save (false);
                }
            }
		}
		
        private void OnTubeManagerResponseRequired (object sender, EventArgs args)
        {
//            DBusActivity activity = sender as DBusActivity;
//            Hyena.Log.DebugFormat ("OnActivityResponseRequired from {0} for {1}", activity.Contact.Handle, activity.Contact.Name);
                             
            //if (activity.InitiatorHandle != Contact.Connection.SelfHandle) {
                Hyena.Log.DebugFormat ("{0} handle {1} accepting tube from ContactSource", Contact.Name, Contact.Handle);
                                          
				ShowResponseMessage ();
//                dialog = new ContactRequestDialog (Contact.Name);
//                dialog.ShowAll ();
//                dialog.Response += delegate (object o, Gtk.ResponseArgs e) {
//                    try {
//                        if (e.ResponseId == Gtk.ResponseType.Accept) {               
//                            activity.Accept ();
//                        } else if (e.ResponseId == Gtk.ResponseType.Reject) {
//                            activity.Reject ();
//                        }
//                    } catch (Exception ex) {
//                        Hyena.Log.Exception (ex);
//                    }
//
//                    if (dialog !=  null) {
//                        dialog.Destroy ();
//                        dialog = null;
//                    }
//                };
                
            //}
        }
    }
}
