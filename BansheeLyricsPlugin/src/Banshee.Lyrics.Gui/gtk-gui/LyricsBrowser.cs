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

namespace Banshee.Lyrics.Gui {
    
    
    public partial class LyricsBrowser {
        
        private Gtk.Frame frame1;
        
        private Gtk.VBox vbox2;
        
        private Gtk.ScrolledWindow lyricsScrollPane;
        
        private Gtk.HTML htmlBrowser;
        private Gtk.TextView textBrowser;
        
        protected virtual void Build ()
        {
        	Gui.Initialize (this);
        	BinContainer.Attach (this);
        	this.Name = "Banshee.Lyrics.Gui.LyricsBrowser";
        	
            this.frame1 = new Gtk.Frame ();
        	this.frame1.CanFocus = true;
        	this.frame1.Name = "frame1";
        	this.frame1.BorderWidth = 0;
        	this.frame1.ShadowType = ((Gtk.ShadowType)(6));
        	
            this.vbox2 = new Gtk.VBox ();
        	this.vbox2.Name = "vbox2";
        	
            this.lyricsScrollPane = new Gtk.ScrolledWindow ();
        	this.lyricsScrollPane.CanFocus = true;
        	this.lyricsScrollPane.Name = "lyricsScrollPane";
        	this.lyricsScrollPane.HscrollbarPolicy = ((Gtk.PolicyType)(2));
        	this.lyricsScrollPane.ShadowType = ((Gtk.ShadowType)(1));
        	this.vbox2.Add (this.lyricsScrollPane);
        	Gtk.Box.BoxChild w1 = ((Gtk.Box.BoxChild)(this.vbox2[this.lyricsScrollPane]));
        	w1.Position = 0;
        	
            this.frame1.Add (this.vbox2);
        	this.Add (this.frame1);
        	if ((this.Child != null)) {
        		this.Child.ShowAll ();
        	}
        	
            this.htmlBrowser = new Gtk.HTML ();
        	this.htmlBrowser.AllowSelection (true);
        	this.htmlBrowser.Editable = false;
        	this.lyricsScrollPane.Add (htmlBrowser);
        	this.htmlBrowser.Show ();
        	
            this.textBrowser = new Gtk.TextView ();
        	this.textBrowser.WrapMode = (Gtk.WrapMode)(2);
        	
            this.Hide ();
        }
		
		public void RemoveShadow () {
			lyricsScrollPane.ShadowType = ((Gtk.ShadowType)(0));
		}
    }
}
