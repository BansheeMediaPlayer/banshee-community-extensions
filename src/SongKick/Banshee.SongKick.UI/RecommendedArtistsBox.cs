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
using System;
using Banshee.SongKick.Search;
using Gtk;
using Hyena;
using System.Collections.Generic;
using Banshee.SongKick.Recommendations;
using System.Linq;

namespace Banshee.SongKick.UI
{
    public class RecommendedArtistsBox : VBox
    {
        SearchView<RecommendationProvider.RecommendedArtist> recommendad_artist_search_view;

        private IList<RecommendationProvider.RecommendedArtist> recommended_artists;

        private Hyena.Data.MemoryListModel<RecommendationProvider.RecommendedArtist> recommended_artist_model = 
            new Hyena.Data.MemoryListModel<RecommendationProvider.RecommendedArtist>();

        public RecommendedArtistsBox ()
        {
            this.Spacing = 2;

            this.recommendad_artist_search_view = new SearchView<RecommendationProvider.RecommendedArtist> 
                (this.recommended_artist_model);

            this.PackStart (this.recommendad_artist_search_view, true, true, 2);
        }

        public IEnumerable<RecommendationProvider.RecommendedArtist> GetRecommendedArtists ()
        {
            var recommendationProvider = new Banshee.SongKick.Search.RecommendationProvider ();
            return recommendationProvider.getRecommendations ();
        }

        private void PresentRecommendedArtists ()
        {
            /*
            System.Threading.Thread thread = 
                new System.Threading.Thread(
                    new System.Threading.ThreadStart( 
                         () => new Banshee.SongKick.Search.RecommendationProvider ()
                         .getRecommendations()
                         .ToList<Banshee.SongKick.Search.RecommendationProvider.RecommendedArtist>()));
            thread.Start();
            */

            ReloadModel ();


            var recommendationProvider = new Banshee.SongKick.Search.RecommendationProvider ();
            recommendationProvider.getRecommendations ();

            ThreadAssist.ProxyToMain (delegate {
                recommended_artist_model.Reload ();
                recommendad_artist_search_view.OnUpdated ();
            });
        }

        public void LoadAndPresentRecommendations ()
        {
            ThreadAssist.SpawnFromMain (() => {
                recommended_artists = GetRecommendedArtists ().ToList();
                ReloadModel ();
                ThreadAssist.ProxyToMain (() => {
                    PresentRecommendedArtists ();
                });

                var processor = new RecommendationProcessor (FillAdditionalInfo);
                processor.EnqueueArtists (recommended_artists);
                processor.ProcessAll ();
            });
        }

        private void FillAdditionalInfo (RecommendationProvider.RecommendedArtist artist, 
            ResultsPage<Banshee.SongKick.Recommendations.Event> songKickFirstAtristEvents)
        {
            artist.NumberOfConcerts = 0;   

            if (songKickFirstAtristEvents.IsStatusOk) {
                artist.NumberOfConcerts = songKickFirstAtristEvents.results.Count;
            }

            ThreadAssist.ProxyToMain (() => {
                    recommended_artist_model.Reload ();
                    recommendad_artist_search_view.OnUpdated ();  
                });
        }

        private void ReloadModel ()
        {
            recommended_artist_model.Clear ();
            foreach (var artist in recommended_artists) {
                recommended_artist_model.Add (artist);
            }
        }
    }
}

