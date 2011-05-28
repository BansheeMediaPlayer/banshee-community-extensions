//
// PartyModeButton.cs
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
    public class PartyModeButton : ClutterToggleButton
    {
        public PartyModeButton (bool toggled) : base (25, 25, toggled)
        {
        }

        public PartyModeButton() : this (false)
        {
        }

        #region Rendering
        protected void DrawSmallCovers (Cairo.Context context, float width, float height, double lwidth) {
            context.Save ();

            double hlwidth = lwidth*0.5;

            context.MoveTo (hlwidth, height-hlwidth);
            context.LineTo (hlwidth, 0.3*(height-lwidth));
            context.LineTo ((width-lwidth)*0.65, hlwidth);
            context.LineTo ((width-lwidth)*0.65, 0.7*(height-lwidth));
            context.ClosePath ();
            context.LineWidth = lwidth;
            context.SetSourceRGBA (0.1,0.1,0.1,1.0);
            context.FillPreserve ();
            context.SetSourceRGBA (1.0,1.0,1.0,0.7);
            context.Stroke ();
            context.Translate ((4+hlwidth), 0);
            context.MoveTo (hlwidth, height-hlwidth);
            context.LineTo (hlwidth, 0.3*(height-lwidth));
            context.LineTo ((width-lwidth)*0.65, hlwidth);
            context.LineTo ((width-lwidth)*0.65, 0.7*(height-lwidth));
            context.ClosePath ();
            context.SetSourceRGBA (0.1,0.1,0.1,1.0);
            context.FillPreserve ();
            context.SetSourceRGBA (1.0,1.0,1.0,0.7);
            context.Stroke ();
            context.Translate (-(4+hlwidth), 0);

            context.Restore ();
        }

        protected override void CreatePassiveTexture (Clutter.CairoTexture texture, int with_state)
        {
            texture.Clear ();
            Cairo.Context context = texture.Create ();

            double lwidth = 1;
            double hlwidth = lwidth*0.5;

            //Draw outline rectangles:
            DrawSmallCovers (context, texture.Width, texture.Height, lwidth);

            //Draw play icon:
            context.MoveTo ((texture.Width-lwidth)*0.5, 0.3*(texture.Height-lwidth));
            context.LineTo ((texture.Width-lwidth)*0.5, texture.Height-hlwidth);
            context.LineTo (texture.Width-hlwidth, 0.65*(texture.Height-lwidth));
            context.ClosePath ();
            context.LineWidth = lwidth;
            double sat = (with_state==0 ? 0.4 : (with_state==1 ? 0.6 : 0.8));
            context.SetSourceRGBA (sat, sat, sat, 1.0);
            context.FillPreserve ();
            context.SetSourceRGB (1.0,1.0,1.0);
            context.Stroke ();

            ((IDisposable) context.Target).Dispose();
            ((IDisposable) context).Dispose();
        }

        protected override void CreateActiveTexture (Clutter.CairoTexture texture, int with_state)
        {
            texture.Clear ();
            Cairo.Context context = texture.Create ();

            double lwidth = 1;
            double hlwidth = lwidth*0.5;

            //Draw outline rectangles:
            DrawSmallCovers (context, texture.Width, texture.Height, lwidth);

            //Draw stop icon:
            double dim = Math.Min (texture.Width*0.6 - hlwidth, texture.Height*0.6 - hlwidth);
            context.Rectangle (texture.Width*0.4, texture.Height*0.4, dim, dim);
            context.LineWidth = lwidth;
            double sat = (with_state==0 ? 0.4 : (with_state==1 ? 0.6 : 0.8));
            context.SetSourceRGBA (sat, sat, sat, 1.0);
            context.FillPreserve ();
            context.SetSourceRGB (1.0,1.0,1.0);
            context.Stroke ();

            ((IDisposable) context.Target).Dispose ();
            ((IDisposable) context).Dispose ();
        }
        #endregion
    }
}
