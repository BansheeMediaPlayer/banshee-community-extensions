// 
// KaraokePane.cs
// 
// Author:
//   Frank Ziegler <funtastix@googlemail.com>
// 
// Copyright (c) 2011 Frank Ziegler
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using Gtk;

using System;

using Mono.Addins;

using Banshee.Collection;
using Banshee.ContextPane;
using Banshee.ServiceStack;
using System.Threading;
using Banshee.MediaEngine;

namespace Banshee.Karaoke.Gui
{
    /// <summary>
    /// extra wrapper for Karaoke view so we can put some stuff around it if needed
    /// </summary>
    public class KaraokePane : Notebook
    {
        private KaraokeView view;
        private ContextPage context_page;
        private TrackInfo track;
        private Timer timer;
        private Label disconnected;

        public KaraokePane (ContextPage context_page)
        {
            ShowBorder = false;
            ShowTabs = false;
            this.context_page = context_page;

            timer = new Timer (TimerTimedOut, null, System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
            ServiceManager.PlayerEngine.ConnectEvent (OnTrackChange, PlayerEvent.StateChange);
            ServiceManager.PlayerEngine.ConnectEvent (OnSeek, PlayerEvent.Seek);

            disconnected = new Label (AddinManager.CurrentLocalizer.GetString (
                "You are disconnected from the internet, so karaoke lyrics are not available."));
            Add (disconnected);
            ShowAll ();
        }

        private KaraokeView View {
            get {
                if (view == null) {
                    this.view = new KaraokeView ();
                    view.Zoom = 1.2f;
                    Add (view);
                    ShowAll ();
                }

                return view;
            }
        }

        public TrackInfo Track {
            get { return track; }
            set {
                track = value;
                if (track != null) {
                    context_page.SetState (Banshee.ContextPane.ContextState.Loading);
                    if (!ServiceManager.Get<Banshee.Networking.Network> ().Connected) {
                        this.CurrentPage = this.PageNum (disconnected);
                    } else {
                        View.LoadLyrics (track);
                        View.LoadStatusChanged += OnViewLoadStatusChanged;
                        timer.Change (0, 2000);
                        this.CurrentPage = this.PageNum (View);
                    }
                    context_page.SetState (Banshee.ContextPane.ContextState.Loaded);
                } else {
                    context_page.SetState (Banshee.ContextPane.ContextState.NotLoaded);
                    timer.Change (System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
                }
            }
        }

        void OnViewLoadStatusChanged (object sender, EventArgs e)
        {
            if (View.LoadStatus == Banshee.WebBrowser.OssiferLoadStatus.FirstVisuallyNonEmptyLayout) {
                SetStyle ();
                View.LoadStatusChanged -= OnViewLoadStatusChanged;
            }
        }

        private void TimerTimedOut (object o)
        {
            if (this.CurrentPage == this.PageNum (disconnected)) {
                return;
            }
            ServiceStack.Application.Invoke (delegate {
                if (track != null && ServiceManager.PlayerEngine.IsPlaying (track) && View.LoadStatus != Banshee.WebBrowser.OssiferLoadStatus.Failed) {
                    SetSongTime ();
                }
            });
        }

        private void PauseScrolling ()
        {
            if (this.CurrentPage == this.PageNum (disconnected)) {
                return;
            }
            View.ExecuteScript (String.Format ("window.postMessage('pause','http://youtubelyric.com');", ServiceManager.PlayerEngine.Position / 1000));
        }

        private void ResumeScrolling ()
        {
            if (this.CurrentPage == this.PageNum (disconnected)) {
                return;
            }
            View.ExecuteScript (String.Format ("window.postMessage('play','http://youtubelyric.com');", ServiceManager.PlayerEngine.Position / 1000));
        }

        private void SetSongTime ()
        {
            if (this.CurrentPage == this.PageNum (disconnected)) {
                return;
            }
            View.ExecuteScript (String.Format ("window.postMessage({0},'http://youtubelyric.com');", ServiceManager.PlayerEngine.Position / 1000));
        }

        private void SetStyle ()
        {
            if (this.CurrentPage == this.PageNum (disconnected)) {
                return;
            }
            View.ExecuteScript ("if (document.getElementById ('lyricbox')) { document.getElementById ('lyricbox').style.fontSize='2.5em'; }");
        }

        private void OnTrackChange (PlayerEventArgs args)
        {
            if (ServiceManager.PlayerEngine.CurrentState == PlayerState.Paused) {
                PauseScrolling ();
                return;
            }
            if (ServiceManager.PlayerEngine.CurrentState == PlayerState.Playing) {
                ResumeScrolling ();
                SetSongTime ();
                return;
            }
        }

        private void OnSeek (PlayerEventArgs args)
        {
            SetSongTime ();
        }
    }
}
