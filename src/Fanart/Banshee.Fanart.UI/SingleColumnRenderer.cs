//
// SingleColumnRenderer.cs
//
// Author:
//   Frank Ziegler <funtastix@googlemail.com>
//
// Copyright (c) 2013 Frank Ziegler
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

using Gtk;
using Mono.Unix;

using Banshee.Collection.Gui;

using Hyena.Data.Gui;

namespace Banshee.Fanart.UI
{
    public class FanartSingleColumn : IArtistListRenderer
    {
        private readonly ArtistColumnCell image_column_cell;
        private readonly Column image_column;
        private readonly ColumnController column_controller;

        public FanartSingleColumn ()
        {
            column_controller = new ColumnController ();
            image_column_cell = new ArtistColumnCell { RenderNameWhenNoImage = true };
            image_column = new Column ("Artist Image", image_column_cell, 1.0);
            column_controller.Add (image_column);
        }

        public virtual String Name {
            get { return Catalog.GetString ("FanArt Single Column"); }
        }

        public ColumnController ColumnController {
            get { return column_controller; }
        }

        public int ComputeRowHeight (Widget w)
        {
            return image_column_cell.ComputeRowHeight (w);
        }
    }
}
