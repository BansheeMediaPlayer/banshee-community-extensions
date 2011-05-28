//
// ClutterArrowButton.cs
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
using Clutter;
using Cairo;

namespace ClutterFlow.Buttons
{

    public class ClutterArrowButton : ClutterButton
    {
        private byte sense = 0;
        public byte Sense {
            get { return sense; }
            set {
                if (value!=sense) {
                    sense = value;
                    CreateTextures();
                    Update();
                }
            }
        }

        public ClutterArrowButton(uint width, uint height, int state, byte sense) : base(width, height, state)
        {
            Sense = sense;
        }

        protected override void CreateTexture (Clutter.CairoTexture texture, int with_state)
        {
            texture.Clear ();
            Cairo.Context context = texture.Create ();

            double lwidth = 1;
            double hlwidth = lwidth*0.5;
            double rotation = Math.PI*(3 - (double) sense) * 0.5;
            PointD center = new PointD(texture.Width*0.5, texture.Height*0.5);

            //Set the correct orientation:
            context.Translate(center.X, center.Y);
            context.Rotate(rotation);
            context.Translate(-center.X, -center.Y);

            //Draw border:
            context.MoveTo        (texture.Width*0.5, hlwidth);
            context.ArcNegative    (texture.Width,center.Y,(texture.Height-lwidth)/2,1.5*Math.PI,0.5*Math.PI);
            context.LineTo        (texture.Width*0.5, texture.Height-hlwidth);
            context.Arc            (texture.Width*0.5,center.Y,(texture.Height-lwidth)/2,0.5*Math.PI,1.5*Math.PI);
            context.ClosePath    ();
            context.LineWidth = lwidth;
            context.SetSourceRGBA(1.0,1.0,1.0, with_state==0 ? 0.4 : (with_state==1 ? 0.6 : 0.8));
            context.FillPreserve();
            context.SetSourceRGB(1.0,1.0,1.0);
            context.Stroke();

            //Draw arrow:
            context.MoveTo        (center.X, center.Y-texture.Height*0.25);
            context.LineTo        (center.X-texture.Height*0.25, center.Y);
            context.LineTo        (center.X, center.Y+texture.Height*0.25);
            context.LineWidth = lwidth*1.5;
            context.SetSourceRGB(0.0,0.0,0.0);
            context.Stroke();

            ((IDisposable) context.Target).Dispose();
            ((IDisposable) context).Dispose();
        }
    }
}
