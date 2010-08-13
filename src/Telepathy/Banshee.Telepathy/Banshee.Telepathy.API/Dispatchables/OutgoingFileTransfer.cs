//
// OutgoingFileTransfer.cs
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
    public class OutgoingFileTransfer : FileTransfer
    {
        internal OutgoingFileTransfer (Contact c,  FileTransferChannel ft) : base (c, ft)
        {
        }

        private static bool auto_start = true;
        public static bool AutoStart {
            get { return auto_start; }
            set { auto_start = value; }
        }

        public static IEnumerable <OutgoingFileTransfer> GetAll (Connection conn)
        {
            foreach (Contact contact in conn.Roster.GetAllContacts ()) {
                DispatchManager dm = contact.DispatchManager;
                foreach (OutgoingFileTransfer ft in dm.GetAll <OutgoingFileTransfer> (contact)) {
                    yield return ft;
                }
            }
        }

        protected override void Transfer ()
        {
            try {
                Socket = new Socket (AddressFamily.Unix, SocketType.Stream, ProtocolType.IP);
                EndPoint ep = new UnixEndPoint (Address);
                Socket.Connect (ep);

                Console.WriteLine ("Sending file");
                SendFile ();
            }
            catch (Exception e) {
                Console.WriteLine (e.ToString ());
                Close ();
            }
        }

        private void SendFile ()
        {
            Console.WriteLine ("In sending thread...");

            try {
                using (FileStream fs = new FileStream (Filename, FileMode.Open, FileAccess.Read)) {
                    byte [] data = new byte[8192];
                    int read;

                     while ( (read = fs.Read (data, 0, data.Length)) > 0) {
                        Socket.Send (data, 0, read, SocketFlags.None );
                        BytesTransferred += read;
                    }
                }
            }
            catch (Exception e) {
                Console.WriteLine (e.ToString());

                Gtk.Application.Invoke (delegate {
                    Close ();
                });
            }
        }

        protected override void OnChannelReady (object sender, EventArgs args)
        {
            base.OnChannelReady (sender, args);
            OnReady (EventArgs.Empty);

            if (State == TransferState.Connected && AutoStart) {
                Start ();
            }
        }
    }
}