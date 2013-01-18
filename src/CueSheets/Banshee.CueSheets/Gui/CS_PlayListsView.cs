using System;
using Banshee.Collection.Gui;
using Hyena.Data.Gui;
using Hyena.Collections;

namespace Banshee.CueSheets
{
	public class CS_PlayListsView : TrackFilterListView<CS_PlayList>
	{
        protected CS_PlayListsView (IntPtr ptr) : base () {}
		
		private CueSheetsView 				_view;
		
		
        public CS_PlayListsView (CueSheetsView view) : base ()
        {
			_view=view;
            column_controller.Add (new Column ("Playlist", new ColumnCellText ("PlsName", true), 1.0));
            ColumnController = column_controller;
        }
		
		protected override bool OnPopupMenu() {
			Gtk.Menu mnu=new Gtk.Menu();
			
			Gtk.ImageMenuItem play=new Gtk.ImageMenuItem(Gtk.Stock.MediaPlay,null);
			play.Activated+=delegate(object sender,EventArgs a) {
				_view.PlayPlayList((CS_PlayList) this.Model[Selection.FirstIndex]);
			};

			//Gtk.ImageMenuItem edit=new Gtk.ImageMenuItem(Gtk.Stock.Edit,null);
			//edit.Activated+=delegate(object sender,EventArgs a) {
			//	_view.EditPlayList(((CS_PlayList) this.Model[Selection.FirstIndex]).getSheet ());
			//};
			
			mnu.Append (play);
			mnu.Append (new Gtk.SeparatorMenuItem());
			//mnu.Append (edit);
			mnu.ShowAll ();
			mnu.Popup();
			
			return false;
		}
	}
}

