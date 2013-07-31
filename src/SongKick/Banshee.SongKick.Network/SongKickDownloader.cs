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
using Banshee.SongKick.Recommendations;

namespace Banshee.SongKick.Network
{
    public class SongKickDownloader
    {
        public string DefaultServiceUri {
            get { return @"http://api.songkick.com/api/3.0/"; }
        }
        private string APIKey { get; set;}

        public SongKickDownloader(String APIKey)
        {
            this.APIKey = APIKey;
        }

        public ResultsPage<Event> getMetroareaMusicEvents(string id, ResultsPage<Event>.GetResultsDelegate getResultsDelegate)
        {
            return getMetroareaMusicEvents (int.Parse (id), getResultsDelegate);
        }

        public ResultsPage<Event> getMetroareaMusicEvents(int id, ResultsPage<Event>.GetResultsDelegate getResultsDelegate)
        {
            // url format:
            // http://api.songkick.com/api/3.0/metro_areas/{metro_area_id}/calendar.json?apikey={your_api_key}
            // example url for events in London:
            // http://api.songkick.com/api/3.0/metro_areas/24426/calendar.json?apikey=Qjqhc2hkfU3BaTx6
            var uriSB = new StringBuilder (DefaultServiceUri);
            uriSB.Append (@"metro_areas/");
            uriSB.Append (id.ToString());
            uriSB.Append (@"/calendar.json?apikey=");
            uriSB.Append (this.APIKey);

            string replyString = Downloader.download(uriSB.ToString());
            var resultsPage = new ResultsPage<Event>(replyString, getResultsDelegate);

            return resultsPage;
        }

        public ResultsPage<Event> getArtistsMusicEvents(long id,  ResultsPage<Event>.GetResultsDelegate getResultsDelegate)
        {
            // http://api.songkick.com/api/3.0/artists/{artist_id}/calendar.json?apikey={your_api_key}

            var uriSB = new StringBuilder (DefaultServiceUri);
            uriSB.Append (@"artists/");
            uriSB.Append (id.ToString());
            uriSB.Append (@"/calendar.json?apikey=");
            uriSB.Append (this.APIKey);

            string replyString = Downloader.download(uriSB.ToString());
            var resultsPage = new ResultsPage<Event>(replyString, getResultsDelegate);

            return resultsPage;

        }

        public ResultsPage<Event> getLocationMusicEvents(long id,  ResultsPage<Event>.GetResultsDelegate getResultsDelegate)
        {
            // http://api.songkick.com/api/3.0/metro_areas/{metro_area_id}/calendar.json?apikey={your_api_key}

            var uriSB = new StringBuilder (DefaultServiceUri);
            uriSB.Append (@"metro_areas/");
            uriSB.Append (id.ToString());
            uriSB.Append (@"/calendar.json?apikey=");
            uriSB.Append (this.APIKey);

            string replyString = Downloader.download(uriSB.ToString());
            var resultsPage = new ResultsPage<Event>(replyString, getResultsDelegate);

            return resultsPage;

        }

        public ResultsPage<Artist> findArtists(string artist, ResultsPage<Artist>.GetResultsDelegate getResultsDelegate)
        {
            //http://api.songkick.com/api/3.0/search/artists.json?query={search_query}&apikey={your_api_key}

            var uriSB = new StringBuilder (DefaultServiceUri);
            uriSB.Append (@"search/artists.json?query=");
            uriSB.Append (System.Uri.EscapeDataString(artist));
            uriSB.Append (@"&apikey=");
            uriSB.Append (this.APIKey);

            string replyString = Downloader.download(uriSB.ToString());
            var resultsPage = new ResultsPage<Artist>(replyString, getResultsDelegate);

            return resultsPage;

        }

        public ResultsPage<Location> findLocation(string location, ResultsPage<Location>.GetResultsDelegate getResultsDelegate)
        {
            // http://api.songkick.com/api/3.0/search/locations.json?query={search_query}&apikey={your_api_key}

            var uriSB = new StringBuilder (DefaultServiceUri);
            uriSB.Append (@"search/locations.json?query=");
            uriSB.Append (System.Uri.EscapeDataString(location));
            uriSB.Append (@"&apikey=");
            uriSB.Append (this.APIKey);

            string replyString = Downloader.download(uriSB.ToString());
            var resultsPage = new ResultsPage<Location>(replyString, getResultsDelegate);

            return resultsPage;

        }

        public ResultsPage<Location> findLocationBasedOnIP(ResultsPage<Location>.GetResultsDelegate getResultsDelegate)
        {
            // http://api.songkick.com/api/3.0/search/locations.json?location=clientip&apikey={your_api_key}
            // i.e. 
            // http://api.songkick.com/api/3.0/search/locations.json?location=clientip&apikey=Qjqhc2hkfU3BaTx6

            var uriSB = new StringBuilder (DefaultServiceUri);
            uriSB.Append (@"search/locations.json?location=clientip&apikey=");
            uriSB.Append (this.APIKey);

            string replyString = Downloader.download(uriSB.ToString());
            var resultsPage = new ResultsPage<Location>(replyString, getResultsDelegate);

            return resultsPage;
        }



        // invalid API Key uri e.g:
        // http://api.songkick.com/api/3.0/metro_areas/24426/calendar.json?apikey=Qjqhc2hkfU3BaTx600
    }
}

