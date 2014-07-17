//
// RecommendedArtistsBox.cs
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

using System.Linq;
using System.Collections.Generic;

using Gtk;

using Hyena.Data.Gui;
using Hyena;

using Banshee.SongKick.Search;
using Banshee.SongKick.Recommendations;

namespace Banshee.SongKick.UI
{
    public class RecommendedArtistsBox : VBox
    {
        private SearchView<RecommendedArtist> recommended_artist_search_view;

        private IList<RecommendedArtist> recommended_artists;
        private readonly SortableMemoryListModel<RecommendedArtist> recommended_artist_model =
            new SortableMemoryListModel<RecommendedArtist> ();

        public event RowActivatedHandler<RecommendedArtist> RowActivated {
            add { recommended_artist_search_view.RowActivated += value; }
            remove { recommended_artist_search_view.RowActivated -= value; }
        }

        public RecommendedArtistsBox ()
        {
            this.Spacing = 2;

            this.recommended_artist_search_view = 
                new SearchView<RecommendedArtist> (this.recommended_artist_model);

            this.PackStart (this.recommended_artist_search_view, true, true, 2);
        }

        public IEnumerable<RecommendedArtist> GetRecommendedArtists ()
        {
            return new RecommendationProvider ().GetRecommendations ();
        }

        private void PresentRecommendedArtists ()
        {
            PopulateModel ();

            ThreadAssist.ProxyToMain (() => {
                recommended_artist_model.Reload ();
                recommended_artist_search_view.OnUpdated ();
            });
        }

        public void LoadAndPresentRecommendations ()
        {
            ThreadAssist.SpawnFromMain (() => {
                recommended_artists = GetRecommendedArtists ().ToList();
                ThreadAssist.ProxyToMain (PresentRecommendedArtists);

                var processor = new RecommendationProcessor (FillAdditionalInfo);
                processor.EnqueueArtists (recommended_artists);
                processor.ProcessAll ();
            });
        }

        private void FillAdditionalInfo (RecommendedArtist artist, 
            ResultsPage<Banshee.SongKick.Recommendations.Event> songKickFirstArtistEvents)
        {
            artist.NumberOfConcerts = 0;

            if (songKickFirstArtistEvents.IsStatusOk) {
                artist.NumberOfConcerts = songKickFirstArtistEvents.results.Count;
            }

            ThreadAssist.ProxyToMain (() => {
                recommended_artist_model.Reload ();
                recommended_artist_search_view.OnUpdated ();
            });
        }

        private void PopulateModel ()
        {
            recommended_artist_model.Clear ();
            foreach (var artist in recommended_artists) {
                recommended_artist_model.Add (artist);
            }
        }
    }
}

