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

using Banshee.Base;
using Banshee.Sources;
using Banshee.Sources.Gui;

// Other namespaces you might want:
using Banshee.ServiceStack;
using Banshee.Preferences;
using Banshee.MediaEngine;
using Banshee.PlaybackController;
using Banshee.Karaoke.Gst;

namespace Banshee.Karaoke
{

    public class KaraokeService : IExtensionService, IDisposable
    {

        Bin audiobin;
        //Bin playbin;
        Bin audiotee;
        Element audiokaraoke;

        public KaraokeService ()
        {
            Hyena.Log.Information ("Testing! Karaoke service has been instantiated!");
        }

        #region IExtensionService implementation
        void IExtensionService.Initialize ()
        {

            bool has_karaoke = Marshaller.CheckGstPlugin ("audiokaraoke");
            Hyena.Log.Debug ("[Karaoke] GstPlugin audiokaraoke" + (has_karaoke ? "" : " not") + " found");
            if (!has_karaoke) {
                Hyena.Log.Warning ("[Karaoke] audiokaraoke is not available, please install gstreamer-good-plugins");
                return;
            }

            //playbin = new Bin (ServiceManager.PlayerEngine.ActiveEngine.GetBaseElements ()[0]);
            audiobin = new Bin (ServiceManager.PlayerEngine.ActiveEngine.GetBaseElements ()[1]);
            audiotee = new Bin (ServiceManager.PlayerEngine.ActiveEngine.GetBaseElements ()[2]);

            audiokaraoke = ElementFactory.Make ("audiokaraoke");

            Hyena.Log.Debug ("[Karaoke] add audiokaraoke to audiobin");
            audiobin.Add (audiokaraoke);

            Hyena.Log.Debug ("[Karaoke] unlink audiotee sink and audiobin sink");
            audiobin.Unlink (audiotee);

            Hyena.Log.Debug ("[Karaoke] link audiokaraoke sink and audiobin sink");
            //audiobin.Link (audiokaraoke);
//    teepad = gst_element_get_pad (player->audiotee, "sink");
//    gst_element_add_pad (player->audiobin, gst_ghost_pad_new ("sink", teepad));
//    gst_object_unref (teepad);

            Hyena.Log.Debug ("[Karaoke] link audiokaraoke sink and audiotee sink");
            //audiokaraoke.Link (audiotee);

            Hyena.Log.InformationFormat ("Testing! Karaoke service has been initialized! {0}", audiokaraoke.GetPathString ());
        }
        #endregion

        #region IDisposable implementation
        void IDisposable.Dispose ()
        {
            Hyena.Log.Information ("Testing! Karaoke service has been disposed!");
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
