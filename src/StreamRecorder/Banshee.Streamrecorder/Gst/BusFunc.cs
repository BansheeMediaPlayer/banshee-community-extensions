//
// BusFunc.cs
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

    public delegate bool BusFunc (IntPtr bus, IntPtr message, IntPtr user_data);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate bool BusFuncNative (IntPtr bus, IntPtr message, IntPtr user_data);

    internal class BusFuncInvoker
    {

        BusFuncNative native_cb;

        ~BusFuncInvoker ()
        {
        }

        internal BusFuncInvoker (BusFuncNative native_cb)
        {
            this.native_cb = native_cb;
        }

        internal Gst.BusFunc Handler {
            get { return new Gst.BusFunc (InvokeNative); }
        }

        bool InvokeNative (IntPtr bus, IntPtr message, IntPtr user_data)
        {
            bool result = native_cb (bus, message, user_data);
            return result;
        }
    }

    internal class BusFuncWrapper
    {

        public bool NativeCallback (IntPtr bus, IntPtr message, IntPtr user_data)
        {
            try {
                bool __ret = managed (bus, message, user_data);
                if (release_on_call)
                    gch.Free ();
                return __ret;
            } catch (Exception e) {
                GLib.ExceptionManager.RaiseUnhandledException (e, false);
                return false;
            }
        }

        bool release_on_call = false;
        GCHandle gch;

        public void PersistUntilCalled ()
        {
            release_on_call = true;
            gch = GCHandle.Alloc (this);
        }

        internal BusFuncNative NativeDelegate;
        BusFunc managed;

        public BusFuncWrapper (Gst.BusFunc managed)
        {
            this.managed = managed;
            if (managed != null)
                NativeDelegate = new BusFuncNative (NativeCallback);
        }

        public static BusFunc GetManagedDelegate (BusFuncNative native)
        {
            if (native == null)
                return null;
            BusFuncWrapper wrapper = (BusFuncWrapper)native.Target;
            if (wrapper == null)
                return null;
            return wrapper.managed;
        }
    }

}
