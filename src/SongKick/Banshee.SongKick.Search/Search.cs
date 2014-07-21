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

using Banshee.SongKick.Recommendations;
using Banshee.SongKick.Network;

namespace Banshee.SongKick.Search
{
    public abstract class Search<T> where T : IResult
    {
        public ResultsPage<T> ResultsPage { get; protected set; }

        public Query LastQuery { get; protected set; }

        protected readonly SongKickDownloader downloader;

        protected Search ()
        {
            downloader = new SongKickDownloader (SongKickCore.APIKey);
        }

        public abstract void GetResultsPage (Query query);

        public override string ToString ()
        {
            return string.Format ("[Search: Query={0}]", LastQuery);
        }
    }

    public class EventsByArtistSearch : Search<Event>
    {
        public override void GetResultsPage (Query query)
        {
            LastQuery = query;
            if (String.IsNullOrEmpty(query.String)) {
                ResultsPage = new ResultsPage<Event> () { error = new ResultsError("Empty query string")};
                return;
            }
            // temporary solution
            // TODO: add meaningful ResultsError
            // TODO: throw Web Exceptions
            try {
                long artistId;
                if (query.Id == null) {
                    var artist_results_page = downloader.FindArtists (query.String, Artists.GetArtistListResultsDelegate);
                    var artist = artist_results_page.results[0];
                    artistId = artist.Id;
                } else {
                    artistId = (long) query.Id;
                }

                ResultsPage = downloader.GetArtistsMusicEvents (artistId , Events.GetMusicEventListResultsDelegate);
            }
            catch (Exception e) {
                ResultsPage = new ResultsPage<Event> () { error = new ResultsError("could not download music events")};
                Hyena.Log.Error (e);
            }
        }
    }

    public class EventsByLocationSearch : Search<Event>
    {
        public override void GetResultsPage (Query query)
        {
            LastQuery = query;
            if (String.IsNullOrEmpty(query.String)) {
                ResultsPage = new ResultsPage<Event> () { error = new ResultsError("Empty query string")};
                return;
            }
            // temporary solution
            // TODO: add meaningful ResultsError
            // TODO: throw Web Exceptions
            try {
                long locationId;
                if (query.Id == null) {
                    var location_results_page = 
                        downloader.FindLocation (query.String, Locations.GetLocationListResultsDelegate);
                    var location = location_results_page.results[0];
                    locationId = location.Id;
                } else {
                    locationId = (long) query.Id;
                }

                ResultsPage = downloader.GetLocationMusicEvents (locationId, Events.GetMusicEventListResultsDelegate);
            }
            catch (Exception e) {
                ResultsPage = new ResultsPage<Event> () { error = new ResultsError("could not download music events")};
                Hyena.Log.Error (e);
            }
        }
    }

    public class LocationSearch : Search<Location>
    {
        public override void GetResultsPage (Query query)
        {
            LastQuery = query;
            if (String.IsNullOrEmpty(query.String)) {
                ResultsPage = new ResultsPage<Location> () { error = new ResultsError("Empty query string")};
                return;
            }
            // temporary solution
            // TODO: add meaningful ResultsError
            // TODO: throw Web Exceptions
            try {
                ResultsPage = downloader.FindLocation (query.String, Locations.GetLocationListResultsDelegate);
            }
            catch (Exception e) {
                ResultsPage = new ResultsPage<Location> () { error = new ResultsError("could not download locations")};
                Hyena.Log.Error (e);
            }
        }
    }

    public class ArtistSearch : Search<Artist> {

        public override void GetResultsPage (Query query)
        {
            LastQuery = query;
            if (String.IsNullOrEmpty(query.String)) {
                ResultsPage = new ResultsPage<Artist> () { error = new ResultsError("Empty query string")};
                return;
            }
            // temporary solution
            // TODO: add meaningful ResultsError
            // TODO: throw Web Exceptions
            try {
                ResultsPage = downloader.FindArtists (query.String, Artists.GetArtistListResultsDelegate);
            }
            catch (Exception e) {
                ResultsPage = new ResultsPage<Artist> () { error = new ResultsError("could not download locations")};
                Hyena.Log.Error (e);
            }
        }
    }
}

