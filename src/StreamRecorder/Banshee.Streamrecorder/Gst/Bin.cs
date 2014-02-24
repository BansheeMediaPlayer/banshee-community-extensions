//
// Bin.cs
//
// Author:
//   Frank Ziegler
//
// Copyright (C) 2009 Frank Ziegler
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
using System.Runtime.InteropServices;
using System.Collections;
using System.Text;

namespace Banshee.Streamrecorder.Gst
{
    public class Bin : Element
    {

        [DllImport("libgstreamer-0.10.so.0")]
        private static extern IntPtr gst_bin_new (IntPtr name);

        public Bin () : base(gst_bin_new (IntPtr.Zero))
        {
        }

        public Bin (IntPtr bin) : base(bin)
        {
        }

        [DllImport("libgstreamer-0.10.so.0")]
        private static extern IntPtr gst_bin_iterate_sorted (IntPtr bin);

        public override string ToString ()
        {
            IntPtr raw_ret = gst_bin_iterate_sorted (raw);
            Iterator ret = raw_ret == IntPtr.Zero ? null : (Iterator) GLib.Opaque.GetOpaque (raw_ret, typeof (Iterator), false);
            IEnumerator e = ret.GetEnumerator ();
            if (e == null) return "null";
            StringBuilder res = new StringBuilder ();
            while (e.MoveNext())
            {
                if (e.Current != null)
                {
                    GstObject o = new GstObject ((IntPtr)(e.Current));
                    if (o != null)
                    {
                        res.Append (o.GetPathString ());
                        res.Append ("!");
                    }
                }
            }
            return res.ToString ().Trim ('!').Replace ("!", " ! ");

        }

        [DllImport("libgstreamer-0.10.so.0")]
        private static extern IntPtr gst_bin_get_by_interface (IntPtr bin, GLib.GType iface);

        public IntPtr GetByInterface (GLib.GType iface)
        {
            return gst_bin_get_by_interface (raw, iface);
        }

        [DllImport("libgstreamer-0.10.so.0")]
        private static extern IntPtr gst_bin_get_by_name (IntPtr bin, IntPtr name);

        public Element GetByName (string name)
        {
            IntPtr native_name = GLib.Marshaller.StringToPtrGStrdup (name);
            IntPtr raw_ret = gst_bin_get_by_name (raw, native_name);
            GLib.Marshaller.Free (native_name);
            return new Element (raw_ret);
        }

        [DllImport("libgstreamer-0.10.so.0")]
        static extern bool gst_bin_add (IntPtr bin, IntPtr element);

        public bool Add (Element element)
        {
            return gst_bin_add (raw, element.ToIntPtr ());
        }

        public void AddMany (Element[] elements)
        {
            foreach (Element element in elements) {
                Add (element);
            }
        }

        [DllImport("libgstreamer-0.10.so.0")]
        static extern bool gst_bin_remove (IntPtr bin, IntPtr element);

        public bool Remove (Element element)
        {
            return gst_bin_remove (raw, element.ToIntPtr ());
        }

    }

}
