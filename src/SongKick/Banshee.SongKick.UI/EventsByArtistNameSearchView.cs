//
// EventSearchView.cs
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
using Hyena.Data;
using Banshee.SongKick.Network;
using Hyena;

namespace Banshee.SongKick.UI
{
    public class EventsByArtistNameSearchView : SearchView<Event>
    {
        public EventsByArtistNameSearchView (MemoryListModel<Event> model)
            : base(model)
        {
            //TODO: delete
            //DownloadAndUpdate("24426");
            /*
            string query = "24426"; // sample query
            System.Threading.Thread thread = 
                new System.Threading.Thread(new System.Threading.ThreadStart( () => DownloadAndUpdate(query)  ));
            thread.Start();
            */
            //model.Add (new Artist(-1, "Test Artist"));
        }

        
        private void DownloadAndUpdate(string query)
        {
            //new MetroareaByIdDownloadJob (24426, SongKickCore.APIKey, Events.GetMusicEventListResultsDelegate);

            var downloader = new SongKickDownloader (SongKickCore.APIKey);

            var resultPage = downloader.getMetroareaMusicEvents (query, Banshee.SongKick.Recommendations.Events.GetMusicEventListResultsDelegate);
            Events events = resultPage.results as Events;

            foreach (var musicEvent in events)
            {
                model.Add(musicEvent);
            }

            ThreadAssist.ProxyToMain (delegate {
                model.Reload ();
                OnUpdated ();
            });
        }
    }
}

