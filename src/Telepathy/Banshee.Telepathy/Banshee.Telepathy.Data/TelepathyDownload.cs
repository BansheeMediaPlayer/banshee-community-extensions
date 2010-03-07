//
// TelepathyDownload.cs
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
using Mono.Unix;

using Banshee.Base;
using Banshee.Collection.Database;
using Banshee.ServiceStack;
using Banshee.Sources;

using Banshee.Telepathy.Gui;
using Banshee.Telepathy.DBus;

using Banshee.Telepathy.API;
using Banshee.Telepathy.API.Dispatchables;

using Hyena;

namespace Banshee.Telepathy.Data
{
    public class TelepathyDownloadKey : TelepathyTransferKey, IEquatable<TelepathyDownloadKey>
    {
        public TelepathyDownloadKey (ContactTrackInfo track) : base (track.Contact, track.ExternalId.ToString ())
        {
            Track = track;
        }
        
        public ContactTrackInfo Track { get; private set; }
        
        public bool Equals (TelepathyDownloadKey other)
        {
            return base.Equals (other);
        }
    }
    
    public class TelepathyDownload : TelepathyTransfer<TelepathyDownloadKey, IncomingFileTransfer>
    {
        public TelepathyDownload (TelepathyDownloadKey key) : base (key)
        {
        }
        
        protected override void Initialize ()
        {
            IncomingFileTransfer.AutoStart = false;
            base.Initialize ();
        }
        
        public override void Queue ()
        {
            IContactSource source = ServiceManager.SourceManager.ActiveSource as IContactSource;
            if (!source.IsDownloadingAllowed) {
                return;
            }
            
            DBusActivity activity = Contact.DispatchManager.Get <DBusActivity> (Contact, MetadataProviderService.BusName);

            if (activity != null) {            
                IMetadataProviderService service = activity.GetDBusObject <IMetadataProviderService> (MetadataProviderService.BusName, MetadataProviderService.ObjectPath);
                if (service != null) {
                    base.Queue ();
                    ThreadAssist.Spawn (delegate {
						lock (sync) {
	                        try {
	                            service.DownloadFile (long.Parse (Key.Name) , "audio/mpeg");
	                        }
	                        catch (Exception e) {
	                            Log.Exception (e);
	                        }
						}
                    });
                }
            }
        }
        
        public override bool Start ()
        {
            if (FileTransfer != null) {
                if (base.Start ()) {
	                FileTransfer.Start ();
	                TelepathyNotification.Create ().Show (FileTransfer.Contact.Name, 
	                    String.Format (Catalog.GetString ("is sending {0} with Banshee"), FileTransfer.Filename));
	                return true;
				}
            }
            
            return false;
        }
        
        private void RefreshListView ()
        {
            DatabaseSource source = ServiceManager.SourceManager.ActiveSource as DatabaseSource;
            
            if (source as ContactSource != null) {
                (source as ContactSource).InvalidateCaches ();
            }
            else if (source as ContactPlaylistSource != null) {
                (source as ContactPlaylistSource).InvalidateCaches ();
            }
        }
        
        private void AcceptTransfer (IncomingFileTransfer transfer)
        {
            if (transfer == null) {
                return;
            }
            
            // passing extension on Uri to allow import after a download
            string ext = Key.Track.Uri.ToString ().Substring (Key.Track.Uri.ToString ().LastIndexOf ('/') + 1);
            string filename = FileNamePattern.BuildFull (ContactSource.TempDownloadDirectory, Key.Track, ext);

            if (transfer.State == API.Dispatchables.TransferState.LocalPending) {
                if (!String.IsNullOrEmpty(filename)) {
                    transfer.Filename = filename;
                    transfer.Accept ();
                } else {
                    transfer.Accept (ContactSource.TempDownloadDirectory);
                }
            }
        }
                
        protected override void OnStateChanged ()
        {
            switch (State) {
            case TransferState.Queued:
            case TransferState.InProgress:
            case TransferState.Completed:
            case TransferState.Cancelled:
            case TransferState.Failed:
                RefreshListView ();
                break;
            }
            
            base.OnStateChanged ();
        }

        protected override void OnTransferResponseRequired (object sender, EventArgs args)
        {
            IncomingFileTransfer transfer = sender as IncomingFileTransfer;
            
            // transfer was cancelled before the channel was available
            if (CancelPending) {
                Cancel ();
                return;
            }
            
            Log.DebugFormat ("{0} handle {1} accepting transfer from ContactSource", Contact.Name, Contact.Handle);
    
            AcceptTransfer (transfer);        
        }
    }
}
