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
        InProgress,
        Completed,
        Cancelled,
        Failed
    }

    public class BytesTransferredEventArgs : EventArgs
    {
        private long bytes;

        public BytesTransferredEventArgs (long bytes)
        {
            this.bytes = bytes;
        }

        public long Bytes {
            get { return bytes; }
        }
    }

    public class TransferStateChangedEventArgs : EventArgs
    {
        private TransferState state;

        public TransferStateChangedEventArgs (TransferState state)
        {
            this.state = state;
        }

        public TransferState State {
            get { return state; }
        }
    }

    public class TransferClosedEventArgs : EventArgs
    {
        private TransferState state_on_close;
        private TransferState previous_state;

        public TransferClosedEventArgs (TransferState state_on_close, TransferState previous_state)
        {
            this.state_on_close = state_on_close;
            this.previous_state = previous_state;
        }

        public TransferState StateOnClose {
            get { return state_on_close; }
        }

        public TransferState PreviousState {
            get { return previous_state; }
        }
    }
    
    public abstract class FileTransfer : Dispatchable
    {
        public event EventHandler <BytesTransferredEventArgs> BytesTransferred;
        
        //public static event EventHandler <TransferClosedEventArgs> TransferClosed;
        public static event EventHandler <TransferStateChangedEventArgs> TransferStateChanged;
        public static event EventHandler <EventArgs> TransferInitialized;

        
        internal FileTransfer (Contact c,  FileTransferChannel ft) : base (c, ft)
        {
            filename = ft.Filename;
            Key = OriginalFilename;
            Initialize ();
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

        private long total_bytes_reported = 0;
        public long TotalBytesReported {
            get { return total_bytes_reported; }
            protected set { total_bytes_reported = value; }
        }
        
        private TransferState state = TransferState.Idle;
        public TransferState State {
            get { return state; }
            protected set {
                if (value < TransferState.Idle || value > TransferState.Failed) {
                    throw new ArgumentOutOfRangeException ("state", "Not of type TransferState");
                }
                state = value;
                OnTransferStateChanged (new TransferStateChangedEventArgs (state));
            }
        }

        private TransferState previous_state = TransferState.Idle;
        protected TransferState PreviousState {
            get { return previous_state; }
            set { if (value < TransferState.Idle || value > TransferState.Failed) {
                    throw new ArgumentOutOfRangeException ("previous_state", "Not of type TransferState");
                }
                previous_state = value;
            }
        }

        private static DispatchableQueue <FileTransfer> queue = new DispatchableQueue<FileTransfer> ();
        internal static DispatchableQueue <FileTransfer> Queue {
            get { return queue; }
        }
        
        public static FileTransfer DequeueIfQueued ()
        {
            return queue.Dequeue ();
        }
        
        public static FileTransfer DequeueIfQueued (Connection conn)
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
        
        protected virtual void Initialize ()
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
            PreviousState = state;
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

            queue.Remove (this);
            
            State = TransferState.InProgress;

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
            OnBytesTransferred (new BytesTransferredEventArgs ((long) bytes - bytes_last_reported));
            bytes_last_reported = (long) bytes;
            TotalBytesReported = (long) bytes;
        }
        
        protected virtual void OnBytesTransferred (BytesTransferredEventArgs args)
        {
            EventHandler <BytesTransferredEventArgs> handler = BytesTransferred;
            if (handler !=  null) {
                handler (this, args);
            }
        }
        
        protected virtual void OnTransferClosed (object sender, EventArgs args)
        {
            OnClosed (new TransferClosedEventArgs (state, previous_state));
            
            queue.Remove (this);

            if (socket != null) {
                try {
                    socket.Close ();
                }
                catch (Exception) {}
            }
            socket = null;
            
//            EventHandler <TransferClosedEventArgs> handler = TransferClosed;
//            if (handler !=  null) {
//                handler (this, new TransferClosedEventArgs (state, previous_state));
//            }
//
//            if (Key != null && AutoRemoveOnClose && Contact != null) {
//                DispatchManager dm = Contact.DispatchManager;
//                dm.Remove (Contact, Key, this.GetType ());
//            }
        }

        protected override void OnChannelReady (object sender, EventArgs args)
        {
            State = TransferState.Connected;
        }
    }
}