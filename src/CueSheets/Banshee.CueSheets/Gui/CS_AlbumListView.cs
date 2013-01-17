using System;
using Banshee.Collection.Gui;

namespace Banshee.CueSheets
{
	public class CS_AlbumListView : AlbumListView
	{
		private CueSheetsView _view;
		private bool       _gridEnabled=true;
		
		public CS_AlbumListView(CueSheetsView view) : base() {
			_view=view;
			_gridEnabled=base.GetAlbumGrid();
			Hyena.Log.Information ("grid enabled="+_gridEnabled);
			EnableGrid ();
		}
		
		public void DisableGrid() {
			if (_gridEnabled) {
				_gridEnabled=false;
				base.SetAlbumGrid (true);
			}
		}
		
		public void EnableGrid() {
			if (!_gridEnabled) {	
				_gridEnabled=true;
				base.SetAlbumGrid (false);
			}
		}
		
		protected override bool OnPopupMenu () {
			Gtk.Menu mnu=new Gtk.Menu();
			
			Gtk.ImageMenuItem play=new Gtk.ImageMenuItem(Gtk.Stock.MediaPlay,null);
			play.Activated+=delegate(object sender,EventArgs a) {
				_view.PlayAlbum((CS_AlbumInfo) this.Model[Selection.FirstIndex]);
			};

			Gtk.ImageMenuItem edit=new Gtk.ImageMenuItem(Gtk.Stock.Edit,null);
			edit.Activated+=delegate(object sender,EventArgs a) {
				_view.EditSheet(((CS_AlbumInfo) this.Model[Selection.FirstIndex]).getSheet ());
			};
			
			Gtk.ImageMenuItem show_file=new Gtk.ImageMenuItem("Show in filesystem");
			show_file.Image=new Gtk.Image(Gtk.Stock.Directory,Gtk.IconSize.Menu);
			show_file.Activated+=delegate(object sender, EventArgs a) {
				_view.OpenContainingFolder((CS_AlbumInfo) this.Model[Selection.FirstIndex]);
			};
			
			mnu.Append (play);
			mnu.Append (new Gtk.SeparatorMenuItem());
			mnu.Append (edit);
			mnu.Append (show_file);
			
			CueSheet s=((CS_AlbumInfo) this.Model[Selection.FirstIndex]).getSheet ();
			if (Mp3Split.DllPresent()) {
				if (Mp3Split.IsSupported(s.musicFileName ())) {
					Gtk.ImageMenuItem split=new Gtk.ImageMenuItem("Split & Write to location");
					split.Image=new Gtk.Image(Gtk.Stock.Convert,Gtk.IconSize.Menu);
					split.Activated+=delegate(object sender,EventArgs a) {
						_view.MusicFileToDevice(((CS_AlbumInfo) this.Model[Selection.FirstIndex]).getSheet ());
					};
					mnu.Append (split);
				}
			}
			
			mnu.ShowAll ();
			mnu.Popup();
			
			return false;
		}
	}	
}

