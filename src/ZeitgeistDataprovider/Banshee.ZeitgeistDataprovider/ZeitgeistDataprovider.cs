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
using System.Collections.Generic;

using Banshee.ServiceStack;
using Banshee.Collection;

using Hyena;
using Zeitgeist;
using Zeitgeist.Datamodel;
using Banshee.MediaEngine;

namespace Banshee.Zeitgeist
{
    public class ZeitgeistDataprovider : IExtensionService, IDisposable
    {
        private LogClient client;
        private TrackInfo current_track;
        private bool hasTrackFinished = false;
        private DataSourceClient dsReg;
        private string actorAppName ="application://banshee.desktop";

        // Used by DataSourceRegistry
        private const string bansheeDataSourceId = "org.bansheeproject.Banshee,dataprovider";
        private const string bansheeDataSourceName = "Banshee Datasource";
        private const string bansheeDataSourceDesc = "This datasource pushes 2 events for banshee - Track Start and Track Stop";

        string IService.ServiceName {
            get { return "ZeitgeistService"; }
        }
        
        public ZeitgeistDataprovider()
        {}
        
        void IExtensionService.Initialize()
        {
            try {
                client = new LogClient ();
                if (client != null) {
                    Log.Debug("Zeitgeist client created");

                    // Try to register the datasource in DataSource registry
                    dsReg = new DataSourceClient();

                    try {
                        // Register the datasource/dataprovider
                        dsReg.RegisterDataSources(bansheeDataSourceId,
                                                    bansheeDataSourceName,
                                                    bansheeDataSourceDesc ,
                                                    PopulateAllDataSourceTemplates()
                                                  );
                    }
                    catch(Exception exp) {
                        // Nazi exception handler
                        // Can be fixed when zeitgeist-sharp is migrated from ndesk-dbus to dbus-sharp
                        Log.Exception(exp);
                    }

                    // Handle the events
                    ServiceManager.PlaybackController.Stopped += HandleServiceManagerPlaybackControllerStopped;
                    ServiceManager.PlayerEngine.ConnectEvent(playerEvent_Handler, PlayerEvent.StartOfStream | PlayerEvent.EndOfStream);

                } else {
                    Log.Warning ("Could not create Zeitgeist client");
                }
            } catch (Exception e) {
                Log.Exception (e);
            }
        }

        private List<Event> PopulateAllDataSourceTemplates()
        {
            List<Event> eventList = new List<Event>(4);

            // A new track is started. The user selected the track
            Event ev1 = new Event();
            ev1.Actor = this.actorAppName;
            ev1.Interpretation = Interpretation.Instance.EventInterpretation.AccessEvent;
            ev1.Manifestation = Manifestation.Instance.EventManifestation.UserActivity;
            Subject sub1 = new Subject();
            sub1.Interpretation = Interpretation.Instance.Media.Audio;
            sub1.Manifestation = Manifestation.Instance.FileDataObject.FileDataObject;
            ev1.Subjects.Add(sub1);
            eventList.Add(ev1);

            // A new track is started. The track gets selected because it was scheduled in a queue
            Event ev2 = new Event();
            ev2.Actor = this.actorAppName;
            ev2.Interpretation = Interpretation.Instance.EventInterpretation.AccessEvent;
            ev2.Manifestation = Manifestation.Instance.EventManifestation.ScheduledActivity;
            Subject sub2 = new Subject();
            sub2.Interpretation = Interpretation.Instance.Media.Audio;
            sub2.Manifestation = Manifestation.Instance.FileDataObject.FileDataObject;
            ev2.Subjects.Add(sub2);
            eventList.Add(ev2);

            // A track ended. The track ended because a new track in the queue has to be started
            Event ev3 = new Event();
            ev3.Actor = this.actorAppName;
            ev3.Interpretation = Interpretation.Instance.EventInterpretation.LeaveEvent;
            ev3.Manifestation = Manifestation.Instance.EventManifestation.ScheduledActivity;
            Subject sub3 = new Subject();
            sub3.Interpretation = Interpretation.Instance.Media.Audio;
            sub3.Manifestation = Manifestation.Instance.FileDataObject.FileDataObject;
            ev3.Subjects.Add(sub3);
            eventList.Add(ev3);

            // A track ended. The track ended because the user selected a new track to play
            Event ev4 = new Event();
            ev4.Actor = this.actorAppName;
            ev4.Interpretation = Interpretation.Instance.EventInterpretation.LeaveEvent;
            ev4.Manifestation = Manifestation.Instance.EventManifestation.ScheduledActivity;
            Subject sub4 = new Subject();
            sub4.Interpretation = Interpretation.Instance.Media.Audio;
            sub4.Manifestation = Manifestation.Instance.FileDataObject.FileDataObject;
            ev4.Subjects.Add(sub4);
            eventList.Add(ev4);

            return eventList;
        }

        void playerEvent_Handler(PlayerEventArgs e)
        {
            if(e.Event == PlayerEvent.EndOfStream) {
                Log.Debug("EndOfStream for : "+ServiceManager.PlaybackController.CurrentTrack.TrackTitle);
                hasTrackFinished = true;
            }

            if(e.Event == PlayerEvent.StartOfStream && current_track != ServiceManager.PlaybackController.CurrentTrack) {
                try {
                    if (current_track != null) {
                        StopTrack (current_track);
                    }

                    Log.Debug("TrackStarted: "+ServiceManager.PlaybackController.CurrentTrack.TrackTitle);
                    Event ev = CreateZgEvent (ServiceManager.PlaybackController.CurrentTrack, Interpretation.Instance.EventInterpretation.AccessEvent);
                    client.InsertEvents (new List<Event> () {ev});

                    current_track = ServiceManager.PlaybackController.CurrentTrack;

                    // Set this as false only after the previous track finished and new track start event has been logged
                    hasTrackFinished = false;

                } catch (Exception ex) {
                    Log.Exception (ex);
                }
            }
        }

        void HandleServiceManagerPlaybackControllerStopped (object sender, EventArgs e)
        {
            StopTrack (ServiceManager.PlaybackController.PriorTrack);
        }

        void StopTrack (TrackInfo track)
        {
            try {
                Event ev = CreateZgEvent (track, Interpretation.Instance.EventInterpretation.LeaveEvent);
                client.InsertEvents (new List<Event> () {ev});

                current_track = null;
            } catch (Exception ex) {
                Log.Exception (ex);
            }
        }

        private Event CreateZgEvent(TrackInfo track, NameUri event_type)
        {
            string uri = track.Uri.AbsoluteUri;
            string trackname = track.TrackTitle;
            string mimetype = track.MimeType;
            string album = track.AlbumTitle;
            string artist = track.ArtistName;

            Event ev = new Event ();

            ev.Actor = actorAppName;
            ev.Timestamp = DateTime.Now;

            // If the track has finished then Event Manifestation is ScheduledActivity else UserActivity
            if(hasTrackFinished)
            {
                ev.Manifestation = Manifestation.Instance.EventManifestation.ScheduledActivity;
            }
            else
            {
                ev.Manifestation = Manifestation.Instance.EventManifestation.UserActivity;
            }

            ev.Interpretation = event_type;

            Subject sub = new Subject ();
            sub.Uri = uri;
            sub.Interpretation = Interpretation.Instance.Media.Audio;
            sub.Manifestation = Manifestation.Instance.FileDataObject.FileDataObject;
            sub.MimeType = mimetype;
            sub.Text = String.Format ("{0} - {1} - {2}", trackname, artist,album);

            ev.Subjects.Add(sub);

            return ev;
        }

        void IDisposable.Dispose()
        {
            ServiceManager.PlaybackController.Stopped -= HandleServiceManagerPlaybackControllerStopped;
            ServiceManager.PlayerEngine.DisconnectEvent(playerEvent_Handler);

            client = null;
        }
    }
}
