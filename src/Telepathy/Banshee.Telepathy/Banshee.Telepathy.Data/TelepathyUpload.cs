//
// TelepathyUpload.cs
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
using Mono.Addins;

using Hyena;

using Banshee.Collection.Database;
using Banshee.ServiceStack;
using Banshee.Telepathy.Gui;

using Banshee.Telepathy.API;
using Banshee.Telepathy.API.Dispatchables;

namespace Banshee.Telepathy.Data
{
    public class TelepathyUploadKey : TelepathyTransferKey, IEquatable<TelepathyUploadKey>
    {
        public TelepathyUploadKey (Contact contact, string name, DatabaseTrackInfo track, string content_type) : base (contact, name)
        {
            ContentType = content_type;
            Track = track;
        }

        public string ContentType { get; private set; }
        public DatabaseTrackInfo Track { get; private set; }

        public bool Equals (TelepathyUploadKey other)
        {
            return base.Equals (other);
        }
    }

    public class TelepathyUpload : TelepathyTransfer<TelepathyUploadKey, OutgoingFileTransfer>
    {
        public TelepathyUpload (TelepathyUploadKey key) : base (key)
        {
        }

        public string ContentType {
            get { return Key.ContentType; }
        }

        protected override void Initialize ()
        {
            OutgoingFileTransfer.AutoStart = false;
            base.Initialize ();
        }

        public override void Queue ()
        {
            DispatchManager dm = Contact.DispatchManager;
            if (!dm.Exists <OutgoingFileTransfer> (Contact, Name)) {

                IDictionary <string, object> properties = new Dictionary <string, object> ();
                properties.Add ("Filename", Name);
                properties.Add ("Description", "Telepathy extension for Banshee transfer");
                properties.Add ("ContentType", ContentType);
                properties.Add ("Size", (ulong) Key.Track.FileSize);

                dm.Request <OutgoingFileTransfer> (Contact, properties);
            }

            base.Queue ();
        }

        public override bool Start ()
        {
			if (State != TransferState.Ready) return false;
			
            if (FileTransfer != null) {
                SetTransferFilename (FileTransfer);
                TelepathyNotification.Create ().Show (FileTransfer.Contact.Name,
                    String.Format (AddinManager.CurrentLocalizer.GetString ("is downloading {0} with Banshee"),
                                   FileTransfer.Filename));
                return base.Start ();
            }

            return false;
        }

        private void SetTransferFilename (FileTransfer ft)
        {
            if (ft != null) {
                string uri = ServiceManager.DbConnection.Query <string> (
                    "SELECT Uri FROM CoreTracks WHERE TrackID = ?", ft.OriginalFilename);

                if (uri != null) {
                    ft.Filename = new SafeUri (uri).LocalPath;
                    ft.Start ();
                }
                else {
                    Log.DebugFormat ("Unable to get Uri for FileTransfer. Closing transfer.");
                    ft.Close ();
                }
            }
        }

        protected override void OnTransferResponseRequired (object sender, EventArgs args)
        {
        }
    }
}