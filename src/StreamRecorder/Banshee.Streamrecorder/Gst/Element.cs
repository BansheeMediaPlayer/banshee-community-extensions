//
// Element.cs
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

    public class Element : GstObject
    {

        public Element (IntPtr element) : base(element)
        {
        }

        [DllImport("libgstreamer-0.10.so.0")]
        static extern bool gst_element_remove_pad (IntPtr element, IntPtr pad);

        public bool RemovePad (Pad pad)
        {
            bool ret = gst_element_remove_pad (raw, pad.ToIntPtr ());
            return ret;
        }

        [DllImport("libgstreamer-0.10.so.0")]
        static extern IntPtr gst_element_get_static_pad (IntPtr element, IntPtr name);

        public Pad GetStaticPad (string name)
        {
            IntPtr native_name = GLib.Marshaller.StringToPtrGStrdup (name);
            Pad ret = new Pad (gst_element_get_static_pad (raw, native_name));
            GLib.Marshaller.Free (native_name);
            return ret;
        }

        [DllImport("libgstreamer-0.10.so.0")]
        static extern bool gst_element_add_pad (IntPtr element, IntPtr pad);

        public bool AddPad (Pad pad)
        {
            return gst_element_add_pad (raw, pad.ToIntPtr ());
        }

        [DllImport("libgstreamer-0.10.so.0", CallingConvention = CallingConvention.Cdecl)]
        static extern bool gst_element_link (IntPtr src, IntPtr dest);

        public bool Link (Element dest)
        {
            bool ret = gst_element_link (raw, dest.ToIntPtr ());
            return ret;
        }

        public void LinkMany (Element[] elements)
        {
            if (elements.Length < 1)
                return;
            this.Link (elements[0]);
            for (int i = 0; i < elements.Length - 1; i++) {
                elements[i].Link (elements[i + 1]);
            }
        }

        [DllImport("libgstreamer-0.10.so.0", CallingConvention = CallingConvention.Cdecl)]
        static extern void gst_element_unlink (IntPtr src, IntPtr dest);

        public void Unlink (Element dest)
        {
            gst_element_unlink (raw, dest.ToIntPtr ());
        }

        [DllImport("libgstreamer-0.10.so.0")]
        static extern int gst_element_set_state (IntPtr element, int state);

        public StateChangeReturn SetState (State state)
        {
            int raw_ret = gst_element_set_state (raw, (int)state);
            StateChangeReturn ret = (StateChangeReturn)raw_ret;
            return ret;
        }

        [DllImport("libgstreamer-0.10.so.0")]
        static extern bool gst_element_send_event (IntPtr element, IntPtr gstevent);

        public bool SendEvent (IntPtr gstevent)
        {
            return gst_element_send_event (raw, gstevent);
        }

        public FileSink ToFileSink ()
        {
            return new FileSink (raw);
        }

    }
}
