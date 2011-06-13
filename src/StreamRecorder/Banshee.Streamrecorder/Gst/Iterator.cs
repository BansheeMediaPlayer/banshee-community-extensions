//
// Iterator.cs
//
// Author:
//   Frank Ziegler
//
// Copyright (c) 2011 Frank Ziegler
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

namespace Banshee.Streamrecorder.Gst
{
    using System;
    using System.Collections;
    using System.Runtime.InteropServices;

    public partial class Iterator : GLib.Opaque, IEnumerable
    {

        public Iterator (IntPtr raw) : base(raw)
        {
        }

        [DllImport("libgstreamer-0.10.so.0")]
        static extern int gst_iterator_next (IntPtr iterator, out IntPtr elem);

        [DllImport("libgstreamer-0.10.so.0")]
        static extern void gst_iterator_resync (IntPtr iterator);

        private class Enumerator : IEnumerator
        {

            Iterator iterator;
            Hashtable seen = new Hashtable ();

            private IntPtr current = IntPtr.Zero;

            public object Current {
                get { return current; }
            }

            public bool MoveNext ()
            {
                IntPtr raw_ret;
                bool retry = false;

                if (iterator.Handle == IntPtr.Zero)
                    return false;

                do {
                    int ret = gst_iterator_next (iterator.Handle, out raw_ret);
                    switch (ret) {
                    case 0:
                        return false;
                    case 1:
                        if (seen.Contains (raw_ret)) {
                            retry = true;
                            break;
                        }
                        seen.Add (raw_ret, null);
                        current = raw_ret;
                        return true;
                    case 2:
                        gst_iterator_resync (iterator.Handle);
                        retry = true;
                        break;
                    default:
                    case 3:
                        throw new Exception ("Error while iterating pads");
                    }
                } while (retry);

                return false;
            }

            public void Reset ()
            {
                seen.Clear ();
                if (iterator.Handle != IntPtr.Zero)
                    gst_iterator_resync (iterator.Handle);
            }

            public Enumerator (Iterator iterator)
            {
                this.iterator = iterator;
            }
        }

        private Enumerator enumerator = null;

        public IEnumerator GetEnumerator ()
        {
            if (this.enumerator == null)
                this.enumerator = new Enumerator (this);
            return this.enumerator;
        }

        [DllImport("libgstreamer-0.10.so.0")]
        static extern void gst_iterator_free (IntPtr iterator);

        ~Iterator ()
        {
            if (Raw != IntPtr.Zero)
                gst_iterator_free (Raw);
        }

    }
}
