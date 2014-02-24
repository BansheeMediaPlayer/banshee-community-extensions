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
extern alias oldGlib;
using OldGLib = oldGlib.GLib;

using System;
using System.Runtime.InteropServices;

namespace Banshee.Streamrecorder.Gst
{

    public delegate PadProbeReturn PadProbeCallback (IntPtr pad, IntPtr probe_info, IntPtr user_data);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate PadProbeReturn PadProbeCallbackNative (IntPtr pad, IntPtr probe_info, IntPtr user_data);

    internal class PadProbeCallbackInvoker
    {

        PadProbeCallbackNative native_cb;

        ~PadProbeCallbackInvoker ()
        {
        }

        internal PadProbeCallbackInvoker (PadProbeCallbackNative native_cb)
        {
            this.native_cb = native_cb;
        }

        internal PadProbeCallback Handler {
            get { return new PadProbeCallback (InvokeNative); }
        }

        PadProbeReturn InvokeNative (IntPtr pad, IntPtr probe_info, IntPtr user_data)
        {
            return native_cb (pad, probe_info, user_data);
        }
    }

    internal class PadProbeCallbackWrapper
    {

        public PadProbeReturn NativeCallback (IntPtr pad, IntPtr probe_info, IntPtr user_data)
        {
            try {
                PadProbeReturn ret;
                ret = managed (pad, probe_info, user_data);
                if (release_on_call)
                    gch.Free ();
                return ret;
            } catch (Exception e) {
                OldGLib.ExceptionManager.RaiseUnhandledException (e, false);
            }
            return PadProbeReturn.GST_PAD_PROBE_DROP;
        }

        bool release_on_call = false;
        GCHandle gch;

        public void PersistUntilCalled ()
        {
            release_on_call = true;
            gch = GCHandle.Alloc (this);
        }

        internal PadProbeCallbackNative NativeDelegate;
        PadProbeCallback managed;

        public PadProbeCallbackWrapper (PadProbeCallback managed)
        {
            this.managed = managed;
            if (managed != null)
                NativeDelegate = new PadProbeCallbackNative (NativeCallback);
        }

        public static PadProbeCallback GetManagedDelegate (PadProbeCallbackNative native)
        {
            if (native == null)
                return null;
            PadProbeCallbackWrapper wrapper = (PadProbeCallbackWrapper)native.Target;
            if (wrapper == null)
                return null;
            return wrapper.managed;
        }

    }
}
