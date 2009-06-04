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

namespace Banshee.Lyrics.Gui {
    
    
    public partial class LyricsWindow {
        
        private const int WIDTH = 410;
        private const int HEIGHT = 425;
        
        private Gtk.VBox vbox1;
        
        private Banshee.Lyrics.Gui.LyricsBrowser lyricsBrowser;
        
        private Gtk.HButtonBox dialog1_ActionArea1;
        
        private Gtk.Button buttonRefresh;
        
        private Gtk.Button buttonSave;
        
        private Gtk.Button buttonClose;
        
        private TrackInfoDisplay track_info_display ;
        
        protected virtual void Build() {
            Gui.Initialize(this);
            // Widget Banshee.Lyrics.Gui.LyricsWindow
            this.Name = "Banshee.Lyrics.Gui.LyricsWindow";
            this.Icon = IconLoader.LoadIcon(this, "banshee", Gtk.IconSize.Menu, 16);
            this.WindowPosition = ((Gtk.WindowPosition)(4));
            this.Resizable = false;
            this.AllowGrow = false;
            this.AllowShrink = true;
            
            this.vbox1 = new Gtk.VBox();
            this.vbox1.Spacing = 6;
            this.vbox1.BorderWidth = ((uint)(11));
            this.vbox1.Name = "vbox1";
            
            this.track_info_display = new ClassicTrackInfoDisplay ();
            track_info_display.Show ();
            
            this.vbox1.Add(this.track_info_display);
            Gtk.Box.BoxChild w1 = ((Gtk.Box.BoxChild)(this.vbox1[this.track_info_display]));
            w1.Position = 0;
            w1.Expand = false;
            
            this.lyricsBrowser = new Banshee.Lyrics.Gui.LyricsBrowser();
            this.lyricsBrowser.Events = ((Gdk.EventMask)(256));
            this.lyricsBrowser.Name = "lyricsBrowser";
            this.vbox1.Add(this.lyricsBrowser);
            Gtk.Box.BoxChild w2 = ((Gtk.Box.BoxChild)(this.vbox1[this.lyricsBrowser]));
            w2.Position = 1;
            
            this.dialog1_ActionArea1 = new Gtk.HButtonBox();
            this.dialog1_ActionArea1.Name = "dialog1_ActionArea1";
            this.dialog1_ActionArea1.Spacing = 6;
            this.dialog1_ActionArea1.BorderWidth = 1;
            this.dialog1_ActionArea1.LayoutStyle = ((Gtk.ButtonBoxStyle)(4));
            
            this.buttonRefresh = new Gtk.Button();
            this.buttonRefresh.CanFocus = true;
            this.buttonRefresh.Name = "buttonRefresh";
            this.buttonRefresh.UseStock = true;
            this.buttonRefresh.UseUnderline = true;
            this.buttonRefresh.Label = "gtk-refresh";
            this.dialog1_ActionArea1.Add(this.buttonRefresh);
            Gtk.ButtonBox.ButtonBoxChild w4 = ((Gtk.ButtonBox.ButtonBoxChild)(this.dialog1_ActionArea1[this.buttonRefresh]));
            w4.Expand = false;
            w4.Fill = false;
            // Container child dialog1_ActionArea1.Gtk.ButtonBox+ButtonBoxChild
            this.buttonClose = new Gtk.Button();
            this.buttonClose.CanDefault = true;
            this.buttonClose.CanFocus = true;
            this.buttonClose.Name = "buttonClose";
            this.buttonClose.UseStock = true;
            this.buttonClose.UseUnderline = true;
            this.buttonClose.Label = "gtk-close";
            this.dialog1_ActionArea1.Add(this.buttonClose);
            Gtk.ButtonBox.ButtonBoxChild w5 = ((Gtk.ButtonBox.ButtonBoxChild)(this.dialog1_ActionArea1[this.buttonClose]));
            w5.Position = 1;
            w5.Expand = false;
            w5.Fill = false;
            
            this.buttonSave = new Gtk.Button();
            this.buttonSave.CanDefault = true;
            this.buttonSave.CanFocus = true;
            this.buttonSave.Hide();
            this.buttonSave.Name = "buttonSave";
            this.buttonSave.UseStock = true;
            this.buttonSave.UseUnderline = true;
            this.buttonSave.Label = "gtk-save";
            this.dialog1_ActionArea1.Add(this.buttonSave);
            Gtk.ButtonBox.ButtonBoxChild w7 = ((Gtk.ButtonBox.ButtonBoxChild)(this.dialog1_ActionArea1[this.buttonSave]));
            w7.Position = 1;
            w7.Expand = false;
            w7.Fill = false;
            
            this.vbox1.Add(this.dialog1_ActionArea1);
            Gtk.Box.BoxChild w6 = ((Gtk.Box.BoxChild)(this.vbox1[this.dialog1_ActionArea1]));
            w6.Position = 2;
            w6.Expand = false;
            w6.Fill = false;
            Gtk.Box.BoxChild w3 = ((Gtk.Box.BoxChild)(this.vbox1[this.vbox1]));
            w3.Position = 0;
            this.Add(this.vbox1);
            if ((this.Child != null)) {
                this.Child.ShowAll();
            }
            
            this.Resizable = true;
            this.HeightRequest = HEIGHT;
            this.WidthRequest = WIDTH;
            this.WindowPosition = Gtk.WindowPosition.Center;
            
            this.Hide();
        }
    }
}