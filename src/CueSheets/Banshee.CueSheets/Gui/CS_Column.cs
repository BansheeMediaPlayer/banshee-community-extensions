using System;
using Hyena.Data.Gui;

namespace Banshee.CueSheets
{
	public class CS_Column : Column
	{
		private string _id;
		
		public CS_Column (string id,string t,ColumnCell c,double w) : base(t,c,w)
		{
			_id=id;
		}
		
		public string id() {
			return _id;
		}
		
		protected override void OnWidthChanged ()
		{
			Hyena.Log.Information ("OnWidthChanged: id="+id()+", width="+Width);
			base.OnWidthChanged ();
		}
		
	}
}

