//
// Gst.cs
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
    public class Marshaller
    {

		private static string gst_version;
		
        private Marshaller () 
        {
        }
		/*
		 * Pipeline AddTee
		 * audiotee=fixture
		 */
        public static bool PlayerAddTee(Gst.Bin audiotee, Gst.Bin element, bool use_pad_block)
        {
			Gst.Bin[] user_bins = new Gst.Bin[2] { audiotee , element };
			GCHandle gch = GCHandle.Alloc(user_bins);
			IntPtr user_data = GCHandle.ToIntPtr(gch);
			
			IntPtr fixture_pad = audiotee.GetStaticPad("sink");
			IntPtr block_pad = gst_pad_get_peer(fixture_pad);
			gst_object_unref(fixture_pad);

			if (use_pad_block)
			{
				string whatpad = ObjectGetPathString(block_pad);
				Hyena.Log.Debug("[Streamrecorder.Gst.Marshaller]<PlayerAddTee> blockin pad " + whatpad + " to perform an operation");

				PadSetBlockedAsync(block_pad, true, ReallyAddTee, user_data);
			} else {
				Hyena.Log.Debug("Streamrecorder.Gst.Marshaller]<PlayerAddTee> not using blockin pad, calling operation directly");
				ReallyAddTee(block_pad, false, user_data);
			}
			gst_object_unref (block_pad);
			return true;
		}
		
		public static void PlayerAddRemoveTeeDone(IntPtr pad, bool blocked, IntPtr new_pad)
		{
			ulong ClockTimeNone = 0xffffffffffffffff;
			IntPtr segment;
			if (new_pad == IntPtr.Zero)
			{
				return;
			}

			// send a very unimaginative new segment through the new pad
			segment = gst_event_new_new_segment (true,1.0, Gst.Format.Default, 0, (long)ClockTimeNone, 0);
			gst_pad_send_event(new_pad, segment);
			//gst_object_unref (new_pad);
		}

		private static void ReallyAddTee(IntPtr pad, bool blocked, IntPtr user_data)
		{
			Hyena.Log.Debug("[Streamrecorder.Gst.Marshaller]<ReallyAddTee> START");
			
			GCHandle gch = GCHandle.FromIntPtr(user_data);
			Gst.Bin[] user_bins = (Gst.Bin[])gch.Target;
			Gst.Bin fixture = user_bins[0];
			Gst.Bin element = user_bins[1];
			
			Hyena.Log.Debug("[Streamrecorder.Gst.Marshaller]<ReallyAddTee> path for fixture: " + ObjectGetPathString(fixture.BinPtr));
			Hyena.Log.Debug("[Streamrecorder.Gst.Marshaller]<ReallyAddTee> path for element: " + ObjectGetPathString(element.BinPtr));

			IntPtr queue;
			IntPtr audioconvert;
			Gst.Bin bin;
			Gst.Bin parent_bin;
			IntPtr sink_pad;
			IntPtr ghost_pad;
			IntPtr element_parent;
			
			element_parent = gst_object_get_parent(element.BinPtr);
			if (element_parent != IntPtr.Zero)
			{
				Hyena.Log.Debug("[Streamrecorder.Gst.Marshaller]<ReallyAddTee>element already linked, exiting. assume double function call");
				gst_object_unref(element_parent);
				return;
			}
			
			Hyena.Log.Debug("[Streamrecorder.Gst.Marshaller]<ReallyAddTee>adding tee " + ObjectGetPathString(element.BinPtr));
			
			/* set up containing bin */
			bin = new Bin ();
			queue = ElementFactoryMake("queue");
			audioconvert = ElementFactoryMake("audioconvert");
			
			ObjectSetBooleanProperty(bin.BinPtr, "async-handling", true);
			ObjectSetIntegerProperty(queue, "max-size-buffers", 3);

			bin.AddMany( new IntPtr[3] { queue, audioconvert, element.BinPtr } );
			ElementLinkMany( new IntPtr[3] { queue, audioconvert, element.BinPtr } );
			
			sink_pad = new Gst.Bin(queue).GetStaticPad("sink");
			ghost_pad = GhostPadNew ("sink", sink_pad);
			bin.AddPad(ghost_pad);
			//gst_object_unref (sink_pad);

			parent_bin = new Gst.Bin(gst_object_get_parent(fixture.BinPtr));
			parent_bin.Add(bin.BinPtr);
			ElementLink(fixture.BinPtr, bin.BinPtr);
			
			if (blocked)
			{
				Hyena.Log.Debug("[Streamrecorder.Gst.Marshaller]<ReallyAddTee> unblocking pad after adding tee");
				
				ElementSetState(parent_bin.BinPtr,Gst.State.Playing);
				gst_object_ref(ghost_pad);
				gst_object_unref(parent_bin.BinPtr);
				PadSetBlockedAsync(pad,false,PlayerAddRemoveTeeDone,ghost_pad);				
			} else {
				ElementSetState(parent_bin.BinPtr,Gst.State.Paused);
				gst_object_ref(ghost_pad);
				gst_object_unref(parent_bin.BinPtr);
				PlayerAddRemoveTeeDone(IntPtr.Zero,false,ghost_pad);
			}

			Hyena.Log.Debug("[Streamrecorder.Gst.Marshaller]<ReallyAddTee> END");

		}
		
		/*
		 * Pipeline RemoveTee
		 */
        public static bool PlayerRemoveTee(Gst.Bin audiotee, Gst.Bin element, bool use_pad_block)
        {
			Gst.Bin[] user_bins = new Gst.Bin[2] { audiotee , element };
			GCHandle gch = GCHandle.Alloc(user_bins);
			IntPtr user_data = GCHandle.ToIntPtr(gch);
			
			IntPtr fixture_pad = audiotee.GetStaticPad("sink");
			IntPtr block_pad = gst_pad_get_peer(fixture_pad);
			gst_object_unref(fixture_pad);

			if (use_pad_block)
			{
				string whatpad = ObjectGetPathString(block_pad);
				Hyena.Log.Debug("[Streamrecorder.Gst.Marshaller]<PlayerRemoveTee> blockin pad " + whatpad + " to perform an operation");

				PadSetBlockedAsync(block_pad, true, ReallyRemoveTee, user_data);
			} else {
				Hyena.Log.Debug("[Streamrecorder.Gst.Marshaller]<PlayerRemoveTee> not using blockin pad, calling operation directly");
				ReallyRemoveTee(block_pad, false, user_data);
			}
			gst_object_unref (block_pad);
			return true;
		}

		private static void ReallyRemoveTee(IntPtr pad, bool blocked, IntPtr user_data)
		{
			GCHandle gch = GCHandle.FromIntPtr(user_data);
			Gst.Bin[] user_bins = (Gst.Bin[])gch.Target;
			//Gst.Bin fixture = user_bins[0];
			Gst.Bin element = user_bins[1];

			Gst.Bin bin;
			Gst.Bin parent_bin;
			
			Hyena.Log.Debug("[Streamrecorder.Gst.Marshaller]<ReallyRemoveTee> removing tee " + ObjectGetName(element));
			
			bin = new Gst.Bin(gst_object_get_parent(element.BinPtr));
			gst_object_ref (bin.BinPtr);

			parent_bin = new Gst.Bin(gst_object_get_parent(bin.BinPtr));
			parent_bin.Remove(bin.BinPtr);

			ElementSetState(bin.BinPtr,Gst.State.Null);
			bin.Remove(element.BinPtr);
			gst_object_unref (bin.BinPtr);

			/* if we're supposed to be playing, unblock the sink */
			if (blocked) {
				Hyena.Log.Debug("[Streamrecorder.Gst.Marshaller]<ReallyRemoveTee>unblocking pad after removing tee");
				PadSetBlockedAsync(pad,false,PlayerAddRemoveTeeDone,IntPtr.Zero);
			}
					
		}

		/*
		 * Location Property Handling
		 */
        public static void GObjectSetLocationProperty (IntPtr gobject, string filename)
        {
			ObjectSetStringProperty (gobject, "location", filename);
        }
        
        /*
         * Tagging
         */
        public static IntPtr TagListNew()
        {
			return gst_tag_list_new();
		}
		
        public static void TagListAddStringValue (IntPtr taglist, Gst.TagMergeMode mode, string tag, string value)
		{
			GLib.Value val = new GLib.Value (GLib.GType.String);
			val.Val = value;
			IntPtr native_tag = GLib.Marshaller.StringToPtrGStrdup (tag);
			gst_tag_list_add_value (taglist, mode, native_tag, ref val);
			GLib.Marshaller.Free (native_tag);
		}

		public static void TagSetterMergeTags(IntPtr tagsetter, IntPtr taglist, Gst.TagMergeMode mode)
		{
			gst_tag_setter_merge_tags(tagsetter, taglist, mode);
		}
        
        /*
         * Initialization
         */
        public static bool Initialize () {
			try 
			{
				 gst_version = VersionString ();
				 DebugSetActive(true);
				 Hyena.Log.Information("[Streamrecorder.Gst.Marshaller] gstreamer version found: " + gst_version);
				 return true;
			}
			catch (Exception e)
			{
				Hyena.Log.Error(e.ToString());
				Hyena.Log.Error(e.Message);
			}
			return false;
		}

		/* Helper Import Wrappers */
		public static bool CheckGstPlugin (string name)
		{
			bool ret = false;
			IntPtr element_factory;
			IntPtr native_name = GLib.Marshaller.StringToPtrGStrdup (name);
			element_factory = gst_element_factory_find (native_name);
			Hyena.Log.Debug("[Streamrecorder.Gst.Marshaller] looking for factory : " + name);
			if (element_factory != IntPtr.Zero) 
			{
				ret = true;
				gst_object_unref(element_factory);
				Hyena.Log.Debug("[Streamrecorder.Gst.Marshaller] found factory for : " + name);
			}
			return ret;
		}
		
		public static string ObjectGetPathString(IntPtr gobject)
		{
			IntPtr raw_ret = gst_object_get_path_string(gobject);
			string ret = GLib.Marshaller.Utf8PtrToString(raw_ret);
			GLib.Marshaller.Free(raw_ret);
			return ret;
		}

		private static string ObjectGetName (IntPtr gobject)
		{
			IntPtr raw_ret = gst_object_get_path_string(gobject);
			string ret = GLib.Marshaller.Utf8PtrToString(raw_ret);
			GLib.Marshaller.Free(raw_ret);
			return ret;
		}
		
		private static string ObjectGetName (Gst.Bin bin)
		{
			return ObjectGetName(bin.BinPtr);
		}

        private static IntPtr ElementFactoryMake(string factoryname)
        {
			IntPtr native_factoryname = GLib.Marshaller.StringToPtrGStrdup (factoryname);
			return gst_element_factory_make(native_factoryname,IntPtr.Zero);
		}

		private static bool ElementLink(IntPtr src, IntPtr dest) {
			bool ret = gst_element_link(src, dest);
			return ret;
		}
		
		private static void ElementLinkMany(IntPtr[] elements)
		{
			for (int i = 0; i < elements.Length - 1; i++)
			{
				ElementLink(elements[i],elements[i+1]);
			}
		}
		
        public static IntPtr GhostPadNew(string name, IntPtr target)
        {
			IntPtr native_name = GLib.Marshaller.StringToPtrGStrdup (name);
			return gst_ghost_pad_new(native_name, target);
		}

		private static Gst.StateChangeReturn ElementSetState(IntPtr element, Gst.State state)
		{
			int raw_ret = gst_element_set_state(element, (int) state);
			Gst.StateChangeReturn ret = (Gst.StateChangeReturn) raw_ret;
			return ret;
		}
        
		public static bool PadSetBlockedAsync(IntPtr pad, bool blocked, PadBlockCallback cb, IntPtr user_data) {
			Hyena.Log.Debug("[Gst.Marshaller]<PadSetBlockedAsync> START");
			PadBlockCallbackWrapper cb_wrapper = new PadBlockCallbackWrapper (cb);
			bool raw_ret = gst_pad_set_blocked_async(pad, blocked, cb_wrapper.NativeDelegate, user_data);
			bool ret = raw_ret;
			Hyena.Log.Debug("[Gst.Marshaller]<PadSetBlockedAsync> END");
			return ret;
		}

		public static GLib.GType TagSetterGetType ()
		{
			return gst_tag_setter_get_type ();
		}		
		
        public static unsafe IntPtr ParseBinFromDescription(string bin_description, bool ghost_unlinked_pads) {
			IntPtr native_bin_description = GLib.Marshaller.StringToPtrGStrdup (bin_description);
			IntPtr error = IntPtr.Zero;
			IntPtr raw_ret = gst_parse_bin_from_description(native_bin_description, ghost_unlinked_pads, out error);
			GLib.Marshaller.Free (native_bin_description);
			if (error != IntPtr.Zero) throw new GLib.GException (error);
			return raw_ret;
        }

        public static unsafe IntPtr ParseLaunch(string bin_description) {
			IntPtr native_bin_description = GLib.Marshaller.StringToPtrGStrdup (bin_description);
			IntPtr error = IntPtr.Zero;
			IntPtr raw_ret = gst_parse_launch(native_bin_description, out error);
			GLib.Marshaller.Free (native_bin_description);
			if (error != IntPtr.Zero) throw new GLib.GException (error);
			return raw_ret;
        }

        public static void ObjectSetStringProperty (IntPtr gobject, string name, string value)
		{
			GLib.Value val = new GLib.Value (GLib.GType.String);
			val.Val = value;
			IntPtr native_name = GLib.Marshaller.StringToPtrGStrdup (name);
			g_object_set_property (gobject, native_name, ref val);
			GLib.Marshaller.Free (native_name);
		}
		
        public static void ObjectSetBooleanProperty (IntPtr gobject, string name, bool value)
		{
			GLib.Value val = new GLib.Value (GLib.GType.Boolean);
			val.Val = value;
			IntPtr native_name = GLib.Marshaller.StringToPtrGStrdup (name);
			g_object_set_property (gobject, native_name, ref val);
			GLib.Marshaller.Free (native_name);
		}
		
        public static void ObjectSetIntegerProperty (IntPtr gobject, string name, int value)
		{
			GLib.Value val = new GLib.Value (GLib.GType.Int);
			val.Val = value;
			IntPtr native_name = GLib.Marshaller.StringToPtrGStrdup (name);
			g_object_set_property (gobject, native_name, ref val);
			GLib.Marshaller.Free (native_name);
		}
        
        public static void DebugSetActive (bool active) {
			gst_debug_set_active (active);
        }

        public static string VersionString () {
			return gst_version_string ();
        }

		/* Helper Imports*/
        [DllImport ("libgstreamer-0.10.so.0")]
        private static extern string gst_version_string ();
        
        [DllImport ("libgstreamer-0.10.so.0")]
        private static extern void gst_debug_set_active (bool active);

		[DllImport ("libgobject-2.0.so.0")]
        private static extern void g_object_set_property (IntPtr gobject, IntPtr property_name, ref GLib.Value value);
		
		[DllImport ("libgobject-2.0.so.0")]
        public static extern void g_signal_connect_data (IntPtr instance, IntPtr detailed_signal, BusFunc cb, IntPtr data, IntPtr zero, uint flags);
		
        [DllImport ("libgstreamer-0.10.so.0")]
		public static extern IntPtr gst_bus_pop (IntPtr bus);

        [DllImport ("libgstreamer-0.10.so.0")]
		private static extern GLib.GType gst_tag_setter_get_type ();

        [DllImport ("libgstreamer-0.10.so.0")]
        private static extern unsafe IntPtr gst_parse_bin_from_description (IntPtr bin_description, bool ghost_unlinked_pads, out IntPtr gerror);
        
        [DllImport ("libgstreamer-0.10.so.0")]
        private static extern unsafe IntPtr gst_parse_launch (IntPtr bin_description, out IntPtr gerror);
        
		[DllImport("libgstreamer-0.10.so.0", CallingConvention = CallingConvention.Cdecl)]
		static extern bool gst_pad_set_blocked_async(IntPtr pad, bool blocked, PadBlockCallbackNative cb, IntPtr user_data);

        [DllImport ("libgstreamer-0.10.so.0")]
        private static extern unsafe IntPtr gst_object_get_parent (IntPtr element);
        
        [DllImport ("libgstreamer-0.10.so.0")]
		static extern int gst_element_set_state(IntPtr element, int state);
		
        [DllImport ("libgstreamer-0.10.so.0")]
        private static extern unsafe IntPtr gst_ghost_pad_new (IntPtr name, IntPtr target);
        
		[DllImport("libgstreamer-0.10.so.0", CallingConvention = CallingConvention.Cdecl)]
		static extern bool gst_element_link(IntPtr src, IntPtr dest);

        [DllImport ("libgstreamer-0.10.so.0")]
        private static extern unsafe IntPtr gst_element_factory_make (IntPtr factoryname, IntPtr name);
        
        [DllImport ("libgstreamer-0.10.so.0")]
		private static extern IntPtr gst_event_new_new_segment (bool update, double rate, Gst.Format format, long start, long stop,long position);

        [DllImport ("libgstreamer-0.10.so.0")]
		private static extern bool gst_pad_send_event (IntPtr pad, IntPtr gevent);

        [DllImport ("libgstreamer-0.10.so.0")]
        private static extern unsafe IntPtr gst_pad_get_peer (IntPtr element);
		
        [DllImport ("libgstreamer-0.10.so.0")]
        private static extern unsafe void gst_object_unref (IntPtr element);
        
        [DllImport ("libgstreamer-0.10.so.0")]
        private static extern unsafe IntPtr gst_object_ref (IntPtr element);
        
        [DllImport ("libgstreamer-0.10.so.0")]
        private static extern unsafe IntPtr gst_object_get_path_string (IntPtr gobject);

        [DllImport ("libgstreamer-0.10.so.0")]
        private static extern unsafe IntPtr gst_tag_list_new ();
        
        [DllImport ("libgstreamer-0.10.so.0")]
        private static extern unsafe void gst_tag_list_add_value (IntPtr taglist, Gst.TagMergeMode mode, IntPtr tag, ref GLib.Value value);
        
        [DllImport ("libgstreamer-0.10.so.0")]
        private static extern unsafe void gst_tag_setter_merge_tags (IntPtr tagsetter, IntPtr taglist, Gst.TagMergeMode mode);

        [DllImport ("libgstreamer-0.10.so.0")]
        private static extern unsafe IntPtr gst_element_factory_find (IntPtr name);

        [DllImport ("libgstreamer-0.10.so.0")]
        public static extern unsafe IntPtr gst_element_get_bus (IntPtr element);

        [DllImport ("libgstreamer-0.10.so.0")]
        public static extern unsafe void gst_bus_add_signal_watch (IntPtr bus);

        [DllImport ("libgstreamer-0.10.so.0")]
        public static extern unsafe GLib.Value gst_structure_get_value (IntPtr structure, IntPtr name);

        [DllImport ("libgstreamer-0.10.so.0")]
        public static extern unsafe IntPtr gst_message_get_structure (IntPtr message);

	}

	/* Helper Classes*/

	public delegate bool BusFunc(IntPtr bus, IntPtr message, IntPtr user_data);

	[UnmanagedFunctionPointer (CallingConvention.Cdecl)]
	internal delegate bool BusFuncNative(IntPtr bus, IntPtr message, IntPtr user_data);

	internal class BusFuncInvoker {

		BusFuncNative native_cb;

		~BusFuncInvoker () {}

		internal BusFuncInvoker (BusFuncNative native_cb) {}

		internal Gst.BusFunc Handler {
			get {
				return new Gst.BusFunc(InvokeNative);
			}
		}

		bool InvokeNative (IntPtr bus, IntPtr message, IntPtr user_data)
		{
			bool result = native_cb (bus, message , user_data);
			return result;
		}
	}

	internal class BusFuncWrapper {

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
			BusFuncWrapper wrapper = (BusFuncWrapper) native.Target;
			if (wrapper == null)
				return null;
			return wrapper.managed;
		}
	}

	public delegate void PadBlockCallback(IntPtr pad, bool blocked, IntPtr user_data);
    
    [UnmanagedFunctionPointer (CallingConvention.Cdecl)]
	internal delegate void PadBlockCallbackNative(IntPtr pad, bool blocked, IntPtr user_data);

	internal class PadBlockCallbackInvoker {

		PadBlockCallbackNative native_cb;

		~PadBlockCallbackInvoker () {}

		internal PadBlockCallbackInvoker (PadBlockCallbackNative native_cb) {}

		internal PadBlockCallback Handler {
			get {
				return new PadBlockCallback(InvokeNative);
			}
		}

		void InvokeNative (IntPtr pad, bool blocked, IntPtr user_data)
		{
			native_cb (pad, blocked, user_data);
		}
	}

	internal class PadBlockCallbackWrapper {

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
			PadBlockCallbackWrapper wrapper = (PadBlockCallbackWrapper) native.Target;
			if (wrapper == null)
				return null;
			return wrapper.managed;
		}

	}

	public enum State {

		VoidPending,
		Null = 1,
		Ready = 2,
		Paused = 3,
		Playing = 4,
	}

	public enum StateChangeReturn {

		Failure,
		Success = 1,
		Async = 2,
		NoPreroll = 3,
	}

	public enum Format {

		Undefined,
		Default = 1,
		Bytes = 2,
		Time = 3,
		Buffers = 4,
		Percent = 5,
	}

	public enum TagMergeMode {

		Undefined,
		ReplaceAll,
		Replace,
		Append,
		Prepend,
		Keep,
		KeepAll,
		Count,
	}
	
	public enum GstPadDirection {
		Unknown,
		Source,
		Sink
	} 

}
