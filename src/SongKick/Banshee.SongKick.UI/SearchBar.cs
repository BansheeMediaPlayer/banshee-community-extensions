//
// SearchBar.cs
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
using Banshee.Widgets;
using Mono.Unix;
using Banshee.SongKick.Search;
using Banshee.SongKick.Recommendations;
using Banshee.SongKick.Network;
using Banshee.SongKick.LocationProvider;
using Hyena;

namespace Banshee.SongKick.UI
{
    public class SearchBar<T> : HBox, ICityNameObserver where T : IResult
    {
        protected SearchEntry search_entry;
        protected Button search_button;
        protected PresentSearch<T> present_search;
        public Search<T> Search { get; set; }
        public string QueryString { 
            get { return search_entry.Query; }
            set { search_entry.Query = value; }
        }

        public SearchBar (PresentSearch<T> presentSearch, Search<T> search)
        {
            present_search = presentSearch;
            Search = search;

            search_entry = new SearchEntry () {
                WidthRequest = 150,
                Visible = true,
                EmptyMessage = "Type your query"
            };

            search_entry.Activated += (o, a) => { search_button.Activate (); };

            if (this.GetType() == typeof(SearchBar<Location>)) {
                LocationProviderManager.Register (this);
            }

            PackStart (search_entry, true, true, 2);

            search_button = new Hyena.Widgets.ImageButton (Catalog.GetString ("_Search"), Stock.Find);
            search_button.Clicked += (o, a) => PerformSearch ();

            PackEnd (search_button, false, false, 2);
        }

        public void PerformSearch() {
            var query = new Banshee.SongKick.Search.Query (null, search_entry.Query);
            PerformSearch (query);
        }

        public void PerformSearch(Banshee.SongKick.Search.Query query) {
            ThreadAssist.ProxyToMain( () => 
                search_entry.Query = query.String);
            System.Threading.Thread thread = 
                new System.Threading.Thread(
                    new System.Threading.ThreadStart( 
                        () => {             
                            Search.GetResultsPage (query);
                            present_search (Search);
                        }));
            thread.Start();
        }

        public void OnCityNameUpdated (string cityName)
        {
            this.search_entry.Text = cityName;
        }
    }
}

