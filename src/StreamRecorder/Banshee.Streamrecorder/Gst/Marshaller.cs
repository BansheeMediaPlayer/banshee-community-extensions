//
// Marshaller.cs
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
    public class Marshaller
    {

        private static string gst_version;

        private Marshaller ()
        {
        }

        /*
         * Initialization
         */
        public static bool Initialize ()
        {
            Init ();
            try {
                gst_version = VersionString ();
                DebugSetActive (false);
                Hyena.Log.Information ("[Streamrecorder.Gst.Marshaller] gstreamer version found: " + gst_version);
                return true;
            } catch (Exception e) {
                Hyena.Log.Error (e.ToString ());
                Hyena.Log.Error (e.Message);
            }
            return false;
        }

        /* Helper Import Wrappers */
        public static bool CheckGstPlugin (string name)
        {
            bool ret = false;
            ElementFactory element_factory;
            element_factory = ElementFactory.Find (name);
            if (!element_factory.IsNull ()) {
                ret = true;
                element_factory.UnRef ();
            }
            return ret;
        }

        public static IntPtr CreateSegment ()
        {
            ulong ClockTimeNone = 0xffffffffffffffffuL;
            return gst_event_new_new_segment (true, 1.0, Gst.Format.Default, 0, (long)ClockTimeNone, 0);
        }

        public static void DebugSetActive (bool active)
        {
            gst_debug_set_active (active);
        }

        public static string VersionString ()
        {
            return gst_version_string ();
        }

        public static IntPtr NewEOSEvent ()
        {
            return gst_event_new_eos ();
        }

        public static void Init ()
        {
            gst_init (IntPtr.Zero, IntPtr.Zero);
        }

        /* Helper Imports*/
        [DllImport("libgstreamer-0.10.so.0")]
        private static extern string gst_version_string ();

        [DllImport("libgstreamer-0.10.so.0")]
        private static extern void gst_debug_set_active (bool active);

        [DllImport("libgobject-2.0.so.0")]
        public static extern void g_signal_connect_data (IntPtr instance, IntPtr detailed_signal, BusFunc cb, IntPtr data, IntPtr zero, uint flags);

        [DllImport("libgstreamer-0.10.so.0")]
        private static extern IntPtr gst_event_new_new_segment (bool update, double rate, Gst.Format format, long start, long stop, long position);

        [DllImport("libgstreamer-0.10.so.0")]
        unsafe public static extern GLib.Value gst_structure_get_value (IntPtr structure, IntPtr name);

        [DllImport("libgstreamer-0.10.so.0")]
        unsafe public static extern IntPtr gst_message_get_structure (IntPtr message);

        [DllImport("libgstreamer-0.10.so.0")]
        unsafe public static extern IntPtr gst_event_new_eos ();

        [DllImport("libgstreamer-0.10.so.0")]
        unsafe public static extern void gst_init (IntPtr argc, IntPtr argv);

    }

}
