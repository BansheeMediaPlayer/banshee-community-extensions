//
// UploadManager.cs
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

using Banshee.Base;
using Banshee.ServiceStack;
using Banshee.Telepathy;

using Banshee.Telepathy.API;
using Banshee.Telepathy.API.Dispatchables;

namespace Banshee.Telepathy.Gui
{
    public class UploadManager : TransferManager
    {
        private TelepathyService service = null;
        
        public UploadManager (TelepathyService service) : base ()
        {
            this.service = service;
        }
        
        protected override void Initialize ()
        {
            OutgoingFileTransfer.AutoStart = false;
            OutgoingFileTransfer.TransferInitialized += OnTransferInitialized;
            OutgoingFileTransfer.TransferStateChanged += OnTransferStateChanged;
            OutgoingFileTransfer.Ready += OnTransferReady;
            OutgoingFileTransfer.Closed += OnTransferClosed;

            base.Initialize ();
        }

        protected override void Dispose (bool disposing)
        {
            if (disposing) {
                OutgoingFileTransfer.TransferInitialized -= OnTransferInitialized;
                OutgoingFileTransfer.TransferStateChanged -= OnTransferStateChanged;
                OutgoingFileTransfer.Ready -= OnTransferReady;
                OutgoingFileTransfer.Closed -= OnTransferClosed;
            }

            base.Dispose (disposing);
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

        public override void CancelAll ()
        {
            foreach (Connection conn in service.GetActiveConnections ()) {
                foreach (OutgoingFileTransfer ft in OutgoingFileTransfer.GetAll (conn)) {
                    ft.Cancel ();
                }
            }
        }

        private void OnTransferInitialized (object sender, EventArgs args)
        {
            OutgoingFileTransfer transfer = sender as OutgoingFileTransfer;

            if (transfer != null) {
                
                transfer.BytesTransferred += delegate(object o, BytesTransferredEventArgs e) {
                    OutgoingFileTransfer ft = o as OutgoingFileTransfer;

                    if (ft != null) {
                        BytesTransferred += e.Bytes;
                        Update ();
                    }                  
                };
                
                BytesExpected += transfer.ExpectedBytes;
                Total++;
                
                Update ();
            }
        }
        
        private void OnTransferStateChanged (object sender, TransferStateChangedEventArgs args)
        {
            OutgoingFileTransfer ft = sender as OutgoingFileTransfer;

            if (ft != null && args.State == TransferState.InProgress) {
                InProgress++;
                Update ();
            }
        }

        private void OnTransferReady (object sender, EventArgs args)
        {
            OutgoingFileTransfer ft = sender as OutgoingFileTransfer;

            if (ft != null) {
                SetTransferFilename (ft);
            }
        }

        private void OnTransferClosed (object sender, EventArgs args)
        {
            TransferClosedEventArgs transfer_args = args as TransferClosedEventArgs;
            OutgoingFileTransfer ft = sender as OutgoingFileTransfer;

            if (ft != null && !Cancelling) {
        
                Log.DebugFormat ("OnTransferClosed: path {0} state {1} previous {2}",
                                 ft.OriginalFilename, 
                                 transfer_args.StateOnClose, 
                                 transfer_args.PreviousState);
                
                // cancelled or failed
                if (transfer_args.StateOnClose > TransferState.Completed) {
                    BytesTransferred -= ft.TotalBytesReported;
                    BytesExpected -= ft.ExpectedBytes;
                    Total--;
                }
                
                // previous state was in progress, completed, cancelled, failed
                if (transfer_args.PreviousState > TransferState.Connected) {
                    InProgress--;
                }
                
                Update ();

                if (BytesExpected == BytesTransferred) {
                    DestroyUserJob ();
                }
            }
        }
        
    }
}