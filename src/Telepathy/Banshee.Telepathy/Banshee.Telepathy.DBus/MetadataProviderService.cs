//
// MetadataProviderService.cs
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

using Banshee.Collection.Database;
using Banshee.ServiceStack;
using Banshee.Telepathy.Data;

using Banshee.Telepathy.API.Dispatchables;

using Hyena;

using DBus;

namespace Banshee.Telepathy.DBus
{
    public enum LibraryType
    {
        Music,
        Video
    };

    public delegate void PermissionSetHandler (bool granted);
    public delegate void DownloadingAllowedHandler (bool allowed);
    public delegate void StreamingAllowedHandler (bool allowed);

    public class MetadataProviderService : IMetadataProviderService
    {
        public event PermissionSetHandler PermissionSet;
		public event EventHandler<EventArgs> PermissionRequired;
        public event DownloadingAllowedHandler DownloadingAllowedChanged;
        public event StreamingAllowedHandler StreamingAllowedChanged;

        private DBusActivity activity;

        private MetadataProviderService ()
        {
        }

        public MetadataProviderService (DBusActivity activity)
        {
            if (activity == null) {
                throw new ArgumentNullException ("activity");
            }

            this.activity = activity;

            ContactContainerSource.DownloadingAllowedChanged += (o, a) => OnDownloadingAllowedChanged (DownloadsAllowed ());
            ContactContainerSource.StreamingAllowedChanged += (o, a) => OnStreamingAllowedChanged (StreamingAllowed ());
        }

        public MetadataProviderService (DBusActivity activity, bool permission) : this (activity)
        {
            this.permission_granted = permission;
        }

        private bool permission_granted = true;
		public bool Permission {
			get { return permission_granted; }
			set {
				permission_granted = value;
				OnPermissionSet (permission_granted);
			}
		}
		
        public bool PermissionGranted ()
        {
            return permission_granted;
        }

        public ObjectPath CreateMetadataProvider (LibraryType type)
        {
            if (activity == null || !permission_granted) {
                return new ObjectPath ("");
            }

            MetadataProvider provider = new MetadataProvider (activity, type);
            activity.RegisterDBusObject (provider, provider.ObjectPath);
            return new ObjectPath (provider.ObjectPath);
            //return ServiceManager.DBusServiceManager.RegisterObject (new MetadataProvider (this, type));
        }

        public ObjectPath CreatePlaylistProvider (int id)
        {
            if (activity == null || !permission_granted) {
                return new ObjectPath ("");
            }

            PlaylistProvider provider = new PlaylistProvider (activity, id);
            activity.RegisterDBusObject (provider, provider.ObjectPath);
            return new ObjectPath (provider.ObjectPath);
        }

        public int[] GetPlaylistIds (LibraryType type)
        {
            //Console.WriteLine ("I am in GetPlaylistIds");
            int primary_source_id = 0;

            switch (type) {
                case LibraryType.Music:
                    primary_source_id = ServiceManager.SourceManager.MusicLibrary.DbId;
                    break;
                case LibraryType.Video:
                    primary_source_id = ServiceManager.SourceManager.VideoLibrary.DbId;
                    break;
            }

            int array_size = ServiceManager.DbConnection.Query<int> (
                "SELECT COUNT(*) FROM CorePlaylists WHERE PrimarySourceID = ?", primary_source_id
            );

            IEnumerable <int> ids = ServiceManager.DbConnection.QueryEnumerable <int> (
                "SELECT PlaylistID FROM CorePlaylists WHERE PrimarySourceID = ?", primary_source_id
            );

            int[] playlist_ids = new int[array_size];
            int index = 0;
            foreach (int id in ids) {
                playlist_ids[index++] = id;
            }

            return playlist_ids;

        }

        public void RequestPermission ()
        {
            if (activity == null || activity.Contact == null) {
                Permission = false;
                return;
            }
            else if (permission_granted) {
                Permission = true;
                return;
            }
			
			OnPermissionRequired (EventArgs.Empty);
			
//            Contact contact = activity.Contact;
//
//            ContactRequestDialog dialog = new ContactRequestDialog (contact.Name);
//            dialog.ShowAll ();
//            dialog.Response += delegate(object o, ResponseArgs e) {
//                if (e.ResponseId == ResponseType.Accept) {
//                    permission_granted = true;
//                }
//
//                dialog.Destroy ();
//                OnPermissionResponse (permission_granted);
//            };
        }

        public void DownloadFile (long external_id, string content_type)
        {
            if (activity == null || activity.Contact == null) {
                return;
            }

            if (!DownloadsAllowed ()) {
                return;
            }

            DatabaseTrackInfo track = DatabaseTrackInfo.Provider.FetchFirstMatching ("TrackID = ?", external_id);
            if (track == null) {
                return;
            }

            if (content_type == string.Empty || content_type == null) {
                content_type = "application/octet-stream";
            }

             TelepathyService.UploadManager.UploadManager.Queue (
                    new TelepathyUpload (new TelepathyUploadKey (activity.Contact, external_id.ToString (), track, content_type))
                );
        }

        public string GetTrackPath (long id)
        {
            string uri =  ServiceManager.DbConnection.Query<string> (
                "SELECT Uri FROM CoreTracks WHERE TrackID = ?", id
            );

            if (!String.IsNullOrEmpty (uri)) {
                return new SafeUri (uri).LocalPath;
            }

            return String.Empty;
        }

        public bool DownloadsAllowed ()
        {
            bool downloading_allowed = ContactContainerSource.AllowDownloadsSchema.Get ();
            return downloading_allowed;
        }

        public bool StreamingAllowed ()
        {
            bool streaming_allowed = ContactContainerSource.AllowStreamingSchema.Get ();
            return streaming_allowed;
        }

		protected virtual void OnPermissionRequired (EventArgs args)
        {
            var handler = PermissionRequired;
            if (handler != null) {
                handler (this, args);
            }
        }
		
        private void OnPermissionSet (bool granted)
        {
            PermissionSetHandler handler = PermissionSet;
            if (handler != null) {
                handler (granted);
            }
        }

        private void OnDownloadingAllowedChanged (bool allowed)
        {
            DownloadingAllowedHandler handler = DownloadingAllowedChanged;
            if (handler != null) {
                handler (allowed);
            }
        }

        private void OnStreamingAllowedChanged (bool allowed)
        {
            StreamingAllowedHandler handler = StreamingAllowedChanged;
            if (handler != null) {
                handler (allowed);
            }
        }

        public static string ObjectPath {
            get { return "/org/bansheeproject/MetadataProviderService"; }
        }

        public static string BusName {
            get { return "org.bansheeproject.MetadataProviderService"; }
        }
    }
}

