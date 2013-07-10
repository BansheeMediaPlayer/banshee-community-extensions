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

namespace Banshee.SongKick.UI
{
    public class SongKickSourceContents : Hyena.Widgets.ScrolledWindow, ISourceContents
    {
        SongKickSource source;

        private Viewport viewport;
        private HBox main_box;
        private Widget menu_box;
        private Widget contents_box;

        private SearchEntry search_entry;

        public SongKickSourceContents ()
        {
            //HscrollbarPolicy = PolicyType.Never;
            //VscrollbarPolicy = PolicyType.Automatic;

            viewport = new Viewport ();
            viewport.ShadowType = ShadowType.None;

            main_box = new HBox () { Spacing = 6, BorderWidth = 5, ReallocateRedraws = true };

            menu_box = BuildTiles();
            contents_box = BuildContents ();

            main_box.PackStart (menu_box, false, false, 0);
            main_box.PackStart (contents_box, true, true, 0);

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

            AddWithFrame (viewport);
            ShowAll ();
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

            var menu_items = new string [] {
                "Personal recommendations", 
                "Find music events by place",
                "Find music events by artist"
            };

            foreach (var item in menu_items) {
                var this_item = item;
                var tile = new ImageButton (this_item, null) {
                    InnerPadding = 4
                };
                tile.LabelWidget.Xalign = 0;
                //tile.Clicked += (o, a) => source.SetSearch (this_cat);

                vbox.PackStart (tile, false, false, 0);
            }

            return vbox;
        }

        Widget BuildContents ()
        {
            var vbox = new VBox () { Spacing = 2 };

            //var search_box = new HBox () { Spacing = 6, BorderWidth = 4 };
            //var label = new Label ("SongKick new UI works");

            search_entry = new SearchEntry () {
                WidthRequest = 150,
                Visible = true,
                EmptyMessage = "Type your query"
            };
            vbox.PackStart (search_entry, false, false, 2);

            return vbox;
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
    }
}
