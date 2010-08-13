//
// TagList.cs
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

namespace Banshee.Streamrecorder.Gst
{

    public class TagList : GstObject
    {

        [DllImport("libgstreamer-0.10.so.0")]
        unsafe private static extern IntPtr gst_tag_list_new ();

        protected TagList (IntPtr taglist) : base(taglist)
        {
        }

        public TagList () : this(gst_tag_list_new ())
        {
        }

        [DllImport("libgstreamer-0.10.so.0")]
        unsafe private static extern void gst_tag_list_add_value (IntPtr taglist, TagMergeMode mode, IntPtr tag, ref GLib.Value value);

        public void AddStringValue (TagMergeMode mode, string tag, string value)
        {
            GLib.Value val = new GLib.Value (GLib.GType.String);
            val.Val = value;
            IntPtr native_tag = GLib.Marshaller.StringToPtrGStrdup (tag);
            gst_tag_list_add_value (raw, mode, native_tag, ref val);
            GLib.Marshaller.Free (native_tag);
        }

    }
}
