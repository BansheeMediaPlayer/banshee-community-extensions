// Widgets.cs created with MonoDevelop
// User: worldmaker at 6:27 PM 6/10/2008
//
// Widgets based upon LastfmSourceContents

using Banshee.Widgets;
using Gtk;
using System;

namespace Magnatune
{
    public class TitledList : VBox
    {
        private Label title;
		private TileView tile_view;

        public TitledList (string title_str) : base ()
        {
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
			
			tile_view = new TileView(1);
			PackStart(tile_view, true, true, 0);
			tile_view.Show();
			
			StyleSet += delegate {
				tile_view.ModifyBg(StateType.Normal, Style.Base(StateType.Normal));
				tile_view.ModifyFg(StateType.Normal, Style.Base(StateType.Normal));
			};
        }
		
		public void SetList()
		{
			tile_view.ClearWidgets();
			MenuTile tile = new MenuTile();
			tile.PrimaryText = "Ambient";
			tile.SecondaryText = "space, drone, loops: find a new reality";
			tile.ButtonPressEvent += TestAmbient;
			tile_view.AddWidget(tile);
			
			tile = new MenuTile();
			tile.PrimaryText = "Classical";
			tile.SecondaryText = "baroque, renaissance, medieval, contemporary, minimalism";
			tile.ButtonPressEvent += TestAmbient;
			tile_view.AddWidget(tile);
			
			tile = new MenuTile();
			tile.PrimaryText = "Electronica";
			tile.SecondaryText = "ambient, IDM, industrial, trance, goa, and 5 zillion more sub-genres";
			tile.ButtonPressEvent += TestAmbient;
			tile_view.AddWidget(tile);
			tile_view.ShowAll();
		}
		
		private void TestAmbient(object sender, EventArgs e)
		{
			Banshee.Base.SafeUri uri = new Banshee.Base.SafeUri("http://magnatune.com/genres/m3u/ambient.m3u");
			Banshee.Streaming.RadioTrackInfo rti = new Banshee.Streaming.RadioTrackInfo(uri);
			try
			{
				rti.Play();
			}
			catch (Exception ex)
			{
				Hyena.Log.Error("Exception: " + ex.ToString());
			}
		}
    }
}