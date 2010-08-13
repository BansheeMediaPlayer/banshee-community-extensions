//
// ColumnCellContactStatusIndicator.cs
//
// Author:
//   Neil Loknath <neil.loknath@gmail.com>
//
// Copyright (C) 2009 Neil Loknath
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

using Hyena.Data.Gui;

using Banshee.Gui;
using Banshee.Collection;
using Banshee.Collection.Gui;
using Banshee.Telepathy.Data;

namespace Banshee.Telepathy.Gui
{
    public class ColumnCellContactStatusIndicator : ColumnCellStatusIndicator
    {
        public ColumnCellContactStatusIndicator (string property) : base (property)
        {
        }

        public ColumnCellContactStatusIndicator (string property, bool expand) : base (property, expand)
        {
        }

        protected override int PixbufCount {
            get { return base.PixbufCount + 1; }
        }

        protected override void LoadPixbufs ()
        {
            base.LoadPixbufs ();

            // Downloading
            Pixbufs[base.PixbufCount + 0] = IconThemeUtils.LoadIcon (PixbufSize, "document-save", "go-bottom");

            // Downloaded
            //Pixbufs[base.PixbufCount + 1] = IconThemeUtils.LoadIcon (PixbufSize, "podcast-new");
        }

        protected override int GetIconIndex (TrackInfo track)
        {
            ContactTrackInfo ci = ContactTrackInfo.From (track);
            if (track == null || ci == null) {
                return -1;
            }

            if (ci.IsDownloading || ci.IsDownloadPending) {
                return base.PixbufCount + 0;
            }
            else {
                return -1;
            }

        }

        public override void Render (CellContext context, StateType state, double cellWidth, double cellHeight)
        {
            ContactTrackInfo ci = ContactTrackInfo.From (BoundTrack);
            if (ci != null) {
                if (ci.IsDownloadPending) {
                    context.Opaque = false;
                }
            }

            base.Render (context, state, cellWidth, cellHeight);
        }
    }
}