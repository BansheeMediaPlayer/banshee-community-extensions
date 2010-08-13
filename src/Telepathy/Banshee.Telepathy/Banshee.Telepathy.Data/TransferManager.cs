//
// TransferManager.cs
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

using Hyena;

namespace Banshee.Telepathy.Data
{
    public class UpdatedEventArgs : EventArgs
    {
        public UpdatedEventArgs (int total, int in_progress, long bytes_expected, long bytes_transferred) : base ()
        {
            this.total = total;
            this.in_progress = in_progress;
            this.bytes_expected = bytes_expected;
            this.bytes_transferred = bytes_transferred;
        }

        private int total;
        public int Total {
            get { return total; }
        }

        private int in_progress;
        public int InProgress {
            get { return in_progress; }
        }

        private long bytes_expected;
        public long BytesExpected {
            get { return bytes_expected; }
        }

        private long bytes_transferred;
        public long BytesTransferred {
            get { return bytes_transferred; }
        }
    }

    public class TransferManager<K, T> : IDisposable where T : Transfer<K> where K : IEquatable<K>
    {
        protected readonly TransferList<K, T> transfers = new TransferList<K, T> ();
        protected readonly IList<K> initiated = new List<K> ();
        protected readonly object sync = new object ();

        public event EventHandler<EventArgs> TransferCompleted;
        public event EventHandler<EventArgs> Completed;
        public event EventHandler<UpdatedEventArgs> Updated;

        private int total = 0;
        public int Total {
            get { return total; }
            protected set { total = value; }
        }

        public int Initiated {
            get { return initiated.Count; }
        }

        private int in_progress = 0;
        public int InProgress {
            get { return in_progress; }
            protected set { in_progress = value; }
        }

        private long bytes_transferred = 0;
        public long BytesTransferred {
            get { return bytes_transferred; }
            protected set { bytes_transferred = value < 0 ? 0 : value; }
        }

        private long bytes_expected = 0;
        public long BytesExpected {
            get { return bytes_expected; }
            protected set { bytes_expected = value < 0 ? 0 : value; }
        }

        private bool cancelling = false;
        public bool Cancelling {
            get { return cancelling; }
            protected set { cancelling = value; }
        }

        private int max_downloads = 2;
        public int MaxConcurrentDownloads {
            get { return max_downloads; }
            set { max_downloads = value; }
        }

        public void Dispose ()
        {
            Dispose (true);
        }

        protected virtual void Dispose (bool disposing)
        {
            if (disposing) {
                foreach (T t in transfers.Values) {
					if (t != null) {
                    	t.Dispose ();
					}
                }
                transfers.Clear ();
            }
        }

        public bool Exists (K key)
        {
            return transfers.ContainsKey (key);
        }

        public T Get (K key)
        {
            if (Exists (key)) {
                return transfers[key];
            }

            return default(T);
        }

        protected virtual void StartReady ()
        {
			lock (sync) {
	            foreach (T t in transfers.Ready ()) {
	                if (Initiated == max_downloads) break;
	                if (t != null) {
	                 	Log.DebugFormat ("Starting download for {0}", t.Key.ToString ());
	                   	if (t.Start ()) initiated.Add (t.Key);
	                }
	            }
			}
        }

        public void CancelAll ()
        {
            foreach (T t in new List<T> (transfers.Values)) {
                t.Cancel ();
            }
        }

        public virtual void Queue (T t)
        {
            if (!transfers.ContainsKey (t.Key)) {
                transfers.Add (t.Key, t);
                t.StateChanged += OnTransferStateChanged;
                t.ProgressChanged += OnTransferProgressChanged;
                t.Queue ();
            }
        }

		protected void CleanUpTransfer (T t)
		{
			CleanUpTransfer (t, true);	
		}
		
        protected virtual void CleanUpTransfer (T t, bool dispose)
        {
            if (t != null) {
                t.StateChanged -= OnTransferStateChanged;
                t.ProgressChanged -= OnTransferProgressChanged;
				if (dispose) {
                	t.Dispose ();
				}
                transfers.Remove (t.Key);
            }
        }

        protected virtual void OnTransferCompleted (T t, EventArgs args)
        {
            var handler = TransferCompleted;
            if (handler != null) {
                handler (t, args);
            }
        }

        protected virtual void OnCompleted (EventArgs args)
        {
            var handler = Completed;
            if (handler != null) {
                handler (this, args);
            }
        }

        protected virtual void OnUpdated ()
        {
            Log.DebugFormat ("OnUpdated: total {0} inprogress {1} expected {2} transferred {3}",
                total, in_progress, bytes_expected, bytes_transferred);

            var handler = Updated;
            if (handler != null) {
                handler (this, new UpdatedEventArgs (total, in_progress, bytes_expected, bytes_transferred));
            }
        }

        protected virtual void OnTransferStateChanged (object sender, TransferEventArgs args)
        {
            Log.DebugFormat ("OnTransferStateChanged: {0}", args.State.ToString ());

            switch (args.State) {
            case TransferState.Queued:
                total++;
                break;
            case TransferState.Ready:
                ThreadAssist.SpawnFromMain (delegate {
                    StartReady ();
                });

                break;
            case TransferState.InProgress:
				bytes_expected += args.BytesExpected;
                in_progress++;
                break;
            case TransferState.Completed:
            case TransferState.Cancelled:
            case TransferState.Failed:
                T t = sender as T;
                CleanUpTransfer (t);

                bool start = true;
                if (args.State == TransferState.Completed) {
                    OnTransferCompleted (t, EventArgs.Empty);
                } else if (args.State == TransferState.Cancelled && args.BytesTransferred == 0) {
                    start = false;
                } else if ((args.State == TransferState.Cancelled || args.State == TransferState.Failed) && args.BytesTransferred > 0) {
                    bytes_expected -= args.BytesExpected;
                    bytes_transferred -= args.BytesTransferred;
                }

                if (args.BytesTransferred > 0) {
                    in_progress--;
                }

                if (initiated.Contains (t.Key)) {
                    initiated.Remove (t.Key);
                }

                total--;

                if (start) {
                    ThreadAssist.SpawnFromMain (delegate {
                        StartReady ();
                    });
                }
                break;
            }

            OnUpdated ();

            if (in_progress == 0 && total == 0) {
                bytes_expected = 0;
                bytes_transferred = 0;
                OnCompleted (EventArgs.Empty);
            }
        }

        private void OnTransferProgressChanged (object sender, TransferProgressEventArgs args)
        {
            //Log.DebugFormat ("OnTransferProgressChanged: transferred {0} expected {1}",
	//                         args.BytesTransferred, args.BytesExpected);
            bytes_transferred += args.Bytes;
            OnUpdated ();
        }
    }
}