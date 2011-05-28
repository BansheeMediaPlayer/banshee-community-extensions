//
// FloatingQueue.cs
//
// Author:
//       Mathijs Dumon <mathijsken@hotmail.com>
//
// Copyright (c) 2010 Mathijs Dumon
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Threading;
using System.Collections.Generic;

using ClutterFlow;

namespace Banshee.ClutterFlow
{
    /// <summary>
    /// The FloatingQueue class runs an IndexedQueue under the hood. It provides you
    /// with fast, focused enqueue and dequeue methods. Thread-safe class.
    /// </summary>
    public class FloatingQueue<T> : IDisposable where T : class, IIndexable
    {
        IndexedQueue<T> queue = new IndexedQueue<T> ();

        // the offset from the focus
        private int offset = 0;
        // positive or negative offset
        private int sign = 1;
        private int focus = 0;
        public int Focus {
            get { lock (SyncRoot) { return focus; } }
            set {
                lock (SyncRoot) {
                    if (focus!=value) {
                        focus = value;
                        ResetFloaters ();
                    }
                }
            }
        }

        public int Count {
            get { lock (SyncRoot) { return queue.Count; } }
        }

        public object SyncRoot {
            get { return queue.SyncRoot; }
        }

        public FloatingQueue ()
        {
            lock (SyncRoot) {
                queue.Changed += HandleChanged;
            }
        }

        protected bool disposed = false;
        public virtual void Dispose ()
        {
            if (disposed)
                return;
            disposed = true;
            lock (SyncRoot) {
                queue.Changed -= HandleChanged;
                queue.Dispose ();
            }
        }


        #region Methods

        protected virtual void ResetFloaters ()
        {
            offset = 0;
            sign = 1;
        }

        public virtual void Enqueue (T item)
        {
            lock (SyncRoot) {
                queue.Add(item);
                Monitor.Pulse (SyncRoot);
            }
        }

        public virtual T Dequeue ()
        {
            lock (SyncRoot) {
                if (queue.Count==0) {
                    //Console.WriteLine("Inside zero count dequeue");
                    return null;
                } else if (queue.Count==1 && queue.TryKey(-1)!=null) {
                    //Console.WriteLine("Inside offscreen dequeue");
                    return queue.PopFrom(-1);
                } else {
                    //Console.WriteLine("Inside normal Dequeue Focus == " + focus + " offset == " + offset);
                    int index = focus + offset * sign;
                    T curr = queue.TryKey(index);
                    while (curr==null || offset == 10000) {
                        //Console.WriteLine("                        WHILE WHILE WHILE");
                        sign = -sign;
                        if (sign < 0)
                            offset++;
                        index = focus + offset * sign;
                        if (sign < 0) {
                            //we do not want offscreens to get loaded yet
                            index = Math.Max(1, index);
                        }
                        curr = queue.TryKey(index);
                    }
                    return queue.PopFrom(index);
                }
            }
        }


        private void HandleChanged(object sender, EventArgs e)
        {
            lock (SyncRoot) {
                ResetFloaters ();
            }
        }
        #endregion
    }
}