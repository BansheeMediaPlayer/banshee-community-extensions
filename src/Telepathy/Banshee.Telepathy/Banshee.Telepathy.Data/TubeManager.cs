//
// TubeManager.cs
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


using Banshee.Telepathy.DBus;
using Banshee.Telepathy.Net;

using Banshee.Telepathy.API;
using Banshee.Telepathy.API.Data;
using Banshee.Telepathy.API.Dispatchables;

namespace Banshee.Telepathy.Data
{
	public class TubeManagerErrorEventArgs : EventArgs
	{
		public TubeManager.ErrorReason error;
		
		public TubeManagerErrorEventArgs (TubeManager.ErrorReason error)
		{
			this.error = error;	
		}
	}
	
	public class TubeManagerStateChangedEventArgs : EventArgs
	{
		public TubeManager.State state;
		
		public TubeManagerStateChangedEventArgs (TubeManager.State state)
		{
			this.state = state;	
		}
	}
	
	public class DownloadedTracksEventArgs : EventArgs
	{
		public LibraryDownload download;
		public object [] tracks;
		public string name;
		
		public DownloadedTracksEventArgs (LibraryDownload download, object [] tracks)
		{
			this.download = download;
			this.tracks = tracks;
		}
		
		public DownloadedTracksEventArgs (LibraryDownload download, object [] tracks, string name) : this (download, tracks)
		{
			this.name = name;	
		}
	}
	
	public class TubeManager : IDisposable
	{
		public event EventHandler <DownloadedTracksEventArgs> TracksDownloaded;
		public event EventHandler <DownloadedTracksEventArgs> PlaylistTracksDownloaded;
		public event EventHandler <TubeManagerStateChangedEventArgs> StateChanged;
		public event EventHandler <EventArgs> ResponseRequired;
		public event EventHandler <EventArgs> Closed;
		public event EventHandler <TubeManagerErrorEventArgs> Error;
		
		public enum State {
		        Unloaded,
		        Waiting,
		        PermissionGranted,
		        PermissionNotGranted,
		        LoadingMetadata,
		        LoadedMetadata,
		        LoadingPlaylists,
		        Loaded
    	};
		
		public enum ErrorReason {
			ClosedBeforeDownloaded,
			ErrorDuringLoad,
			ErrorDuringPlaylistLoad,
    	};
		
        private const int chunk_length = 250;
        private readonly LibraryDownloadMonitor download_monitor = new LibraryDownloadMonitor ();
		private MetadataProviderService provider_service;
		
		// call DBus method asynchronously
		private delegate bool GetBoolPropertyCaller ();
        private GetBoolPropertyCaller permission_caller;
        private GetBoolPropertyCaller downloading_caller;

		public TubeManager (Contact c)
		{
			Contact = c;
			Initialize ();
		}
		
		public Contact Contact { get; private set; }
		
		private State current_state = State.Unloaded;
		public State CurrentState {
			get { return current_state; }
			private set {
				if (current_state != value) {
					current_state = value;
					OnStateChanged (new TubeManagerStateChangedEventArgs (value));
				}
			}
		}
		
		public DBusActivity CurrentActivity { get; private set; }
		
		public bool IsDownloadingAllowed { get; private set; }

//		public static void StopSharing (Contact contact)
//        {
//            if (contact == null) {
//                return;
//            }
//
//            if (source.CurrentActivity != null) {
//                source.CurrentActivity.Close ();
//            }
//
//            StopStreaming (source);
//        }

        public static void StopStreaming (Contact contact)
        {
            if (contact == null) {
                return;
            }

            Activity activity = contact.DispatchManager.Get <StreamActivityListener> (contact, StreamingServer.ServiceName);
            if (activity != null) {
                activity.Close ();
            }
        }
		
        private void Initialize ()
        {
			Hyena.Log.Debug ("TubeManager.Initialize()");
            // let's listen for requests from our contact
            if (Contact != null) {
				Hyena.Log.Debug ("Listening for dispatched channels");
                Contact.DispatchManager.Dispatched += OnDispatched;
            }

            download_monitor.AllFinished += OnAllDownloadsFinished;
            download_monitor.AllProcessed += OnAllDownloadsProcessed;
        }

		public void Dispose ()
		{
			Dispose (true);
		}
		
		protected virtual void Dispose (bool disposing)
		{
			if (disposing) {
				if (Contact != null) {
                	Contact.DispatchManager.Dispatched -= OnDispatched;
            	}

            	UnregisterHandlers ();
			}
		}
		
		public void Browse ()
		{
			EnsureDBusActivity ();

            if (CurrentActivity != null) {
                // user clicked to browse a contact, but contact on the other end sent a
                // request also. The tube is probably slow and states have not changed yet,
                // so set waiting
                // TODO there is probably a race condition here - TEST
                if (CurrentActivity.State == ActivityState.RemotePending) {
                    CurrentState = State.Waiting;
                } else {
                    LoadData ();
                }
            } else {
                if (Contact.SupportedChannels.GetChannelInfo <DBusTubeChannelInfo> (MetadataProviderService.BusName) != null) {
                    if (CurrentState == State.Unloaded) {
						CurrentState = State.Waiting;
                        RequestDBusTube ();
                    }
                }
            }
		}
		
		public void AcceptBrowseRequest ()
		{
			if (provider_service == null) {
				CurrentActivity.Accept ();
			} else {
				provider_service.Permission = true;
			}
		}
		
		public void RejectBrowseRequest ()
		{
			if (provider_service == null) {
				CurrentActivity.Reject ();
			} else {
				provider_service.Permission = false;
			}
		}
		
		private void EnsureDBusActivity ()
        {
            if (Contact == null) {
                return;
            }

            if (CurrentActivity == null) {
                DispatchManager dm = Contact.DispatchManager;
                CurrentActivity = dm.Get <DBusActivity> (Contact, MetadataProviderService.BusName);
            }
        }

        private void RequestDBusTube ()
        {
            IDictionary <string, object> properties = new Dictionary <string, object> ();
            properties.Add ("ServiceName", MetadataProviderService.BusName);

            try {
                Contact.DispatchManager.Request <DBusActivity> (Contact, properties);
            } catch (Exception e) {
                Hyena.Log.Exception (e);
            }
        }

        private void RequestStreamTube ()
        {
            try {
                if (!Contact.DispatchManager.Exists <StreamActivityListener> (Contact, StreamingServer.ServiceName)) {
                    IDictionary <string, object> properties = new Dictionary <string, object> ();
                    properties.Add ("Service", StreamingServer.ServiceName);

                    Contact.DispatchManager.Request <StreamActivityListener> (Contact, properties);
                }
            } catch (Exception e) {
                Hyena.Log.Exception (e);
            }
        }
		
		private void RegisterActivityServices ()
        {
            RegisterActivityServices (true);
        }

        private void RegisterActivityServices (bool permission)
        {
            if (CurrentActivity == null) {
                return;
            }

            provider_service = new MetadataProviderService (CurrentActivity, permission);
			provider_service.PermissionRequired += (o, a) => {
				//ShowResponseMessage (o as MetadataProviderService);
				OnResponseRequired (EventArgs.Empty);
			};
			
            CurrentActivity.RegisterDBusObject (provider_service, MetadataProviderService.ObjectPath);
        }
		
		private bool IsActivityMatch (DBusActivity activity)
		{
			return (activity != null &&
                activity.Contact.Equals (Contact) &&
                activity.Service.Equals (MetadataProviderService.BusName));
		}
		
	    private void LoadData ()
        {
			Hyena.Log.Debug ("TubeManager.LoadData ()");
			
            if (CurrentState >= State.LoadingMetadata) {
                return;
            } else if (CurrentActivity == null) {
                return;
            } else if (CurrentActivity.State != ActivityState.Connected) {
                Hyena.Log.Debug (String.Format ("activity state {0} is invalid.", CurrentActivity.State));
                return;
            }

            IMetadataProviderService service = CurrentActivity.GetDBusObject <IMetadataProviderService> (MetadataProviderService.BusName, MetadataProviderService.ObjectPath);
            if (service == null) {
                Hyena.Log.Debug ("ContactSource.LoadData found service null");
                return;
            }

            try {
                // call MetadataProviderService.PermissionGranted () asynchronously to prevent blocking the UI
                // when Telepathy tubes are slow
                if (CurrentState <= State.Waiting) {
					Hyena.Log.Debug ("Setting to waiting state");
					CurrentState = State.Waiting;
                    permission_caller = new GetBoolPropertyCaller (service.PermissionGranted);
                    permission_caller.BeginInvoke (new AsyncCallback (delegate (IAsyncResult result) {
						if (CurrentState != State.Waiting) {
                			return;
            			}

			            GetBoolPropertyCaller caller = (GetBoolPropertyCaller) result.AsyncState;
			            bool granted = caller.EndInvoke (result);
			
			            if (granted) {
			                CurrentState = State.PermissionGranted;
			            } else {
			                CurrentState = State.PermissionNotGranted;
			            }
			
			            LoadData ();
						
					}), permission_caller);

                } else if (CurrentState == State.PermissionGranted) {
                    service.DownloadingAllowedChanged += delegate (bool allowed) {
                        IsDownloadingAllowed = allowed;
                    };

                    // determine if downloading is allowed asynchronously
                    downloading_caller = new GetBoolPropertyCaller (service.DownloadsAllowed);
                    downloading_caller.BeginInvoke (new AsyncCallback (delegate (IAsyncResult result) {
						
					    GetBoolPropertyCaller caller = (GetBoolPropertyCaller) result.AsyncState;
            			IsDownloadingAllowed = caller.EndInvoke (result);
						
					}), downloading_caller);

                    // clean up any residual tracks
                    download_monitor.Reset ();
                    CurrentState = State.LoadingMetadata;

                    string metadata_path = service.CreateMetadataProvider (LibraryType.Music).ToString ();
                    IMetadataProvider library_provider = CurrentActivity.GetDBusObject <IMetadataProvider> (MetadataProvider.BusName, metadata_path);

                    LibraryDownload download = new LibraryDownload ();
                    download_monitor.Add (metadata_path, download);

                    download.ProcessIncomingPayloads (delegate (object sender, object [] o) {
                        OnTracksDownloaded (new DownloadedTracksEventArgs (sender as LibraryDownload, o));
                    });

                    library_provider.ChunkReady += OnLibraryChunkReady;
                    library_provider.GetChunks (chunk_length);

                    download_monitor.Start ();

                } else if (CurrentState == State.PermissionNotGranted) {
					CurrentState = State.Waiting;
                    service.PermissionSet += OnPermissionSet;
                    service.RequestPermission ();
                }
            } catch (Exception e) {
                Hyena.Log.Exception (e);
				ResetState ();
				OnError (new TubeManagerErrorEventArgs (ErrorReason.ErrorDuringLoad));
            }
        }

        private void LoadPlaylists ()
        {
            if (CurrentActivity == null) {
                return;
            } else if (CurrentActivity.State != ActivityState.Connected) {
                Hyena.Log.Debug (String.Format ("activity state {0} is invalid.", CurrentActivity.State));
                return;
            } else if (CurrentState != State.LoadedMetadata) {
                Hyena.Log.Debug (String.Format ("state {0} is invalid.", CurrentState));
                return;
            }

            try {
                IMetadataProviderService service = CurrentActivity.GetDBusObject <IMetadataProviderService> (MetadataProviderService.BusName, MetadataProviderService.ObjectPath);
                int [] playlist_ids = service.GetPlaylistIds (LibraryType.Music);

                download_monitor.Reset ();

                if (playlist_ids.Length == 0) {
                    CurrentState = State.Loaded;
                } else {
                    foreach (int id in playlist_ids) {
                        string playlist_path = service.CreatePlaylistProvider (id).ToString ();
                        IPlaylistProvider playlist_provider = CurrentActivity.GetDBusObject <IPlaylistProvider>
                        (PlaylistProvider.BusName, playlist_path);

                        LibraryDownload download = new LibraryDownload ();
                        download_monitor.Add (playlist_path, download);
                        //download_monitor.AssociateObject (playlist_path, new ContactPlaylistSource (playlist_provider.GetName (), this));

                        download.ProcessIncomingPayloads (delegate (object sender, object [] o) {
                            OnPlaylistTracksDownloaded (new DownloadedTracksEventArgs (sender as LibraryDownload, o, playlist_provider.GetName ()));
                        });

                        playlist_provider.ChunkReady += OnPlaylistChunkReady;
                        playlist_provider.GetChunks (chunk_length);
                    }

                    download_monitor.Start ();
                }
            } catch (Exception e) {
                Hyena.Log.Exception (e);
				ResetState ();
                OnError (new TubeManagerErrorEventArgs (ErrorReason.ErrorDuringPlaylistLoad));
            }
        }

		private void ResetState ()
        {
            CurrentState = State.Unloaded;
            download_monitor.Reset ();
        }
		
		private void UnregisterHandlers ()
        {
            if (CurrentActivity != null) {
                CurrentActivity.ResponseRequired -= OnActivityResponseRequired;
                CurrentActivity.Ready -= OnActivityReady;
                CurrentActivity.Closed -= OnActivityClosed;
                CurrentActivity = null;
				provider_service = null;
            }
        }
		
		private void OnDispatched (object sender, EventArgs args)
        {
			Hyena.Log.Debug ("TubeManager.OnDispatched:");
			
            DBusActivity activity = sender as DBusActivity;
            if (IsActivityMatch (activity)) {
                Hyena.Log.Debug ("Registering event handlers for dispatched activity...");
                activity.ResponseRequired += OnActivityResponseRequired;
                activity.Ready += OnActivityReady;
                activity.Closed += OnActivityClosed;

                CurrentActivity = activity;
            }
        }
		
		private void OnActivityReady (object sender, EventArgs args)
        {
            DBusActivity activity = sender as DBusActivity;
            Hyena.Log.DebugFormat ("ContactSource OnReady for {0}", Contact.Name);

            // TODO decide if this is the right place for this
            // one contact may not stream, so the tube may not be
            // necessary. But, the OnReady and OnPermissionRequired events
            // only get raised for one contact.
            RequestStreamTube ();

            if (activity.InitiatorHandle != Contact.Connection.SelfHandle) {
                RegisterActivityServices ();

                // tube was not ready at the time user clicked source
                // so it was put into waiting state
                if (CurrentState == State.Waiting) {
                    LoadData ();
                }
            } else {
                RegisterActivityServices (false);
                LoadData ();
            }
        }

        private void OnActivityClosed (object sender, EventArgs args)
        {
            DBusActivity activity = sender as DBusActivity;
			ResetState ();

            if (activity.InitiatorHandle == Contact.Connection.SelfHandle) {
                StopStreaming (Contact);

                // the tube was closed before the library was fully downloaded
                // this seems to occur randomly
                if (!download_monitor.ProcessingFinished ()) {
                    OnError (new TubeManagerErrorEventArgs (ErrorReason.ClosedBeforeDownloaded));
                }
            }

			OnClosed (EventArgs.Empty);
            UnregisterHandlers ();
        }

        private void OnActivityResponseRequired (object sender, EventArgs args)
        {
            DBusActivity activity = sender as DBusActivity;
            Hyena.Log.DebugFormat ("OnActivityResponseRequired from {0} for {1}", activity.Contact.Handle, activity.Contact.Name);

            if (activity.InitiatorHandle != Contact.Connection.SelfHandle) {
                Hyena.Log.DebugFormat ("{0} handle {1} accepting tube from ContactSource", Contact.Name, Contact.Handle);
				OnResponseRequired (EventArgs.Empty);
    		}
        }
		
		private void OnAllDownloadsFinished (object sender, EventArgs args)
        {
            if (CurrentState == State.LoadingMetadata) {
                CurrentState = State.LoadedMetadata;
            } else {
                CurrentState = State.Loaded;
            }
        }

        private void OnAllDownloadsProcessed (object sender, EventArgs args)
        {
            if (CurrentState == State.LoadedMetadata) {
                //FIXME delay required to let tracks save to database
                //after download
                GLib.Timeout.Add (1000, delegate {
                    LoadPlaylists ();
					return false;
                });
            }
        }
		
		private void OnPermissionSet (bool granted)
        {
            if (granted) {
                Hyena.Log.Debug ("Permission granted");
                CurrentState = State.PermissionGranted;
                LoadData ();
            } else {
                Hyena.Log.Debug ("Permission denied");
                ResetState ();
            }
        }
		
		private void OnLibraryChunkReady (string object_path, IDictionary<string, object>[] chunk,
                           long timestamp, int seq_num, int total)
        {
            Hyena.Log.DebugFormat ("Library Chunk Ready timestamp {0} seq {1} tracks {2} path {3}",
                       timestamp, seq_num, chunk.Length, object_path);

            LibraryDownload current_download = download_monitor.Get (object_path);
            if (current_download != null)  {
                if (!current_download.IsStarted) {
                    Hyena.Log.Debug ("Initializing download");
                    current_download.Timestamp = timestamp;
                    current_download.TotalExpected = total;
                }

                current_download.UpdateDownload (timestamp, seq_num, chunk.Length, chunk);
            }
        }

        private void OnPlaylistChunkReady (string object_path, IDictionary<string, object>[] chunk,
                           long timestamp, int seq_num, int total)
        {
            Hyena.Log.DebugFormat ("Playlist Chunk Ready timestamp {0} seq {1} tracks {2} path {3}",
                       timestamp, seq_num, chunk.Length, object_path);

            LibraryDownload current_download = download_monitor.Get (object_path);
            if (current_download != null) {
                if (!current_download.IsStarted) {
                    Hyena.Log.Debug ("Initializing download");
                    current_download.Timestamp = timestamp;
                    current_download.TotalExpected = total;
                }

                 current_download.UpdateDownload (timestamp, seq_num, chunk.Length, chunk);
            }
        }
		
		protected virtual void OnStateChanged (TubeManagerStateChangedEventArgs args)
		{
			var handler = StateChanged;
			if (handler != null) {
				handler (this, args);
			}
		}
		
		protected virtual void OnTracksDownloaded (DownloadedTracksEventArgs args)
		{
			var handler = TracksDownloaded;
			if (handler != null) {
				handler (this, args);
			}
		}
		
		protected virtual void OnPlaylistTracksDownloaded (DownloadedTracksEventArgs args)
		{
			var handler = PlaylistTracksDownloaded;
			if (handler != null) {
				handler (this, args);
			}
		}
		
		protected virtual void OnResponseRequired (EventArgs args)
		{
			var handler = ResponseRequired;
			if (handler != null) {
				handler (this, args);
			}
		}
		
		protected virtual void OnClosed (EventArgs args)
		{
			var handler = Closed;
			if (handler != null) {
				handler (this, args);
			}
		}
		
		protected virtual void OnError (TubeManagerErrorEventArgs args)
		{
			var handler = Error;
			if (handler != null) {
				handler (this, args);
			}
		}
	}
}
