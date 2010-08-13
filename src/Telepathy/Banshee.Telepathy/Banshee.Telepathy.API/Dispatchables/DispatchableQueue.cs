//
// DispatchableQueue.cs
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

namespace Banshee.Telepathy.API.Dispatchables
{
    // TODO not a "true" queue. Not really paying attention to order.
    // Maybe this is useless and should just use a Queue <T>. Fine for now,
    // I guess.
    internal class DispatchableQueue <T> where T : Dispatchable
    {
        private readonly IDictionary <Connection, IList <T>> queue = new Dictionary <Connection, IList <T>> ();

        private int item_count = 0;
        public int Count ()
        {
            /*
            int total = 0;

            lock (queue) {
                foreach (KeyValuePair <Connection, IList <T>> kv in queue) {
                    total += kv.Value.Count;
                }
            }

            return total;
            */

            return item_count;
        }

        public int Count (Connection conn)
        {
            if (conn == null) {
                throw new ArgumentNullException ("conn");
            }

            lock (queue) {
                if (queue.ContainsKey (conn)) {
                    return queue[conn].Count;
                }
            }

            return 0;
        }

        public void Enqueue (T obj)
        {
            if (obj == null) {
                throw new ArgumentNullException ("obj");
            }

            Connection conn = obj.Contact.Connection;

            lock (queue) {
                if (!queue.ContainsKey (conn)) {
                    queue.Add (conn, new List <T> ());
                }

                queue[conn].Add (obj);
                item_count++;
            }
        }

        public T Dequeue ()
        {
            lock (queue) {
                foreach (Connection conn in queue.Keys) {
                    if (conn != null) {
                        return Dequeue (conn);
                    }
                }
            }

            return null;
        }

        public T Dequeue (Connection conn)
        {
            T popped = null;

            lock (queue) {
                if (queue.ContainsKey (conn)) {
                    if (queue[conn].Count > 0) {
                        popped = queue[conn][0];
                        queue[conn].Remove (popped);
                        item_count--;
                    }
                }
            }

            return popped;
        }

        public void Remove (T obj)
        {
            if (obj == null) {
                throw new ArgumentNullException ("obj");
            }

            //Console.WriteLine ("DispatchableQueue.Remove called");
            Connection conn = obj.Contact.Connection;

            lock (queue) {
                if (queue.ContainsKey (conn)) {
                    if (queue[conn].Contains (obj)) {
                        queue[conn].Remove (obj);
                        Console.WriteLine ("Removed {0} from queue", (obj as FileTransfer).OriginalFilename);
                        item_count--;
                    }
                }
            }
        }

        public bool Exists (T obj)
        {
            if (obj == null) {
                throw new ArgumentNullException ("obj");
            }

            Connection conn = obj.Contact.Connection;

            lock (queue) {
                if (queue.ContainsKey (conn)) {
                    if (queue[conn].Contains (obj)) {
                        return true;
                    }
                }
            }

            return false;
        }

        public void Empty ()
        {
            lock (queue) {
                queue.Clear ();
                item_count = 0;
            }
        }

    }
}