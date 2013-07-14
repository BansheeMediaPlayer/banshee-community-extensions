//
// Downloader.cs
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
using System.Net;
using System.IO;
using System.Text;
using Banshee.SongKick.Recommendations;

namespace Banshee.SongKick.Network
{
    public static class Downloader
    {
        /**
         * method for synchronic download of data
         */
        public static string download(string uri)
        {
            // System.Net.WebException: Error: NameResolutionFailure
            // is rised in some cases with no apparent reason


            if (String.IsNullOrEmpty(uri))
            {
                throw new ArgumentException("Specify uri of resource you want to download");
            }

            string response;
            using (WebClient client = new WebClient ())
            {
                response = client.DownloadString(uri);
            }

            return response;

            // Another version:
            /*
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
            WebResponse response = request.GetResponse();
            return new StreamReader(response.GetResponseStream(), Encoding.UTF8).ReadToEnd();
            */

            // Another version:
            /*
            HttpWebResponse response = null;

            var request = (HttpWebRequest) WebRequest.Create (uri);                
            response = (HttpWebResponse) request.GetResponse ();
            
            if (response.StatusCode != HttpStatusCode.OK) {
                return null;
            }
            
            using (Stream stream = response.GetResponseStream ()) {
                using (StreamReader reader = new StreamReader (stream)) {
                    return reader.ReadToEnd ();
                }
            }
            */
        }
    }

    
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

        public ResultsPage<Event> getMetroareaMusicEvents(string query, ResultsPage<Event>.GetResultsDelegate getResultsDelegate)
        {
            var id = int.Parse (query);

            var uriSB = new StringBuilder (DefaultServiceUri);
            uriSB.Append (@"metro_areas/");
            uriSB.Append (id.ToString());
            uriSB.Append (@"/calendar.json?apikey=");
            uriSB.Append (this.APIKey);

            string replyString = Downloader.download(uriSB.ToString());
            var resultsPage = new ResultsPage<Event>(replyString, getResultsDelegate);

            return resultsPage;
        }
    }
}

