//
// PadBlockCallback.cs
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

    public delegate void PadBlockCallback (IntPtr pad, bool blocked, IntPtr user_data);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void PadBlockCallbackNative (IntPtr pad, bool blocked, IntPtr user_data);

    internal class PadBlockCallbackInvoker
    {

        PadBlockCallbackNative native_cb;

        ~PadBlockCallbackInvoker ()
        {
        }

        internal PadBlockCallbackInvoker (PadBlockCallbackNative native_cb)
        {
            this.native_cb = native_cb;
        }

        internal PadBlockCallback Handler {
            get { return new PadBlockCallback (InvokeNative); }
        }

        void InvokeNative (IntPtr pad, bool blocked, IntPtr user_data)
        {
            native_cb (pad, blocked, user_data);
        }
    }

    internal class PadBlockCallbackWrapper
    {

        public void NativeCallback (IntPtr pad, bool blocked, IntPtr user_data)
        {
            try {
                managed (pad, blocked, user_data);
                if (release_on_call)
                    gch.Free ();
            } catch (Exception e) {
                GLib.ExceptionManager.RaiseUnhandledException (e, false);
            }
        }

        bool release_on_call = false;
        GCHandle gch;

        public void PersistUntilCalled ()
        {
            release_on_call = true;
            gch = GCHandle.Alloc (this);
        }

        internal PadBlockCallbackNative NativeDelegate;
        PadBlockCallback managed;

        public PadBlockCallbackWrapper (PadBlockCallback managed)
        {
            this.managed = managed;
            if (managed != null)
                NativeDelegate = new PadBlockCallbackNative (NativeCallback);
        }

        public static PadBlockCallback GetManagedDelegate (PadBlockCallbackNative native)
        {
            if (native == null)
                return null;
            PadBlockCallbackWrapper wrapper = (PadBlockCallbackWrapper)native.Target;
            if (wrapper == null)
                return null;
            return wrapper.managed;
        }

    }
}
