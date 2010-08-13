//
// FileTransfer.cs
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
using System.Net.Sockets;
using System.Threading;

using Banshee.Telepathy.API.Channels;

namespace Banshee.Telepathy.API.Dispatchables
{
    public enum TransferState {
        Idle,
        LocalPending,
        RemotePending,
        Connected,
        Initiated,
        InProgress,
        Completed,
        Cancelled,
        Failed
    }

    public class BytesTransferredEventArgs : EventArgs
    {
        public BytesTransferredEventArgs (long bytes)
        {
            this.bytes = bytes;
        }

        private long bytes;
        public long Bytes {
            get { return bytes; }
        }
    }

    public class TransferStateChangedEventArgs : EventArgs
    {
        public TransferStateChangedEventArgs (TransferState state)
        {
            this.state = state;
        }

        private TransferState state;
        public TransferState State {
            get { return state; }
        }
    }

    public class TransferClosedEventArgs : EventArgs
    {
        public TransferClosedEventArgs (TransferState state, long expected_bytes, long bytes_reported)
        {
            this.state = state;
            this.expected_bytes = expected_bytes;
            this.bytes_reported = bytes_reported;
        }

        private TransferState state;
        public TransferState State {
            get { return state; }
        }

        private long expected_bytes;
        public long ExpectedBytes {
            get { return expected_bytes; }
        }

        private long bytes_reported;
        public long BytesReported {
            get { return bytes_reported; }
        }
    }

    public abstract class FileTransfer : Dispatchable
    {
        public event EventHandler <BytesTransferredEventArgs> ProgressChanged;
        public event EventHandler <TransferStateChangedEventArgs> TransferStateChanged;
        public event EventHandler <EventArgs> TransferInitialized;

        internal FileTransfer (Contact c,  FileTransferChannel ft) : base (c, ft)
        {
            filename = ft.Filename;
            Key = OriginalFilename;
        }

        private Socket socket;
        protected Socket Socket {
            get { return socket; }
            set { socket = value; }
        }

        public string Address {
            get { return (Channel as FileTransferChannel).Address; }
        }

        private string filename;
        public string Filename {
            get { return filename; }
            set {
                if (value == null) {
                    throw new ArgumentNullException ("filename");
                }
                else {
                    FileInfo f = new FileInfo (value);
                    if (f != null && !f.Directory.Exists) {
                        Directory.CreateDirectory (f.DirectoryName);
                    }
                }

                filename = value;
            }
        }

        public string OriginalFilename {
            get { return (Channel as FileTransferChannel).Filename; }
        }

        public long ExpectedBytes {
            get { return (Channel as FileTransferChannel).Size; }
        }

        private long bytes_reported = 0;
        public long BytesReported {
            get { return bytes_reported; }
            protected set { bytes_reported = value; }
        }

        private long bytes_transferred = 0;
        public long BytesTransferred {
            get { return bytes_transferred; }
            protected set { bytes_transferred = value; }
        }

        public bool IsComplete {
            get { return ExpectedBytes == BytesTransferred; }
        }

        private TransferState state = TransferState.Idle;
        public TransferState State {
            get { return state; }
            protected set {
                if (value < TransferState.Idle || value > TransferState.Failed) {
                    throw new ArgumentOutOfRangeException ("state", "Not of type TransferState");
                }

                if (state != value) {
                    state = value;
                    OnTransferStateChanged (new TransferStateChangedEventArgs (state));
                }
            }
        }

        private static DispatchableQueue <FileTransfer> queue = new DispatchableQueue<FileTransfer> ();
        internal static DispatchableQueue <FileTransfer> Queue {
            get { return queue; }
        }

        public static FileTransfer Dequeue ()
        {
            return queue.Dequeue ();
        }

        public static FileTransfer Dequeue (Connection conn)
        {
            if (conn == null) {
                throw new ArgumentNullException ("conn");
            }

            return queue.Dequeue (conn);
        }

        public static int QueuedCount ()
        {
            return queue.Count ();
        }

        public static int QueuedCount (Connection conn)
        {
            if (conn == null) {
                throw new ArgumentNullException ("conn");
            }

            return queue.Count (conn);
        }

        internal protected override void Initialize ()
        {
            OnTransferInitialized (EventArgs.Empty);
            FileTransferChannel ft = Channel as FileTransferChannel;
            ft.ChannelReady += OnChannelReady;
            ft.Closed += OnTransferClosed;
            ft.IFileTransfer.TransferredBytesChanged += OnTransferredBytesChanged;
            State = TransferState.RemotePending;
            ft.Process ();
        }

        protected override void Dispose (bool disposing)
        {
            if (IsDisposed) {
                return;
            }

            if (disposing) {

                if (Channel != null) {
                    if (state != TransferState.Idle) {
                        this.Close ();
                    }
                    FileTransferChannel ft = Channel as FileTransferChannel;
                    if (ft != null) {
                        ft.ChannelReady -= OnChannelReady;
                        ft.Closed -= OnTransferClosed;
                        ft.IFileTransfer.TransferredBytesChanged -= OnTransferredBytesChanged;
                        ft.Dispose ();
                        Channel = null;
                    }
                }

                if (socket != null) {
                    try {
                        socket.Close ();
                    }
                    catch (Exception) {}
                }
                socket = null;
            }

            base.Dispose (disposing);
        }


        public void Close ()
        {
            if (Channel != null) {
                Channel.Close ();
            }
        }

        public void Cancel ()
        {
			Console.WriteLine ("FileTransfer.Cancel () called");
            State = TransferState.Cancelled;
            Close ();
        }

        public void Start ()
        {
            if (IsClosed || IsDisposed) {
                return;
            } else if (state < TransferState.Connected) {
                throw new InvalidOperationException ("Transfer state is not connected.");
            } else if (state > TransferState.Connected) {
                throw new InvalidOperationException ("Transfer has already been started.");
            }

            State = TransferState.Initiated;
            queue.Remove (this);

            ThreadPool.QueueUserWorkItem (delegate {
                Transfer ();
            });
        }

        protected abstract void Transfer ();

        protected virtual void OnTransferInitialized (EventArgs args)
        {
            EventHandler <EventArgs> handler = TransferInitialized;
            if (handler != null) {
                handler (this, args);
            }
        }

        protected virtual void OnTransferStateChanged (TransferStateChangedEventArgs args)
        {
            EventHandler <TransferStateChangedEventArgs> handler = TransferStateChanged;
            if (handler != null) {
                handler (this, args);
            }
        }

        protected long bytes_last_reported = 0;
        protected void OnTransferredBytesChanged (ulong bytes)
        {
            State = TransferState.InProgress;
            OnProgressChanged (new BytesTransferredEventArgs ((long) bytes - bytes_last_reported));
            bytes_last_reported = (long) bytes;
            BytesReported = (long) bytes;
        }

        protected virtual void OnProgressChanged (BytesTransferredEventArgs args)
        {
            EventHandler <BytesTransferredEventArgs> handler = ProgressChanged;
            if (handler !=  null) {
                handler (this, args);
            }
        }

        protected virtual void OnTransferClosed (object sender, EventArgs args)
        {
            if (State != TransferState.Cancelled) {
                if (IsComplete) {
                    State = TransferState.Completed;
                } else {
                    State = TransferState.Failed;
                }
            }

            OnClosed (new TransferClosedEventArgs (state, ExpectedBytes, BytesReported));

            queue.Remove (this);

            if (socket != null) {
                try {
                    socket.Close ();
                }
                catch (Exception) {}
            }
            socket = null;
        }

        protected override void OnChannelReady (object sender, EventArgs args)
        {
            State = TransferState.Connected;
        }
    }
}