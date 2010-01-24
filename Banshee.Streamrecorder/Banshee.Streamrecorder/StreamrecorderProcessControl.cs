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
using System.Runtime.InteropServices;

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
        private bool is_streamripper;
        private bool is_mplayer;
        private bool streamripper_oggstream_workaround_enabled;
        private bool streamrecorder_process_restarted;
        private Process streamrecorder_process;
        
        private IntPtr playbin;
        private IntPtr audiotee;
        private IntPtr encoder_bin;
        
        private static string oggstream_error_text = "SR_ERROR_CANT_WRITE_TO_FILE" ;
        private static string mplayer_process_name = "mplayer" ;
        private static string streamripper_process_name = "streamripper" ;

        public StreamrecorderProcessControl () 
        {
			IntPtr [] elements = ServiceManager.PlayerEngine.ActiveEngine.GetBaseElements();
			playbin = elements[0];
			audiotee = elements[2];
			encoder_bin = gst_parse_bin_from_description ("lame name=audio_encoder ! gnomevfssink name=file_sink", true) ;        }

		private Process InitProcess()
		{
            Process p = new Process();
            if (is_streamripper)
            {
				p.StartInfo.FileName = streamripper_process_name;
			}
			if (is_mplayer)
			{
				p.StartInfo.FileName = mplayer_process_name;
			}
            p.EnableRaisingEvents = true;
			p.StartInfo.RedirectStandardError = true;
			p.StartInfo.RedirectStandardOutput = true;
			p.StartInfo.UseShellExecute = false;
			p.ErrorDataReceived += new DataReceivedEventHandler(StreamripperErrorDataHandler) ;
            p.OutputDataReceived += new DataReceivedEventHandler(StreamripperOutputDataHandler) ;
            //p.Exited += new EventHandler (StreamripperExitedHandler) ;
            return p;
		}

        public void StartRecording () 
        {
            Hyena.Log.Debug ("[StreamrecorderProcessControl] <StartRecording> START");
            
            streamrecorder_process = InitProcess();

            if (!SetParameters ())
                return;

            try {
                streamrecorder_process.Start ();
				streamrecorder_process.BeginOutputReadLine();
				streamrecorder_process.BeginErrorReadLine();
            }
            catch (Exception e) {
                Hyena.Log.Exception ("[StreamrecorderProcessControl] <StartRecording> Couldn't start", e);
            }
                
            Hyena.Log.DebugFormat ("[StreamrecorderProcessControl] <StartRecording> Arguments {0}", streamrecorder_process.StartInfo.Arguments);
            Hyena.Log.Debug ("[StreamrecorderProcessControl] <StartRecording> END");
        }

        public void StopRecording () 
        {
            Hyena.Log.Debug ("[StreamrecorderProcessControl] <StopRecording> STOPPED");

			if (streamrecorder_process == null)
			{
				return;
			}

            try {
                if (!streamrecorder_process.HasExited)
                {
                    streamrecorder_process.Kill ();
                    streamrecorder_process.Close ();
				}
            } catch (Exception e) {
                Hyena.Log.Exception ("[StreamrecorderProcessControl] <StopRecording> Couldn't stop", e);
            }
        }

        public bool SetParameters () 
        {
            if (String.IsNullOrEmpty (uri))
                return false;

			if (is_streamripper)
			{
				streamrecorder_process.StartInfo.Arguments = uri;

				if (!String.IsNullOrEmpty (output_file_format_pattern))
					streamrecorder_process.StartInfo.Arguments += " -t -D " + output_file_format_pattern + " ";

				if (!String.IsNullOrEmpty (output_directory))
					streamrecorder_process.StartInfo.Arguments += " -d " + output_directory + " ";
					
				if (this.streamripper_oggstream_workaround_enabled)
					streamrecorder_process.StartInfo.Arguments += " -A -a ";
			}
			if (is_mplayer)
			{
				streamrecorder_process.StartInfo.Arguments = " -noframedrop -noconfig all -nolirc -slave -dumpstream " + uri;

				if (!String.IsNullOrEmpty (output_file))
				{
					streamrecorder_process.StartInfo.Arguments += " -dumpfile " + output_file;
				}
				else return false;
			}

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
			if (this.uri == null || !this.uri.Equals(uri))
			{
				streamripper_oggstream_workaround_enabled = false;
				streamrecorder_process_restarted = false;
			}
			is_streamripper = CheckHttpHeader(uri);
			is_mplayer = !is_streamripper;
            this.uri = uri;
        }

        public bool CheckHttpHeader (string uri)
        {
            if (uri == null) {
                Hyena.Log.Debug ("[StreamrecorderService] <CheckHttpHeader> END. Recording not ready");
                return false;
            }
			return Regex.Match(uri,"^http://" ).Success;
        }

        void StreamripperOutputDataHandler(object sendingProcess, 
            DataReceivedEventArgs outLine)
        {
            if (!String.IsNullOrEmpty(outLine.Data))
            {
				Hyena.Log.Information("[StreamrecorderProcessControl Info]" + outLine.Data) ;
            }
        }
        
        void StreamripperErrorDataHandler(object sendingProcess, 
            DataReceivedEventArgs errLine)
        {
            if (!String.IsNullOrEmpty(errLine.Data))
            {
				bool ogg_error = Regex.Match(errLine.Data, oggstream_error_text ).Success ;
				if (ogg_error)
				{
					Hyena.Log.Warning("[StreamrecorderProcessControl Error Caught]: " + errLine.Data) ;
					streamripper_oggstream_workaround_enabled = true;
					if (!streamrecorder_process_restarted)
					{
						Hyena.Log.Warning("[StreamrecorderProcessControl]: Restarting");
						StopRecording();
						StartRecording();
					}
					else
					{
						Hyena.Log.Error("[StreamrecorderProcessControl]: Restarting already tried. Exiting.");
					}
					streamrecorder_process_restarted = true;
				} else {
					Hyena.Log.Error("[StreamrecorderProcessControl Uncaught Error]: " + errLine.Data) ;
				}

            }
        }

        [DllImport ("gstreamer.so")]
        private static extern IntPtr gst_parse_bin_from_description (string desc, bool ukn);

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

	def set_recording (self, enabled):
		self.recording = enabled
		p = self.shell.get_player ().get_property ('player')
		if enabled:
			p.add_tee (self.encoder_bin)
		else:
			p.remove_tee (self.encoder_bin)
		
	def get_preferred_audio_profile (self):
		profile_id = self.client.get_string ('/apps/rhythmbox/library_preferred_format')
		gdir = GCONF_GST_AUDIO_PROFILE % (profile_id)
		pipeline = self.client.get_string (gdir + '/pipeline')
		extension = self.client.get_string (gdir + '/extension')
		return [profile_id, pipeline, extension]

 */

    }

}
