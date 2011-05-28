//
// ClutterSliderHandleButton.cs
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

    public class ClutterSliderHandleButton : ClutterButton
    {
        protected Clutter.Text label;
        public string Label {
            get { return label.Value; }
            set {
                if (value!=label.Value) {
                    label.Value = value;
                    Update ();
                }
            }
        }

        public ClutterSliderHandleButton(uint width, uint height, int state) : base(width, height, state) {
            label = new Text("Sans Bold 10", "", new Clutter.Color(0.0f,0.0f,0.0f,0.8f));
            Add (label);
            label.SetAnchorPoint (label.Width*0.5f, label.Height*0.5f);
            label.SetPosition (this.Width*0.5f,this.Height*0.5f);
            label.ShowAll();
        }

        public override void Update ()
        {
            base.Update ();
            label.SetAnchorPoint (label.Width*0.5f, label.Height*0.5f);
            label.SetPosition (this.Width*0.5f,this.Height*0.5f);
            label.ShowAll ();
        }


        protected override void CreateTexture (Clutter.CairoTexture texture, int with_state) {
            texture.Clear();
            Cairo.Context context = texture.Create ();

            context.Translate(texture.Width*0.5,texture.Height*0.5);
            context.Arc(0,0,(texture.Height-1)*0.5,0,2*Math.PI);
            context.ClosePath();
            context.SetSourceRGBA(1.0,1.0,1.0, with_state==0 ? 0.3 : (with_state==1 ? 0.5 : 0.7));
            context.FillPreserve();
            context.SetSourceRGB(1.0,1.0,1.0);
            context.LineWidth = 1;
            context.Stroke();

            ((IDisposable) context.Target).Dispose();
            ((IDisposable) context).Dispose();
        }
    }
}