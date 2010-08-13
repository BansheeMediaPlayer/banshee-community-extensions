//
// ContactTrackInfo.cs
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
using System.IO;
using System.Collections.Generic;
using System.Reflection;

using Hyena;

using Banshee.Collection;
using Banshee.Collection.Database;
using Banshee.Telepathy.DBus;

using Banshee.Telepathy.API;
using Banshee.Telepathy.API.Dispatchables;

namespace Banshee.Telepathy.Data
{
    public class ContactTrackInfo : DatabaseTrackInfo
    {
        private ContactTrackInfo () : base ()
        {
            CanPlay = false;
            CanSaveToDatabase = false;
        }

        public ContactTrackInfo (DatabaseTrackInfo track, ContactSource source) : this ()
        {
            if (track == null) {
                throw new ArgumentNullException ("track");
            }
            else if (source == null) {
                throw new ArgumentNullException ("source");
            }

            this.TrackId = track.TrackId;
            this.ExternalId = track.ExternalId;
            this.AlbumTitle = track.AlbumTitle;
            this.ArtistName = track.ArtistName;
            this.TrackNumber = track.TrackNumber;
            this.TrackTitle = track.TrackTitle;
            this.Uri = track.Uri;

            PrimarySource = source;
        }

        public ContactTrackInfo (IDictionary <string, object> track, ContactSource source) : this ()
        {
            if (track == null) {
                throw new ArgumentNullException ("track");
            }
            else if (source == null) {
                throw new ArgumentNullException ("source");
            }

            //TimeSpan = double
            //DateTime = long
            //Uri = string

			if (track.ContainsKey ("TrackId")) {
            	this.ExternalId = (long) (int) track["TrackId"];    // needed for playlists
			} else {
				ExternalId = -1;
			}
			
            MediaAttributes = TrackMediaAttributes.AudioStream | TrackMediaAttributes.Music;

            // dictionary key will match up to keyValuePair key = ExportName
            // of ExportableAttribute
            foreach (KeyValuePair<string, PropertyInfo> iter in GetExportableProperties (typeof (TrackInfo))) {
                try {
                    PropertyInfo property = iter.Value;
                    Type type = property.PropertyType;

                    if (track.ContainsKey (iter.Key) && property.CanWrite) {
                        if (type.Equals (typeof (DateTime))) {
                            property.SetValue (this, new DateTime ((long) track[iter.Key]), null);
                        } else if (type.Equals (typeof (TimeSpan))) {
                            property.SetValue (this, TimeSpan.FromSeconds ((double) track[iter.Key]), null);
                        } else if (type.Equals (typeof (SafeUri))) {
                            property.SetValue (this, new SafeUri ((string) track[iter.Key]), null);
                        } else if (type.Equals (typeof (TrackMediaAttributes))) {
                            // ignoring
                        } else {
                            property.SetValue (this, Convert.ChangeType (track[iter.Key], type), null);
                        }
                    }

                } catch (Exception e) {
                    Log.Exception (e);
                }
            }

            PrimarySource = source;
            remote_path = LocalPath ?? String.Empty;

            string ext = Path.GetExtension (LocalPath);

            Uri = new SafeUri (String.Format (
                "{0}{1}/{2}", TelepathyService.ProxyServer.HttpBaseAddress, this.ExternalId, ext.Substring (1)));

        }

        public static ContactTrackInfo From (TrackInfo track)
        {
            if (track != null) {
                ContactTrackInfo ci = track.ExternalObject as ContactTrackInfo;
                return ci;
            }
            return null;
        }

        public static IEnumerable<ContactTrackInfo> From (IEnumerable<TrackInfo> tracks)
        {
            foreach (TrackInfo track in tracks) {
                ContactTrackInfo ci = From (track);
                if (ci != null) {
                    yield return ci;
                }
            }
        }

        public Contact Contact {
            get { return (PrimarySource as ContactSource).Contact; }
        }

        public TelepathyDownload FileTransfer {
            get {
                TelepathyDownloadManager manager = TelepathyService.DownloadManager.DownloadManager;
                return manager.Get (new TelepathyDownloadKey (this));
            }
        }

        public bool IsDownloading {
            get {
                return FileTransfer != null ? FileTransfer.IsDownloading : false;
            }
        }

        public bool IsDownloadPending {
            get {
                return FileTransfer != null ? FileTransfer.IsDownloadingPending : false;
            }
        }

        private string remote_path = null;
        public string RemotePath {
            get {
                if (remote_path == null) {
                    DBusActivity activity = Contact.DispatchManager.Get <DBusActivity> (Contact, MetadataProviderService.BusName);

                    if (activity != null) {
                        IMetadataProviderService service = activity.GetDBusObject <IMetadataProviderService> (MetadataProviderService.BusName, MetadataProviderService.ObjectPath);

                        if (service != null) {
                            remote_path = service.GetTrackPath (this.ExternalId);
                        }
                    }
                }

                return remote_path;
            }
            set { remote_path = value; }
        }

        public void CancelTransfer ()
        {
            if (IsDownloading || IsDownloadPending) {
                FileTransfer.Cancel ();
            }
        }
    }
}