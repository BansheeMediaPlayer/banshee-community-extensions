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

using Banshee.Gui.Widgets;

namespace Banshee.Lyrics.Gui
{

    public partial class LyricsWindow
    {

        private const int WIDTH = 410;
        private const int HEIGHT = 425;

        private Gtk.VBox vbox1;

        private Banshee.Lyrics.Gui.LyricsBrowser lyricsBrowser;

        private Gtk.HButtonBox dialog1_ActionArea1;

        private Gtk.Button buttonRefresh;

        private Gtk.Button buttonSave;

        private Gtk.Button buttonClose;

        private Gtk.Frame frame1;

        private Gtk.ScrolledWindow lyricsScrollPane;

        private Gtk.TextView textBrowser;

        private TrackInfoDisplay track_info_display;

        protected virtual void Build ()
        {
            Gui.Initialize (this);
            this.Resizable = true;
            this.HeightRequest = HEIGHT;
            this.WidthRequest = WIDTH;
            this.WindowPosition = Gtk.WindowPosition.Center;
            this.Name = "LyricsWindow";
            this.Icon = IconLoader.LoadIcon (this, "banshee", Gtk.IconSize.Menu, 16);
            this.AllowGrow = false;
            this.AllowShrink = true;
            
            this.vbox1 = new Gtk.VBox ();
            this.vbox1.Spacing = 6;
            this.vbox1.BorderWidth = ((uint)(12));
            this.vbox1.Name = "vbox1";
            
            this.track_info_display = new ClassicTrackInfoDisplay ();
            track_info_display.Show ();
            
            this.vbox1.PackStart (this.track_info_display, false, false, 0);
            
            this.lyricsBrowser = new Banshee.Lyrics.Gui.LyricsBrowser ();
            this.lyricsBrowser.Events = ((Gdk.EventMask)(256));
            this.lyricsBrowser.Name = "lyricsBrowser";
            
            
            this.lyricsScrollPane = new Gtk.ScrolledWindow ();
            this.lyricsScrollPane.CanFocus = true;
            this.lyricsScrollPane.Name = "lyricsScrollPane";
            this.lyricsScrollPane.HscrollbarPolicy = ((Gtk.PolicyType)(2));
            this.lyricsScrollPane.ShadowType = ((Gtk.ShadowType)(1));
            this.lyricsScrollPane.Add (this.lyricsBrowser);
            
            this.frame1 = new Gtk.Frame ();
            this.frame1.CanFocus = true;
            this.frame1.Name = "frame1";
            this.frame1.BorderWidth = 0;
            this.frame1.ShadowType = ((Gtk.ShadowType)(6));
            this.frame1.Add (this.lyricsScrollPane);
            
            this.vbox1.PackStart (this.frame1, true, true, 0);
            
            this.dialog1_ActionArea1 = new Gtk.HButtonBox ();
            this.dialog1_ActionArea1.Name = "dialog1_ActionArea1";
            this.dialog1_ActionArea1.Spacing = 6;
            this.dialog1_ActionArea1.BorderWidth = 1;
            this.dialog1_ActionArea1.LayoutStyle = ((Gtk.ButtonBoxStyle)(4));
            
            this.buttonRefresh = new Gtk.Button ();
            this.buttonRefresh.CanFocus = true;
            this.buttonRefresh.Name = "buttonRefresh";
            this.buttonRefresh.UseStock = true;
            this.buttonRefresh.UseUnderline = true;
            this.buttonRefresh.Label = "gtk-refresh";
            
            this.buttonClose = new Gtk.Button ();
            this.buttonClose.CanDefault = true;
            this.buttonClose.CanFocus = true;
            this.buttonClose.Name = "buttonClose";
            this.buttonClose.UseStock = true;
            this.buttonClose.UseUnderline = true;
            this.buttonClose.Label = "gtk-close";
            
            this.buttonSave = new Gtk.Button ();
            this.buttonSave.CanDefault = true;
            this.buttonSave.CanFocus = true;
            this.buttonSave.Hide ();
            this.buttonSave.Name = "buttonSave";
            this.buttonSave.UseStock = true;
            this.buttonSave.UseUnderline = true;
            this.buttonSave.Label = "gtk-save";
            
            this.dialog1_ActionArea1.PackStart (this.buttonRefresh, false, false, 0);
            this.dialog1_ActionArea1.PackStart (this.buttonSave, false, false, 0);
            this.dialog1_ActionArea1.PackStart (this.buttonClose, false, false, 0);
            
            this.vbox1.PackStart (this.dialog1_ActionArea1, false, false, 0);
            
            this.Add (this.vbox1);
            if (this.Child != null) {
                this.Child.ShowAll ();
            }
            
            this.textBrowser = new Gtk.TextView ();
            this.textBrowser.WrapMode = (Gtk.WrapMode)(2);
            
            this.Hide ();
        }
    }
}
