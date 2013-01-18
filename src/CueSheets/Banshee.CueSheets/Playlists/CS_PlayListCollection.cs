using System;
using System.Collections.Generic;

namespace Banshee.CueSheets
{
	public class CS_PlayListCollection : List<CS_PlayList>
	{
		private CS_TrackInfoDb		_db;
		private CueSheetCollection 	_csc;
		
		public CS_PlayListCollection (CS_TrackInfoDb db,CueSheetCollection csc) {
			_csc=csc;
			_db=db;
		}
		
		public void Load() {
			Reload ();			
		}
		
		public void Reload() {
			base.Clear ();
			List<string> keys=_db.getKeysStartingWith("playlist:");
			foreach (string key in keys) {
				Hyena.Log.Information ("adding key"+key);
				string name=key.Substring(9);
				CS_PlayList pl=new CS_PlayList(_db,name,_csc);
				Add (pl);
			}
			base.Sort (new CS_PlayList.Comparer());
		}
		
		public CS_PlayList NewPlayList(string name) {
			CS_PlayList pls=new CS_PlayList(_db,name,_csc);
			pls.Save ();
			return pls;
		}
	}
}

