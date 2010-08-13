//
// FileTransferChannel.cs
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
using System.Text;

using Banshee.Telepathy.API.DBus;

using Telepathy;

namespace Banshee.Telepathy.API.Channels
{
    public delegate object AsyncProvideFileCaller (SocketAddressType address_type,
                                                   SocketAccessControl access_control,
                                                   object access_control_param);

    internal sealed class FileTransferChannel : RequestedChannel
    {
        private SocketAccessControl socket_ac = SocketAccessControl.Localhost;
        private SocketAddressType socket_type = SocketAddressType.Unix;

        public event EventHandler <EventArgs> TransferProvided;

        public FileTransferChannel (Connection conn, string object_path,
                                uint initiator_handle, uint target_handle,
                                string filename, string content_type, long size) : base (conn, object_path, initiator_handle, target_handle)
        {
            Filename = filename;
            ContentType = content_type;
            Size = size;

            Initialize ();
        }

        private object address;
        public string Address {
            get {
                if (address != null) {
                    return Encoding.ASCII.GetString ((byte[]) address);
                }
                else {
                    return null;
                }
            }
        }

        private string filename;
        public string Filename {
            get { return filename; }
            private set {
                if (value == null) {
                    throw new ArgumentNullException ("filename");
                }

                filename = value;
            }
        }

        private string content_type;
        public string ContentType {
            get { return content_type; }
            private set {
                if (value == null) {
                    throw new ArgumentNullException ("content_type");
                }

                content_type = value;
            }
        }

        private long size;
        public long Size {
            get { return size; }
            private set {
                if (value == 0) {
                    throw new ArgumentException ("Size must be > 0.");
                }

                size = value;
            }
        }

        private IFileTransfer ft;
        internal IFileTransfer IFileTransfer {
            get { return ft; }
        }

        protected override void SetProxyObject ()
        {
            ft = DBusUtility.GetProxy <IFileTransfer> (Connection.BusName, ObjectPath);
        }

        private void Initialize ()
        {
            ft.FileTransferStateChanged += OnFileTransferStateChanged;
            ft.InitialOffsetDefined += OnInitialOffsetDefined;
            ft.Closed += OnFileTransferClosed;

            if (!SetSocketType ()) {
                throw new InvalidOperationException ("No supported sockets.");
            }
        }

        private bool SetSocketType ()
        {
            IDictionary <uint, uint[]> supported_sockets =
                (IDictionary <uint, uint[]>) DBusUtility.GetProperty (BusType.Session,
                                                    Connection.BusName,
                                                    ObjectPath,
                                                    Constants.CHANNEL_TYPE_FILETRANSFER,
                                                    "AvailableSocketTypes");

            bool supported = false;

            if (supported_sockets.ContainsKey ((uint)SocketAddressType.Unix)) {
               supported =  true;
            }
            // TODO commented out for now, since we can't use IPv4 until Mono 2.4
            /*
            else if (supported_sockets.ContainsKey ((uint)SocketAddressType.IPv4)) {
                socket_type = SocketAddressType.IPv4;
                supported = true;
            }
            */
            return supported;
        }

        public void Process ()
        {
            if (InitiatorHandle != Connection.SelfHandle) {
                //Log.Debug ("Raising event as tube has been offered");
                OnTransferProvided (EventArgs.Empty);
            }
            else {
                Provide ();
            }
        }

        private AsyncProvideFileCaller pf_caller;
        private IAsyncResult pf_result;
        public void Provide ()
        {
            pf_caller = new AsyncProvideFileCaller (ft.ProvideFile);
            pf_result = pf_caller.BeginInvoke (socket_type, socket_ac, "", null, null);

            Console.WriteLine ("FileTransfer from {0} offered", address);
        }

        public void Accept ()
        {
            address = ft.AcceptFile (socket_type, socket_ac, "", 0);
            Console.WriteLine ("FileTransfer from {0} accepted", address);
        }

        private void OnFileTransferStateChanged (FileTransferState state, FileTransferStateChangeReason reason)
        {
            //Console.WriteLine ("OnFileTransferStateChanged: state {0}", state);

            switch (state) {
                case FileTransferState.Open:
                    if (pf_result != null) {
                        address = pf_caller.EndInvoke (pf_result);
                        pf_result = null;
                    }

                    OnChannelReady (EventArgs.Empty);
                    break;

                case FileTransferState.Completed:
                    Close ();
                    break;

                case FileTransferState.Cancelled:
                    Close ();
                    break;
            }

        }

        private void OnInitialOffsetDefined (ulong offset)
        {
            //Console.WriteLine ("OnInitialOffsetDefined: offset {0}", offset);
        }

        private void OnFileTransferClosed ()
        {
            IsClosed = true;
            OnClosed (EventArgs.Empty);
        }

        private void OnTransferProvided (EventArgs args)
        {
            EventHandler <EventArgs> handler = TransferProvided;
            if (handler != null) {
                handler (this, args);
            }
        }

        protected override void Dispose (bool disposing)
        {
            if (disposing) {
                if (ft != null) {
                    try {
                        ft.Closed -= OnFileTransferClosed;
                        ft.InitialOffsetDefined -= OnInitialOffsetDefined;
                    }
                    catch {}
                }
            }

            base.Dispose (disposing);
        }

        public override void Close ()
        {
            try {
                ft.Close ();
            }
            catch {}
        }
    }
}