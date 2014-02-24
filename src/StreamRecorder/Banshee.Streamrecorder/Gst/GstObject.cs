//
// Object.cs
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

    public class GstObject
    {

        protected IntPtr raw;

        public GstObject (IntPtr gstobject)
        {
            this.raw = gstobject;
        }

        public bool IsNull ()
        {
            return (raw == IntPtr.Zero);
        }

        [DllImport ("libgobject-2.0.so.0")]
        private static extern void g_object_get_property (IntPtr gobject, IntPtr property_name, ref OldGLib.Value value);

        public IntPtr GetProperty (string name)
        {
            OldGLib.Value val = new OldGLib.Value ();
            val.Init (OldGLib.GType.Object);
            IntPtr native_name = OldGLib.Marshaller.StringToPtrGStrdup (name);
            g_object_get_property (raw, native_name, ref val);
            OldGLib.Marshaller.Free (native_name);
            return ((OldGLib.Object)(val.Val)).Handle;
        }

        public string GetStringProperty (string name)
        {
            OldGLib.Value val = new OldGLib.Value ();
            val.Init (OldGLib.GType.String);
            IntPtr native_name = OldGLib.Marshaller.StringToPtrGStrdup (name);
            g_object_get_property (raw, native_name, ref val);
            OldGLib.Marshaller.Free (native_name);
            return val.Val as string;
        }

        [DllImport("libgstreamer-1.0.so.0")]
        unsafe private static extern void gst_util_set_object_arg (IntPtr gstobject, IntPtr name, IntPtr value);

        public void SetProperty (string name, string value)
        {
            IntPtr native_name = OldGLib.Marshaller.StringToPtrGStrdup (name);
            IntPtr native_value = OldGLib.Marshaller.StringToPtrGStrdup (value);
            gst_util_set_object_arg (raw, native_name, native_value);
            OldGLib.Marshaller.Free (native_name);
            OldGLib.Marshaller.Free (native_value);
        }

        [DllImport("libgobject-2.0.so.0")]
        private static extern void g_object_set_property (IntPtr gobject, IntPtr property_name, ref OldGLib.Value value);

        public void SetProperty (string name, Element value)
        {
            OldGLib.Value val = new OldGLib.Value (OldGLib.GType.Object);
            val.Val = OldGLib.Object.GetObject (value.ToIntPtr ());
            IntPtr native_name = OldGLib.Marshaller.StringToPtrGStrdup (name);
            g_object_set_property (raw, native_name, ref val);
            OldGLib.Marshaller.Free (native_name);
        }

        public void SetStringProperty (string name, string value)
        {
            OldGLib.Value val = new OldGLib.Value (value);
            IntPtr native_name = OldGLib.Marshaller.StringToPtrGStrdup (name);
            g_object_set_property (raw, native_name, ref val);
            OldGLib.Marshaller.Free (native_name);
        }

        public void SetBooleanProperty (string name, bool value)
        {
            OldGLib.Value val = new OldGLib.Value (OldGLib.GType.Boolean);
            val.Val = value;
            IntPtr native_name = OldGLib.Marshaller.StringToPtrGStrdup (name);
            g_object_set_property (raw, native_name, ref val);
            OldGLib.Marshaller.Free (native_name);
        }

        public void SetIntegerProperty (string name, int value)
        {
            OldGLib.Value val = new OldGLib.Value (OldGLib.GType.Int);
            val.Val = value;
            IntPtr native_name = OldGLib.Marshaller.StringToPtrGStrdup (name);
            g_object_set_property (raw, native_name, ref val);
            OldGLib.Marshaller.Free (native_name);
        }

        public void SetFloatProperty (string name, float value)
        {
            OldGLib.Value val = new OldGLib.Value (OldGLib.GType.Float);
            val.Val = value;
            IntPtr native_name = OldGLib.Marshaller.StringToPtrGStrdup (name);
            g_object_set_property (raw, native_name, ref val);
            OldGLib.Marshaller.Free (native_name);
        }

        public IntPtr ToIntPtr ()
        {
            return raw;
        }

        [DllImport("libgstreamer-1.0.so.0")]
        unsafe private static extern IntPtr gst_object_get_path_string (IntPtr gstobject);

        public string GetPathString ()
        {
            IntPtr raw_ret = gst_object_get_path_string (raw);
            string ret = OldGLib.Marshaller.Utf8PtrToString (raw_ret);
            OldGLib.Marshaller.Free (raw_ret);
            return ret;
        }

        [DllImport("libgstreamer-1.0.so.0")]
        unsafe private static extern IntPtr gst_object_get_parent (IntPtr gstobject);

        public GstObject GetParent ()
        {
            return new GstObject (gst_object_get_parent (raw));
        }

        [DllImport("libgstreamer-1.0.so.0")]
        unsafe private static extern void gst_object_unref (IntPtr gstobject);

        public void UnRef ()
        {
            gst_object_unref (raw);
        }

        [DllImport("libgstreamer-1.0.so.0")]
        unsafe private static extern IntPtr gst_object_ref (IntPtr element);

        public void Ref ()
        {
            gst_object_ref (raw);
        }

    }

}
