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

namespace Banshee.VoiceControl
{
    delegate void AsrResultCallback (IntPtr sender, string text, string uttid);
    delegate void AsrVoidCallback (IntPtr sender);

    static class libvoicecontrol
    {
        static void Main ()
        {
            Gtk.Application.Init ();

            IntPtr pipeline = voicecontrol_init_pipeline (null, null,
            (sender, text, uttid) => {
                Console.WriteLine ("Partial {0}", text);
            }, (sender, text, uttid) => {
                Console.WriteLine ("Full {0}", text);
            });
            if (pipeline == IntPtr.Zero) {
                throw new Exception ("Failed to create pipeline");
            }
            voicecontrol_start_listening (pipeline);

            Gtk.Application.Run ();
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