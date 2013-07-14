//
// DownloadJobs.cs
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
using Hyena.Jobs;
using Banshee.SongKick.Network;
using Hyena;
using Banshee.SongKick.Recommendations;
using System.Text;

namespace Banshee.SongKick.Network
{
    public class DownloadJob<T> : SimpleAsyncJob where T : Result
    {
        protected string Uri { get; set; }
        protected ResultsPage<T>.GetResultsDelegate GetResultsDelegate { get; set; }
        public string DefaultServiceUri {
            get { return @"http://api.songkick.com/api/3.0/"; }
        }

        protected DownloadJob ()
        {
        }

        public DownloadJob (string uri, ResultsPage<T>.GetResultsDelegate getResultsDelegate)
        {
            this.Uri = uri;
            this.GetResultsDelegate = getResultsDelegate;
        }

        protected override void Run ()
        {
            string replyString = Downloader.download(Uri);

            //var reply = new ResultsPage(replyString, (Object o) => {return null;});
            var resultsPage = new ResultsPage<T>(replyString, GetResultsDelegate);

            Log.Debug("SongKick: Recieved server's reply: " 
                      + replyString.Substring(0, Math.Min(replyString.Length, 30)) + "...");
            Log.Debug("SongKick: Parsed results page:" + resultsPage.ToString());
        }
    }

    public class MetroareaByIdDownloadJob : DownloadJob<Event>
    {
        public MetroareaByIdDownloadJob(long id, string apiKey, ResultsPage<Event>.GetResultsDelegate getResultsDelegate)
        {
            // example string for events in London:
            // http://api.songkick.com/api/3.0/metro_areas/24426/calendar.json?apikey=Qjqhc2hkfU3BaTx6
            var uriSB = new StringBuilder (DefaultServiceUri);
            uriSB.Append (@"metro_areas/");
            uriSB.Append (id.ToString());
            uriSB.Append (@"/calendar.json?apikey=");
            uriSB.Append (apiKey);

            this.Uri = uriSB.ToString ();
            this.GetResultsDelegate = getResultsDelegate;
        }
    }
}

