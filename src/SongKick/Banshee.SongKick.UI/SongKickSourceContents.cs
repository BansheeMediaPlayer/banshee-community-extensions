//
// SongKickSourceContents.cs
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
using Banshee.Sources.Gui;
using Hyena.Widgets;
using Gtk;
using Banshee.Widgets;
using Banshee.SongKick.Recommendations;
using Banshee.SongKick.Search;
using Hyena;
using System.Linq;
using System.Collections.Generic;

namespace Banshee.SongKick.UI
{
    public class SongKickSourceContents : Hyena.Widgets.ScrolledWindow, ISourceContents 
    {
        SongKickSource source;

        private Viewport viewport;
        private HBox main_box;
        private Widget menu_box;

        private Box contents_box;
        // contents_box has exacltly one child at a time:
        private SearchEventsBox search_by_artist_contents_box;
        private RecommendedArtistsBox recommendations_contents_box;

        SongKickViewInfo presonal_recommendation_view ;
        SongKickViewInfo search_by_artist_view ;

        public SongKickSourceContents ()
        {
            //HscrollbarPolicy = PolicyType.Never;
            //VscrollbarPolicy = PolicyType.Automatic;

            viewport = new Viewport ();
            viewport.ShadowType = ShadowType.None;

            main_box = new HBox () { Spacing = 6, BorderWidth = 5, ReallocateRedraws = true };


            search_by_artist_contents_box = new SearchEventsBox ();
            recommendations_contents_box = new RecommendedArtistsBox();
            recommendations_contents_box.RowActivated += OnRecommendedArtistRowActivate;

            menu_box = BuildTiles();

            main_box.PackStart (menu_box, false, false, 0);
            contents_box = new HBox ();
            main_box.PackStart (contents_box, true, true, 0);

            // set default contents box
            contents_box.PackStart (recommendations_contents_box, true, true, 0);

            // Clamp the width, preventing horizontal scrolling
            /*
            SizeAllocated += delegate (object o, SizeAllocatedArgs args) {
                // TODO '- 10' worked for Nereid, but not for Cubano; properly calculate the right width we should request
                main_box.WidthRequest = args.Allocation.Width - 30;
            };
            */

            viewport.Add (main_box);

            StyleSet += delegate {
                viewport.ModifyBg (StateType.Normal, Style.Base (StateType.Normal));
                viewport.ModifyFg (StateType.Normal, Style.Text (StateType.Normal));
            };

            recommendations_contents_box.LoadAndPresentRecommendations ();

            AddWithFrame (viewport);
            ShowAll ();
        }

        internal class SongKickViewInfo {
            internal string Name { get; set; }
            internal ImageButton Button { get; set; } 
            internal Box CorrespondingBox { get; set; } 

            internal SongKickViewInfo (string name, Box correspondingBox) {
                Name = name;
                CorrespondingBox = correspondingBox;
            }
        }

        public bool SetSource (Banshee.Sources.ISource source)
        {
            if (source == null) {
                return false;
            } else {
                this.source = source as SongKickSource;
                return true;
            }
        }

        private Widget BuildTiles ()
        {
            var vbox = new VBox () { Spacing = 12, BorderWidth = 4 };

            var titleLabel = new Label ("Menu:");

            vbox.PackStart (titleLabel, false, false, 0);

            this.presonal_recommendation_view = new SongKickViewInfo("Personal recommendations", recommendations_contents_box);
            this.search_by_artist_view = new SongKickViewInfo("Find music events by artist", search_by_artist_contents_box);

            var songkickViews = new SongKickViewInfo [] {
                presonal_recommendation_view,
                search_by_artist_view
            };

            foreach (var view in songkickViews) {
                var button = new ImageButton (view.Name, null) {
                    InnerPadding = 4
                };
                view.Button = button;
                button.LabelWidget.Xalign = 0;
                button.Clicked += (o, a) => this.SetView (view);

                vbox.PackStart (button, false, false, 0);
            }

            // add clickable SongKick logo:

            vbox.PackEnd (new SongKickLogo(), false, false, 0);

            return vbox;
        }

        void SetView (SongKickViewInfo view)
        {
            foreach (Widget w in contents_box.AllChildren) {
                contents_box.Remove (w);
            }

            contents_box.PackStart(view.CorrespondingBox, true, true, 0);
            ShowAll ();
        }

        public void ResetSource ()
        {
            source = null;
        }

        public Banshee.Sources.ISource Source {
            get { return source; }
        }

        public Gtk.Widget Widget {
            get { return this; }
        }

        public class SongKickLogo : EventBox {
            public SongKickLogo() {
                var songKickImage = new Image(Gdk.Pixbuf.LoadFromResource ("concerts_by_songkick.png"));
                this.Add (songKickImage);
                this.VisibleWindow = false;
                this.ButtonReleaseEvent += new ButtonReleaseEventHandler (OpenSongKickInBrowser);
            }

            static void OpenSongKickInBrowser (object obj, ButtonReleaseEventArgs args)
            {
                System.Diagnostics.Process.Start (@"http://songkick.com");
            }
        }

        protected void OnRecommendedArtistRowActivate (object o, Hyena.Data.Gui.RowActivatedArgs<RecommendationProvider.RecommendedArtist> args) {
            var recommendedArtist = args.RowValue;
            search_by_artist_contents_box.Search (recommendedArtist.Name);
            SetView (search_by_artist_view);
        }
        /*
        // Fetching results:
        protected void Reload ()
        {
            model.Clear ();
            Hyena.ThreadAssist.SpawnFromMain (delegate {
                ThreadedFetch (0);
            });
        }

        int total_results;
        string status_text;
        Banshee.SongKick.Search.Search search;

        private void ThreadedFetch (int page)
        {
            bool success = false;
            total_results = 0;
            status_text = "";
            Exception err = null;
            int old_page = search.Page;
            search.Page = page;

            Hyena.ThreadAssist.ProxyToMain (delegate {
                SetStatus (Catalog.GetString ("Searching the Internet Archive"), false, true, "gtk-find");
            });

            IA.SearchResults results = null;

            try {
                results = search.GetResults ();
                total_results = results.TotalResults;
            } catch (System.Net.WebException e) {
                Hyena.Log.Exception ("Error searching the SongKick", e);
                results = null;
                err = e;
            }

            if (results != null) {
                try {

                    foreach (var result in results) {
                        model.Add (result);

                        // HACK to remove ugly empty description
                        //if (track.Comment == "There is currently no description for this item.")
                        //track.Comment = null;
                    }

                    success = true;
                } catch (Exception e) {
                    err = e;
                    Hyena.Log.Exception ("Error searching the Internet Archive", e);
                }
            }

            if (success) {
                int count = model.Count;
                if (total_results == 0) {
                    Hyena.ThreadAssist.ProxyToMain (delegate {
                        songKickSource.SetStatus (Catalog.GetString ("No matches."), false, false, "gtk-info");
                    });
                } else {
                    Hyena.ThreadAssist.ProxyToMain (ClearMessages);
                    status_text = String.Format (Catalog.GetPluralString (
                        "Showing 1 match", "Showing 1 to {0:N0} of {1:N0} total matches", total_results),
                                                 count, total_results
                                                 );
                }
            } else {
                search.Page = old_page;
                ThreadAssist.ProxyToMain (delegate {
                    var web_e = err as System.Net.WebException;
                    if (web_e != null && web_e.Status == System.Net.WebExceptionStatus.Timeout) {
                        SetStatus (Catalog.GetString ("Timed out searching the Internet Archive"), true);
                        CurrentMessage.AddAction (new MessageAction (Catalog.GetString ("Try Again"), (o, a) => {
                            if (page == 0) Reload (); else FetchMore ();
                        }));
                    } else {
                        SetStatus (Catalog.GetString ("Error searching the Internet Archive"), true);
                    }
                });
            }

            ThreadAssist.ProxyToMain (delegate {
                model.Reload ();
                OnUpdated ();
            });
        }
        */
    }
}

