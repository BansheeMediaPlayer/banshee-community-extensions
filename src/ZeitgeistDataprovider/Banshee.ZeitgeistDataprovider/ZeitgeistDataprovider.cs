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
using System.Collections.Generic;

using Banshee.Base;
using Banshee.ServiceStack;
using Banshee.Preferences;
using Banshee.MediaEngine;
using Banshee.Collection;
using Banshee.PlaybackController;

using Zeitgeist;
using Hyena;
using Zeitgeist.Datamodel;

namespace Banshee.Zeitgeist
{
    public class ZeitgeistDataprovider : IExtensionService, IDisposable
    {
        string IService.ServiceName {
            get { return "ZeitgeistService"; }
        }
        
        public ZeitgeistDataprovider()
        {}
        
        void IExtensionService.Initialize()
        {
            Log.Debug("Initializing Zeitgeist Dataprovider Plugin");

            try
            {
                client = new LogClient();
                if(client != null)
                {
                    ServiceManager.PlaybackController.TrackStarted += HandleServiceManagerPlaybackControllerTrackStarted;
                    ServiceManager.PlaybackController.Stopped += HandleServiceManagerPlaybackControllerStopped;
                }
            }
            catch(Exception)
            {
            }
        }

        void HandleServiceManagerPlaybackControllerTrackStarted (object sender, EventArgs e)
        {
            try
            {
                if(current_track != null)
                {
                    StopTrack(current_track);
                }

                Event ev = CreateZgEvent(ServiceManager.PlaybackController.CurrentTrack, Interpretation.Instance.EventInterpretation.AccessEvent);
                client.InsertEvents(new List<Event>() {ev});

                current_track = ServiceManager.PlaybackController.CurrentTrack;
            }
            catch(Exception)
            {}
        }

        void HandleServiceManagerPlaybackControllerStopped (object sender, EventArgs e)
        {
            StopTrack(ServiceManager.PlaybackController.PriorTrack);
        }

        void StopTrack(TrackInfo track)
        {
            try
            {
                Event ev = CreateZgEvent(track, Interpretation.Instance.EventInterpretation.LeaveEvent);
                client.InsertEvents(new List<Event>() {ev});

                current_track = null;
            }
            catch(Exception)
            {}
        }

        private Event CreateZgEvent(TrackInfo track, NameUri event_type)
        {
            string uri = track.Uri.ToString();
            string trackname = track.TrackTitle.ToString ();
            string mimetype = track.MimeType.ToString();
            string album = track.AlbumTitle.ToString();
            string artist = track.ArtistName.ToString();

            Event ev = new Event();

            ev.Actor = "application://banshee-1.desktop";
            ev.Timestamp = DateTime.Now;
            ev.Manifestation = Manifestation.Instance.EventManifestation.UserActivity;
            ev.Interpretation = event_type;

            Subject sub = new Subject();
            sub.Uri = uri;
            sub.Interpretation = Interpretation.Instance.Media.Audio;
            sub.Manifestation = Manifestation.Instance.FileDataObject.FileDataObject;
            sub.MimeType = mimetype;
            sub.Text = string.Format("{0} - {1} - {2}", trackname, artist,album);

            ev.Subjects.Add(sub);

            return ev;
        }

        void IDisposable.Dispose()
        {
            Log.Debug ("Disposing Zeitgeist Dataprovider Plugin");

            client = null;
        }

        LogClient client;

        TrackInfo current_track;
    }
}
