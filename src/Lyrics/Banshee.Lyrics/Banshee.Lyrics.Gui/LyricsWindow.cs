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

using Mono.Addins;

using Gtk;

using Banshee.Gui;
using Banshee.Gui.Widgets;
using Banshee.Collection;
using Banshee.MediaEngine;
using Banshee.ServiceStack;

namespace Banshee.Lyrics.Gui
{
    public class LyricsWindow : Window
    {
        private int current_mode;

        public static int HTML_MODE = 0;
        public static int INSERT_MODE = 1;

        private TrackInfo saved_track;

        private ClassicTrackInfoDisplay track_info_display;
        private LyricsBrowser lyrics_browser;
        private ScrolledWindow lyrics_pane;
        private Button refresh_button;
        private Button save_button;
        private TextView text_view;

        public LyricsWindow () : base (WindowType.Toplevel)
        {
            InitComponents ();
        }

        private void InitComponents ()
        {
            Resizable = true;
            HeightRequest = 425;
            WidthRequest = 410;
            WindowPosition = WindowPosition.Center;
            Icon = IconThemeUtils.LoadIcon ("banshee", 16);

            var vbox = new VBox () {
                Spacing = 6,
                BorderWidth = 12
            };

            track_info_display = new ClassicTrackInfoDisplay ();
            vbox.PackStart (track_info_display, false, false, 0);

            lyrics_browser = new LyricsBrowser ();

            lyrics_pane = new ScrolledWindow ();
            lyrics_pane.Add (lyrics_browser);

            var frame = new Frame ();
            frame.Add (lyrics_pane);

            vbox.PackStart (frame, true, true, 0);

            var button_box = new HButtonBox () {
                Spacing = 6,
                BorderWidth = 1,
                LayoutStyle = ButtonBoxStyle.End
            };

            var copy_button = new Button ("gtk-copy") {
                TooltipText = AddinManager.CurrentLocalizer.GetString ("Copy lyrics to clipboard")
            };
            var close_button = new Button ("gtk-close");
            refresh_button = new Button ("gtk-refresh");
            save_button = new Button ("gtk-save");

            button_box.PackStart (copy_button, false, false, 0);
            button_box.PackStart (refresh_button, false, false, 0);
            button_box.PackStart (save_button, false, false, 0);
            button_box.PackStart (close_button, false, false, 0);

            vbox.PackStart (button_box, false, false, 0);

            Add (vbox);
            if (Child != null) {
                Child.ShowAll ();
            }

            text_view = new TextView ();
            text_view.WrapMode = WrapMode.Word;

            Hide ();

            KeyPressEvent += OnKeyPressed;
            DeleteEvent += delegate(object o, DeleteEventArgs args) {
                OnClose (this, null);
                args.RetVal = true;
            };

            refresh_button.Clicked += OnRefresh;
            save_button.Clicked += OnSaveLyrics;
            close_button.Clicked += OnClose;
            copy_button.Clicked += OnCopy;

            lyrics_browser.AddLinkClicked += ManuallyAddLyrics;
            LyricsManager.Instance.LoadStarted += lyrics_browser.OnLoading;
            LyricsManager.Instance.LoadFinished += lyrics_browser.LoadString;
            SwitchTo (HTML_MODE);
        }

        public void ForceUpdate ()
        {
            if (ServiceManager.PlayerEngine.CurrentTrack == null) {
                return;
            }

            string window_title = ServiceManager.PlayerEngine.CurrentTrack.TrackTitle;
            string by_str = " " + AddinManager.CurrentLocalizer.GetString ("by") + " ";
            string artist = ServiceManager.PlayerEngine.CurrentTrack.ArtistName;
            if (artist == null) {
                artist = AddinManager.CurrentLocalizer.GetString ("Unknown Artist");
            }
            window_title += by_str + artist;
            Title = window_title;

        }

        public void OnPlayerEngineEventChanged (PlayerEventArgs args)
        {
            if (args.Event != PlayerEvent.StartOfStream && args.Event != PlayerEvent.TrackInfoUpdated) {
                return;
            }

            ForceUpdate ();
        }

        public LyricsBrowser Browser {
            get { return lyrics_browser; }
        }

        public new void Show ()
        {
            if (current_mode != HTML_MODE) {
                SwitchTo (HTML_MODE);
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
            Hide ();

            /*deselect the toggle action "Show lyrics" in the View menu */
            InterfaceActionService action_service = ServiceManager.Get<InterfaceActionService> ();
            ToggleAction show_lyrics_action = (ToggleAction)action_service.FindAction ("Lyrics.ShowLyricsAction");
            if (show_lyrics_action != null) {
                show_lyrics_action.Active = false;
            }
        }

        private void OnRefresh (object sender, EventArgs args)
        {
            Browser.OnRefresh ();
        }

        private void OnCopy (object sender, EventArgs args)
        {
            Browser.CopyLyricsToClipboard ();
        }

        private void ManuallyAddLyrics (object sender, EventArgs args)
        {
            SwitchTo (INSERT_MODE);
        }

        public void SwitchTo (int mode)
        {
            lyrics_pane.Remove (lyrics_pane.Child);
            if (mode == HTML_MODE) {
                refresh_button.Show ();
                save_button.Hide ();
                lyrics_pane.Add (lyrics_browser);
            } else {
                save_button.Show ();
                refresh_button.Hide ();

                lyrics_pane.Add (text_view);
                text_view.Buffer.Text = "";
                text_view.GrabFocus ();

                saved_track = ServiceManager.PlayerEngine.CurrentTrack;
            }

            lyrics_pane.ResizeChildren ();
            lyrics_pane.ShowAll ();

            current_mode = mode;
        }

        public void OnSaveLyrics (object sender, EventArgs args)
        {
            string lyrics = text_view.Buffer.Text;
            LyricsManager.Instance.SaveLyrics (saved_track, lyrics, true);

            /*refresh all the views. Now the track is taken from the cache */
            if (saved_track == ServiceManager.PlayerEngine.CurrentTrack) {
                LyricsManager.Instance.FetchLyrics (saved_track);
            }

            SwitchTo (HTML_MODE);
        }
    }
}