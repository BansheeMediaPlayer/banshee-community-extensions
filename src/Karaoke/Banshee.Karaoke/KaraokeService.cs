//
// KaraokeService.cs
//
// Authors:
//   Frank Ziegler <funtastix@googlemail.com>
//
// Copyright (C) 2011 Frank Ziegler
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

using Mono.Addins;

using Banshee.ServiceStack;
using Banshee.Streamrecorder.Gst;

namespace Banshee.Karaoke
{

    public class KaraokeService : IExtensionService, IDelayedInitializeService, IDisposable
    {

        Bin audiobin;
        Bin playbin;
        Bin audiotee;
        Element audiokaraoke;

        bool has_karaoke = false;

        public KaraokeService ()
        {
        }

        #region IExtensionService implementation
        void IExtensionService.Initialize ()
        {

            has_karaoke = Marshaller.CheckGstPlugin ("audiokaraoke");
            Hyena.Log.Debug ("[Karaoke] GstPlugin audiokaraoke" + (has_karaoke ? "" : " not") + " found");
            if (!has_karaoke) {
                Hyena.Log.Warning ("[Karaoke] audiokaraoke is not available, please install gstreamer-good-plugins");
                return;
            }
        }

        void IDelayedInitializeService.DelayedInitialize ()
        {
            if (!has_karaoke) return;

            playbin = new Bin (ServiceManager.PlayerEngine.ActiveEngine.GetBaseElements ()[0]);
            audiobin = new Bin (ServiceManager.PlayerEngine.ActiveEngine.GetBaseElements ()[1]);
            audiotee = new Bin (ServiceManager.PlayerEngine.ActiveEngine.GetBaseElements ()[2]);

            if (playbin.IsNull ())
                Hyena.Log.Debug ("[Karaoke] Playbin is not yet initialized, cannot start Karaoke Mode");

            audiokaraoke = audiobin.GetByName ("karaoke");

            if (audiokaraoke.IsNull ())
            {
                //make a fakesink and set it as playbin audio-sink target
                Element fakesink = ElementFactory.Make ("fakesink");
                playbin.SetProperty ("audio-sink", fakesink);

                audiokaraoke = ElementFactory.Make ("audiokaraoke","karaoke");

                //add audiokaraoke to audiobin
                audiobin.Add (audiokaraoke);

                //setting new audiobin sink to audiokaraoke sink
                GhostPad teepad = new GhostPad (audiobin.GetStaticPad ("sink").ToIntPtr ());
                Pad audiokaraokepad = audiokaraoke.GetStaticPad ("sink");
                teepad.SetTarget (audiokaraokepad);

                //link audiokaraoke sink and audiotee sink
                audiokaraoke.Link (audiotee);

                //reset audio-sink property to modified audiobin
                playbin.SetProperty ("audio-sink", audiobin);
            }

            audiokaraoke.SetFloatProperty ("level", 1);
            audiokaraoke.SetFloatProperty ("mono-level", 1);

            //Hyena.Log.DebugFormat ("Karaoke service has been initialized! {0}", audiobin.ToString ());
        }
        #endregion

        #region IDisposable implementation
        void IDisposable.Dispose ()
        {
            if (!playbin.IsNull ())
            {
                audiokaraoke.SetFloatProperty ("level", 0);
                audiokaraoke.SetFloatProperty ("mono-level", 0);
            }
        }
        #endregion

        /// <summary>
        /// The service name
        /// </summary>
        string IService.ServiceName {
            get { return "KaraokeService"; }
        }

    }
}
