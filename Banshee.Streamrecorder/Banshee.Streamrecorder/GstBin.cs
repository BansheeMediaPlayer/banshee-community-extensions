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

namespace Banshee.Streamrecorder
{
    public class GstBin
    {

		private IntPtr bin;
		
        public GstBin (IntPtr bin) 
        {
			this.bin = bin;
        }
        
        public IntPtr GstBinPtr
        {
			get { return bin; }
			set { this.bin = value ; }
		}

        [DllImport ("libgstreamer-0.10.so.0")]
		private static extern IntPtr gst_bin_get_by_interface (IntPtr bin, uint iface);
		public IntPtr GetByInterface(uint iface)
		{
			return gst_bin_get_by_interface(bin, iface);
		}

        [DllImport ("libgstreamer-0.10.so.0")]
		private static extern IntPtr gst_bin_get_by_name (IntPtr bin, string name);
		
		public IntPtr GetByName (string name)
		{
			return gst_bin_get_by_name(bin, name);
		}
    }

}
