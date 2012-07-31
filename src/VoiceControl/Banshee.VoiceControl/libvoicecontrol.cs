// 
// libvoicecontrol.cs
//  
// Author:
//       banshee <${AuthorEmail}>
// 
// Copyright (c) 2012 banshee
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Runtime.InteropServices;
using Banshee.ServiceStack;

namespace Banshee.VoiceControl
{
    delegate void AsrResultCallback (IntPtr sender, string text, string uttid);
    delegate void AsrVoidCallback (IntPtr sender);

    public class libvoicecontrol : IExtensionService, IDisposable, IDelayedInitializeService
    {
             string IService.ServiceName {
            get { return "VoiceControlService"; }
        }
        void IExtensionService.Initialize ()
        {}

        public void DelayedInitialize ()
        {
            //string path= Environment.CurrentDirectory ;

            IntPtr pipeline = voicecontrol_init_pipeline ("/home/banshee/Sphinx/TAR7552/7552.lm", "/home/banshee/Sphinx/TAR7552/7552.dic",
            (sender, text, uttid) => {
                Console.WriteLine ("Partial {0}", text);
                //ProcessCommand(text);
            }, (sender, text, uttid) => {
                Console.WriteLine ("Full {0}", text);
                if (text != null)
                    ProcessCommand(text);
            });
            if (pipeline == IntPtr.Zero) {
                throw new Exception ("Failed to create pipeline");
            }
            voicecontrol_start_listening (pipeline);
        }
        public void Dispose ()
        {
        }
        public void ProcessCommand(string commandText){
            if(commandText.Equals("PLAY")){
               Banshee.ServiceStack.ServiceManager.PlayerEngine.Play();
            }
            else if(commandText.Equals("PAUSE")){
                Banshee.ServiceStack.ServiceManager.PlayerEngine.Pause();
            }
            else if(commandText.Equals("STOP")){
                Banshee.ServiceStack.ServiceManager.PlayerEngine.Pause();
            }
        }

        [DllImport ("libvoicecontrol")]
        static extern IntPtr voicecontrol_init_pipeline (
            string language_model_file,
            string dictionary_file,
            AsrResultCallback partial_result,
            AsrResultCallback result);

        [DllImport ("libvoicecontrol")]
        static extern void voicecontrol_start_listening (IntPtr pipeline);
    }
}