using System;
using Banshee.Collection.Gui;
using Hyena.Data.Gui;

namespace Banshee.CueSheets
{
	 
	public class ComposerListView : TrackFilterListView<CS_ComposerInfo>
	{
		public ComposerListView ()  : base ()
        {
            column_controller.Add (new Column ("Composer", new ColumnCellText ("DisplayName", true), 1.0));
            ColumnController = column_controller;
		}
	}
}

