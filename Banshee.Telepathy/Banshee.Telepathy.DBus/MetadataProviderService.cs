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

using Gtk;
using System;
using System.Collections.Generic;

using Banshee.Base;
using Banshee.Collection.Database;
using Banshee.ServiceStack;

using Banshee.Telepathy.API;
using Banshee.Telepathy.API.Dispatchables;

using Banshee.Telepathy.Data;
using Banshee.Telepathy.Gui;

using Hyena.Data.Sqlite;
    
using NDesk.DBus;

namespace Banshee.Telepathy.DBus
{
    public enum LibraryType
    {
        Music, 
        Video
    };

    public delegate void PermissionResponseHandler (bool granted);
    
    //[DBusExportable (ServiceName = "Telepathy")]
    public class MetadataProviderService : IMetadataProviderService//, IDBusExportable
    {
        public event PermissionResponseHandler PermissionResponse;
        
        private DBusActivity activity;
        
        private MetadataProviderService ()
        {
        }
        
        public MetadataProviderService (DBusActivity activity)
        {
            this.activity = activity;
        }

        public MetadataProviderService (DBusActivity activity, bool permission) : this (activity)
        {
            this.permission_granted = permission;
        }

        private bool permission_granted = true;
        public bool PermissionGranted () 
        {
            return permission_granted; 
        }
        
        public ObjectPath CreateMetadataProvider (LibraryType type)
        {
            if (!permission_granted) {
                return new ObjectPath ("");
            }
            
            MetadataProvider provider = new MetadataProvider (activity, type);
            activity.RegisterDBusObject (provider, provider.ObjectPath);
            return new ObjectPath (provider.ObjectPath);
            //return ServiceManager.DBusServiceManager.RegisterObject (new MetadataProvider (this, type));
        }
        
        public ObjectPath CreatePlaylistProvider (int id)
        {
            if (!permission_granted) {
                return new ObjectPath ("");
            }
            
            PlaylistProvider provider = new PlaylistProvider (activity, id);
            activity.RegisterDBusObject (provider, provider.ObjectPath);
            return new ObjectPath (provider.ObjectPath);
            //return ServiceManager.DBusServiceManager.RegisterObject (new PlaylistProvider (this, id));
        }
        
        public int[] GetPlaylistIds (LibraryType type)
        {
            Console.WriteLine ("I am in GetPlaylistIds");
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
            if (permission_granted) {
                OnPermissionResponse (permission_granted);
                return;
            }
            
            Contact contact = activity.Contact;
            
            ContactRequestDialog dialog = new ContactRequestDialog ("Contact Request", 
                                                                    String.Format ("{0} would like to browse your music library.",
                                                                    contact.Name));
            dialog.ShowAll ();
            dialog.Response += delegate(object o, ResponseArgs e) {
                if (e.ResponseId == ResponseType.Accept) {               
                    permission_granted = true;
                }
                
                dialog.Destroy ();
                OnPermissionResponse (permission_granted);
            };
        }

        public void DownloadFile (long external_id, string content_type)
        {
            DatabaseTrackInfo track = DatabaseTrackInfo.Provider.FetchFirstMatching ("TrackID = ?", external_id);
            if (track == null) {
                return;
            }
            
            if (content_type == string.Empty || content_type == null) {
                content_type = "application/octet-stream";
            }

            Contact contact = activity.Contact;
            DispatchManager dm = contact.DispatchManager;
            
            if (!dm.Exists <OutgoingFileTransfer> (contact, external_id.ToString ())) {
                
                IDictionary <string, object> properties = new Dictionary <string, object> ();
                properties.Add ("Filename", external_id.ToString ());
                properties.Add ("ContentType", content_type);
                properties.Add ("Size", (ulong) track.FileSize);
    
                dm.Request <OutgoingFileTransfer> (contact, properties);
            }
        }

        public string GetTrackPath (long id)
        {
            string uri =  ServiceManager.DbConnection.Query<string> (
                "SELECT Uri FROM CoreTracks WHERE TrackID = ?", id
            );

            return new SafeUri (uri).LocalPath;
        }

        public bool DownloadsAllowed ()
        {
            return ContactContainerSource.AllowDownloadsSchema.Get ();
        }
            
        private void OnPermissionResponse (bool granted)
        {
            PermissionResponseHandler handler = PermissionResponse;
            if (handler != null) {
                handler (granted);
            }
        }
        
        public static string ObjectPath {
            get { return "/org/bansheeproject/MetadataProviderService"; }
        }

        public static string BusName {
            get { return "org.bansheeproject.MetadataProviderService"; }
        }

/*
        IDBusExportable IDBusExportable.Parent { 
            get { return null; }
        }
        
        string IService.ServiceName {
            get { return "MetadataProviderService"; }
        }
*/
    }
}
        