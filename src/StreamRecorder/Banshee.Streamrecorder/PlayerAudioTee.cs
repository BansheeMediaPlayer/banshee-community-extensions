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
extern alias oldGlib;
using OldGLib = oldGlib.GLib;

using System;
using Banshee.Streamrecorder.Gst;
using System.Runtime.InteropServices;

namespace Banshee.Streamrecorder
{

    /// <summary>
    /// An object to capsule the gstreamer audio tee of the player and provide functionality to attach and remove a pipeline
    /// </summary>
    public class PlayerAudioTee : Bin
    {

        private bool attached;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="audiotee">
        /// A <see cref="IntPtr"/> referencing an audio player tee element
        /// </param>
        public PlayerAudioTee (IntPtr audiotee) : base(audiotee)
        {
            attached = false;
        }

        /// <summary>
        /// Attaches a pipeline/bin to the audio player tee, optionally using pad blocking
        /// </summary>
        /// <param name="bin">
        /// A <see cref="Bin"/> to attach to the player tee
        /// </param>
        /// <param name="use_pad_block">
        /// A <see cref="System.Boolean"/> indicating whether to use pad blocking or not
        /// </param>
        /// <returns>
        /// A <see cref="System.Boolean"/> -- always true
        /// </returns>
        public bool AddBin (Bin bin, bool use_pad_block)
        {
            Bin[] user_bins = new Bin[2] { new Bin (this.ToIntPtr ()), bin };
            GCHandle gch = GCHandle.Alloc (user_bins);
            IntPtr user_data = GCHandle.ToIntPtr (gch);

            Pad fixture_pad = this.GetStaticPad ("sink");
            Pad block_pad = fixture_pad.GetPeer ();
            fixture_pad.UnRef ();

            if (use_pad_block) {
                ulong blocking_probe = block_pad.AddProbe (PadProbeType.GST_PAD_PROBE_TYPE_BLOCK, ReallyAddBinBlocked, user_data, null);
                Hyena.Log.DebugFormat ("[Streamrecorder.PlayerAudioTee]<AddBin> blockin pad {0} to perform an operation ({1})",block_pad.GetPathString (),blocking_probe);
            } else {
                Hyena.Log.Debug ("Streamrecorder.PlayerAudioTee]<AddBin> not using blockin pad, calling operation directly");
                ReallyAddBinUnblocked (block_pad.ToIntPtr (), IntPtr.Zero, user_data);
            }
            block_pad.UnRef ();

            return true;
        }

        private PadProbeReturn ReallyAddBinBlocked (IntPtr pad, IntPtr probe_info, IntPtr user_data)
        {
            return ReallyAddBin (pad, probe_info, user_data, true);
        }

        private PadProbeReturn ReallyAddBinUnblocked (IntPtr pad, IntPtr probe_info, IntPtr user_data)
        {
            return ReallyAddBin (pad, probe_info, user_data, false);
        }

        /// <summary>
        /// Helper function to really attach a bin to the audio player tee
        /// </summary>
        /// <param name="pad">
        /// A <see cref="IntPtr"/> referencing the pad that may need to be unblocked
        /// </param>
        /// <param name="blocked">
        /// A <see cref="System.Boolean"/> indicating if the pad is blocked
        /// </param>
        /// <param name="user_data">
        /// A <see cref="IntPtr"/> containing references to the bin and the audio tee
        /// </param>
        private PadProbeReturn ReallyAddBin (IntPtr pad, IntPtr probe_info, IntPtr user_data, bool blocked)
        {
            GCHandle gch = GCHandle.FromIntPtr (user_data);
            Bin[] user_bins = (Gst.Bin[])gch.Target;
            Bin fixture = user_bins[0];
            Bin element = user_bins[1];

            Element queue;
            Element audioconvert;
            Bin bin;
            Bin parent_bin;
            Pad sink_pad;
            Pad ghost_pad;
            GstObject element_parent;

            element_parent = element.GetParent ();
            if (element_parent != null && !element_parent.IsNull ()) {
                Hyena.Log.Debug ("[Streamrecorder.PlayerAudioTee]<ReallyAddBin>element already linked, exiting. assume double function call");
                element_parent.UnRef ();
                return PadProbeReturn.GST_PAD_PROBE_OK;
            }

            /* set up containing bin */
            bin = new Bin ();
            queue = ElementFactory.Make ("queue");
            audioconvert = ElementFactory.Make ("audioconvert");

            bin.SetBooleanProperty ("async-handling", true);
            queue.SetIntegerProperty ("max-size-buffers", 10);

            bin.AddMany (new Element[3] { queue, audioconvert, element });
            queue.LinkMany (new Element[2] { audioconvert, element });

            sink_pad = queue.GetStaticPad ("sink");
            ghost_pad = new GhostPad ("sink", sink_pad);
            bin.AddPad (ghost_pad);

            parent_bin = new Bin (fixture.GetParent ().ToIntPtr ());
            parent_bin.Add (bin);
            fixture.Link (bin);

            attached = true;

            if (blocked) {
                Hyena.Log.Debug ("[Streamrecorder.PlayerAudioTee]<ReallyAddBin> unblocking pad after adding tee");

                parent_bin.SetState (State.Playing);
                ghost_pad.Ref ();
                parent_bin.UnRef ();
//                new Pad (pad).AddProbe (PadProbeType.GST_PAD_PROBE_TYPE_BLOCK, AddRemoveBinDone, ghost_pad.ToIntPtr (), null);
                PadProbeInfo info = (PadProbeInfo) Marshal.PtrToStructure (probe_info, typeof (PadProbeInfo));
                new Pad (pad).RemoveProbe (info.id);
                AddRemoveBinDone (IntPtr.Zero, IntPtr.Zero, ghost_pad.ToIntPtr ());
            } else {
                parent_bin.SetState (State.Paused);
                ghost_pad.Ref ();
                parent_bin.UnRef ();
                AddRemoveBinDone (IntPtr.Zero, IntPtr.Zero, ghost_pad.ToIntPtr ());
            }
            return PadProbeReturn.GST_PAD_PROBE_OK;

        }

        /// <summary>
        /// Removes a pipeline/bin from the audio player tee, optionally using pad blocking
        /// </summary>
        /// <param name="bin">
        /// The <see cref="Bin"/> to remove
        /// </param>
        /// <param name="use_pad_block">
        /// A <see cref="System.Boolean"/> indicating whether to use pad blocking or not
        /// </param>
        /// <returns>
        /// A <see cref="System.Boolean"/> -- always true
        /// </returns>
        public bool RemoveBin (Bin bin, bool use_pad_block)
        {
            IntPtr user_data = bin.ToIntPtr ();

            Pad fixture_pad = this.GetStaticPad ("sink");
            Pad block_pad = fixture_pad.GetPeer ();
            fixture_pad.UnRef ();

            if (use_pad_block) {
                ulong blocking_probe = block_pad.AddProbe (PadProbeType.GST_PAD_PROBE_TYPE_BLOCK, ReallyRemoveBinBlocked, user_data, null);
                Hyena.Log.DebugFormat ("[Streamrecorder.PlayerAudioTee]<RemoveBin> blockin pad {0} to perform an operation ({1})", block_pad.GetPathString (), blocking_probe);
            } else {
                Hyena.Log.Debug ("[Streamrecorder.PlayerAudioTee]<RemoveBin> not using blockin pad, calling operation directly");
                ReallyRemoveBinUnblocked (block_pad.ToIntPtr (), IntPtr.Zero, user_data);
            }
            block_pad.UnRef ();
            return true;
        }

        private PadProbeReturn ReallyRemoveBinBlocked (IntPtr pad, IntPtr probe_info, IntPtr user_data)
        {
            return ReallyRemoveBin (pad, probe_info, user_data, true);
        }

        private PadProbeReturn ReallyRemoveBinUnblocked (IntPtr pad, IntPtr probe_info, IntPtr user_data)
        {
            return ReallyRemoveBin (pad, probe_info, user_data, false);
        }

        /// <summary>
        /// Helper function to really remove the bin from the audio player tee
        /// </summary>
        /// <param name="pad">
        /// A <see cref="IntPtr"/> referencing the pad that may need to be unblocked
        /// </param>
        /// <param name="blocked">
        /// A <see cref="System.Boolean"/> indicating if the pad is blocked
        /// </param>
        /// <param name="user_data">
        /// A <see cref="IntPtr"/> containing a reference to the bin
        /// </param>
        private PadProbeReturn ReallyRemoveBin (IntPtr pad, IntPtr probe_info, IntPtr user_data, bool blocked)
        {
            Bin element = new Bin (user_data);

            Bin bin;
            Bin parent_bin;

            string element_path = element.GetPathString ();
            if (!element_path.Contains (":") && element_path.StartsWith ("/0x")) {
                Hyena.Log.Debug ("[Streamrecorder.PlayerAudioTee]<ReallyRemoveBin> element empty, assume disposed, exiting: " + element_path);
                return PadProbeReturn.GST_PAD_PROBE_OK;
            }

            bin = new Bin (element.GetParent ().ToIntPtr ());
            bin.Ref ();

            parent_bin = new Bin (bin.GetParent ().ToIntPtr ());
            parent_bin.Remove (bin);

            bin.SetState (State.Null);
            bin.Remove (element);
            bin.UnRef ();

            attached = false;

            /* if we're supposed to be playing, unblock the sink */
            if (blocked) {
                Hyena.Log.Debug ("[Streamrecorder.PlayerAudioTee]<ReallyRemoveBin> unblocking pad after removing tee");
//                new Pad (pad).AddProbe (PadProbeType.GST_PAD_PROBE_TYPE_BLOCK, AddRemoveBinDone, IntPtr.Zero, null);
                PadProbeInfo info = (PadProbeInfo) Marshal.PtrToStructure (probe_info, typeof (PadProbeInfo));
                new Pad (pad).RemoveProbe (info.id);
                AddRemoveBinDone (IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
            }

            return PadProbeReturn.GST_PAD_PROBE_OK;
        }

        /// <summary>
        /// Helper function to revive the player pipeline after add/remove operations
        /// </summary>
        /// <param name="pad">
        /// A <see cref="IntPtr"/> referencing the pad that might have been blocked
        /// </param>
        /// <param name="blocked">
        /// A <see cref="System.Boolean"/> indicating whether the pad was blocked
        /// </param>
        /// <param name="new_pad">
        /// A <see cref="IntPtr"/> referencing the new (ghost) pad that waits for an event or Zero for removed bins
        /// </param>
        public PadProbeReturn AddRemoveBinDone (IntPtr pad, IntPtr probe_info, IntPtr new_pad)
        {
            IntPtr segment_event;
            if (new_pad == IntPtr.Zero) {
                return PadProbeReturn.GST_PAD_PROBE_OK;
            }

            // send a very unimaginative new segment through the new pad
            segment_event = Marshaller.CreateSegmentEvent ();
            new Pad (new_pad).SendEvent (segment_event);
            return PadProbeReturn.GST_PAD_PROBE_OK;

        }

        /// <summary>
        /// Indicates if a (recording) pipeline is attached to this player tee
        /// </summary>
        /// <returns>
        /// A <see cref="System.Boolean"/> indicating whether a (recording) pipeline is attached to this player tee
        /// </returns>
        public bool IsAttached ()
        {
            return attached;
        }
    }
}
