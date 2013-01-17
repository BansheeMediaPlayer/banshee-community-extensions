using System;
using Banshee.Collection.Gui;
using Hyena.Data.Gui;

namespace Banshee.CueSheets
{
	public class CS_ComposerListView : TrackFilterListView<CS_ComposerInfo>
	{
		public CS_ComposerListView ()  : base ()
        {
            column_controller.Add (new Column ("Composer", new ColumnCellText ("DisplayName", true), 1.0));
            ColumnController = column_controller;
		}
		
		protected override bool OnPopupMenu() {
			return false;
		}
	}
}

