using System;

namespace Banshee.CueSheets
{
	public class CS_Info
	{
		public CS_Info ()
		{
		}
		
		public static string Version() {
			return "0.0.6";
		}
		
		public static string [] Authors() {
			return new string[] {"Hans Oesterholt"};		
		}
		
		public static string Website() {
			return "http://oesterholt.net?env=data&page=banshee-cuesheets";
		}
		
		public static string Info() {
			return "CueSheets is an extension that allows you to play music from cuesheets in banshee";
		}
	}
}

