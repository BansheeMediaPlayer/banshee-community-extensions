//
// Bus.cs
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

    public class Bus : GstObject
    {

        public Bus (IntPtr bus) : base(bus)
        {
        }

        [DllImport("libgstreamer-0.10.so.0")]
        public static extern IntPtr gst_bus_pop (IntPtr bus);

        protected IntPtr Pop ()
        {
            return gst_bus_pop (raw);
        }

        public GLib.Value PopMessageStructure (string name)
        {
            IntPtr structure = Gst.Marshaller.gst_message_get_structure (Pop ());
            if (structure == IntPtr.Zero) {
                return new GLib.Value (IntPtr.Zero);
            }
            IntPtr native_name = GLib.Marshaller.StringToPtrGStrdup (name);
            GLib.Value val = Gst.Marshaller.gst_structure_get_value (structure, native_name);
            GLib.Marshaller.Free (native_name);
            return val;
        }

        [DllImport("libgstreamer-0.10.so.0")]
        unsafe public static extern void gst_bus_add_signal_watch (IntPtr bus);

        public void AddSignalWatch ()
        {
            gst_bus_add_signal_watch (raw);
        }
    }
}
