//
// Parse.cs
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

    public class Parse
    {

        private Parse ()
        {
        }

        [DllImport("libgstreamer-0.10.so.0")]
        unsafe private static extern IntPtr gst_parse_launch (IntPtr bin_description, out IntPtr gerror);

        unsafe public static Pipeline Launch (string pipeline_description)
        {
            IntPtr native_bin_description = GLib.Marshaller.StringToPtrGStrdup (pipeline_description);
            IntPtr error = IntPtr.Zero;
            IntPtr raw_ret = gst_parse_launch (native_bin_description, out error);
            GLib.Marshaller.Free (native_bin_description);
            if (error != IntPtr.Zero)
                throw new GLib.GException (error);
            return new Pipeline (raw_ret);
        }

        [DllImport("libgstreamer-0.10.so.0")]
        unsafe private static extern IntPtr gst_parse_bin_from_description (IntPtr bin_description, bool ghost_unlinked_pads, out IntPtr gerror);

        unsafe public static Bin BinFromDescription (string bin_description, bool ghost_unlinked_pads)
        {
            IntPtr native_bin_description = GLib.Marshaller.StringToPtrGStrdup (bin_description);
            IntPtr error = IntPtr.Zero;
            IntPtr raw_ret = gst_parse_bin_from_description (native_bin_description, ghost_unlinked_pads, out error);
            GLib.Marshaller.Free (native_bin_description);
            if (error != IntPtr.Zero)
                throw new GLib.GException (error);
            return new Bin (raw_ret);
        }

    }
}
