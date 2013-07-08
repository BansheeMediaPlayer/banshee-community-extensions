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

namespace Banshee.SongKick.Network
{
    public class DownloadRecommendationsJob : SimpleAsyncJob
    {
        private string uri;
        private ResultsPage.GetResultsDelegate getResultsDelegate;

        public DownloadRecommendationsJob (string uri, ResultsPage.GetResultsDelegate getResultsDelegate)
        {
            this.uri = uri;
            this.getResultsDelegate = getResultsDelegate;
        }

        protected override void Run ()
        {
            string replyString = Downloader.download(uri);

            //var reply = new ResultsPage(replyString, (Object o) => {return null;});
            var resultsPage = new ResultsPage(replyString, getResultsDelegate);

            Log.Debug("SongKick: Recieved server's reply: " 
                      + replyString.Substring(0, Math.Min(replyString.Length, 30)) + "...");
            Log.Debug("SongKick: Parsed results page:" + resultsPage.ToString());
        }
    }
}

