//
// SongKickDownloader.cs
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
using System.Text;

using Microsoft.FSharp.Core;

using Banshee.SongKick.Recommendations;

using CacheService;

namespace Banshee.SongKick.Network
{
    public class SongKickDownloader
    {
        private Cache cache = CacheManager.GetInstance.Initialize ("songkick");

        public string DefaultServiceUri {
            get { return @"http://api.songkick.com/api/3.0/"; }
        }
        private string apiKey;

        public SongKickDownloader (string apiKey)
        {
            this.apiKey = apiKey;
        }

        public ResultsPage<Event> GetMetroareaMusicEvents (string id, ResultsPage<Event>.GetResultsDelegate getResultsDelegate)
        {
            return GetMetroareaMusicEvents (int.Parse (id), getResultsDelegate);
        }

        public ResultsPage<Event> GetMetroareaMusicEvents (int id, ResultsPage<Event>.GetResultsDelegate getResultsDelegate)
        {
            // url format:
            // http://api.songkick.com/api/3.0/metro_areas/{metro_area_id}/calendar.json?apikey={your_api_key}
            // example url for events in London:
            // http://api.songkick.com/api/3.0/metro_areas/24426/calendar.json?apikey=Qjqhc2hkfU3BaTx6
            string replyString;
            string key = id.ToString ();

            if (IsThereCachedReplyWithKey (key)) {
                replyString = GetCachedReply (key);
            } else {
                var uriSB = new StringBuilder (DefaultServiceUri);
                uriSB.Append (@"metro_areas/");
                uriSB.Append (key);
                uriSB.Append (@"/calendar.json?apikey=");
                uriSB.Append (this.apiKey);

                replyString = Downloader.Download (uriSB.ToString ());
                CacheReply (key, replyString);
            }

            var resultsPage = new ResultsPage<Event> (replyString, getResultsDelegate);
            return resultsPage;
        }

        public ResultsPage<Event> GetArtistsMusicEvents (long id,  ResultsPage<Event>.GetResultsDelegate getResultsDelegate)
        {
            // http://api.songkick.com/api/3.0/artists/{artist_id}/calendar.json?apikey={your_api_key}
            string replyString;
            string key = id.ToString ();

            if (IsThereCachedReplyWithKey (key)) {
                replyString = GetCachedReply (key);
            } else {
                var uriSB = new StringBuilder (DefaultServiceUri);
                uriSB.Append (@"artists/");
                uriSB.Append (key);
                uriSB.Append (@"/calendar.json?apikey=");
                uriSB.Append (this.apiKey);

                replyString = Downloader.Download (uriSB.ToString ());
                CacheReply (key, replyString);
            }

            return new ResultsPage<Event> (replyString, getResultsDelegate);
        }

        public ResultsPage<Event> GetLocationMusicEvents (long id, ResultsPage<Event>.GetResultsDelegate getResultsDelegate)
        {
            // http://api.songkick.com/api/3.0/metro_areas/{metro_area_id}/calendar.json?apikey={your_api_key}
            string replyString;
            string key = id.ToString ();

            if (IsThereCachedReplyWithKey (key)) {
                replyString = GetCachedReply (key);
            } else {
                var uriSB = new StringBuilder (DefaultServiceUri);
                uriSB.Append (@"metro_areas/");
                uriSB.Append (key);
                uriSB.Append (@"/calendar.json?apikey=");
                uriSB.Append (this.apiKey);

                replyString = Downloader.Download (uriSB.ToString ());
                CacheReply (key, replyString);
            }
            return new ResultsPage<Event> (replyString, getResultsDelegate);
        }

        public ResultsPage<Artist> FindArtists (string artist, ResultsPage<Artist>.GetResultsDelegate getResultsDelegate)
        {
            //http://api.songkick.com/api/3.0/search/artists.json?query={search_query}&apikey={your_api_key}
            string replyString;

            if (IsThereCachedReplyWithKey (artist)) {
                replyString = GetCachedReply (artist);
            } else {
                var uriSB = new StringBuilder (DefaultServiceUri);
                uriSB.Append (@"search/artists.json?query=");
                uriSB.Append (Uri.EscapeDataString (artist));
                uriSB.Append (@"&apikey=");
                uriSB.Append (this.apiKey);

                replyString = Downloader.Download (uriSB.ToString ());
                CacheReply (artist, replyString);
            }

            return new ResultsPage<Artist> (replyString, getResultsDelegate);
        }

        public ResultsPage<Location> FindLocation(string location, ResultsPage<Location>.GetResultsDelegate getResultsDelegate)
        {
            // http://api.songkick.com/api/3.0/search/locations.json?query={search_query}&apikey={your_api_key}
            string replyString;

            if (IsThereCachedReplyWithKey (location)) {
                replyString = GetCachedReply (location);
            } else {
                var uriSB = new StringBuilder (DefaultServiceUri);
                uriSB.Append (@"search/locations.json?query=");
                uriSB.Append (System.Uri.EscapeDataString (location));
                uriSB.Append (@"&apikey=");
                uriSB.Append (this.apiKey);

                replyString = Downloader.Download (uriSB.ToString ());
                CacheReply (location, replyString);
            }

            return new ResultsPage<Location> (replyString, getResultsDelegate);
        }

        public ResultsPage<Location> FindLocationBasedOnIP (ResultsPage<Location>.GetResultsDelegate getResultsDelegate)
        {
            // http://api.songkick.com/api/3.0/search/locations.json?location=clientip&apikey={your_api_key}
            // i.e. 
            // http://api.songkick.com/api/3.0/search/locations.json?location=clientip&apikey=Qjqhc2hkfU3BaTx6

            var uriSB = new StringBuilder (DefaultServiceUri);
            uriSB.Append (@"search/locations.json?location=clientip&apikey=");
            uriSB.Append (this.apiKey);

            string replyString = Downloader.Download (uriSB.ToString ());
            return new ResultsPage<Location> (replyString, getResultsDelegate);
        }

        private bool IsThereCachedReplyWithKey (string key)
        {
            return (cache.Get (key)) != null;
        }

        private string GetCachedReply (string key)
        {
            return cache.Get (key).Value.ToString ();
        }

        private void CacheReply (string key, string value)
        {
            cache.Add (key, value);
        }

        // invalid API Key uri e.g:
        // http://api.songkick.com/api/3.0/metro_areas/24426/calendar.json?apikey=Qjqhc2hkfU3BaTx600
    }
}

