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
        public static int counter=1;
             string IService.ServiceName {
            get { return "VoiceControlService"; }
        }
        void IExtensionService.Initialize ()
        {}

        public void DelayedInitialize ()
        {
            string path= Environment.CurrentDirectory ;
           path= path.Remove(path.Length-3,3);
           path= string.Concat(path,"src/VoiceControl/langModel/7552.lm");
            Console.WriteLine(path);
           // IntPtr pipeline = voicecontrol_init_pipeline ("/home/banshee/Sphinx/TAR7552/7552.lm", "/home/banshee/Sphinx/TAR7552/7552.dic",
            try {
            IntPtr pipeline = voicecontrol_init_pipeline (path, null,
            (sender, text, uttid) => {
                    try {
                Console.WriteLine ("Partial {0}", text);
                 if (!string.IsNullOrEmpty(text))
                {

               ProcessCommand(text , counter);
                   // counter= counter + 1;
                        }

                        }
                 catch(Exception ex){
                Console.WriteLine ("Error processing Partial command '{0}':\n{1}", text, ex);
                }
            }, (sender, text, uttid) => {
                    try {
                Console.WriteLine ("Full {0}", text);
               if (!string.IsNullOrEmpty(text))
                    ProcessCommand(text,counter);
                                        }
                    catch (Exception ex) {
                    Console.WriteLine ("Error processing Full command '{0}':\n{1}", text, ex);
                    }
            });
            if (pipeline == IntPtr.Zero) {
                throw new Exception ("Failed to create pipeline");
            }
            voicecontrol_start_listening (pipeline);
            }
            catch(Exception e){
                Console.WriteLine("first "+e.Message);
            }
        }
        public void Dispose ()
        {
            Console.WriteLine("Dispose");
        }
        public void ProcessCommand(string commandText, int counter){
            try{
            Console.WriteLine ("3 "+counter, counter);
            if(string.Equals(commandText,@"PLAY")&& counter<2){
                Console.WriteLine ("1 "+counter, counter);
               ServiceManager.PlayerEngine.TogglePlaying();
                counter++;
                Console.WriteLine ("2 "+counter, counter);
            }
            //else{
             //   Console.WriteLine("NO Change");
            //}
            else if(string.Equals(commandText,"STOP")||string.Equals(commandText,"PAUSE")){
                    Console.WriteLine ("STOP 1 "+counter, counter);

                   if(ServiceManager.PlayerEngine.CanPause)
                    {
                        Console.WriteLine ("STOP 2 "+counter, counter);
                        ServiceManager.PlayerEngine.Pause();

                    }
                    else
                        Console.WriteLine("Can't Stop");
           }
           // else if(commandText.Equals("STOP")){
             //   Banshee.ServiceStack.ServiceManager.PlayerEngine.Pause();
          //  }
            }
            catch(Exception e){
                Console.WriteLine("Second "+e.Message);
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