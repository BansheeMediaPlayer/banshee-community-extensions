//
// StreamripperProcess.cs
//
// Author:
//   Frank Ziegler
//   based on Banshee-Streamripper by Akseli Mantila <aksu@paju.oulu.fi>
//
// Copyright (C) 2009 Akseli Mantila
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
    public class StreamripperProcess : Process
    {
        private string output_file_format_pattern;
        private string output_directory;
        private string uri;
        private bool isogg;

        public StreamripperProcess () 
        {
            StartInfo.FileName = "streamripper";
        }
        
        public void StartRecording () 
        {
            Hyena.Log.Debug ("[StreamripperProcess] <StartRecording> START");
            
            if (!SetParameters ())
                return;
          
            try {
                Start ();
            }
            catch (Exception e) {
                Hyena.Log.Exception ("[StreamripperProcess] <StartRecording> Couldn't start", e);
            }
                
            Hyena.Log.DebugFormat ("[StreamripperProcess] <StartRecording> Arguments {0}", StartInfo.Arguments);
            Hyena.Log.Debug ("[StreamripperProcess] <StartRecording> END");
        }

        public void StopRecording () 
        {
            Hyena.Log.Debug ("[StreamripperProcess] <StopRecording> STOPPED");

            try {
                if (!HasExited)
                    Kill ();   
            } catch (Exception e) {
                Hyena.Log.Exception ("[StreamripperProcess] <StopRecording> Couldn't stop", e);
            }
        }

        public bool SetParameters () 
        {
            if (String.IsNullOrEmpty (uri))
                return false;

            StartInfo.Arguments = uri;

            if (!String.IsNullOrEmpty (output_file_format_pattern))
                StartInfo.Arguments += " -t -D " + output_file_format_pattern + " ";

            if (!String.IsNullOrEmpty (output_directory))
                StartInfo.Arguments += " -d " + output_directory + " ";
                
            if (this.isogg)
				StartInfo.Arguments += " -A -a ";

            return true;
        }
        
        public void SetOutputFileFormatPattern (string pattern) 
        {
            output_file_format_pattern = pattern;
        }

        public void SetOutputDirectory (string directory) 
        {
            output_directory = directory;
        }

        public void SetStreamURI (string uri) 
        {
            this.uri = uri;
			Regex objOggPattern=new Regex("^.*ogg.*$");
			if (objOggPattern.IsMatch(uri))
			{
				this.isogg = true;
			}
			else
			{
				this.isogg = false;
			}
        }
    }

}
