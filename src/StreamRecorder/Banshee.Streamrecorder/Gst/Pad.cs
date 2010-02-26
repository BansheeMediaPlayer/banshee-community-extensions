//
// Pad.cs
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

    public class Pad : GstObject
    {

        public Pad (IntPtr pad) : base(pad)
        {
        }

        [DllImport("libgstreamer-0.10.so.0")]
        unsafe private static extern IntPtr gst_pad_new (IntPtr name, PadDirection direction);

        public Pad (PadDirection direction) : this(IntPtr.Zero, direction)
        {
        }

        public Pad (string name, PadDirection direction) : base(IntPtr.Zero)
        {
            IntPtr native_name = GLib.Marshaller.StringToPtrGStrdup (name);
            raw = gst_pad_new (native_name, direction);
            GLib.Marshaller.Free (native_name);
        }

        protected Pad (IntPtr native_name, PadDirection direction) : this(gst_pad_new (native_name, direction))
        {
        }

        [DllImport("libgstreamer-0.10.so.0")]
        unsafe private static extern IntPtr gst_pad_get_peer (IntPtr element);

        public Pad GetPeer ()
        {
            return new Pad (gst_pad_get_peer (raw));
        }

        [DllImport("libgstreamer-0.10.so.0")]
        private static extern bool gst_pad_send_event (IntPtr pad, IntPtr gevent);

        public bool SendEvent (IntPtr segment)
        {
            return gst_pad_send_event (raw, segment);
        }

        [DllImport("libgstreamer-0.10.so.0", CallingConvention = CallingConvention.Cdecl)]
        static extern bool gst_pad_set_blocked_async (IntPtr pad, bool blocked, PadBlockCallbackNative cb, IntPtr user_data);

        public bool SetBlockedAsync (bool blocked, PadBlockCallback cb, IntPtr user_data)
        {
            Hyena.Log.Debug ("[Streamrecorder.Gst.Pad]<PadSetBlockedAsync> START");
            PadBlockCallbackWrapper cb_wrapper = new PadBlockCallbackWrapper (cb);
            bool ret = gst_pad_set_blocked_async (raw, blocked, cb_wrapper.NativeDelegate, user_data);
            Hyena.Log.Debug ("[Streamrecorder.Gst.Pad]<PadSetBlockedAsync> END");
            return ret;
        }

        [DllImport("libgstreamer-0.10.so.0")]
        private static extern bool gst_pad_set_blocked (IntPtr pad, bool blocked);

        public bool SetBlocked (bool blocked)
        {
            return gst_pad_set_blocked (raw, blocked);
        }

        public GhostPad ToGhostPad ()
        {
            return new GhostPad (raw);
        }
    }
}
