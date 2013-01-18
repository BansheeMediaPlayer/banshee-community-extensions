using System;
using Banshee.Collection.Gui;
using Hyena.Data.Gui;
using Banshee.Collection;
using System.Collections.Generic;

namespace Banshee.CueSheets
{
	public class CS_TrackListView : TrackFilterListView<CueSheetEntry>
	{
        protected CS_TrackListView (IntPtr ptr) : base () {}
		
		private CueSheetsView _view;
		private CS_Column 		  _nr;
		private CS_Column	      _track;
		private CS_Column		  _piece;
		private CS_Column		  _artist;
		private CS_Column		  _composer;
		private CS_Column		  _length;
		
        public CS_TrackListView (CueSheetsView view) : base ()
        {
			_view=view;
			Hyena.Log.Information ("view="+_view);
			
			_nr=new CS_Column ("tracknr","Nr", new ColumnCellText ("TrackNumber", true),1.0);
            column_controller.Add (_nr);
			
			_track=new CS_Column ("track","Track", new ColumnCellText ("TrackTitle", true),1.0);
			column_controller.Add (_track);
			
			_piece=new CS_Column ("piece","Piece", new ColumnCellText ("Piece", true),1.0);
			column_controller.Add (_piece);
			
			_artist=new CS_Column ("artist","Artist", new ColumnCellText ("ArtistName", true),1.0);
			column_controller.Add (_artist);

			_composer=new CS_Column ("composer","Composer", new ColumnCellText ("Composer", true),1.0);
			column_controller.Add (_composer);

			_length=new CS_Column ("length","Length", new ColumnCellText ("Length", true),1.0);
			column_controller.Add (_length);

            ColumnController = column_controller;
			base.HeaderVisible=true;
			
        }
			                         
		protected override bool OnPopupMenu() {
			return false;
		}
		
		private List<CueSheetEntry> _dragentry=new List<CueSheetEntry>();
		public List<CueSheetEntry> DragData {
			get { return _dragentry; }
			set { _dragentry=value; }
		}
		
		protected override void OnDragDataGet (Gdk.DragContext context, Gtk.SelectionData selection_data, uint info, uint time_)
		{
			Console.WriteLine ("getting data?");
			//TrackListModel model=_view.GetSource().TrackModel;
			CS_TrackListModel model=(CS_TrackListModel) this.Model;
            System.Text.StringBuilder sb = new System.Text.StringBuilder ();
			List<CueSheetEntry> l=new List<CueSheetEntry>();
			
            foreach (TrackInfo track in model.SelectedItems) {
				CueSheetEntry e=(CueSheetEntry) track;
				l.Add (e);	
				Console.WriteLine ("id="+e.id());
				//string id=e.id ();
                //sb.Append (id);
				//sb.Append ("#@#");
            }
			byte [] data = System.Text.Encoding.UTF8.GetBytes (sb.ToString ());
			selection_data.Set (context.Targets[0], 8, data, data.Length);
			Console.WriteLine(sb.ToString ());
			//context.Data.Add ("tracks",sb.ToString ());
			DragData=l;
		}
	}
}

