//
// Transfer.cs
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

namespace Banshee.Telepathy.Data
{
    public enum TransferState {
        None,
        Queued,
        Ready,
        Initiated,
        InProgress,
        Completed,
        Cancelled,
        Failed
    }

    public class TransferEventArgs : EventArgs
    {
        public TransferEventArgs (TransferState state, long bytes_expected, long bytes_transferred) : base ()
        {
            State = state;
            BytesExpected = bytes_expected;
            BytesTransferred = bytes_transferred;
        }

        public TransferState State { get; private set; }
        public long BytesExpected { get; private set; }
        public long BytesTransferred { get; private set; }
    }

    public class TransferProgressEventArgs : EventArgs
    {
        public TransferProgressEventArgs (long bytes, long bytes_expected, long bytes_transferred) : base ()
        {
            Bytes = bytes;
            BytesExpected = bytes_expected;
            BytesTransferred = bytes_transferred;
        }

        public long Bytes { get; private set; }
        public long BytesExpected { get; private set; }
        public long BytesTransferred { get; private set; }
    }

    public abstract class Transfer<T> : IDisposable where T : IEquatable<T>
    {
        public event EventHandler<TransferEventArgs> StateChanged;
        public event EventHandler<TransferProgressEventArgs> ProgressChanged;

        public Transfer (T key)
        {
            this.key = key;
        }

        private T key;
        public T Key {
            get { return key; }
        }

        // make this property protected to let classes set the state without raising an event
        // use with care
        protected TransferState state = TransferState.None;
        public TransferState State {
            get { return state; }
            protected set {
                if (state != value) {
                    state = value;
                    OnStateChanged ();
                }
            }
        }

        public bool IsDownloading {
            get { return state == TransferState.InProgress; }
        }

        public bool IsDownloadingPending {
            get { return state > TransferState.None && state < TransferState.InProgress; }
        }

        private long expected_bytes;
        public long ExpectedBytes {
            get { return expected_bytes; }
            protected set { expected_bytes = value; }
        }

        private long bytes_transferred;
        public long BytesTransferred {
            get { return bytes_transferred; }
            protected set {
                if (bytes_transferred == 0) {
                    State = TransferState.InProgress;
                }
                OnProgressChanged (value - bytes_transferred);
                bytes_transferred = value;
            }
        }

        public void Dispose ()
        {
            Dispose (true);
        }

        protected abstract void Dispose (bool disposing);

        public virtual void Queue ()
        {
            State = TransferState.Queued;
        }

        public virtual bool Start ()
        {
			if (state == TransferState.Ready) {
            	State = TransferState.Initiated;
            	return true;
			}
			
			return false;
		}

        public virtual void Cancel ()
        {
            State = TransferState.Cancelled;
        }

        protected virtual void OnStateChanged ()
        {
            var handler = StateChanged;
            if (handler != null) {
                handler (this, new TransferEventArgs (state, expected_bytes, bytes_transferred));
            }
        }

        protected virtual void OnProgressChanged (long bytes)
        {
            var handler = ProgressChanged;
            if (handler != null) {
                handler (this, new TransferProgressEventArgs (bytes, expected_bytes, bytes_transferred));
            }
        }
    }
}