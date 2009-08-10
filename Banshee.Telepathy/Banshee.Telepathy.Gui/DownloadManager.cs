//
// DownloadManager.cs
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

using Hyena;
using Hyena.Jobs;

using Banshee.ServiceStack;
using Banshee.Sources;

using Banshee.Telepathy;
using Banshee.Telepathy.Data;

using Banshee.Telepathy.API;
using Banshee.Telepathy.API.Dispatchables;

namespace Banshee.Telepathy.Gui
{
    public class DownloadManager : TransferManager
    {
        TelepathyService service = null;

        public DownloadManager (TelepathyService service) : base ()
        {
            this.service = service;
            Title = Catalog.GetString ("Download(s) from Contacts");
            CancelMessage = Catalog.GetString ("Downloads are in progress. Would you like to cancel them?");
        }

        private int max_downloads = 2;
        public int MaxConcurrentDownloads {
            get { return max_downloads; }
            set { max_downloads = value; }
        }
        
        protected override void Initialize ()
        {
            IncomingFileTransfer.AutoStart = false;
            IncomingFileTransfer.TransferInitialized += OnTransferInitialized;
            IncomingFileTransfer.Ready += OnTransferReady;
            IncomingFileTransfer.TransferClosed += OnTransferClosed;

            base.Initialize ();
        }

        protected override void Dispose (bool disposing)
        {
            if (disposing) {
                IncomingFileTransfer.TransferInitialized -= OnTransferInitialized;
                IncomingFileTransfer.Ready -= OnTransferReady;
                IncomingFileTransfer.TransferClosed -= OnTransferClosed;
            }

            base.Dispose (disposing);
        }

        private int StartQueued (int max)
        {
            int started = 0;
            
            while (FileTransfer.QueuedCount () > 0 && started < max) {
            
                FileTransfer transfer = FileTransfer.DequeueIfQueued ();
                
                if (transfer != null) {
                    Log.DebugFormat ("Starting download for {0}", transfer.OriginalFilename);
                    transfer.Start ();
                    started++;
                }
                
            } 

            return started;
        }

        private void RefreshListView ()
        {
            DatabaseSource source = ServiceManager.SourceManager.ActiveSource as DatabaseSource;
            
            if (source as ContactSource != null) {
                (source as ContactSource).NotifyTracksChanged ();
            }
            else if (source as ContactPlaylistSource != null) {
                ((source as ContactPlaylistSource).Parent as ContactSource).NotifyTracksChanged ();
            }
        }

        private void ImportTrack (string path)
        {
            Banshee.Library.LibraryImportManager import_manager = ServiceManager.Get <Banshee.Library.LibraryImportManager> ();

            if (import_manager.ImportTrack (path) != null) {
                import_manager.NotifyAllSources ();
            }
        }

        public override void CancelAll ()
        {
            foreach (Connection conn in service.GetActiveConnections ()) {
                foreach (IncomingFileTransfer ft in IncomingFileTransfer.GetAll (conn)) {
                    ft.Cancel ();
                }
            }

            RefreshListView ();
        }

        private void OnTransferInitialized (object sender, EventArgs args)
        {
            IncomingFileTransfer transfer = sender as IncomingFileTransfer;

            if (transfer != null) {
                
                transfer.BytesTransferred += delegate(object o, BytesTransferredEventArgs e) {
                    FileTransfer ft = o as FileTransfer;

                    if (ft != null) {
                        BytesTransferred += e.Bytes;
                        Update ();
                    }                  
                };
                
                BytesExpected += transfer.ExpectedBytes;
                Total++;
                
                Update ();
                RefreshListView ();
            }
        }
        
        private void OnTransferReady (object sender, EventArgs args)
        {
            IncomingFileTransfer ft = sender as IncomingFileTransfer;

            if (ft != null) {
                
                if (InProgress < MaxConcurrentDownloads) {
                    if (ft.State == TransferState.Connected) {
                        ft.Start ();
                        InProgress++;
                    }
                }
                
                Update ();
                RefreshListView ();
            }
        }

        private void OnTransferClosed (object sender, TransferClosedEventArgs args)
        {
            IncomingFileTransfer ft = sender as IncomingFileTransfer;

            if (ft != null && !Cancelling) {
                
                Log.DebugFormat ("OnTransferClosed: path {0} state {1} previous {2}", 
                                 ft.OriginalFilename, 
                                 args.StateOnClose, 
                                 args.PreviousState);
                
                // cancelled or failed
                if (args.StateOnClose > TransferState.Completed) {
                    BytesTransferred -= ft.TotalBytesReported;
                    BytesExpected -= ft.ExpectedBytes;
                    Total--;
                } else if (args.StateOnClose == TransferState.Completed) {
                    ImportTrack (ft.Filename);
                }
                
                // previous state was in progress, completed, cancelled, failed
                if (args.PreviousState > TransferState.Connected) {
                    InProgress += StartQueued (MaxConcurrentDownloads - (InProgress - 1)) - 1;
                }
                
                Update ();
                RefreshListView ();

                if (BytesExpected == BytesTransferred) {
                    DestroyUserJob ();
                }
            }
        }
    }
}