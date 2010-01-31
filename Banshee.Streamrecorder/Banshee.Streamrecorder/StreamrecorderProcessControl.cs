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

using System.Text.RegularExpressions;

using Mono.Addins;

using Banshee.ServiceStack;
using Banshee.Collection;

using Hyena;

namespace Banshee.Streamrecorder
{
    public class StreamrecorderProcessControl
    {
        private string output_file_format_pattern;
        private string output_directory;
        private string uri;
        private string output_file;
        
        private IntPtr playbin;
        private IntPtr audiotee;
        private IntPtr encoder_bin;

        public StreamrecorderProcessControl () 
        {
			IntPtr [] elements = ServiceManager.PlayerEngine.ActiveEngine.GetBaseElements();
			playbin = elements[0];
			audiotee = elements[2];
			encoder_bin = Gst.ParseBinFromDescription ("lame name=audio_encoder ! gnomevfssink name=file_sink", true) ;
        }

		private void InitControl()
		{
			/*
			def create_recorder (self):
				#self.encoder_bin = gst.parse_bin_from_description ('lame name=audio_encoder ! gnomevfssink name=file_sink', True)
				#self.encoder_bin = gst.parse_bin_from_description ('lame name=audio_encoder ! id3v2mux name=tagger ! gnomevfssink name=file_sink', True)
				#print 'Using AudioProfile: %s, %s, %s' % (self.audio_profile[0], self.audio_profile[1], self.audio_profile[2])
				
				self.encoder_bin = gst.parse_bin_from_description ('audioresample ! audioconvert ! %s ! gnomevfssink name=file_sink' % (self.audio_profile[1]), True)
				#print '%s' % (self.audio_profile[0])
				print '%s' % (self.audio_profile[1])
				#print '%s' % (self.audio_profile[2])

				#self.audio_encoder = self.encoder_bin.get_by_name ('audio_encoder')
				self.tagger = self.encoder_bin.get_by_interface (gst.TagSetter)
				print self.tagger
				self.file_sink.set_property ('location', file_uri)
				self.file_sink = self.encoder_bin.get_by_name ('file_sink')

				# This prevents gnomevfs totally screwing up rhythmbox (gst 0.10.15+)
				if gst.version()[1] == 10 and gst.version()[2] >= 15:
					self.file_sink.set_property ('sync', True)
					self.file_sink.set_property ('async', False)
				self.file_sink.connect ('allow-overwrite', self.allow_overwrite_cb)

				self.ghostpad = self.encoder_bin.get_pad ('sink')
			*/
		}

        public void StartRecording () 
        {
            Hyena.Log.Debug ("[StreamrecorderProcessControl] <StartRecording> START");
            
            if (!SetParameters ())
                return;

			//add tee
			//* request a pad from tee
			//* set it blocked
			//* link recording branch
			//* set it to playing
			//* unset pad-block         
                
            Hyena.Log.Debug ("[StreamrecorderProcessControl] <StartRecording> END");
        }

        public void StopRecording () 
        {
            Hyena.Log.Debug ("[StreamrecorderProcessControl] <StopRecording> STOPPED");

			//remove tee
			//* set recording_branch to paused
			//* request a pad from tee
			//* set it blocked
			//* unlink recording branch
			//* unset pad-block         

        }

        public bool SetParameters () 
        {
            if (String.IsNullOrEmpty (uri))
                return false;

			//set up file for recording
			
            return true;
        }
        
        public void SetOutputParameters (string directory, string filename, string pattern) 
        {
            SetOutputFileFormatPattern(pattern);
            SetOutputDirectory(directory);
            SetOutputFile(filename);
        }
        
        private void SetOutputFileFormatPattern (string pattern) 
        {
            output_file_format_pattern = pattern;
        }

        private void SetOutputDirectory (string directory) 
        {
            output_directory = directory;
        }

        private void SetOutputFile (string fullfilename) 
        {
            output_file =  Regex.Replace(output_directory, @" ", "\\ ") + Path.DirectorySeparatorChar + Regex.Replace(fullfilename, @" ", "_");
        }

        public void SetStreamURI (string uri) 
        {
            this.uri = uri;
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

	def get_preferred_audio_profile (self):
		profile_id = self.client.get_string ('/apps/rhythmbox/library_preferred_format')
		gdir = GCONF_GST_AUDIO_PROFILE % (profile_id)
		pipeline = self.client.get_string (gdir + '/pipeline')
		extension = self.client.get_string (gdir + '/extension')
		return [profile_id, pipeline, extension]

 */

    }

}
