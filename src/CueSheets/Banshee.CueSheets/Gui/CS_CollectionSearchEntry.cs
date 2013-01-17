using System;

namespace Banshee.CueSheets
{
	public class CS_CollectionSearchEntry : Gtk.Entry
	{
		private CueSheetsView _view;
		
		public CS_CollectionSearchEntry (CueSheetsView view) : base() {
			_view=view;
			Hyena.Log.Information (_view.ToString ());
			base.TextInserted+=delegate(object sender,Gtk.TextInsertedArgs args) {
				Console.WriteLine (base.Text);
			};
			base.TextDeleted+=delegate(object sender,Gtk.TextDeletedArgs args) {
				Console.WriteLine (base.Text);
			};
		}
		
	}
}

