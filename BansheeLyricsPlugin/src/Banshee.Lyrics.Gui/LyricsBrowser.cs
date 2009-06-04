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
        
        private string browser_str;
        
        public LyricsBrowser ()
        {
            this.Build ();
            
            LyricsManager.Instance.LoadingLyricEvent += OnLoadingLyric;
            LyricsManager.Instance.LyricChangedEvent += OnLyricChanged;
            
            this.htmlBrowser.LinkClicked += new LinkClickedHandler (OnLinkClicked);
            this.htmlBrowser.ButtonPressEvent += new ButtonPressEventHandler (OnButtonPress);
            SwitchTo (LyricsBrowser.HTML_MODE);
        }
        
        /*depends on the mode (HTML or INSERT) show the browser or the text area */
        public void SwitchTo (int mode)
        {
            this.lyricsScrollPane.Remove (this.lyricsScrollPane.Child);
            
            if (mode == LyricsBrowser.HTML_MODE) {
                this.lyricsScrollPane.Add (this.htmlBrowser);
            } else {
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
                this.browser_str = args.error;
            } else if (args.suggestion != null) {
                this.browser_str = GetSuggestionString (args.suggestion);
            } else if (args.lyric != null) {
                this.browser_str = args.lyric;
            } else {
                this.browser_str = Catalog.GetString("No lyric found.");
            }
            
            Gtk.Application.Invoke (LoadString);
        }
        
        private string GetSuggestionString (string lyric_suggestion)
        {
            StringBuilder sb = new StringBuilder ();
            sb.Append ("<b>" + Catalog.GetString ("Lyric not found") + "</b>");
            sb.Append ("<br><a href=\"" + Catalog.GetString ("add") + "\">");
            sb.Append (Catalog.GetString ("Click here to manually add a new lyric"));
            sb.Append ("</a>");
            sb.Append ("<br><br>");
            sb.Append (Catalog.GetString ("Suggestions:"));
            sb.Append (lyric_suggestion);
            
            return sb.ToString ();
        }
        
        private void OnLoadingLyric (object o, EventArgs args)
        {
            this.browser_str = Catalog.GetString ("Loading lyric...");
            Gtk.Application.Invoke (LoadString);
        }
        
        public string GetText () {
            return textBrowser.Buffer.Text;
        }
        
        /*prevent multi threading problems */
        private void LoadString (object o, EventArgs args)
        {
            this.browser_str = this.browser_str == null ? "" : this.browser_str;
            LoadString (browser_str);
        }
        
        public void LoadString (string str)
        {
            HTMLStream html_stream = this.htmlBrowser.Begin ("text/html; charset=utf-8");
            html_stream.Write (str);
            this.htmlBrowser.End (html_stream, HTMLStreamStatus.Ok);
        }
        
        private void OnLinkClicked (object obj, LinkClickedArgs args)
        {
            if (args.Url == Catalog.GetString ("add")) {
                this.SwitchTo (INSERT_MODE);
            } else {
                this.LoadString (LyricsManager.Instance.GetLyricsFromLyrc (args.Url));
            }
        }
        
        private void OnSelect (object sender, EventArgs args)
        {
            this.htmlBrowser.SelectAll ();
        }
        
        private void OnCopy (object sender, EventArgs args)
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
            
            //handle activate event
            copyItem.Activated += new EventHandler (OnCopy);
            selectAllItem.Activated += new EventHandler (OnSelect);
            
            //add item to the menu
            menu.Append (selectAllItem);
            menu.Append (copyItem);
            
            //show the menu
            menu.ShowAll ();
            menu.Popup ();
        }
    }
}