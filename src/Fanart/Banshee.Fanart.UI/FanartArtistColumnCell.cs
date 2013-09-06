//
// FanartArtistColumnCell.cs
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
using Banshee.Gui;
using Gtk;
using Hyena.Data.Gui;
using Hyena.Gui;

namespace Banshee.Fanart.UI
{
    public class FanartArtistColumnCell : ColumnCell
    {


        public FanartArtistColumnCell () : base (null, true)
        {
        }

        public override void Render (CellContext context, StateType state, double cellWidth, double cellHeight)
        {
            int image_size = 20;

            // TODO: improve image
            var defaultImage = PixbufImageSurface.Create (IconThemeUtils.LoadIcon (image_size, "applications-multimedia"));
            var image = defaultImage;

            bool is_default = false;

            int x = 1;
            int x_offset = 1;
            int y = 1;
            int y_offset = 1;

            var image_render_width = defaultImage.Width + 2;
            var image_render_height = defaultImage.Height + 2;

            // TODO: fix w, h, width and height calculations 
            ArtworkRenderer.RenderThumbnail (context.Context, image, false, x + x_offset, y + y_offset,
                                             image_render_width + x_offset * 2, image_render_height + y_offset * 2, !is_default, context.Theme.Context.Radius);
        }
    }
}

