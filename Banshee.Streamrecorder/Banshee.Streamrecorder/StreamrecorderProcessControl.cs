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
        private bool is_streamripper;
        private bool is_mplayer;
        private bool streamripper_oggstream_workaround_enabled;
        private bool streamrecorder_process_restarted;
        private Process streamrecorder_process;
        private static string oggstream_error_text = "SR_ERROR_CANT_WRITE_TO_FILE" ;
        private static string mplayer_process_name = "mplayer" ;
        private static string streamripper_process_name = "streamripper" ;

        public StreamrecorderProcessControl () 
        {
			streamripper_oggstream_workaround_enabled = false;
			streamrecorder_process_restarted = false;
			is_streamripper = false;
			is_mplayer = false;
        }

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
            output_file =  Regex.Replace(output_directory, @" ", "\\ ") + "/" + Regex.Replace(fullfilename, @" ", "_");
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
				Console.WriteLine("[StreamrecorderProcessControl Info]" + outLine.Data) ;
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
					Console.WriteLine("[StreamrecorderProcessControl Error Caught]: " + errLine.Data) ;
					streamripper_oggstream_workaround_enabled = true;
					if (!streamrecorder_process_restarted)
					{
						Console.WriteLine("[StreamrecorderProcessControl]: Restarting");
						StopRecording();
						StartRecording();
					}
					else
					{
						Console.WriteLine("[StreamrecorderProcessControl]: Restarting already tried. Exiting.");
					}
					streamrecorder_process_restarted = true;
				} else {
					Console.WriteLine("[StreamrecorderProcessControl Uncaught Error]: " + errLine.Data) ;
				}

            }
        }

        /*void StreamripperExitedHandler (object sendingProcess, 
            System.EventArgs eventArgs) 
        {
				Console.WriteLine("[StreamrecorderProcessControl Info]: Recording Stopped on Error... Should be restarting");
		}*/
		
    }

}
