//  
// Author:
//   Christian Martellini <christian.martellini@gmail.com>
//
// Copyright (C) 2009 Christian Martellini
// 
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// 

using Gtk;
using System;
using System.Text;

using Mono.Unix;
using System.Threading;

using Banshee.ServiceStack;

namespace Banshee.Lyrics.Gui
{
    public delegate void ChangeModeEventHandler (object o, ChangeModeEventArgs e);
    
    public class ChangeModeEventArgs:EventArgs
    {
        public readonly int mode;

        public ChangeModeEventArgs (int mode)
        {
            this.mode = mode;
        }
    }
    
    public partial class LyricsBrowser : Gtk.Bin
    {
        public event ChangeModeEventHandler ChangeModeEvent;
        public static int HTML_MODE = 0;
        public static int INSERT_MODE = 1;
        
        private string current_artist;
        private string current_title;

		private bool enableEditMode = true;
        private string browser_content;
        
        public LyricsBrowser (bool enableEditMode) : this()
        {
            this.enableEditMode = enableEditMode;
            this.RemoveShadow ();
        }
		
        public LyricsBrowser ()
        {
            this.Build ();
            
            LyricsManager.Instance.LoadingLyricEvent += OnLoading;
            LyricsManager.Instance.LyricChangedEvent += OnLyricChanged;
            
            this.htmlBrowser.LinkClicked += new LinkClickedHandler (OnLinkClicked);
            this.htmlBrowser.ButtonPressEvent += new ButtonPressEventHandler (OnButtonPress);
            SwitchTo (LyricsBrowser.HTML_MODE);
        }
		
        /* Show the browser in HTML mode or the textArea in INSERT mode */
        public void SwitchTo (int mode)
        {
            if (mode == LyricsBrowser.INSERT_MODE && !this.enableEditMode) {
                return;
            }
			
            this.lyricsScrollPane.Remove (this.lyricsScrollPane.Child);
            
            if (mode == LyricsBrowser.HTML_MODE) {
                this.lyricsScrollPane.Add (this.htmlBrowser);
            } else {
				/* save current track information */
                this.current_artist = ServiceManager.PlayerEngine.CurrentTrack.ArtistName;
                this.current_title = ServiceManager.PlayerEngine.CurrentTrack.TrackTitle;

                this.lyricsScrollPane.Add (this.textBrowser);
                this.textBrowser.Buffer.Text = "";
                this.textBrowser.GrabFocus ();
            }
			
            this.lyricsScrollPane.ResizeChildren ();
            this.lyricsScrollPane.ShowAll ();
            
            if (this.ChangeModeEvent != null) {
                this.ChangeModeEvent (this, new ChangeModeEventArgs (mode));
            }
        }
        
        public void OnLyricChanged (object o, LyricEventArgs args)
        {
            if (args.error != null) {
                this.browser_content = args.error;
            } else if (args.suggestion != null) {
                this.browser_content = GetSuggestionString (args.suggestion);
            } else if (args.lyric != null) {
                this.browser_content = args.lyric;
            } else {
                this.browser_content = Catalog.GetString("No lyric found.");
            }
            
            Gtk.Application.Invoke (LoadString);
        }
        
        private string GetSuggestionString (string lyric_suggestion)
        {
            StringBuilder sb = new StringBuilder ();
            sb.Append ("<b>" + Catalog.GetString ("No lyric found.") + "</b>");
            if (enableEditMode) {
				sb.Append ("<br><a href=\"" + Catalog.GetString ("add") + "\">");
				sb.Append (Catalog.GetString ("Click here to manually add a new lyric"));
				sb.Append ("</a>");
            }
            sb.Append ("<br><br>");
            sb.Append (Catalog.GetString ("Suggestions:"));
            sb.Append (lyric_suggestion);
            
            return sb.ToString ();
        }
        
        private void OnLoading (object o, EventArgs args)
        {
            this.browser_content = "<b>" + Catalog.GetString ("Loading...") + "</b>";
            Gtk.Application.Invoke (LoadString);
        }
        
        public string GetText () {
            return textBrowser.Buffer.Text;
        }
        
        /*prevent multi threading problems */
        private void LoadString (object o, EventArgs args)
        {
            this.browser_content = this.browser_content == null ? "" : this.browser_content;
            LoadString (browser_content);
        }
        
        public void LoadString (string str)
        {
        	if (str == null) {
        		str = "";
        	}
            HTMLStream html_stream = this.htmlBrowser.Begin ("text/html; charset=utf-8");
            html_stream.Write (str);
            this.htmlBrowser.End (html_stream, HTMLStreamStatus.Ok);
        }
        
        private void OnLinkClicked (object obj, LinkClickedArgs args)
        {
            if (args.Url == Catalog.GetString ("add")) {
                this.SwitchTo (INSERT_MODE);
            } else {
                /* an event is launch to update the browser */
                LyricsManager.Instance.GetLyricsFromLyrc (args.Url);
            }
        }
        
        protected void OnSelect (object sender, EventArgs args)
        {
            this.htmlBrowser.SelectAll ();
        }

		public void OnRefresh (object sender, EventArgs args)
		{
			Thread t = new Thread (new ThreadStart (LyricsManager.Instance.RefreshLyrics));
			t.Start ();
		}

        protected void OnCopy (object sender, EventArgs args)
        {
            this.htmlBrowser.Copy ();
        }
        
        private void OnButtonPress (object sender, ButtonPressEventArgs args)
        {
        	if (args.Event.Button != 3) {
        		return;
        	}
        	
            Menu menu = new Menu ();
        	ImageMenuItem copyItem = new ImageMenuItem (Stock.Copy, null);
        	ImageMenuItem selectAllItem = new ImageMenuItem (Stock.SelectAll, null);
        	ImageMenuItem refreshItem = new ImageMenuItem (Stock.Refresh, null);
        	
            //handle activate event
        	copyItem.Activated += new EventHandler (OnCopy);
        	selectAllItem.Activated += new EventHandler (OnSelect);
        	refreshItem.Activated += new EventHandler (OnRefresh);
			
			//add item to the menu
        	menu.Append (selectAllItem);
        	menu.Append (copyItem);
        	menu.Append (new SeparatorMenuItem() );
        	menu.Append (refreshItem);
			
            //show the menu
            menu.ShowAll ();
            menu.Popup ();
        }
		
		public void SaveLyric() {			
            LyricsManager.Instance.AddLyrics (current_artist, current_title, GetText ());
            LoadString (GetText ());
            SwitchTo (LyricsBrowser.HTML_MODE);
		}
    }
}