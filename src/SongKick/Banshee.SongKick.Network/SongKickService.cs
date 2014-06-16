//
// SongKickService.cs
//
// Author:
//   Tomasz Maczyński <tmtimon@gmail.com>
//
// Copyright 2013 Tomasz Maczyński
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
using System.Collections.Generic;

using Mono.Unix;

using Banshee.Base;
using Banshee.ServiceStack;
using Banshee.Networking;
using Banshee.Sources;
using Notifications;

using Hyena;

using Banshee.SongKick.Recommendations;
using Banshee.SongKick.Search;
using Banshee.SongKick.CityProvider;
using Banshee.SongKick.UI;

namespace Banshee.SongKick.Network
{
    public class SongKickService : IExtensionService, ICityObserver, IDisposable
    {
        private string current_city_name = "";
        private uint refresh_timeout_id;
        private uint refresh_timeout;

        private Results<Event> local_events = new Results<Event> ();

        private LocalEventsSource events_source;

        public SongKickService ()
        {
            events_source = new LocalEventsSource (local_events);

            CityProviderManager.Register (this);

            ThreadAssist.SpawnFromMain (delegate {
                RefreshLocalConcertsList ();

                refresh_timeout = 1000 * 60 * 60 * 12; //Every 12 hours try to refresh again
                refresh_timeout_id = Application.RunTimeout (refresh_timeout, RefreshLocalConcertsList);

                ServiceManager.Get<Networking.Network> ().StateChanged += OnNetworkStateChanged;
            });
        }

        private void OnNetworkStateChanged (object o, NetworkStateChangedArgs args)
        {
            RefreshLocalConcertsList ();
        }

        private bool RefreshLocalConcertsList()
        {
            if (!ServiceManager.Get<Networking.Network> ().Connected ||
                !CityProviderManager.HasProvider)
                return true;

            Hyena.Log.Debug ("Refreshing list of local concerts");
            Kernel.Scheduler.Schedule (new Kernel.DelegateJob (delegate {
                var search = new EventsByArtistSearch ();
                var recommended_artists = new RecommendationProvider().getRecommendations();

                foreach (RecommendedArtist artist in recommended_artists) {
                    search.GetResultsPage (new Search.Query (null, artist.Name));
                    foreach (var res in search.ResultsPage.results) {
                        if (IsItMyCity(res.Location.Name) && !local_events.Contains(res)) {
                            local_events.Add(res);
                        }
                    }
                }
                NotifyUser ();
            }));
            return true;
        }

        private bool IsItMyCity (string x) {
            //ex: Mountain View, CA, US
            //ex: Madrid, Spain
            //ex: St. Petersburg, Russian Federation
            var subs = x.Split(',');
            foreach (var sub in subs) {
                //ex: "Madrid" && " Spain"
                //ex: "Mountain View" && " CA" && " US"
                //ex: "St. Petersburg" && " Russian Federation"
                if (current_city_name.Contains (sub)) {
                    return true;
                } else if (sub.Contains(". ")) {
                    var subsub = sub.Split ('.');
                    foreach (var item in subsub) {
                        if (current_city_name.Contains (item))
                            return true;
                    }
                }
            }
            return false;
        }

        private void NotifyUser()
        {
            foreach (var src in ServiceManager.SourceManager.Sources) {
                if (src is SongKickSource && !src.ContainsChildSource(events_source)) {
                    src.AddChildSource (events_source);
                }
            }

            events_source.view.UpdateEvents (local_events);

            foreach (var e in local_events) {
                var notification = new Notification ();
                notification.Body = e.DisplayName;
                notification.Summary = String.Format ("New event in {0}!", current_city_name);
                notification.Show ();
            }
        }

        public void UpdateCity (string cityName)
        {
            current_city_name = cityName;
            RefreshLocalConcertsList ();
        }

        public void Initialize ()
        {
            /*
            //for testing only:

            //text file:
            //string uri = @"http://textfiles.serverrack.net/computers/1003v-mm";

            //events in London:
            //string uri = @"http://api.songkick.com/api/3.0/metro_areas/24426/calendar.json?apikey=Qjqhc2hkfU3BaTx6";

            //events recomended for tmtimon user
            //string uri =  @"http://api.songkick.com/api/3.0/users/tmtimon/calendar.json?reason=tracked_artist&apikey=Qjqhc2hkfU3BaTx6"

            //invalid API key:
            //string uri = @"http://api.songkick.com/api/3.0/metro_areas/24426/calendar.json?apikey=invalidKey";


            //var downloadJob = new DownloadJob(uri, Events.GetMusicEventListResultsDelegate);  // test for DownloadJob
            var downloadJob = new MetroareaByIdDownloadJob (24426, SongKickCore.APIKey, Events.GetMusicEventListResultsDelegate);
            
            scheduler.Add(downloadJob);
            */
        }

        public void Dispose ()
        {
            //scheduler.CancelAll (true);
            Application.IdleTimeoutRemove (refresh_timeout_id);
            refresh_timeout_id = 0;

            ServiceManager.Get<Networking.Network> ().StateChanged -= OnNetworkStateChanged;
        }

        public string ServiceName {
            get { return "SongKickService"; }
        }
    }
}

