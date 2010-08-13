//
// BaseProvider.cs
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

namespace Banshee.Telepathy.DBus
{
    public abstract class BaseProvider : IDisposable
    {
        public event ChunkReadyHandler ChunkReady;
        public event SingleChunkReadyHandler SingleChunkReady;

        protected readonly object payload_lock = new object ();
        protected readonly object timestamp_lock = new object ();
        protected IDictionary <int, IDictionary <string, object> []> buffer = null;
        private int id;

        public BaseProvider ()
        {

        }

        private bool use_buffer = true;
        public virtual bool UseBuffer {
            get { return use_buffer; }
            protected set {
                lock (payload_lock) {
                    use_buffer = value;
                }
            }
        }

        public int Id {
            get { return id; }
            protected set { id = value; }
        }

        private long current_timestamp = 0;
        public virtual long CurrentTimestamp {
            get { return current_timestamp; }
            protected set { current_timestamp = value; }

        }

        public abstract void GetChunks (int chunk_size);
        public abstract void GetChunk (long timestamp, int sequence_num);

        protected void OnChunkReady (string object_path,
                                        IDictionary <string, object> [] chunk,
                                        long timestamp,
                                        int seq_num,
                                        int total)
        {
            if (ChunkReady != null) {
                Console.WriteLine ("ChunkReady event raised. seq_num {0} total {1}",
                                   seq_num, total);
                ChunkReady (object_path, chunk, timestamp, seq_num, total);
            }
        }

        protected void OnSingleChunkReady (string object_path,
                                           IDictionary <string, object> [] chunk,
                                           long timestamp, int seq_num)
        {
            if (SingleChunkReady != null) {
                SingleChunkReady (object_path, chunk, timestamp, seq_num);
            }
        }

        public void Dispose ()
        {
            Dispose (true);
        }

        protected virtual void Dispose (bool disposing)
        {
            if (disposing) {
                buffer.Clear ();
                buffer = null;
            }
        }
    }

    // send sequence number and total (total items expected to be sent)
    // so user knows when all data has been sent even if chunks come out of order
    public delegate void ChunkReadyHandler (string object_path,
                                                IDictionary <string, object> [] chunk,
                                                long timestamp,
                                                int seq_num,
                                                int total);
    public delegate void SingleChunkReadyHandler (string object_path,
                                                  IDictionary <string, object> [] chunk,
                                                  long timestamp, int seq_num);
}
