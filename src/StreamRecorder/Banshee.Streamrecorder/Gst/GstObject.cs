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
        private static extern void g_object_get_property (IntPtr gobject, IntPtr property_name, ref GLib.Value value);

        public IntPtr GetProperty (string name)
        {
            GLib.Value val = new GLib.Value ();
            val.Init (GLib.GType.Object);
            IntPtr native_name = GLib.Marshaller.StringToPtrGStrdup (name);
            g_object_get_property (raw, native_name, ref val);
            GLib.Marshaller.Free (native_name);
            return ((GLib.Object)(val.Val)).Handle;
        }

        public string GetStringProperty (string name)
        {
            GLib.Value val = new GLib.Value ();
            val.Init (GLib.GType.String);
            IntPtr native_name = GLib.Marshaller.StringToPtrGStrdup (name);
            g_object_get_property (raw, native_name, ref val);
            GLib.Marshaller.Free (native_name);
            return val.Val as string;
        }

        [DllImport("libgstreamer-0.10.so.0")]
        unsafe private static extern void gst_util_set_object_arg (IntPtr gstobject, IntPtr name, IntPtr value);

        public void SetProperty (string name, string value)
        {
            IntPtr native_name = GLib.Marshaller.StringToPtrGStrdup (name);
            IntPtr native_value = GLib.Marshaller.StringToPtrGStrdup (value);
            gst_util_set_object_arg (raw, native_name, native_value);
            GLib.Marshaller.Free (native_name);
            GLib.Marshaller.Free (native_value);
        }

        [DllImport("libgobject-2.0.so.0")]
        private static extern void g_object_set_property (IntPtr gobject, IntPtr property_name, ref GLib.Value value);

        public void SetProperty (string name, Element value)
        {
            GLib.Value val = new GLib.Value (GLib.GType.Object);
            val.Val = GLib.Object.GetObject (value.ToIntPtr ());
            IntPtr native_name = GLib.Marshaller.StringToPtrGStrdup (name);
            g_object_set_property (raw, native_name, ref val);
            GLib.Marshaller.Free (native_name);
        }

        public void SetStringProperty (string name, string value)
        {
            GLib.Value val = new GLib.Value (GLib.GType.String);
            val.Val = value;
            IntPtr native_name = GLib.Marshaller.StringToPtrGStrdup (name);
            g_object_set_property (raw, native_name, ref val);
            GLib.Marshaller.Free (native_name);
        }

        public void SetBooleanProperty (string name, bool value)
        {
            GLib.Value val = new GLib.Value (GLib.GType.Boolean);
            val.Val = value;
            IntPtr native_name = GLib.Marshaller.StringToPtrGStrdup (name);
            g_object_set_property (raw, native_name, ref val);
            GLib.Marshaller.Free (native_name);
        }

        public void SetIntegerProperty (string name, int value)
        {
            GLib.Value val = new GLib.Value (GLib.GType.Int);
            val.Val = value;
            IntPtr native_name = GLib.Marshaller.StringToPtrGStrdup (name);
            g_object_set_property (raw, native_name, ref val);
            GLib.Marshaller.Free (native_name);
        }

        public void SetFloatProperty (string name, float value)
        {
            GLib.Value val = new GLib.Value (GLib.GType.Float);
            val.Val = value;
            IntPtr native_name = GLib.Marshaller.StringToPtrGStrdup (name);
            g_object_set_property (raw, native_name, ref val);
            GLib.Marshaller.Free (native_name);
        }

        public IntPtr ToIntPtr ()
        {
            return raw;
        }

        [DllImport("libgstreamer-0.10.so.0")]
        unsafe private static extern IntPtr gst_object_get_path_string (IntPtr gstobject);

        public string GetPathString ()
        {
            IntPtr raw_ret = gst_object_get_path_string (raw);
            string ret = GLib.Marshaller.Utf8PtrToString (raw_ret);
            GLib.Marshaller.Free (raw_ret);
            return ret;
        }

        [DllImport("libgstreamer-0.10.so.0")]
        unsafe private static extern IntPtr gst_object_get_parent (IntPtr gstobject);

        public GstObject GetParent ()
        {
            return new GstObject (gst_object_get_parent (raw));
        }

        [DllImport("libgstreamer-0.10.so.0")]
        unsafe private static extern void gst_object_unref (IntPtr gstobject);

        public void UnRef ()
        {
            gst_object_unref (raw);
        }

        [DllImport("libgstreamer-0.10.so.0")]
        unsafe private static extern IntPtr gst_object_ref (IntPtr element);

        public void Ref ()
        {
            gst_object_ref (raw);
        }

    }

}
