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

using Mono.Unix;

using Banshee.ContextPane;

namespace Banshee.Lyrics.Gui
{
    public class LyricsPane : VBox
    {
        private Gtk.Label label;
        public LyricsBrowser browser;
        private Gtk.ScrolledWindow scrollPane;

        private ContextPage context_page;

        private string track_title;
        private string track_artist;

        public LyricsPane (ContextPage context_page)
        {
            this.context_page = context_page;
            InitComponents ();
        }

        public string TrackArtist {
            get { return track_artist; }
            set {track_artist = value; }
        }
        
        public string TrackTitle {
            get { return track_title; }
            set { track_title = value; UpdateLabel ();}
        }

        
        public void InitComponents ()
        {
            this.browser = new LyricsBrowser ();
            LyricsManager.Instance.LoadStarted += this.OnLoadStarted;
            LyricsManager.Instance.LoadFinished += this.OnLoadFinished;

            browser.InsertModeAvailable = false;
            label = new Label ();
            label.Xalign = 0;

            Gtk.Alignment label_align = new Gtk.Alignment (0, 0, 0, 0);
            label_align.TopPadding = 5;
            label_align.LeftPadding = 10;
            label_align.Add (label);

            this.scrollPane = new Gtk.ScrolledWindow ();
            this.scrollPane.HscrollbarPolicy = ((Gtk.PolicyType)(2));
            this.scrollPane.ShadowType = Gtk.ShadowType.None;
            this.scrollPane.Add (this.browser);

            PackStart (label_align, false, true, 0);
            PackStart (this.scrollPane, true, true, 0);

            this.ShowAll ();
        }

        private void OnLoadStarted (object o, EventArgs args)
        {
            context_page.SetState (Banshee.ContextPane.ContextState.Loading);
        }

        private void OnLoadFinished (object o, LoadFinishedEventArgs args)
        {
            this.browser.LoadString (o, args);

            UpdateLabel ();

            context_page.SetState (Banshee.ContextPane.ContextState.Loaded);
        }
        
        private void UpdateLabel ()
        {
            if (!string.IsNullOrEmpty (track_title)) {
                label.Text = "<b>" + track_title + Catalog.GetString (" lyric") + "</b>";
                label.UseMarkup = true;
            }
            this.ShowAll ();
        }
    }
}
