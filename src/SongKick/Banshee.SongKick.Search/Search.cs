//
// Search.cs
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
using Banshee.SongKick.Recommendations;
using Banshee.SongKick.Network;
using Hyena.Jobs;

namespace Banshee.SongKick.Search
{
    public abstract class Search<T> where T : IResult
    {
        public Query Query { get; protected set; }
        public ResultsPage<T> ResultsPage { get; protected set; }

        protected SongKickDownloader downloader;

        public Search ()
        {
            downloader = new SongKickDownloader (SongKickCore.APIKey);
        }

        public abstract void GetResultsPage (Query query);

        public override string ToString ()
        {
            return string.Format ("[Search: Query={0}]", Query);
        }
    }

    public class EventsByArtistSearch : Search<Event> {

        public EventsByArtistSearch() 
            : base()
        {
        }

        public override void GetResultsPage (Query query)
        {
            Query = query;
            // temporary solution
            // TODO: add meaningful ResultsError
            // TODO: throw Web Exceptions
            try {
                var artist_results_page = 
                    downloader.findArtists (Query.String, Banshee.SongKick.Recommendations.Artists.GetArtistListResultsDelegate);
                var artist = artist_results_page.results[0];
                ResultsPage = downloader.getArtistsMusicEvents(artist.Id, Banshee.SongKick.Recommendations.Events.GetMusicEventListResultsDelegate);
            }
            catch (Exception e) {
                ResultsPage = new ResultsPage<Event> () { error = new ResultsError("could not download music events")};
                Hyena.Log.Exception (e);
            }
        }
    }

    public class EventsByLocationSearch : Search<Event> {

        public EventsByLocationSearch() 
            : base()
        {
        }

        public override void GetResultsPage (Query query)
        {
            Query = query;
            // temporary solution
            // TODO: add meaningful ResultsError
            // TODO: throw Web Exceptions
            try {
                var location_results_page = 
                    downloader.findLocation (Query.String, Banshee.SongKick.Recommendations.Locations.GetLocationListResultsDelegate);
                var location = location_results_page.results[0];
                ResultsPage = downloader.getLocationMusicEvents(location.Id, Banshee.SongKick.Recommendations.Events.GetMusicEventListResultsDelegate);
            }
            catch (Exception e) {
                ResultsPage = new ResultsPage<Event> () { error = new ResultsError("could not download music events")};
                Hyena.Log.Exception (e);
            }
        }
    }

    public class LocationSearch : Search<Location> {

        public LocationSearch() 
            : base()
        {
        }

        public override void GetResultsPage (Query query)
        {
            Query = query;
            // temporary solution
            // TODO: add meaningful ResultsError
            // TODO: throw Web Exceptions
            try {
                ResultsPage = 
                    downloader.findLocation (Query.String, Banshee.SongKick.Recommendations.Locations.GetLocationListResultsDelegate);
            }
            catch (Exception e) {
                ResultsPage = new ResultsPage<Location> () { error = new ResultsError("could not download locations")};
                Hyena.Log.Exception (e);
            }
        }
    }

    public class ArtistSearch : Search<Artist> {

        public ArtistSearch() 
            : base()
        {
        }

        public override void GetResultsPage (Query query)
        {
            Query = query;
            // temporary solution
            // TODO: add meaningful ResultsError
            // TODO: throw Web Exceptions
            try {
                ResultsPage = 
                    downloader.findArtists (Query.String, Banshee.SongKick.Recommendations.Artists.GetArtistListResultsDelegate);
            }
            catch (Exception e) {
                ResultsPage = new ResultsPage<Artist> () { error = new ResultsError("could not download locations")};
                Hyena.Log.Exception (e);
            }
        }
    }
}

