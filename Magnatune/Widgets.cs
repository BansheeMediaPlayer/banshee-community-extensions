// Widgets.cs created with MonoDevelop
// User: worldmaker at 6:27 PM 6/10/2008
//
// Widgets based upon LastfmSourceContents

using Banshee.Streaming;
using Banshee.Widgets;
using Gtk;
using System;
using System.Collections.Generic;

namespace Magnatune
{
    public class TitledList : VBox
    {
        private Label title;
		private TileView tile_view;
		
		private Dictionary<string, Genre> genre_map;

        public TitledList (string title_str) : base ()
        {
			genre_map = new Dictionary<string, Genre>();
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
			
			tile_view = new TileView(2);
			PackStart(tile_view, true, true, 0);
			tile_view.Show();
			
			StyleSet += delegate {
				tile_view.ModifyBg(StateType.Normal, Style.Base(StateType.Normal));
				tile_view.ModifyFg(StateType.Normal, Style.Base(StateType.Normal));
			};
        }
		
		public void SetList()
		{
			List<Genre> genres = RadioSource.GetGenres();
			genre_map.Clear();
			tile_view.ClearWidgets();
			foreach (Genre genre in genres)
			{
				MenuTile tile = new MenuTile();
				tile.PrimaryText = genre.Title;
				genre_map.Add(genre.Title, genre);
				tile.SecondaryText = genre.Description;
				tile.ButtonPressEvent += PlayGenre;
				tile_view.AddWidget(tile);
			}
			tile_view.ShowAll();
		}
		
		private void PlayGenre(object sender, ButtonPressEventArgs args)
		{
			MenuTile tile = sender as MenuTile;
			Genre g = genre_map[tile.PrimaryText];
			string type = RadioSource.MembershipTypeSchema.Get();
			RadioTrackInfo rti;
			if (type != "")
			{
				string user = RadioSource.UsernameSchema.Get();
				string pass = RadioSource.PasswordSchema.Get();
				rti = new RadioTrackInfo(g.GetM3uUri(type, user, pass));
			}
			else
			{
				rti = new RadioTrackInfo(g.GetM3uUri());
			}
			rti.Play();
		}
    }
}