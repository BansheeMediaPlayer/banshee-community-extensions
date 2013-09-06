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
            int spacing = 2;

            int thumb_size = 22;
            int image_size = thumb_size - (2 * spacing);

            // TODO: improve image
            var defaultImage = PixbufImageSurface.Create (IconThemeUtils.LoadIcon (image_size, "applications-multimedia"));
            var image = defaultImage;

            bool has_border = false;

            ArtworkRenderer.RenderThumbnail (context.Context, image, false, 
                                            spacing, spacing,
                                            thumb_size, thumb_size, 
                                            false, context.Theme.Context.Radius);
        }
    }
}

