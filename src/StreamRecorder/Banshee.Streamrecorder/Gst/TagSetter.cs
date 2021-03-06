//
// TagSetter.cs
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
extern alias oldGlib;
using OldGLib = oldGlib.GLib;

using System;
using System.Runtime.InteropServices;

namespace Banshee.Streamrecorder.Gst
{

    public class TagSetter : GstObject
    {

        public TagSetter (IntPtr tagsetter) : base(tagsetter)
        {
        }

        [DllImport("libgstreamer-1.0.so.0")]
        private static extern OldGLib.GType gst_tag_setter_get_type ();

        public static new OldGLib.GType GetType ()
        {
            return gst_tag_setter_get_type ();
        }

        [DllImport("libgstreamer-1.0.so.0")]
        unsafe private static extern void gst_tag_setter_merge_tags (IntPtr tagsetter, IntPtr taglist, TagMergeMode mode);

        public void MergeTags (TagList taglist, Gst.TagMergeMode mode)
        {
            gst_tag_setter_merge_tags (raw, taglist.ToIntPtr (), mode);
        }

    }
}
