// 
// ContextPage.cs
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
using System;

using Mono.Addins;

using Banshee.ContextPane;

using Gtk;

using Hyena;

namespace Banshee.Karaoke.Gui
{
    public class ContextPage : BaseContextPage
    {
        private KaraokePane pane;

        public ContextPage ()
        {
            Id = "karaoke";
            Name = AddinManager.CurrentLocalizer.GetString ("Karaoke");

            Gdk.Pixbuf icon = new Gdk.Pixbuf (System.Reflection.Assembly.GetExecutingAssembly ()
                                              .GetManifestResourceStream ("microphone.png"));
            IconTheme.AddBuiltinIcon ("microphone", 100, icon);
            IconNames = new string[] { "microphone" , "gtk-edit" };
        }

        internal void SetState (ContextState state)
        {
            State = state;
        }

        public override void SetTrack (Banshee.Collection.TrackInfo track)
        {
            pane.Track = track;
        }

        public override Widget Widget {
            get { return pane ?? (pane = new KaraokePane (this)); }
        }
    }
}
