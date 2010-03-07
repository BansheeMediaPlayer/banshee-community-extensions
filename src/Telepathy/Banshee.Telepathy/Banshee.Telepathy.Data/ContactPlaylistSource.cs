// 
// ContactPlaylistSource.cs
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

using Hyena;
using Hyena.Data.Sqlite;

using Banshee.Base;
using Banshee.ServiceStack;
using Banshee.Collection;
using Banshee.Collection.Database;
using Banshee.Playlist;
using Banshee.Sources;

using Banshee.Telepathy.API;

namespace Banshee.Telepathy.Data
{
    public class ContactPlaylistSource : PlaylistSource, IContactSource
    {
        private HyenaSqliteCommand insert_track_command = new HyenaSqliteCommand (@"
            INSERT INTO CorePlaylistEntries (PlaylistID, TrackID) 
                SELECT ?, TrackID FROM CoreTracks WHERE PrimarySourceID = ? AND ExternalID IN (?)"
        );
        
        private ContactSource parent;
        public Contact Contact {
            get {
                if (parent != null) {
                    return parent.Contact;
                }
                return null;
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
        
        public bool IsDownloadingAllowed {
            get {
                if (parent != null) {
                    return parent.IsDownloadingAllowed;
                }
                
                return true;
            }
        }
        
        public ContactPlaylistSource (string name, ContactSource parent) : base (name, parent)
        {
            if (parent == null) {
                throw new ArgumentNullException ("parent");
            }
            
            this.parent = parent;
            Save ();
        }
        
        public ContactPlaylistSource (IDictionary <string, object> [] playlist, string name, ContactSource parent) : this (name, parent)
        {
            if (playlist == null) {
                throw new ArgumentNullException ("playlist");
            }
            
            AddTracks (playlist);
        }
        
        public override bool CanDeleteTracks {
            get { return false; }
        }
        
        public override bool CanAddTracks {
            get { return false; }
        }
        
        public override bool CanRename {
            get { return false; }
        }
        
        public override bool CanUnmap {
            get { return false; }
        }

        public override bool HasEditableTrackProperties {
            get { return false; }
        }

        internal new void InvalidateCaches ()
        {
            ThreadAssist.SpawnFromMain (delegate {
                base.InvalidateCaches ();
            });
        }
        
        public void AddTracks (IDictionary <string, object> [] tracks)
        {
            int count = 0;
            if (tracks.Length > 0) {
                int [] external_ids = new int [tracks.Length];
                int i = 0;
                foreach (IDictionary <string, object> track in tracks) {
                    external_ids[i++] = (int) track["TrackID"];
                    count++;
                }

                if (count > 0) {
                    ServiceManager.DbConnection.Execute (insert_track_command, DbId, parent.DbId, external_ids);
                }
            }
            
            SavedCount += count;

            ThreadAssist.ProxyToMain (delegate {
                OnUpdated ();
            });
        }
    }
}
