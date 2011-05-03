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

using Mono.Addins;

using Banshee.ContextPane;

using Gtk;

using Hyena;

namespace Banshee.Lyrics.Gui
{
    public class ContextPage : BaseContextPage
    {
        private LyricsPane lyrics_pane;

        public ContextPage ()
        {
            Id = "lyrics";
            Name = AddinManager.CurrentLocalizer.GetString ("Lyrics");
            IconNames = new string[] { "preferences-desktop-font" , "gtk-edit" };
        }

        internal void SetState (ContextState state)
        {
            State = state;
        }

        public override void SetTrack (Banshee.Collection.TrackInfo track)
        {
            lyrics_pane.UpdateLabel (track.DisplayTrackTitle);
        }

        public override Widget Widget {
            get { return lyrics_pane ?? (lyrics_pane = new LyricsPane (this)); }
        }
    }
}