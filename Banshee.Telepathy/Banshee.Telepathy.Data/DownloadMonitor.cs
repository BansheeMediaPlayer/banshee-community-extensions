//
// DownloadMonitor.cs
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
    internal delegate void AllFinishedHandler (object sender, EventArgs args);
    
    internal class DownloadMonitor
    {
        private IDictionary <string, Download> downloads = new Dictionary <string, Download> ();
        private IDictionary <Download, object> associated = new Dictionary <Download, object> ();
        
        public event AllFinishedHandler AllFinished;
        
        public DownloadMonitor ()
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
        
        public void Add (string key, Download d)
        {
            if (monitoring) {
                throw new InvalidOperationException ("Can't add while monitoring.");
            }
            
            lock (downloads) {
                if (!downloads.ContainsKey (key)) {
                    downloads.Add (key, d);
                    d.Finished += OnDownloadFinished;
                    Log.DebugFormat ("Download added for {0}", key);
                }
            }
        }

        public void AssociateObject (string key, object o)
        {
            AssociateObject (Get (key), o);
        }
        
        public void AssociateObject (Download d, object o)
        {
            if (d == null) {
                throw new ArgumentNullException ("d");
            }
            
            lock (associated) {
                if (!associated.ContainsKey (d)) {
                    associated.Add (d, o);
                }
            }
        }

        public object GetAssociatedObject (string key)
        {
            return GetAssociatedObject (Get (key));
        }
        
        public object GetAssociatedObject (Download d)
        {
            if (d == null) {
                throw new ArgumentNullException ("d");
            }
            
            lock (associated) {
                if (associated.ContainsKey (d)) {
                    return associated[d];
                }
            }

            return null;
        }
        
        public Download Get (string key)
        {
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
            monitoring = false;
            downloads.Clear ();
            associated.Clear ();
        }

        public bool MonitoredFinished ()
        {
            lock (downloads) {
                foreach (Download d in downloads.Values) {
                    if (!d.IsFinished) return false;
                }
            }

            return true;
        }
        
        protected virtual void OnAllFinished (EventArgs args)
        {
            AllFinishedHandler handler = AllFinished;
            if (handler != null) {
                handler (this, args);
            }
        }

        private void OnDownloadFinished (object sender, EventArgs args)
        {
            
            Download download = sender as Download;

            if (downloads.Values.Contains (download)) {
                if (monitoring && MonitoredFinished ()) {
                    OnAllFinished (EventArgs.Empty);
                }
            }
        }
    }

    internal delegate void FinishedHandler (object sender, EventArgs args);
    
    internal class Download
    {
        private long timestamp = 0;
        private int last_sequence_num;
        private long total_downloaded;
        private long total_expected;
        

        public event FinishedHandler Finished;
        
        public Download ()
        {
            this.last_sequence_num = 0;
            this.total_downloaded = 0;
        }
        
        public Download (long timestamp, long total_expected) : this ()
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
            set { processed = value; }
        }
        
        public void UpdateDownload (long timestamp, int seq, int chunk_size)
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

            if (IsFinished) {
                OnFinished (EventArgs.Empty);
            }
        }

        protected virtual void OnFinished (EventArgs args)
        {
            FinishedHandler handler = Finished;
            if (handler != null) {
                handler (this, args);
            }
        }
    }
}
