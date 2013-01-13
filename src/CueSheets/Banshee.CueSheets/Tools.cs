using System;
using System.Text.RegularExpressions;

namespace Banshee.CueSheets
{
	public class Tools
	{
		public Tools ()
		{
		}
		
		static public bool isUnixLike() {
			System.OperatingSystem os=Environment.OSVersion;
			PlatformID id=os.Platform;
			return id==PlatformID.Unix || id==PlatformID.MacOSX;
		}
		
		static public string basename(string filename) {
			if (isUnixLike ()) {
				return Regex.Replace (filename,"^([^/]*[/])+","");
			} else {
				return Regex.Replace (filename,"^([^\\]*[\\])+","");
			}
		}
		
		static public string makefile(string dir,string filename) {
			if (isUnixLike ()) {
				return dir+"/"+filename;
			} else {
				return dir+"\\"+filename;
			}
		}
		
		static public string firstpart(string path) {
			if (isUnixLike ()) {
				string d=Regex.Replace (path,"^[/]","");
				string r=Regex.Replace (d,"[/].*$","");
				return r;
			} else {
				string d=Regex.Replace (path,"^[\\]","");
				string r=Regex.Replace (d,"[\\].*$","");
				return r;
			}
		}
	}
}

