//
// Recorder.cs
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
using Banshee.Streamrecorder.Gst;

using Hyena;

namespace Banshee.Streamrecorder
{
    public class Recorder
    {
        private string output_directory;
        private string output_file;
        private string file_extension;
        
        private PlayerAudioTee audiotee;
        private Bin encoder_bin;
        private TagSetter tagger;
        private FileSink file_sink;
        private GhostPad ghost_pad;
        
        private Pipeline silence_pipeline;
        private GhostPad silence_ghost_pad;
        private Bus bus;
        
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
        
        public Recorder () 
        {
			if (Marshaller.Initialize()) 
			{
				has_lame = Marshaller.CheckGstPlugin("lame") && Marshaller.CheckGstPlugin("id3v2mux");
				Hyena.Log.Information("[Streamrecorder] GstPlugin lame" + (has_lame ? "" : " not") + " found");
				encoders.Add(new Encoder(lame_name, lame_pipeline, lame_extension));
				
				has_vorbis = Marshaller.CheckGstPlugin("vorbisenc") && Marshaller.CheckGstPlugin("oggmux") && Marshaller.CheckGstPlugin("oggmux");
				Hyena.Log.Information("[Streamrecorder] GstPlugin vorbis" + (has_vorbis ? "" : " not") + " found");
				encoders.Add(new Encoder(vorbis_name, vorbis_pipeline, vorbis_extension, true));

				has_flac = Marshaller.CheckGstPlugin("flacenc") && Marshaller.CheckGstPlugin("flactag");
				Hyena.Log.Information("[Streamrecorder] GstPlugin flac" + (has_flac ? "" : " not") + " found");
				encoders.Add(new Encoder(flac_name, flac_pipeline, flac_extension));

				has_level = Marshaller.CheckGstPlugin("level");
				Hyena.Log.Information("[Streamrecorder] GstPlugin level" + (has_level ? "" : " not") + " found");
				Hyena.Log.Debug("gstreamer initialized");
			}
			else
			{
				Hyena.Log.Debug("an error occurred during gstreamer initialization, aborting.");
			}
        }

		public bool Create()
		{
			Hyena.Log.Debug("[Streamrecoder.Recorder]<Create> START");
			audiotee = new PlayerAudioTee ( ServiceManager.PlayerEngine.ActiveEngine.GetBaseElements()[2] );

			string bin_description = BuildPipeline();

			if (bin_description.Equals(""))
			{
				return false;
			}
			
			encoder_bin = Parse.BinFromDescription (bin_description, true) ;

			Hyena.Log.Debug("[Streamrecoder.Recorder]<Create> encoder_bin created: " + encoder_bin.GetPathString ());
			
			tagger = new TagSetter(encoder_bin.GetByInterface(TagSetter.GetType()));
			file_sink = encoder_bin.GetByName("file_sink").ToFileSink();

			file_sink.Location = output_file + file_extension;
			file_sink.SetBooleanProperty("sync",true);
			file_sink.SetBooleanProperty("async",false);
			
			GLib.Object.GetObject(file_sink.ToIntPtr ()).AddNotification ("allow-overwrite", OnAllowOverwrite); 

			ghost_pad = encoder_bin.GetStaticPad("sink").ToGhostPad();

			Hyena.Log.Debug("[Streamrecoder.Recorder]<Create> END");
			
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
				file_extension = encoder.FileExtension;
			}
			
			return pipeline;
		}
		
		public List<Encoder> Encoders
		{
			get { return encoders; }
		}
		
		public string SetActiveEncoder(string active_encoder)
		{
			string ret = null;
			if (active_encoder == null) return null;
			foreach(Encoder encoder in encoders)
			{
				if (encoder.ToString().Equals(active_encoder))
				{
					encoder.IsPreferred = true;
					ret = encoder.ToString();
				} else {
					encoder.IsPreferred = false;
				}
			}
			if (ret == null)
			{
				ret = GetFirstAvailableEncoder().ToString();
			}
			return ret;
		}
		
		private Encoder GetFirstPreferredEncoder()
		{
			foreach (Encoder encoder in encoders)
			{
				if (encoder.IsPreferred) return encoder;
			}
			return null;
		}
		
		public Encoder GetFirstAvailableEncoder()
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
			Hyena.Log.Information("[Recorder]<AddSilenceDetector> START");
			if (has_level)
			{
				silence_pipeline = Parse.Launch("audioconvert name=src0 ! level message=true interval=1000000 ! filesink location=/home/dingsi/test.out name=fake_sink");
				//silence_pipeline = Parse.Launch("audioconvert name=src0 ! cutter ! fakesink name=fake_sink"));
				
				silence_ghost_pad = new GhostPad("src", silence_pipeline.GetByName("src0").GetStaticPad("src"));
				
				Element fake_sink = silence_pipeline.GetByName("fake_sink");
				fake_sink.SetBooleanProperty("sync",false);
  
				bus = silence_pipeline.GetBus ();

				Hyena.Log.Information("[Recorder]<AddSilenceDetector> connecting bus " + bus.GetPathString ());

				if (bus.ToIntPtr() == IntPtr.Zero) 
				{
					return;
				}

				bus.AddSignalWatch();
				
				IntPtr native_message = GLib.Marshaller.StringToPtrGStrdup ("message::level");
				Gst.Marshaller.g_signal_connect_data(bus.ToIntPtr (), native_message, BusEventCallback, IntPtr.Zero, IntPtr.Zero, 1);
				audiotee.AddBin(silence_pipeline,true);
				GLib.Marshaller.Free (native_message);
			}

			Hyena.Log.Information("[Recorder]<AddSilenceDetector> END");

		}

		private bool BusEventCallback (IntPtr bus, IntPtr message, IntPtr user_data)
		{
			Hyena.Log.Information("[Recorder]<BusEventCallback> START");
			IntPtr structure = Gst.Marshaller.gst_message_get_structure(message);
			IntPtr native_name = GLib.Marshaller.StringToPtrGStrdup ("peak");
			GLib.Value val = Gst.Marshaller.gst_structure_get_value(structure, native_name);
			GLib.ValueArray peaks = val.Val as GLib.ValueArray;
			int peak = (int)peaks[0];
			if (peak < -50) Hyena.Log.Information("[Recorder]<BusEventCallback> Silence detected");
			GLib.Marshaller.Free (native_name);

			Hyena.Log.Information("[Recorder]<BusEventCallback> END");

			return true;
		}				

		private void OnAllowOverwrite(object o, GLib.NotifyArgs args)
		{
			Hyena.Log.Debug ("[Recorder] <OnAllowOverwrite> Called");
		}

        public void StartRecording (bool blocked) 
        {
            Hyena.Log.Debug ("[Recorder] <StartRecording> START");

			if (Create())
			{
				audiotee.AddBin(encoder_bin,blocked);
				//AddSilenceDetector();
			}
			
            Hyena.Log.Debug ("[Recorder] <StartRecording> END");
        }

        public void StopRecording (bool blocked) 
        {
            Hyena.Log.Debug ("[Recorder] <StopRecording> STOPPED");

			if (encoder_bin != null && !encoder_bin.IsNull())
			{
				audiotee.RemoveBin(encoder_bin,blocked);
			}
			
			//Silence Detector Message Test
			//IntPtr msg = bus.Pop ();
			//if (msg == IntPtr.Zero) Hyena.Log.Information ("[Recorder] <StopRecording> No Messages");

        }
        
        public bool AddStreamTags(TrackInfo track, bool splitfiles)
        {
			Hyena.Log.Debug("[Recorder]<AddStreamTags> START");
			if (track == null || tagger == null) return false;
			
			Hyena.Log.Debug("[Recorder]<AddStreamTags>caught metadata ArtistName:" + track.ArtistName);
			Hyena.Log.Debug("[Recorder]<AddStreamTags>caught metadata Genre:" + track.Genre);
			Hyena.Log.Debug("[Recorder]<AddStreamTags>caught metadata TrackTitle:" + track.TrackTitle);
			Hyena.Log.Debug("[Recorder]<AddStreamTags>caught metadata AlbumArtist:" + track.AlbumArtist);
			RadioTrackInfo radio_track = track as RadioTrackInfo;
			Hyena.Log.Debug("[Recorder]<AddStreamTags>caught metadata Station:" + radio_track.ParentTrack.TrackTitle);

			if (tagger.IsNull())
			{
				Hyena.Log.Debug("[Recorder]<AddStreamTags>tagger is null, not tagging!");
				return false;
			}
			
			TagList taglist = new TagList ();
			taglist.AddStringValue(TagMergeMode.ReplaceAll,"title",track.TrackTitle);
			taglist.AddStringValue(TagMergeMode.ReplaceAll,"genre",track.Genre);
			taglist.AddStringValue(TagMergeMode.ReplaceAll,"artist",track.ArtistName);
			taglist.AddStringValue(TagMergeMode.ReplaceAll,"album-artist",track.AlbumArtist);
			
			tagger.MergeTags(taglist, TagMergeMode.ReplaceAll);
			
			if (splitfiles && file_sink != null && track.ArtistName.Length > 0)
			{
				SetMetadataFilename(track.TrackTitle, track.ArtistName);
				SetNewTrackLocation(output_file + file_extension);
			}

			Hyena.Log.Debug("[Recorder]<AddStreamTags> END");

			return true;
		}
				
		public string SetMetadataFilename(string title, string artist)
		{
			string new_name = artist + "_-_" + title;
			string test_name = new_name;
			int i = 0;
			while (File.Exists(output_directory + Path.DirectorySeparatorChar + SetOutputFile(test_name) + file_extension))
			{
				i++;
				test_name = new_name + "(" + i + ")";
			}
			new_name = test_name;
			SetOutputFile(new_name);
			return output_file;
		}

        public void SetOutputParameters (string directory, string filename) 
        {
            SetOutputDirectory(directory);
            SetOutputFile(filename);
        }
        
        private void SetOutputDirectory (string directory) 
        {
            output_directory = directory;
        }

        private string SetOutputFile (string fullfilename) 
        {
			string cleanfilename = fullfilename;
			char[] invalid_chars = Path.GetInvalidFileNameChars ();
			foreach (char invalid_char in invalid_chars)
			{
				cleanfilename = cleanfilename.Replace(invalid_char.ToString (), "_").Trim('_');
			}
			output_file =  output_directory + Path.DirectorySeparatorChar + cleanfilename;
			return cleanfilename;
		}

		
		private void SetNewTrackLocation(string new_location)
		{
			Pad teepad = ghost_pad.GetPeer();
			teepad.SetBlocked(true);
			encoder_bin.SendEvent(Marshaller.NewEOSEvent());
			encoder_bin.SetState(State.Null);
			file_sink.Location = new_location;
			encoder_bin.SetState(State.Ready);
			//if (recording) {
			encoder_bin.SetState(State.Playing);
			//}
			teepad.SetBlocked(false);
		}

    }

}
