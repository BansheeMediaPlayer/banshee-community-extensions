using System;
using Banshee.Collection;
using System.Collections.Generic;

namespace Banshee.CueSheets
{
	public class CS_TrackListModel :  BansheeListModel<CueSheetEntry> {
		
		private CueSheet 			_sheet;
		private List<CueSheetEntry> store;
		
        public CS_TrackListModel () {
			Selection=new Hyena.Collections.Selection();
			store=new List<CueSheetEntry>();
        }

        public override void Clear () {
			// does nothing 
        }
	
		public void SetSheet(CueSheet s) {
			_sheet=s;
		}

        public override void Reload () {
			store.Clear ();
			int i=0;
			for(i=0;i<_sheet.nEntries ();i++) {
				CueSheetEntry e=_sheet.entry (i);
				store.Add (e);
				/*double l=e.length ();
				int t=(int) (l*100.0);
				int m=t/(60*100);
				int secs=(t/100)%60;
				string ln=String.Format ("{0:00}:{0:00}",m,secs);
				store.AppendValues (i+1,e.title (),e.getPiece (),e.performer (),e.getComposer(),ln);*/
			}
			base.RaiseReloaded ();
        }
	
        public override int Count {
            get { 
				return store.Count;
			}
        }
	
		public override CueSheetEntry this[int index] {
			get {
				return store[index];
			} 	
		}

		/*
		public CS_TrackListModel () : base(typeof(int),
		                               typeof(string),
		                               typeof(string),
		                               typeof(string),
		                               typeof(string),
		                               typeof(string))
		{
			
		}*/

	}
}

