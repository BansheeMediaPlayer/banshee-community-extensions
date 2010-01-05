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

namespace Banshee.Lyrics.Gui
{
    public class LyricPane : VBox
    {
        private Gtk.Label label;
        private LyricsBrowser browser;
        private Gtk.ScrolledWindow scrollPane;

        private string last_track_name;
        public void SetTrackName (String track_name)
        {
            if (!String.IsNullOrEmpty (track_name) && track_name != last_track_name) {
                last_track_name = track_name;
                label.Text = "<b>" + last_track_name + Catalog.GetString(" lyric") + "</b>";
                label.UseMarkup = true;
            }
            
        }
        
        public LyricPane ()
        {
            this.browser = new LyricsBrowser ();
            this.browser.InsertModeAvailable = false;
            
            label = new Label ();
            label.Xalign = 0;
            
            Gtk.Alignment label_align = new Gtk.Alignment (0, 0, 0, 0);
            label_align.TopPadding = 5;
            label_align.LeftPadding = 9;
            label_align.Add (label);
            
            this.scrollPane = new Gtk.ScrolledWindow ();
            this.scrollPane.HscrollbarPolicy = ((Gtk.PolicyType)(2));
            this.scrollPane.ShadowType = Gtk.ShadowType.None;
            this.scrollPane.Add (this.browser);
            
            PackStart (label_align, false, true, 0);
            PackStart (this.scrollPane, true, true, 0);
            
            this.ShowAll();
        }
    }
}
