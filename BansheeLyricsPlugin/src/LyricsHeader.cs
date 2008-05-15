// LyricsHeader.cs created with MonoDevelop
// User: lilith at 13:40 03/04/2008
//
// To change standard headers go to Edit->Preferences->Coding->Standard Headers
//

using System;
using Gtk;
using Gdk;

namespace Banshee.Plugins.Lyrics
{
	
	
	public partial class LyricsHeader : Gtk.Bin
	{
		private const  int	 size 			= 40;
		private Pixbuf 		 pixbuf_image;
		private string 		 hex_blend;
		
		public LyricsHeader()
		{
			this.Build();
			
			imgArtist.SetSizeRequest(size, size);
			
			this.lblTitle.Ellipsize=Pango.EllipsizeMode.End;
			this.lblAlbum_Artist.Ellipsize=Pango.EllipsizeMode.End;
	
			Gdk.Color blend = ColorBlend(
            	this.lblAlbum_Artist.Style.Background(StateType.Normal),
            	this.lblAlbum_Artist.Style.Foreground(StateType.Normal));
            hex_blend = String.Format("#{0:x2}{1:x2}{2:x2}", blend.Red, blend.Green, blend.Blue);
			
			this.Hide();
		}
		public void Update (string artist, string title,string album, Pixbuf cover){
			if (Constants.current_mode == Constants.INSERT_MODE)
				return;
			
			this.Show();
			
			if(title == null || title == String.Empty){
				this.lblTitle.Markup	= "<b>"+Constants.unknown_title_string+"</b>";	
			}else{
				this.lblTitle.Markup	= "<b>"+title+"</b>";
			}
			
			if(artist==null || artist==String.Empty){
				artist	= Constants.unknown_artist_string;
			}
			
			if(album == null || album == String.Empty) {
				this.lblAlbum_Artist.Markup = String.Format(
                    "<span color=\"{0}\">{1}</span>  {2}",
                    hex_blend, 
                    GLib.Markup.EscapeText(Constants.lbl_artist),
                    GLib.Markup.EscapeText(artist));
            } else {
                this.lblAlbum_Artist.Markup = String.Format(
                    "<span color=\"{0}\">{1}</span>  {3}  <span color=\"{0}\">{2}</span>  {4}",
                    hex_blend, 
                    GLib.Markup.EscapeText(Constants.lbl_artist),
                    GLib.Markup.EscapeText(Constants.lbl_album),
                    GLib.Markup.EscapeText(artist), 
                    GLib.Markup.EscapeText(album));
            }
			pixbuf_image	= cover.ScaleSimple(size - 4, size - 4, InterpType.Bilinear);
			Pixbuf container = new Pixbuf(Gdk.Colorspace.Rgb, true, 8, size, size);
            Pixbuf container2 = new Pixbuf(Gdk.Colorspace.Rgb, true, 8, size - 2, size - 2);
            
            container.Fill(0x00000077);
            container2.Fill(0xffffff55);
            
            container2.CopyArea(0, 0, container2.Width, container2.Height, container, 1, 1);
            pixbuf_image.CopyArea(0, 0, pixbuf_image.Width, pixbuf_image.Height, container, 2, 2);            
			imgArtist.Pixbuf=container;
		}
		
		public static Gdk.Color ColorBlend(Gdk.Color a, Gdk.Color b)
        {
            // at some point, might be nice to allow any blend?
            double blend = 0.5;

            if(blend < 0.0 || blend > 1.0) {
                throw new ApplicationException("blend < 0.0 || blend > 1.0");
            }
            
            double blendRatio = 1.0 - blend;

            int aR = a.Red >> 8;
            int aG = a.Green >> 8;
            int aB = a.Blue >> 8;

            int bR = b.Red >> 8;
            int bG = b.Green >> 8;
            int bB = b.Blue >> 8;

            double mR = aR + bR;
            double mG = aG + bG;
            double mB = aB + bB;

            double blR = mR * blendRatio;
            double blG = mG * blendRatio;
            double blB = mB * blendRatio;

            Gdk.Color color = new Gdk.Color((byte)blR, (byte)blG, (byte)blB);
            Gdk.Colormap.System.AllocColor(ref color, true, true);
            return color;
        }
	}
}
