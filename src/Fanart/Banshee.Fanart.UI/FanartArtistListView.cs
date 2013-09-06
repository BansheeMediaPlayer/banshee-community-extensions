//
// FanartArtistListView.cs
//
// Author:
//   Tomasz Maczyński <tmtimon@gmail.com>
//
// Copyright 2013 Tomasz Maczyński
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
using Banshee.Collection.Gui;
using Banshee.Collection;
using Hyena.Data.Gui;

namespace Banshee.Fanart.UI
{
    public class FanartArtistListView : TrackFilterListView<ArtistInfo>
    {
        private FanartArtistColumnCell image_column_cell;
        private Column image_column;
        private Column artist_name_column;

        protected FanartArtistListView (IntPtr ptr) : base () {}

        public FanartArtistListView () : base ()
        {
            image_column_cell = new FanartArtistColumnCell ();

            image_column = new Column ("Artist Image", image_column_cell, 0.2);
            artist_name_column = new Column ("Artist", new ColumnCellText ("DisplayName", true), 1.0);

            column_controller.Add (image_column);
            column_controller.Add (artist_name_column);

            ColumnController = column_controller;
        }
       
        // TODO add context menu for artists/albums...probably need a Banshee.Gui/ArtistActions.cs file.  Should
        // make TrackActions.cs more generic with regards to the TrackSelection stuff, using the new properties
        // set on the sources themselves that give us access to the IListView<T>.
        /*protected override bool OnPopupMenu ()
        {
            ServiceManager.Get<InterfaceActionService> ().TrackActions["TrackContextMenuAction"].Activate ();
            return true;
        }*/
    }
}

