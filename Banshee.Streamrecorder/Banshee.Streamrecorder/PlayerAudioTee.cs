//
// PlayerAudioTee.cs
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
using Banshee.Streamrecorder.Gst;

using Hyena;

namespace Banshee.Streamrecorder
{
	
	public class PlayerAudioTee : Bin
	{
		
		public PlayerAudioTee(IntPtr audiotee) : base (audiotee) {}

			public bool AddBin(Bin bin, bool use_pad_block)
        {
			Bin[] user_bins = new Bin[2] { new Bin(this.ToIntPtr()) , bin };
			GCHandle gch = GCHandle.Alloc(user_bins);
			IntPtr user_data = GCHandle.ToIntPtr(gch);
			
			Pad fixture_pad = this.GetStaticPad("sink");
			Pad block_pad = fixture_pad.GetPeer ();
			fixture_pad.UnRef();

			if (use_pad_block)
			{
				Hyena.Log.Debug("[Streamrecorder.Gst.Marshaller]<PlayerAddTee> blockin pad " + block_pad.GetPathString() + " to perform an operation");

				block_pad.SetBlockedAsync(true, ReallyAddBin, user_data);
			} else {
				Hyena.Log.Debug("Streamrecorder.Gst.Marshaller]<PlayerAddTee> not using blockin pad, calling operation directly");
				ReallyAddBin(block_pad.ToIntPtr (), false, user_data);
			}
			block_pad.UnRef();
			return true;
		}
		
		private static void ReallyAddBin(IntPtr pad, bool blocked, IntPtr user_data)
		{
			Hyena.Log.Debug("[Streamrecorder.Gst.Marshaller]<ReallyAddTee> START" + (blocked ? " (blocked)" : " (unblocked)"));
			
			GCHandle gch = GCHandle.FromIntPtr(user_data);
			Bin[] user_bins = (Gst.Bin[])gch.Target;
			Bin fixture = user_bins[0];
			Bin element = user_bins[1];
			
			Hyena.Log.Debug("[Streamrecorder.Gst.Marshaller]<ReallyAddTee> path for fixture: " + fixture.GetPathString());
			Hyena.Log.Debug("[Streamrecorder.Gst.Marshaller]<ReallyAddTee> path for element: " + element.GetPathString());

			Element queue;
			Element audioconvert;
			Bin bin;
			Bin parent_bin;
			Pad sink_pad;
			Pad ghost_pad;
			GstObject element_parent;
			
			element_parent = element.GetParent();
			if (element_parent != null && !element_parent.IsNull())
			{
				Hyena.Log.Debug("[Streamrecorder.Gst.Marshaller]<ReallyAddTee>element already linked, exiting. assume double function call");
				element_parent.UnRef();
				return;
			}
			
			Hyena.Log.Debug("[Streamrecorder.Gst.Marshaller]<ReallyAddTee>adding tee " + element.GetPathString());
			
			/* set up containing bin */
			bin = new Bin ();
			queue = ElementFactory.Make("queue");
			audioconvert = ElementFactory.Make("audioconvert");
			
			bin.SetBooleanProperty("async-handling", true);
			queue.SetIntegerProperty("max-size-buffers", 10);

			bin.AddMany( new Element[3] { queue, audioconvert, element } );
			queue.LinkMany( new Element[2] { audioconvert, element } );
			
			sink_pad = queue.GetStaticPad("sink");
			ghost_pad = new GhostPad ("sink", sink_pad);
			bin.AddPad(ghost_pad);

			parent_bin = new Bin(fixture.GetParent().ToIntPtr());
			parent_bin.Add(bin);
			fixture.Link(bin);
			
			if (blocked)
			{
				Hyena.Log.Debug("[Streamrecorder.Gst.Marshaller]<ReallyAddTee> unblocking pad after adding tee");
				
				parent_bin.SetState(State.Playing);
				ghost_pad.Ref();
				parent_bin.UnRef();
				new Pad(pad).SetBlockedAsync(false,AddRemoveBinDone,ghost_pad.ToIntPtr ());				
			} else {
				parent_bin.SetState(State.Paused);
				ghost_pad.Ref();
				parent_bin.UnRef();
				AddRemoveBinDone(IntPtr.Zero,false,ghost_pad.ToIntPtr ());
			}

			Hyena.Log.Debug("[Streamrecorder.Gst.Marshaller]<ReallyAddTee> END");

		}
		
		/*
		 * Pipeline RemoveTee
		 */
        public bool RemoveBin(Bin bin, bool use_pad_block)
        {
			IntPtr user_data = bin.ToIntPtr ();
			
			Pad fixture_pad = this.GetStaticPad("sink");
			Pad block_pad = fixture_pad.GetPeer();
			fixture_pad.UnRef();

			if (use_pad_block)
			{
				Hyena.Log.Debug("[Streamrecorder.Gst.Marshaller]<PlayerRemoveTee> blockin pad " + block_pad.GetPathString() + " to perform an operation");

				block_pad.SetBlockedAsync(true, ReallyRemoveBin, user_data);
			} else {
				Hyena.Log.Debug("[Streamrecorder.Gst.Marshaller]<PlayerRemoveTee> not using blockin pad, calling operation directly");
				ReallyRemoveBin(block_pad.ToIntPtr (), false, user_data);
			}
			block_pad.UnRef();
			return true;
		}

		private static void ReallyRemoveBin(IntPtr pad, bool blocked, IntPtr user_data)
		{
			Bin element = new Bin (user_data);

			Bin bin;
			Bin parent_bin;
			
			Hyena.Log.Debug("[Streamrecorder.Gst.Marshaller]<ReallyRemoveTee> removing tee " + element.GetPathString());
			
			bin = new Bin(element.GetParent().ToIntPtr());;
			bin.Ref();

			parent_bin = new Bin(bin.GetParent().ToIntPtr());
			parent_bin.Remove(bin);

			bin.SetState(State.Null);
			bin.Remove(element);
			bin.UnRef();

			/* if we're supposed to be playing, unblock the sink */
			if (blocked) {
				Hyena.Log.Debug("[Streamrecorder.Gst.Marshaller]<ReallyRemoveTee>unblocking pad after removing tee");
				new Pad(pad).SetBlockedAsync(false,AddRemoveBinDone,IntPtr.Zero);
			}
					
		}

		public static void AddRemoveBinDone(IntPtr pad, bool blocked, IntPtr new_pad)
		{
			IntPtr segment;
			if (new_pad == IntPtr.Zero)
			{
				return;
			}

			// send a very unimaginative new segment through the new pad
			segment = Marshaller.CreateSegment ();
			new Pad(new_pad).SendEvent(segment);
		}

	}
}
