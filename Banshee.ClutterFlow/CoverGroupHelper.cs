using System;
using System.Runtime.InteropServices;

using Clutter;
using Banshee.Gui;
using Banshee.ServiceStack;
using Banshee.Collection.Gui;

namespace Banshee.ClutterFlow
{
	
	public delegate void CoverEventHandler(CoverGroup cover, EventArgs e);
	
	public static class CoverGroupHelper
	{

		public static event CoverEventHandler NewCurrentCover;
		public static void InvokeNewCurrentCover(CoverGroup cover ) {
			if (NewCurrentCover!=null) {
				//Hyena.Log.Information("NewCurrentCover event is invoked with " + cover.Album.Title);
				NewCurrentCover(cover, EventArgs.Empty);
			}
		}
		
		private static Gdk.Pixbuf default_cover;
		public static Gdk.Pixbuf DefaultCover {
			get { return default_cover; }
		}
		
		private static bool is_setup = false;
		public static bool IsSetup {
			get { return is_setup; }
		}
		
		private static ArtworkManager artwork_manager;
		public static ArtworkManager GetArtworkManager {
			get { return artwork_manager; }
		}
		
		public static void Setup(int coverWidth) {
			if (!is_setup) {
				artwork_manager = ServiceManager.Get<ArtworkManager> ();
				default_cover = IconThemeUtils.LoadIcon (coverWidth, "media-optical", "browser-album-cover");
			}
				
			is_setup = true;
		}
		
		public static Gdk.Pixbuf Lookup(string artworkId, int size) {
			Gdk.Pixbuf pb = artwork_manager == null ? null 
                : artwork_manager.LookupScalePixbuf(artworkId, size);
			return pb ?? default_cover;
		}
	}
}
