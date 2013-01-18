using System;
using Banshee.Collection.Gui;
using Hyena.Data.Gui;
using System.Collections.Generic;

namespace Banshee.CueSheets
{
	public class CS_PlayListView : TrackFilterListView<CueSheetEntry>
	{
        protected CS_PlayListView (IntPtr ptr) : base () {}
		
        public CS_PlayListView () : base ()
        {
            column_controller.Add (new Column ("Entry", new ColumnCellText ("EntryName", true), 1.0));
            ColumnController = column_controller;
			base.ForceDragDestSet=true;
        }
		
		protected override bool OnPopupMenu() {
			return false;
		}
		
		protected override bool OnDragDrop(Gdk.DragContext drg,int x,int y,uint time) {
			Gtk.Widget w=Gtk.Drag.GetSourceWidget (drg);
			if (w is CS_TrackListView) {
				CS_TrackListView v=(CS_TrackListView) w;
				CS_PlayListModel model=(CS_PlayListModel) this.Model;
				CS_PlayList pls=model.PlayList;
				if (pls!=null) {
					List<CueSheetEntry> l=v.DragData;
					foreach (CueSheetEntry e in l) {
						pls.Add (e);
					}
					model.Reload ();
					pls.Save ();
				}
			}
			return false;
		}
		
		
	}
}

