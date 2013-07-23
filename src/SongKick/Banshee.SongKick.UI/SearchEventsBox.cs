//
// SearchEventsBox.cs
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
using Gtk;
using Banshee.SongKick.Recommendations;
using Hyena;
using Banshee.SongKick.Search;

namespace Banshee.SongKick.UI
{
    public class SearchEventsBox : VBox
    {  
        private SearchView<Event> event_search_view;

        private SearchBar<Event> event_search_bar;

        private Hyena.Data.MemoryListModel<Event> event_model = 
            new Hyena.Data.MemoryListModel<Event>();

        public SearchEventsBox ()
        {
            this.Spacing = 2;

            // add search entry:
            this.event_search_bar = new SearchBar<Event> (presentEventSearch, new EventsByArtistSearch());
            this.PackStart (event_search_bar, false, false, 2);

            //add search results view:
            this.event_search_view = new SearchView<Event> (this.event_model);
            this.PackStart (event_search_view, true, true, 2);
        }

        public void Search(string query) {
            event_search_bar.Query = query;
            event_search_bar.PerformSearch ();
        }

        public void presentEventSearch (Search<Event> search)
        {
            Hyena.Log.Information (String.Format("SearchEventsBox: performing search: {0}", search.ToString()));

            event_model.Clear ();

            if (search.ResultsPage.IsWellFormed && search.ResultsPage.IsStatusOk) 
            {
                foreach (var result in search.ResultsPage.results) {
                    event_model.Add (result);
                }
            }

            ThreadAssist.ProxyToMain (delegate {
                event_model.Reload ();
                event_search_view.OnUpdated ();
            });
        }
    }
}

