using System;
using Banshee.Collection.Gui;

namespace Banshee.CueSheets
{
	public class CS_ArtistListView : ArtistListView {
		
		public CS_ArtistListView() : base() {
		}
		
		protected override bool OnPopupMenu() {
			return false;
		}
	}
}

