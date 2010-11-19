//
// ZeitgeistDataprovider.cs
//
// Authors:
//   Manish Sinha <mail@manishsinha.net>
//   Randal Barlow <email dot tehk at gmail dot com>
//
// Copyright (C) 2010 Manish Sinha
// Copyright (C) 2010 Randal Barlow
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
using Gtk;

using Banshee.Base;
using Banshee.ServiceStack;
using Banshee.Preferences;
using Banshee.MediaEngine;	
using Banshee.PlaybackController;

using Zeitgeist;
using Zeitgeist.Datamodel;

namespace Banshee.Zeitgeist
{
    public class ZeitgeistDataprovider : Banshee.ServiceStack.IExtensionService, IDisposable
    {
        string IService.ServiceName {
            get { return "ZeitgeistService"; }
        }
        
        public ZeitgeistDataprovider()
        {}
        
        void IExtensionService.Initialize()
        {
            try
            {
                client = new LogClient();
                ServiceManager.PlaybackController.TrackStarted += HandleServiceManagerPlaybackControllerTrackStarted;
                ServiceManager.PlaybackController.Stopped += HandleServiceManagerPlaybackControllerStopped;
            }
            catch(Exception e)
            {
            }
        }

        void HandleServiceManagerPlaybackControllerStopped (object sender, EventArgs e)
        {
            string uri = ServiceManager.PlaybackController.CurrentTrack.Uri.ToString();
        }

        void HandleServiceManagerPlaybackControllerTrackStarted (object sender, EventArgs e)
        {
            string uri = ServiceManager.PlaybackController.CurrentTrack.Uri.ToString();
        }

        void IDisposable.Dispose()
        {
            client = null;
        }

        LogClient client;
    }
}
