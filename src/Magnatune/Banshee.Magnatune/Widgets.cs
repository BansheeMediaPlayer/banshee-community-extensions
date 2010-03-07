/*
Magnatune Plugin for Banshee. Widgets for Gtk based upon Last.fm
widgets (from LastFmSourceContents).

Copyright 2008 Max Battcher <me@worldmaker.net>.

Permission is hereby granted, free of charge, to any person obtaining
a copy of this software and associated documentation files (the
"Software"), to deal in the Software without restriction, including
without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to
permit persons to whom the Software is furnished to do so, subject to
the following conditions:

The above copyright notice and this permission notice shall be
included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using Banshee.Streaming;
using Banshee.Widgets;
using Hyena;
using Gtk;
using System;
using System.Collections.Generic;

namespace Banshee.Magnatune
{
    public class TitledList : VBox
    {
        private Label title;
        private TileView tile_view;

        private Dictionary<string, Genre> genre_map;

        public TitledList (string title_str) : base()
        {
            genre_map = new Dictionary<string, Genre> ();
            title = new Label ();
            title.Xalign = 0;
            title.Ellipsize = Pango.EllipsizeMode.End;
            title.Markup = String.Format ("<b>{0}</b>", GLib.Markup.EscapeText (title_str));

            PackStart (title, false, false, 0);
            title.Show ();

            StyleSet += delegate {
                title.ModifyBg (StateType.Normal, Style.Base (StateType.Normal));
                title.ModifyFg (StateType.Normal, Style.Text (StateType.Normal));
            };

            tile_view = new TileView (2);
            PackStart (tile_view, true, true, 0);
            tile_view.Show ();

            StyleSet += delegate {
                tile_view.ModifyBg (StateType.Normal, Style.Base (StateType.Normal));
                tile_view.ModifyFg (StateType.Normal, Style.Base (StateType.Normal));
            };
        }

        public void SetList ()
        {
            List<Genre> genres = RadioSource.GetGenres ();
            genre_map.Clear ();
            tile_view.ClearWidgets ();
            foreach (Genre genre in genres) {
                MenuTile tile = new MenuTile ();
                tile.PrimaryText = genre.Title;
                genre_map.Add (genre.Title, genre);
                tile.SecondaryText = genre.Description;
                tile.ButtonPressEvent += PlayGenre;
                tile_view.AddWidget (tile);
            }
            tile_view.ShowAll ();
        }

        private void PlayGenre (object sender, ButtonPressEventArgs args)
        {
            MenuTile tile = sender as MenuTile;
            Genre g = genre_map[tile.PrimaryText];
            string type = RadioSource.MembershipTypeSchema.Get ();
            RadioTrackInfo rti;
            if (type != "") {
                string user = RadioSource.UsernameSchema.Get ();
                string pass = RadioSource.PasswordSchema.Get ();
                rti = new RadioTrackInfo (g.GetM3uUri (type, user, pass));
            } else {
                rti = new RadioTrackInfo (g.GetM3uUri ());
            }
            Log.Debug (string.Format ("Tuning Magnatune to {0}", g.GetM3uUri ()), null);
            rti.Play ();
        }
    }
}
