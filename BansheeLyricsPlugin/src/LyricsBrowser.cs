// LyricsBrowser.cs created with MonoDevelop
// User: sgrang at 10:56 PM 4/2/2008
//
// To change standard headers go to Edit->Preferences->Coding->Standard Headers
//

using System;
using Gtk;

using System.Threading;

namespace Banshee.Plugins.Lyrics
{
	/*used to store current displayed track*/
	public class SavedTrackInfo
	{
		
		public SavedTrackInfo(){
		}
		public string			artist;
		public string			title;
		public string			saved_artist;
		public string			saved_title;
	}
	
	public partial class LyricsBrowser : Gtk.Bin
	{
		private 		HTML 				htmlBrowser;
		private 		TextView			textBrowser;
		private static	SavedTrackInfo		trackInfo;
		
		public LyricsBrowser()
		{
			this.Build();
			InitComponents();
		}
		
		private void InitComponents()
		{			
			trackInfo = new SavedTrackInfo();
			
			buttonSave.Clicked			+= new EventHandler(OnSave);
			LyricsPlugin.LyricEvent += new LyricEventHandler(OnLyricEvent);
			
			InitHtmlBrowser();
			InitTextBrowser();
			
			SwitchTo(Constants.HTML_MODE);
		}
		
		private void InitHtmlBrowser()
		{			
			htmlBrowser = new HTML();
			htmlBrowser.AllowSelection(true);
			htmlBrowser.Editable=false;
			
			htmlBrowser.LinkClicked 	 	  += new LinkClickedHandler(OnLinkClicked);
			htmlBrowser.ButtonPressEvent 	  += new ButtonPressEventHandler (OnButtonPress);
			
			this.lyricsScrollPane.Add((Widget)htmlBrowser);
			htmlBrowser.Show();
		}
		
		private void InitTextBrowser()
		{
			textBrowser=new TextView();
			textBrowser.WrapMode=(Gtk.WrapMode)(2);
		}

		public void SwitchTo(int mode)
		{
			if (mode == Constants.HTML_MODE)
			{
				Constants.current_mode=mode;
			
				dialog1_ActionArea.HideAll();
				buttonSave.Visible=false;
			
				this.lyricsScrollPane.Remove(this.lyricsScrollPane.Child);
				this.lyricsScrollPane.Add((Widget)htmlBrowser);
				this.htmlBrowser.ResizeChildren();
				this.lyricsScrollPane.ResizeChildren();
				textBrowser.Hide();
				htmlBrowser.Show();
			}else
			{
				Constants.current_mode	= mode;
				
				trackInfo.saved_artist	= trackInfo.artist;
				trackInfo.saved_title		= trackInfo.title;
				
				buttonSave.Visible=true;
				dialog1_ActionArea.ShowAll();
				
				this.lyricsScrollPane.Remove(this.lyricsScrollPane.Child);
				this.lyricsScrollPane.Add((Widget)textBrowser);
				
				this.textBrowser.GrabFocus();
				
				htmlBrowser.Hide();
				textBrowser.Show();
			}
		}
		
		public void Update(string artist, string title)
		{
				if (artist == null || title == null)
					return;
				
				if (trackInfo.artist==artist && trackInfo.title==title)
					return;
				
				trackInfo.artist	= artist;
				trackInfo.title		= title;
				
				LoadFromString(Constants.loading_string);
				
				Thread t = new Thread (new ParameterizedThreadStart (LyricsManager.Instance.GetLyrics));
				t.Start ((System.Object)new LyricParam(artist,title,null,null));
		}
		
		private void LoadFromString(string lyric)
		{
			HTMLStream html_stream = this.htmlBrowser.Begin ("text/html; charset=utf-8");
			html_stream.Write (lyric);
			this.htmlBrowser.End (html_stream, HTMLStreamStatus.Ok);
		}
		
		private void LoadFromString(object sender, System.EventArgs args)
		{
			string lyric = LyricsManager.Instance.Lyric;
			if (lyric==null)
				lyric = Constants.download_error_string + "<br><a href=\""+Constants.add_href_changed+"\">"+ Constants.add_lyric_string;
				
			LoadFromString(lyric);
		}
		
		public void HideButtonArea()
		{
			dialog1_ActionArea.HideAll();
			buttonSave.Visible=false;
		}
		
		/*event handlers*/
		public void OnLyricEvent(System.Object o , LyricEventArgs e)
		{
			Gtk.Application.Invoke(LoadFromString);
		}
		
		void OnLinkClicked (object obj, LinkClickedArgs args)
		{
			Console.WriteLine(args.Url);
			
			if(args.Url == Constants.add_href_changed){
				this.SwitchTo(Constants.INSERT_MODE);
			}else{
				Thread t = new Thread (new ParameterizedThreadStart (LyricsManager.Instance.GetLyrics));
				t.Start ((System.Object)new LyricParam(null,null,args.Url,null));
			}
		}

		public void OnRefresh(object sender, EventArgs args)
		{
				if (trackInfo.artist==null || trackInfo.title == null)
					return;

				LoadFromString(Constants.loading_string);					
				Thread t = new Thread (new ParameterizedThreadStart (LyricsManager.Instance.RefreshLyrics));
				t.Start ((System.Object)new LyricParam(trackInfo.artist,trackInfo.title,null,null));
		}
		
		void OnSave(object sender, EventArgs args)
		{
			string lyric	= this.textBrowser.Buffer.Text;
			this.textBrowser.Buffer.Text="";
			LyricsManager.Instance.AddLyrics(trackInfo.saved_artist,trackInfo.saved_title,lyric);
			SwitchTo(Constants.HTML_MODE);
			LyricsPlugin.OnTextSaveEvent(this,new TextSaveEventArgs(null));
		}
				
		void OnSelect(object sender, EventArgs args)
		{
			this.htmlBrowser.SelectAll();
		}
		
		void OnCopy(object sender, EventArgs args)
		{
			this.htmlBrowser.Copy();
		}
		
		void OnButtonPress(object sender,ButtonPressEventArgs args){
			if(args.Event.Button == 3) {
				//create
				Menu menu = new Menu();
										
				ImageMenuItem refreshItem	= new ImageMenuItem(Stock.Refresh,null);
				ImageMenuItem copyItem 		= new ImageMenuItem(Stock.Copy, null);
				ImageMenuItem selectAllItem	= new ImageMenuItem(Stock.SelectAll,null);
				
				//handle activate event
				copyItem.Activated 			+= new EventHandler (OnCopy);	
				selectAllItem.Activated += new EventHandler (OnSelect);
				refreshItem.Activated		+= new EventHandler (OnRefresh);
				
				//add item to the menu
				menu.Append(selectAllItem);
				menu.Append(copyItem);
				menu.Append(new SeparatorMenuItem());
				menu.Append(refreshItem);
				
				//show the menu
				menu.ShowAll();
				menu.Popup();				
			
			}
		}

		}

	public class LyricParam {
		public string artist;
		public string title;
		public string url;
		public string lyric;
		
		public LyricParam(string artist,string title,string url,string lyric){
			this.artist=artist;
			this.title=title;
			this.url=url;
			this.lyric=lyric;
		}
	}
}