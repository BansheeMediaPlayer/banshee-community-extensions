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

using System;
using System.IO;
using System.Threading;
using Mono.Unix;

using Gtk;
using Gdk;

using Banshee.ServiceStack;
using Banshee.MediaEngine;

namespace Banshee.Lyrics.Gui
{
    public partial class LyricsWindow : Gtk.Window
    {
        private string saved_artist;
        private string saved_title;
        
        public LyricsWindow () : base (Gtk.WindowType.Toplevel)
        {
            this.Build ();
            InitComponents ();
        }

        private void InitComponents ()
        {
            this.KeyPressEvent += OnKeyPressed;
            this.DeleteEvent += delegate (object o, DeleteEventArgs args)
            {
                OnClose (this, null);
                args.RetVal = true;
            };

            this.buttonRefresh.Clicked += new EventHandler (OnRefresh);
            this.buttonSave.Clicked += new EventHandler (OnSaveLyric);
            this.buttonClose.Clicked += new EventHandler (OnClose);
            this.lyricsBrowser.ChangeModeEvent += new ChangeModeEventHandler (OnBrowserChangeMode);
        }

        public void ForceUpdate ()
        {
            if (ServiceManager.PlayerEngine.CurrentTrack == null) {
                return;
            }

            string window_title = ServiceManager.PlayerEngine.CurrentTrack.TrackTitle;
            string by_str = " " + Catalog.GetString ("by") + " ";
            string artist = ServiceManager.PlayerEngine.CurrentTrack.ArtistName;
            if (artist == null) {
                artist = Catalog.GetString("Unknown Artist");
            }
            window_title += by_str + artist;
            this.Title = window_title;
        }

        public void OnPlayerEngineEventChanged (PlayerEventArgs args)
        {
            if (args.Event != PlayerEvent.StartOfStream && args.Event != PlayerEvent.TrackInfoUpdated) {
                return;
            }
            ForceUpdate ();
        }
        
        public LyricsBrowser GetBrowser ()
        {
            return this.lyricsBrowser;
        }
        
        public new void Show ()
        {
            this.lyricsBrowser.SwitchTo (LyricsBrowser.HTML_MODE);
            base.Show ();
        }
        
        /*event handlers */
        void OnKeyPressed (object sender, KeyPressEventArgs args)
        {
            if (args.Event.Key == Gdk.Key.Escape) {
                OnClose (this, null);
            }
        }
        
        void OnClose (object sender, EventArgs args)
        {
            this.Hide ();
        }
        
        void OnRefresh (object sender, EventArgs args)
        {
            Thread t = new Thread (new ThreadStart (LyricsManager.Instance.RefreshLyrics));
            t.Start ();
        }
        void OnBrowserChangeMode (object sender, ChangeModeEventArgs e)
        {
            if (e.mode == LyricsBrowser.INSERT_MODE) {
                this.buttonSave.Show();
                this.buttonRefresh.Hide();
                this.saved_artist = ServiceManager.PlayerEngine.CurrentTrack.ArtistName;
                this.saved_title = ServiceManager.PlayerEngine.CurrentTrack.TrackTitle;
            } else {
                this.buttonSave.Hide();
                this.buttonRefresh.Show();
            }
        }
        
        public void OnSaveLyric (object sender, EventArgs args)
        {
            string lyric = lyricsBrowser.GetText ();
            LyricsManager.Instance.AddLyrics (saved_artist, saved_title, lyric);
            
            lyricsBrowser.LoadString(lyric);
            lyricsBrowser.SwitchTo (LyricsBrowser.HTML_MODE);
        }
    }
}
