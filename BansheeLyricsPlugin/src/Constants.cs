// Constants.cs created with MonoDevelop
// User: sgrang at 18:25 02/09/2007
//
// To change standard headers go to Edit->Preferences->Coding->Standard Headers
//

using System;
using Mono.Unix;
using Banshee.Base;
using Banshee.MediaEngine;
using Gdk;
using Gtk;

namespace Banshee.Plugins.Lyrics{
	
	/*an adapter class to reuse existing code beetween 0.13.* and 1.0*/
	public class AdapterCurrentTrack{

		public AdapterCurrentTrack(){
		}
		
		public TrackInfo GetCurrentTrack(){
			return PlayerEngineCore.CurrentTrack;
		}
		
		public string GetTitle(){
			return PlayerEngineCore.CurrentTrack.Title;
		}
		
		public string GetArtist(){
			return PlayerEngineCore.CurrentTrack.Artist;
		}
		
		public string GetAlbum(){
			return PlayerEngineCore.CurrentTrack.Album;
		}
	}

	/*an adapter class to reuse existing code beetween 0.13.* and 1.0*/
	public class BansheeWidgets{
		
		private static AdapterCurrentTrack current_track = new AdapterCurrentTrack();		
		
		public static UIManager GetUiManager(){
			return Globals.ActionManager.UI;
		}
		
		public static AdapterCurrentTrack CurrentTrack{
			get{return current_track;}
		}
		
		public static Pixbuf GetCover(){
			Pixbuf pixbuf_image = null;
			string cover = PlayerEngineCore.CurrentTrack.CoverArtFileName;
			if(cover==null || cover==""){
				pixbuf_image = Banshee.Base.Branding.DefaultCoverArt;
			}else{
				pixbuf_image = new Pixbuf(cover);
			}
			return pixbuf_image;
		}

		public static NetworkDetect GetNetworkDetect(){
			return NetworkDetect.Instance;
		}
	}
	
	/*Projects constants*/
	public class Constants
	{
		/*gconf key*/
		public static string path_key				= "/apps/banshee/plugins/lyrics/lyrics_cache";
		
		/*default plugin dir*/
		public static string default_lyrics_dir		= "/.config/banshee/plugins/lyrics/";
	
		public static int 	 current_mode			= HTML_MODE;
		public static int	 HTML_MODE			    = 0;
		public static int	 INSERT_MODE		    = 1;
		
		public static string lyric_action			= "ShowLyricsAction";
		public static string find_error_string		= Catalog.GetString("Unable to find Lyrics : ");
		public static string download_error_string	= Catalog.GetString("Error downloading lyric...");
		public static string suggestion_error_string= Catalog.GetString("Error downloading suggestions...");
		public static string loading_string 		= Catalog.GetString("Loading lyric...");
		public static string no_network_string		= Catalog.GetString("You don't seem to be connected to internet.<br>Check your network connection.");
		public static string add_href_changed       = Catalog.GetString("add");
		public static string add_lyric_string		= Catalog.GetString("Click here to manually add a new lyric");
		public static string lbl_album				= Catalog.GetString("from");
		public static string lbl_artist				= Catalog.GetString("by");
		public static string show_lyrics			= Catalog.GetString("Show Lyrics");
		public static string plugin_description		= Catalog.GetString("A Banshee plugin that retrieves and displays lyrics");
		public static string unknown_artist_string  = Catalog.GetString("Unknown Artist");
		public static string unknown_title_string   = Catalog.GetString("Unknown Title");
	}
}
