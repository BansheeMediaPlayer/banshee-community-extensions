using System;

namespace Banshee.CueSheets
{
	public class CS_PlayListEntry
	{
		private CueSheetEntry _e;
		
		public CS_PlayListEntry (CueSheetEntry e) {
			_e=e;
			Hyena.Log.Information ("ple: construct entry="+_e);
		}
		
		public CueSheetEntry GetCueSheetEntry() {
			Hyena.Log.Information ("ple: entry");
			Hyena.Log.Information ("ple: entry="+_e);
			return _e;
		}
		
		public override string ToString() {
			return "ple: "+_e.ToString ();
		}
	}
}

