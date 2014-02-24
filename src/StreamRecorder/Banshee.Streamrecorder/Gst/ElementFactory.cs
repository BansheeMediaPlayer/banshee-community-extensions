//
// ElementFactory.cs
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


    public class ElementFactory : GstObject
    {

        public ElementFactory (IntPtr elementfactory) : base(elementfactory)
        {
        }

        [DllImport("libgstreamer-1.0.so.0")]
        unsafe private static extern IntPtr gst_element_factory_make (IntPtr factoryname, IntPtr name);

        public static Element Make (string factoryname)
        {
            IntPtr native_factoryname = OldGLib.Marshaller.StringToPtrGStrdup (factoryname);
            IntPtr raw_ret = gst_element_factory_make (native_factoryname, IntPtr.Zero);
            OldGLib.Marshaller.Free (native_factoryname);
            return new Element (raw_ret);
        }

        public static Element Make (string factoryname, string name)
        {
            IntPtr native_factoryname = OldGLib.Marshaller.StringToPtrGStrdup (factoryname);
            IntPtr native_name = OldGLib.Marshaller.StringToPtrGStrdup (name);
            IntPtr raw_ret = gst_element_factory_make (native_factoryname, native_name);
            OldGLib.Marshaller.Free (native_factoryname);
            OldGLib.Marshaller.Free (native_name);
            return new Element (raw_ret);
        }

        [DllImport("libgstreamer-1.0.so.0")]
        unsafe private static extern IntPtr gst_element_factory_find (IntPtr name);

        public static ElementFactory Find (string name)
        {
            IntPtr native_name = OldGLib.Marshaller.StringToPtrGStrdup (name);
            ElementFactory element_factory = new ElementFactory (gst_element_factory_find (native_name));
            OldGLib.Marshaller.Free (native_name);
            return element_factory;
        }

    }
}
