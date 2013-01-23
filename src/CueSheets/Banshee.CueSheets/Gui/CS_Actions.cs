using System;
using Banshee.I18n;

namespace Banshee.CueSheets
{
	public class CS_Actions : Banshee.Gui.BansheeActionGroup
	{
		private CueSheetsSource _src;
		
		public CS_Actions (CueSheetsSource src) : base ("CueSheets") {
			
			_src=src;
			
			/*base.Add (
				new Gtk.ActionEntry ("Playlists", Gtk.Stock.MediaPlay,
			                	 Catalog.GetString ("Playlists"), 
			                	 null, null, 
			                	 (o, a) => { _src.HandlePlayList(); }
				)
			);*/
			
            base.AddImportant (
                new Gtk.ActionEntry ("Synchronize", Gtk.Stock.Refresh, 
			                	 Catalog.GetString ("Refresh CueSheets"), 
			                	 null, null, 
			                	 (o, a) => { _src.RefreshCueSheets(); }
                )
			);
			
            AddUiFromFile ("GlobalUI.xml");

            Register ();
		}
	}
}

