// win.cs created with MonoDevelop
// User: lilith at 13:17 03/04/2008
//
// To change standard headers go to Edit->Preferences->Coding->Standard Headers
//

using System;
using Gtk;
using Gdk;
using GtkSharp;

using GConf;
using System.IO;
using Mono.Unix;

namespace Banshee.Plugins.Lyrics
{
public partial class LyricsWindow : Gtk.Window
{
	
	private const int WIDTH		= 400;
	private const int HEIGHT	= 435;
	public LyricsWindow() : base(Gtk.WindowType.Toplevel)
	{
		this.Build();
		InitComponents();
		this.Hide();			
	}
	
	private void InitComponents()
	{
		this.Resizable=true;
		this.HeightRequest=HEIGHT;
		this.WidthRequest=WIDTH;
		this.WindowPosition=WindowPosition.Center;
		
		this.DeleteEvent += delegate(object o, DeleteEventArgs args){
			OnClose(this, null);
			args.RetVal = true;
		};
		
		buttonClose.Clicked		   += new EventHandler(OnClose);
		LyricsPlugin.TextSaveEvent += new TextSaveEventHandler(OnLyricTextSave);
		
		lyricsBrowser.HideButtonArea();
	}
	
	public void Update ()
	{
		this.Title = BansheeWidgets.CurrentTrack.GetTitle();
		if (BansheeWidgets.CurrentTrack.GetArtist()!=null && !BansheeWidgets.CurrentTrack.GetArtist().Equals("") )
			this.Title +=  " " + Constants.lbl_artist + " " + BansheeWidgets.CurrentTrack.GetArtist();
		
		UpdateHeader();
		UpdateBrowser();
	}
	
	public void UpdateHeader()
	{
		string artist= BansheeWidgets.CurrentTrack.GetArtist();
		string title = BansheeWidgets.CurrentTrack.GetTitle();
		string album = BansheeWidgets.CurrentTrack.GetAlbum();
		Pixbuf cover = BansheeWidgets.GetCover();
		this.lyricsheader.Update(artist, title, album, cover);
	}
	
	public void UpdateBrowser()
	{
	  //launch lyric dowload
	  string artist= BansheeWidgets.CurrentTrack.GetArtist();
	  string title = BansheeWidgets.CurrentTrack.GetTitle();
	  
	  this.lyricsBrowser.Update(artist,title);	
	}
	
	public new void Show()
	{
		this.lyricsBrowser.SwitchTo(Constants.HTML_MODE);
		base.Show();
	}
	
	/*begin event handlers*/
	void OnClose(object sender, EventArgs args)
	{
		this.Hide();
	}
	
	void OnLyricTextSave (object sender, TextSaveEventArgs e)
	{
		this.Update();
	}
}
}
