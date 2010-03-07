//
// LibraryDownloadMonitor.cs
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
using System.Threading;

using Hyena;

namespace Banshee.Telepathy.Data
{
    internal class LibraryDownloadMonitor
    {
        private readonly IDictionary <string, LibraryDownload> downloads = new Dictionary <string, LibraryDownload> ();
        //private readonly IDictionary <LibraryDownload, object> associated = new Dictionary <LibraryDownload, object> ();
        
        public event EventHandler <EventArgs> AllFinished;
        public event EventHandler <EventArgs> AllProcessed;
        
        public LibraryDownloadMonitor ()
        {
        }

        private bool monitoring = false;
        public bool Monitoring {
            get { return monitoring; }
        }

        public int Count {
            get {
                lock (downloads) {
                    return downloads.Count;
                }
            }
        }
        
        public void Start ()
        {
            monitoring = true;
            if (MonitoredFinished ()) {
                OnAllFinished (EventArgs.Empty);
            }
        }
        
        public void Add (string key, LibraryDownload d)
        {
            if (key == null) {
                throw new ArgumentNullException ("key");
            }
            else if (d == null) {
                throw new ArgumentNullException ("d");
            }
            else if (monitoring) {
                throw new InvalidOperationException ("Can't add while monitoring.");
            }
            
            lock (downloads) {
                if (!downloads.ContainsKey (key)) {
                    downloads.Add (key, d);
                    d.Finished += OnDownloadFinished;
                    d.ProcessingComplete += OnDownloadProcessed;
                    Log.DebugFormat ("Download added for {0}", key);
                }
            }
        }

//        public void AssociateObject (string key, object o)
//        {
//            AssociateObject (Get (key), o);
//        }
//        
//        public void AssociateObject (LibraryDownload d, object o)
//        {
//            if (d == null) {
//                throw new ArgumentNullException ("d");
//            }
//            
//            lock (associated) {
//                if (!associated.ContainsKey (d)) {
//                    associated.Add (d, o);
//                }
//            }
//        }
//
//        public object GetAssociatedObject (string key)
//        {
//            return GetAssociatedObject (Get (key));
//        }
//        
//        public object GetAssociatedObject (LibraryDownload d)
//        {
//            if (d == null) {
//                throw new ArgumentNullException ("d");
//            }
//            
//            lock (associated) {
//                if (associated.ContainsKey (d)) {
//                    return associated[d];
//                }
//            }
//
//            return null;
//        }
        
        public LibraryDownload Get (string key)
        {
            if (key == null) {
                throw new ArgumentNullException ("key");
            }
            
            Log.DebugFormat ("Getting download with key {0}", key);
            
            lock (downloads) {
                if (downloads.ContainsKey (key)) {
                    Log.DebugFormat ("Key found");
                    return downloads[key];
                }
            }

            return null;
        }

        public void Reset ()
        {
            Log.Debug ("resetting downloads");
            
            lock (downloads) {
                foreach (LibraryDownload d in downloads.Values) {
                    d.StopProcessing ();
                }
                
                downloads.Clear ();
            }
            
            //associated.Clear ();
            
            monitoring = false;
        }

        public bool MonitoredFinished ()
        {
            lock (downloads) {
                foreach (LibraryDownload d in downloads.Values) {
                    if (!d.IsFinished) return false;
                }
            }

            return true;
        }

        public bool ProcessingFinished ()
        {
            lock (downloads) {
                foreach (LibraryDownload d in downloads.Values) {
                    if (!d.Processed) return false;
                }
            }

            return true;
        }
        
        protected virtual void OnAllFinished (EventArgs args)
        {
            EventHandler <EventArgs> handler = AllFinished;
            if (handler != null) {
                handler (this, args);
            }
        }

        protected virtual void OnAllProcessed (EventArgs args)
        {
            EventHandler <EventArgs> handler = AllProcessed;
            if (handler != null) {
                handler (this, args);
            }
        }

        private void OnDownloadFinished (object sender, EventArgs args)
        {
            LibraryDownload download = sender as LibraryDownload;

            if (download != null && downloads.Values.Contains (download)) {
                download.StopProcessing ();
                
                if (monitoring && MonitoredFinished ()) {
                    OnAllFinished (EventArgs.Empty);
                }
            }
        }

        private void OnDownloadProcessed (object sender, EventArgs args)
        {
            LibraryDownload download = sender as LibraryDownload;

            if (download != null && downloads.Values.Contains (download)) {
                if (monitoring && ProcessingFinished ()) {
                    OnAllProcessed (EventArgs.Empty);
                }
            }
        }
    }

    public delegate void PayloadHandler (object sender, object [] o);
    
    public class LibraryDownload
    {
        private long timestamp = 0;
        private int last_sequence_num;
        private long total_downloaded;
        private long total_expected;
        
        private readonly ManualResetEvent manual_event = new ManualResetEvent (false);
        private readonly Queue <object [] > queue = new Queue<object [] > ();
        private readonly object sync = new object ();
        
        public event EventHandler <EventArgs> Finished;
        public event EventHandler <EventArgs> ProcessingComplete;
        
        public LibraryDownload ()
        {
            this.last_sequence_num = 0;
            this.total_downloaded = 0;
        }
        
        public LibraryDownload (long timestamp, long total_expected) : this ()
        {
            this.timestamp = timestamp;
            this.total_expected = total_expected;
        }

        public long Timestamp {
            get { return timestamp; }
            set { timestamp = value; }
        }

        public long TotalExpected {
            get { return total_expected; }
            set { total_expected = value; }
        }

        public int LastSequenceNum {
            get { return last_sequence_num; }
        }

        public long TotalDownloaded {
            get { return total_downloaded; }
        }

        public bool IsFinished {
            get { return IsStarted && total_downloaded == total_expected; }
        }

        public bool IsStarted {
            get { return timestamp != 0; }
        }

        private bool processed = false;
        public bool Processed {
            get { return processed; }
        }

        private bool processing = false;
        private PayloadHandler payload_handler;
        public void ProcessIncomingPayloads (PayloadHandler handler)
        {
            if (processing) {
                throw new InvalidOperationException ("Already processing.");
            }
            
            payload_handler = handler;
            processing = true;

            ThreadAssist.Spawn (Process);
        }

        public void StopProcessing ()
        {
            processing = false;
            manual_event.Set ();
        }

        private void Process ()
        {
            var handler = payload_handler;
            
            while (processing) {
                manual_event.WaitOne ();
                while (handler != null && QueueCount () > 0) {
                    handler (this, Dequeue ());
                }
            }

            // flush queue
            for (int i = QueueCount () - 1; i >= 0; i--) {
                handler (this, Dequeue ());
            }

            ThreadAssist.ProxyToMain (delegate {
                processed = true;
                OnProcessingComplete (EventArgs.Empty);
            });
        }

        private int QueueCount () 
        {
            lock (sync) {
                return queue.Count;
            }
        }
        
        private object[] Dequeue ()
        {
            lock (sync) {
                manual_event.Reset ();
                return queue.Dequeue ();
            }
        }
        
        private void Enqueue (object[] o)
        {
            lock (sync) {
                queue.Enqueue (o);
                manual_event.Set ();
            }
        }
        
        public void UpdateDownload (long timestamp, int seq, int chunk_size, object [] payload)
        {
            long expected_stamp = this.timestamp;
            int expected_seq = last_sequence_num + 1;

            //TODO maybe overkill here? DBus tubes are said to be ordered and reliable.
            // Good test of that, anyway!
            if (timestamp != expected_stamp) {
                throw new InvalidOperationException ("Unexpected timestamp.");
            } else if (expected_seq != seq) {
                throw new InvalidOperationException ("Out of sequence.");
            }
            
            last_sequence_num = seq;
            total_downloaded += chunk_size;

            Log.DebugFormat ("UpdateDownload: expected {0} downloaded {1}",
                             total_expected, total_downloaded);

            Enqueue (payload);
            
            if (IsFinished) {
                OnFinished (EventArgs.Empty);
            }
        }

        protected virtual void OnFinished (EventArgs args)
        {
            EventHandler <EventArgs> handler = Finished;
            if (handler != null) {
                handler (this, args);
            }
        }

        protected virtual void OnProcessingComplete (EventArgs args)
        {
            EventHandler <EventArgs> handler = ProcessingComplete;
            if (handler != null) {
                handler (this, args);
            }
        }
    }
}
