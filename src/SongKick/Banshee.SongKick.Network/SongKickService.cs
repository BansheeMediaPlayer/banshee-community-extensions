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

using Banshee.ServiceStack;
using Banshee.Networking;
using Banshee.Sources;

using Hyena.Jobs;

using Banshee.SongKick.Recommendations;
using Banshee.SongKick.Search;
using Banshee.SongKick.CityProvider;


namespace Banshee.SongKick.Network
{
    public class SongKickService : IExtensionService, ICityObserver
    {
        //Scheduler scheduler = new Scheduler ();
        private string current_city_name = "";
        private uint refresh_timeout_id = 0;

        public SongKickService ()
        {
            CityProviderManager.Register (this);

            // Every 20 sec try to refresh again
            refresh_timeout_id = Application.RunTimeout (1000 * 20, RefreshLocalConcertsList);
        }

        private bool RefreshLocalConcertsList()
        {
            Hyena.Log.Debug ("Refreshing list of local concerts");
            Banshee.Kernel.Scheduler.Schedule (new Banshee.Kernel.DelegateJob (delegate {
                DateTime now = DateTime.Now;
                var search = new EventsByArtistSearch ();
                var recommended_artists = new Search.RecommendationProvider ().getRecommendations ();

                foreach (RecommendedArtist artist in recommended_artists) {
                    search.GetResultsPage (new Banshee.SongKick.Search.Query (null, artist.Name));
                    foreach (var res in search.ResultsPage.results) {
                        if (IsItMyCity(res.Location.Name)) {
                            Hyena.Log.InformationFormat ("This gig takes place in your city: {0} ", res.DisplayName);

                            foreach(var src in ServiceManager.SourceManager.Sources) {
                                if (src is SongKickSource) {
                                    var msg = new SourceMessage(src);
                                    msg.ClearActions();
                                    msg.Text = String.Format("This gig takes place in your city: {0}!", res.DisplayName);
                                    msg.CanClose = true;
                                    msg.AddAction (new MessageAction (Catalog.GetString ("More info"), delegate {
                                        System.Diagnostics.Process.Start (res.Uri);
                                        msg.IsHidden = true;
                                    }));
                                    src.PushMessage(msg);
                                    src.NotifyUser();
                                }
                            }
                            }
                        }
                    }
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

        public void UpdateCity (string cityName)
        {
            current_city_name = cityName;
        }

        public string ServiceName {
            get { return "SongKickService"; }
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
        }
    }
}

