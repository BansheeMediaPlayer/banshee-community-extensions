//
// StreamrecorderProcessControl.cs
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
using System.Collections.Generic;

using System.Text.RegularExpressions;

using Mono.Addins;

using Banshee.Streaming;
using Banshee.ServiceStack;
using Banshee.Collection;
using Banshee.Collection.Database;

using Hyena;

namespace Banshee.Streamrecorder
{
    public class StreamrecorderProcessControl
    {
        private string output_directory;
        private string output_file;
        
        private Gst.Bin audiotee;
        private Gst.Bin encoder_bin;
        private IntPtr tagger;
        private IntPtr file_sink;
        private IntPtr ghost_pad;
        
        private Gst.Bin silence_pipeline;
        private IntPtr silence_ghost_pad;
        private IntPtr bus;
        
        private List<Encoder> encoders = new List<Encoder> ();
        private bool has_lame;
        private const string lame_name = "LAME MP3 Audio Encoder";
        private const string lame_pipeline = "! lame name=audio_encoder ! id3v2mux name=tagger ";
        private const string lame_extension = ".mp3";
        private bool has_vorbis;
        private const string vorbis_name = "Ogg/Vorbis Audio Encoder";
        private const string vorbis_pipeline = "! vorbisenc name=audio_encoder ! vorbistag name=tagger ! oggmux ";
        private const string vorbis_extension = ".ogg";
        private bool has_flac;
        private const string flac_name = "FLAC Audio Encoder";
        private const string flac_pipeline = "! flacenc name=audio_encoder ! flactag name=tagger ";
        private const  string flac_extension = ".flac";
        private bool has_level;
        
        public StreamrecorderProcessControl () 
        {
			if (Gst.Marshaller.Initialize()) 
			{
				has_lame = Gst.Marshaller.CheckGstPlugin("lame") && Gst.Marshaller.CheckGstPlugin("id3v2mux");
				Hyena.Log.Information("[Streamrecorder] GstPlugin lame" + (has_lame ? "" : " not") + " found");
				encoders.Add(new Encoder(lame_name, lame_pipeline, lame_extension));
				
				has_vorbis = Gst.Marshaller.CheckGstPlugin("vorbisenc") && Gst.Marshaller.CheckGstPlugin("oggmux") && Gst.Marshaller.CheckGstPlugin("oggmux");
				Hyena.Log.Information("[Streamrecorder] GstPlugin vorbis" + (has_vorbis ? "" : " not") + " found");
				encoders.Add(new Encoder(vorbis_name, vorbis_pipeline, vorbis_extension, true));

				has_flac = Gst.Marshaller.CheckGstPlugin("flacenc") && Gst.Marshaller.CheckGstPlugin("flactag");
				Hyena.Log.Information("[Streamrecorder] GstPlugin flac" + (has_flac ? "" : " not") + " found");
				encoders.Add(new Encoder(flac_name, flac_pipeline, flac_extension));

				has_level = Gst.Marshaller.CheckGstPlugin("level");
				Hyena.Log.Information("[Streamrecorder] GstPlugin level" + (has_level ? "" : " not") + " found");
				Hyena.Log.Debug("gstreamer initialized");
			}
			else
			{
				Hyena.Log.Debug("an error occurred during gstreamer initialization, aborting.");
			}
        }

		public bool CreateRecorder()
		{
			Hyena.Log.Debug("<Streamrecorder:InitControl> START");
			audiotee = new Gst.Bin ( ServiceManager.PlayerEngine.ActiveEngine.GetBaseElements()[2] );

			string pipeline = BuildPipeline();

			if (pipeline.Equals(""))
			{
				return false;
			}
			
			encoder_bin = new Gst.Bin(Gst.Marshaller.ParseBinFromDescription (pipeline, true)) ;

			Hyena.Log.Debug("<Streamrecorder:InitControl> encoder_bin created: " + Gst.Marshaller.ObjectGetPathString(encoder_bin.BinPtr));
			
			tagger = encoder_bin.GetByInterface(Gst.Marshaller.TagSetterGetType());
			file_sink = encoder_bin.GetByName("file_sink");
			
			Gst.Marshaller.GObjectSetLocationProperty(file_sink,output_file);
			Gst.Marshaller.ObjectSetBooleanProperty(file_sink, "sync", true);
			Gst.Marshaller.ObjectSetBooleanProperty(file_sink, "async", false);
			
			GLib.Object.GetObject(file_sink).AddNotification ("allow-overwrite", OnAllowOverwrite); 

			ghost_pad = encoder_bin.GetStaticPad("sink");

			Hyena.Log.Debug("<Streamrecorder:InitControl> END");
			
			return true;
		}

		private string BuildPipeline ()
		{
			string pipeline = "";
			string pipeline_start = "audioresample ! audioconvert ";
			string pipeline_end = "! gnomevfssink name=file_sink";
			Encoder encoder = GetFirstAvailableEncoder ();
			
			if (encoder != null)
			{
				pipeline = pipeline_start + encoder.Pipeline + pipeline_end;
			}
			
			return pipeline;
		}
		
		public List<Encoder> Encoders
		{
			get { return encoders; }
		}
		
		private Encoder GetFirstPreferredEncoder()
		{
			foreach (Encoder encoder in encoders)
			{
				if (encoder.IsPreferred) return encoder;
			}
			return null;
		}
		
		private Encoder GetFirstAvailableEncoder()
		{
			Encoder encoder = GetFirstPreferredEncoder ();
			if (encoder == null && encoders.Count >= 1 )
			{
				encoder = encoders[0];
			}
			return encoder;
		}

		/*
		 * Silence Detection
		 */
		
		private void AddSilenceDetector()
		{
			Hyena.Log.Information("[StreamrecorderProcessControl]<AddSilenceDetector> START");
			if (has_level)
			{
				silence_pipeline = new Gst.Bin(Gst.Marshaller.ParseLaunch("audioconvert name=src0 ! level message=true interval=1000000 ! filesink location=/home/dingsi/test.out name=fake_sink"));
				//silence_pipeline = new Gst.Bin(Gst.Marshaller.ParseLaunch("audioconvert ! cutter ! fakesink name=fake_sink"));
				
				silence_ghost_pad = Gst.Marshaller.GhostPadNew("src", new Gst.Bin(silence_pipeline.GetByName("src0")).GetStaticPad( "src"));
				
				IntPtr fake_sink = silence_pipeline.GetByName("fake_sink");
				Gst.Marshaller.ObjectSetBooleanProperty(fake_sink, "sync", false);
  
				bus = Gst.Marshaller.gst_element_get_bus (silence_pipeline.BinPtr);

				Hyena.Log.Information("[StreamrecorderProcessControl]<AddSilenceDetector> connecting bus " + Gst.Marshaller.ObjectGetPathString(bus));

				if (bus == IntPtr.Zero) return;
				Gst.Marshaller.gst_bus_add_signal_watch(bus);
				
				IntPtr native_message = GLib.Marshaller.StringToPtrGStrdup ("message::level");
				Gst.Marshaller.g_signal_connect_data(bus, native_message, BusEventCallback, IntPtr.Zero, IntPtr.Zero, 1);
				Gst.Marshaller.PlayerAddTee(audiotee,silence_pipeline,true);
			}

			Hyena.Log.Information("[StreamrecorderProcessControl]<AddSilenceDetector> END");

		}

		private bool BusEventCallback (IntPtr bus, IntPtr message, IntPtr user_data)
		{
			Hyena.Log.Information("[StreamrecorderProcessControl]<BusEventCallback> START");
			IntPtr structure = Gst.Marshaller.gst_message_get_structure(message);
			IntPtr native_name = GLib.Marshaller.StringToPtrGStrdup ("peak");
			GLib.Value val = Gst.Marshaller.gst_structure_get_value(structure, native_name);
			GLib.ValueArray peaks = val.Val as GLib.ValueArray;
			int peak = (int)peaks[0];
			if (peak < -50) Hyena.Log.Information("[StreamrecorderProcessControl]<BusEventCallback> Silence detected");

			Hyena.Log.Information("[StreamrecorderProcessControl]<BusEventCallback> END");

			return true;
		}				

		private void OnAllowOverwrite(object o, GLib.NotifyArgs args)
		{
			Hyena.Log.Debug ("[StreamrecorderProcessControl] <OnAllowOverwrite> Called");
		}

        public void StartRecording () 
        {
            Hyena.Log.Debug ("[StreamrecorderProcessControl] <StartRecording> START");

			if (CreateRecorder())
			{
				Gst.Marshaller.PlayerAddTee(audiotee,encoder_bin,true);
				//AddSilenceDetector();
			}
			
            Hyena.Log.Debug ("[StreamrecorderProcessControl] <StartRecording> END");
        }

        public void StopRecording () 
        {
            Hyena.Log.Debug ("[StreamrecorderProcessControl] <StopRecording> STOPPED");

			if (ghost_pad != IntPtr.Zero)
			{
				Gst.Marshaller.PlayerRemoveTee(audiotee,encoder_bin,true);
			}
			
			//Silence Detector Message Test
			//IntPtr msg = Gst.Marshaller.gst_bus_pop(bus);
			//if (msg == IntPtr.Zero) Hyena.Log.Information ("[StreamrecorderProcessControl] <StopRecording> No Messages");

        }
        
        public bool AddStreamTags(TrackInfo track)
        {
			Hyena.Log.Information("[StreamrecorderProcessControl]<AddStreamTags> START");
			if (track == null) return false;
			
			Hyena.Log.Debug("[StreamrecorderProcessControl]<AddStreamTags>caught metadata ArtistName:" + track.ArtistName);
			Hyena.Log.Debug("[StreamrecorderProcessControl]<AddStreamTags>caught metadata Genre:" + track.Genre);
			Hyena.Log.Debug("[StreamrecorderProcessControl]<AddStreamTags>caught metadata TrackTitle:" + track.TrackTitle);
			Hyena.Log.Debug("[StreamrecorderProcessControl]<AddStreamTags>caught metadata AlbumArtist:" + track.AlbumArtist);
			RadioTrackInfo radio_track = track as RadioTrackInfo;
			Hyena.Log.Debug("[StreamrecorderProcessControl]<AddStreamTags>caught metadata Station:" + radio_track.ParentTrack.TrackTitle);

			if (tagger == IntPtr.Zero)
			{
				Hyena.Log.Information("[StreamrecorderProcessControl]<AddStreamTags>tagger is null, not tagging!");
				return false;
			}

			
			IntPtr taglist = Gst.Marshaller.TagListNew();
			Gst.Marshaller.TagListAddStringValue(taglist,Gst.TagMergeMode.ReplaceAll,"title",track.TrackTitle);
			Gst.Marshaller.TagListAddStringValue(taglist,Gst.TagMergeMode.ReplaceAll,"genre",track.Genre);
			Gst.Marshaller.TagListAddStringValue(taglist,Gst.TagMergeMode.ReplaceAll,"artist",track.ArtistName);
			Gst.Marshaller.TagListAddStringValue(taglist,Gst.TagMergeMode.ReplaceAll,"album-artist",track.AlbumArtist);
			
			Gst.Marshaller.TagSetterMergeTags(tagger, taglist, Gst.TagMergeMode.ReplaceAll);

			Hyena.Log.Information("[StreamrecorderProcessControl]<AddStreamTags> END");

			return true;
		}

        public void SetOutputParameters (string directory, string filename) 
        {
            //SetOutputFileFormatPattern(pattern);
            SetOutputDirectory(directory);
            SetOutputFile(filename);
        }
        
        //private void SetOutputFileFormatPattern (string pattern) 
        //{
        //    output_file_format_pattern = pattern;
        //}

        private void SetOutputDirectory (string directory) 
        {
            output_directory = directory;
        }

        private void SetOutputFile (string fullfilename) 
        {
			string cleanfilename = fullfilename;
			char[] invalid_chars = Path.GetInvalidFileNameChars ();
			foreach (char invalid_char in invalid_chars)
			{
				cleanfilename = cleanfilename.Replace(invalid_char.ToString (), "_");
			}
			output_file =  output_directory + Path.DirectorySeparatorChar + cleanfilename;
		}

/*

	def on_extra_metadata_notify (self, db, entry, field, metadata):
		self.streaming_title = metadata

		# set a new filename on the sink so it splits streaming audio
		# automatically into tracks
		teepad = self.ghostpad.get_peer ()
		teepad.set_blocked (True)
		self.encoder_bin.send_event (gst.event_new_eos())
		self.encoder_bin.set_state (gst.STATE_NULL)
		self.set_recording_uri ()
		self.encoder_bin.set_state (gst.STATE_READY)
		if self.recording:
			self.encoder_bin.set_state (gst.STATE_PLAYING)
		teepad.set_blocked (False)


 */

    }

}
