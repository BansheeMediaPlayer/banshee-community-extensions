using System;
using System.Collections.Generic;

namespace Banshee.CueSheets
{
	public class CueSheetCollection : List<CueSheet>
	{
		public CueSheetCollection () : base() {
		}
		
		public CueSheetEntry FindEntry(string cuesheet_id,string entry_id) {
			int i,N;
			//Hyena.Log.Information ("Finding "+cuesheet_id);
			for(i=0,N=base.Count;i<N && base[i].id ()!=cuesheet_id;i++);
			//Hyena.Log.Information ("i="+i+", N="+N);
			if (i==N) {
				return null;
			} else {
				CueSheet s=base[i];
				int k,M;
				for(k=0,M=s.nEntries ();k<M && s.entry (k).id ()!=entry_id;k++);
				if (k==M) {
					return null;
				} else {
					return s.entry (k);
				}
			}
		}
	}
}

