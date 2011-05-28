//
// FullscreenButton.cs
//
// Author:
//       Mathijs Dumon <mathijsken@hotmail.com>
//
// Copyright (c) 2010 Mathijs Dumon
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
using Cairo;

using Clutter;
using ClutterFlow.Buttons;

namespace Banshee.ClutterFlow.Buttons
{

    public class FullscreenButton : ClutterToggleButton
    {

        public FullscreenButton (bool toggled) : base (25, 25, toggled)
        {
        }

        public FullscreenButton () : this (false)
        {
        }

        #region Rendering
        private void Render (Clutter.CairoTexture texture, int with_state, bool outwards)
        {

            texture.Clear ();
            Cairo.Context context = texture.Create ();

            double lwidth = 1;
            double hlwidth = lwidth*0.5;

            //Draw outline rectangles:
            context.Rectangle (hlwidth, hlwidth, texture.Width - lwidth, texture.Height - lwidth);
                context.SetSourceRGB (1.0, 1.0, 1.0);
                context.LineWidth = lwidth;
                context.StrokePreserve ();
                double sat = (with_state==0 ? 0.4 : (with_state==1 ? 0.6 : 0.8));
                context.SetSourceRGB (sat, sat, sat);
                context.Fill ();

            double dim = 4;
            context.MoveTo (-dim, 0);
            context.LineTo (outwards ? 0 : -dim, outwards ? 0 : dim);
            context.LineTo (0, dim);
            context.MoveTo (-dim, dim);
            context.LineTo (0, 0);
            context.ClosePath ();
            Cairo.Path arrow = context.CopyPath ();
            context.NewPath ();

            double margin = 2 + hlwidth ;
            PointD center = new PointD (texture.Width*0.5, texture.Height*0.5);
            PointD transl = new PointD (center.X - margin, -(center.Y - margin));
            context.LineWidth = lwidth;
            sat =  (with_state==1 ? 0.0 : 1.0);
            context.SetSourceRGB (sat,sat,sat);

            context.Translate (center.X, center.Y);
            for (int i = 0; i < 4; i++) {
                context.Rotate (Math.PI * 0.5 * i);
                context.Translate (transl.X, transl.Y);
                context.AppendPath (arrow);
                context.Stroke ();
                context.Translate (-transl.X, -transl.Y);
            }

            ((IDisposable) arrow).Dispose ();
            ((IDisposable) context.Target).Dispose ();
            ((IDisposable) context).Dispose ();
        }

        protected override void CreatePassiveTexture (Clutter.CairoTexture texture, int with_state)
        {
            Render (texture, with_state, true);
        }

        protected override void CreateActiveTexture (Clutter.CairoTexture texture, int with_state)
        {
            Render (texture, with_state, false);
        }
        #endregion
    }
}
