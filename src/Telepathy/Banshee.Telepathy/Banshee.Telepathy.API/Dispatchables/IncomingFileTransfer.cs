//
// IncomingFileTransfer.cs
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
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

using Mono.Unix;

using Banshee.Telepathy.API.Channels;

namespace Banshee.Telepathy.API.Dispatchables
{
    public class IncomingFileTransfer : FileTransfer
    {
        internal IncomingFileTransfer (Contact c,  FileTransferChannel ft) : base (c, ft)
        {
        }

        private static bool auto_accept = false;
        public static bool AutoAccept {
            get { return auto_accept; }
            set { auto_accept = value; }
        }

        private static bool auto_start = true;
        public static bool AutoStart {
            get { return auto_start; }
            set { auto_start = value; }
        }

        public static IEnumerable <IncomingFileTransfer> GetAll (Connection conn)
        {
            foreach (Contact contact in conn.Roster.GetAllContacts ()) {
                DispatchManager dm = contact.DispatchManager;
                foreach (IncomingFileTransfer ft in dm.GetAll <IncomingFileTransfer> (contact)) {
                    yield return ft;
                }
            }
        }

        internal protected override void Initialize ()
        {
            FileTransferChannel ft = Channel as FileTransferChannel;
            ft.TransferProvided += OnTransferProvided;

            base.Initialize ();
        }

        protected override void Dispose (bool disposing)
        {
            if (IsDisposed) {
                return;
            }
            else if (disposing) {
                FileTransferChannel ft = Channel as FileTransferChannel;

                if (ft != null) {
                    ft.TransferProvided -= OnTransferProvided;
                }
            }

            base.Dispose (disposing);
        }


        private void UpdateFilePath (string folder)
        {
            if (folder == null) {
                throw new ArgumentNullException ("folder");
            }

            FileInfo f = new FileInfo (Filename);

            if (f != null) {
                Filename = folder.EndsWith ("/") ? folder + f.Name : folder + "/" + f.Name;
            }
        }

        public void Accept ()
        {
            Accept (null);
        }

        public void Accept (string folder)
        {
            if (State != TransferState.LocalPending) {
                throw new InvalidOperationException (String.Format ("TransferState is {0} but expected is {1}",
                                                                    State, TransferState.LocalPending));
            }

            if (folder != null) {
                UpdateFilePath (folder);
            }

            (Channel as FileTransferChannel).Accept ();
            State = TransferState.RemotePending;
        }

        protected override void Transfer ()
        {
            try {
                Socket = new Socket (AddressFamily.Unix, SocketType.Stream, ProtocolType.IP);
                EndPoint ep = new UnixEndPoint (Address);
                Socket.Connect (ep);

                Console.WriteLine ("Receiving file {0}", OriginalFilename);
                ReceiveFile ();
            }
            catch (Exception e) {
                Console.WriteLine (e.ToString ());
                Close ();
            }
        }

        private void ReceiveFile ()
        {
            Console.WriteLine ("In receiving thread...");

            try {
                using (BinaryWriter bwriter = new BinaryWriter ( File.Open (Filename, FileMode.Create))) {
                    byte [] data = new byte[8192];
                    int bytes_received;

                    while ( (bytes_received = Socket.Receive (data, 0, data.Length, SocketFlags.None)) > 0) {
                        bwriter.Write (data, 0, bytes_received);
                        BytesTransferred += bytes_received;
                    }

                    bwriter.Close ();
                }
            }
            catch (Exception e) {
                Console.WriteLine (e.ToString ());

                Gtk.Application.Invoke (delegate {
                    Close ();
                });
            }
        }

        private void OnTransferProvided (object sender, EventArgs args)
        {
            State = TransferState.LocalPending;

            if (AutoAccept) {
                this.Accept ();
            } else {
                OnResponseRequired (EventArgs.Empty);
            }
        }

        protected override void OnChannelReady (object sender, EventArgs args)
        {
            base.OnChannelReady (sender, args);

            if (!AutoStart) {
                Queue.Enqueue (this);
            }

            OnReady (EventArgs.Empty);

            if (State == TransferState.Connected && AutoStart) {
                Start ();
            }
        }
    }
}