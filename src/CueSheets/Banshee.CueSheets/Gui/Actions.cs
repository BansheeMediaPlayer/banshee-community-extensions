using System;
using Banshee.I18n;

namespace Banshee.CueSheets
{
	public class Actions : Banshee.Gui.BansheeActionGroup
	{
		private CueSheetsSource _src;
		
		public Actions (CueSheetsSource src) : base ("CueSheets") {
			
			_src=src;
			
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

