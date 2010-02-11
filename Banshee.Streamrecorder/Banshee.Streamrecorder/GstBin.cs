//
// GstBin.cs
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
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Runtime.InteropServices;

using System.Text.RegularExpressions;

using Mono.Addins;

using Banshee.ServiceStack;
using Banshee.Collection;

using Hyena;

namespace Banshee.Streamrecorder.Gst
{
    public class Bin
    {

		private IntPtr bin;
		
        [DllImport ("libgstreamer-0.10.so.0")]
		private static extern IntPtr gst_bin_new (IntPtr name);

		public Bin ()
		{
			this.bin = gst_bin_new (IntPtr.Zero);
		}
		
        public Bin (IntPtr bin) 
        {
			this.bin = bin;
        }
        
        public IntPtr BinPtr
        {
			get { return bin; }
			set { this.bin = value ; }
		}

        [DllImport ("libgstreamer-0.10.so.0")]
		private static extern IntPtr gst_bin_get_by_interface (IntPtr bin, GLib.GType iface);

		public IntPtr GetByInterface(GLib.GType iface)
		{
			return gst_bin_get_by_interface(bin, iface);
		}

        [DllImport ("libgstreamer-0.10.so.0")]
		private static extern IntPtr gst_bin_get_by_name (IntPtr bin, IntPtr name);
		
		public IntPtr GetByName (string name)
		{
			IntPtr native_name = GLib.Marshaller.StringToPtrGStrdup (name);
			return gst_bin_get_by_name(bin, native_name);
		}

        [DllImport ("libgstreamer-0.10.so.0")]
		static extern IntPtr gst_element_get_pad (IntPtr element, IntPtr name);
		
		public IntPtr GetPad (string name)
		{
			IntPtr native_name = GLib.Marshaller.StringToPtrGStrdup (name);
			return gst_element_get_pad(bin, native_name);
		}
		
        [DllImport ("libgstreamer-0.10.so.0")]
		static extern bool gst_element_add_pad (IntPtr element, IntPtr pad);
		
		public bool AddPad (IntPtr pad)
		{
			return gst_element_add_pad(bin, pad);
		}
		
        [DllImport ("libgstreamer-0.10.so.0")]
		static extern bool gst_bin_add (IntPtr bin, IntPtr element);
		
		public bool Add (IntPtr element)
		{
			return gst_bin_add(bin, element);
		}
		
		public void AddMany(IntPtr[] elements)
		{
			bool ret;
			foreach (IntPtr element in elements)
			{
				ret = Add(element);
			}
		}

        [DllImport ("libgstreamer-0.10.so.0")]
		static extern bool gst_bin_remove (IntPtr bin, IntPtr element);
		
		public bool Remove (IntPtr element)
		{
			return gst_bin_remove(bin, element);
		}
		
    }

}
