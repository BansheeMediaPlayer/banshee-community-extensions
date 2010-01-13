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

using Banshee.Gui;
using Banshee.Collection;
using Banshee.MediaEngine;
using Banshee.ServiceStack;

namespace Banshee.Lyrics.Gui
{
    public partial class LyricsWindow : Gtk.Window
    {
        private TrackInfo saved_track;

        private int current_mode;

        public static int HTML_MODE = 0;
        public static int INSERT_MODE = 1;

        public LyricsWindow () : base(Gtk.WindowType.Toplevel)
        {
            this.Build ();
            InitComponents ();
        }

        private void InitComponents ()
        {
            this.KeyPressEvent += OnKeyPressed;
            this.DeleteEvent += delegate(object o, DeleteEventArgs args) {
                OnClose (this, null);
                args.RetVal = true;
            };

            this.buttonRefresh.Clicked += new EventHandler (OnRefresh);
            this.buttonSave.Clicked += new EventHandler (OnSaveLyric);
            this.buttonClose.Clicked += new EventHandler (OnClose);

            this.lyricsBrowser.AddLinkClicked += ManuallyAddLyric;
            LyricsManager.Instance.LoadStarted += this.lyricsBrowser.OnLoading;
            LyricsManager.Instance.LoadFinished += this.lyricsBrowser.LoadString;
            this.SwitchTo (HTML_MODE);
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
                artist = Catalog.GetString ("Unknown Artist");
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
            if (current_mode != HTML_MODE) {
                this.SwitchTo (HTML_MODE);
            }
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
            
            /*deselect the toggle action "Show lyrics" in the View menu */
            InterfaceActionService action_service = ServiceManager.Get<InterfaceActionService> ();
            ToggleAction show_lyrics_action = (ToggleAction)action_service.FindAction ("Lyrics.ShowLyricsAction");
            if (show_lyrics_action != null) {
                show_lyrics_action.Active = false;
            }
        }

        private void OnRefresh (object sender, EventArgs args)
        {
            this.GetBrowser ().OnRefresh ();
        }

        private void ManuallyAddLyric (object sender, EventArgs args)
        {
            this.SwitchTo (INSERT_MODE);
        }

        public void SwitchTo (int mode)
        {
            this.lyricsScrollPane.Remove (this.lyricsScrollPane.Child);
            if (mode == HTML_MODE) {
                this.buttonRefresh.Show ();
                this.buttonSave.Hide ();
                this.lyricsScrollPane.Add (this.lyricsBrowser);
            } else {
                this.buttonSave.Show ();
                this.buttonRefresh.Hide ();

                this.lyricsScrollPane.Add (this.textBrowser);
                this.textBrowser.Buffer.Text = "";
                this.textBrowser.GrabFocus ();

                this.saved_track = ServiceManager.PlayerEngine.CurrentTrack;
            }

            this.lyricsScrollPane.ResizeChildren ();
            this.lyricsScrollPane.ShowAll ();

            current_mode = mode;
        }

        public void OnSaveLyric (object sender, EventArgs args)
        {
            string lyric = this.textBrowser.Buffer.Text;
            LyricsManager.Instance.SaveLyric (saved_track, lyric, true);

            /*refresh all the views. Now the track is taken from the cache */
            if (saved_track == ServiceManager.PlayerEngine.CurrentTrack) {
                LyricsManager.Instance.FetchLyric (saved_track);
            }

            this.SwitchTo (HTML_MODE);
        }
    }
}