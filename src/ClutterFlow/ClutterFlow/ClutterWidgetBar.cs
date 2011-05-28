//
// ClutterWidgetBar.cs
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

namespace ClutterFlow
{

    public class ClutterWidgetBar : Group
    {

        protected CairoTexture text;

        protected float marginX = 6;
        public float MarginX {
            get { return marginX; }
            set {
                if (value!=marginX) {
                    marginX = value;
                    UpdatePositions ();
                }
            }
        }

        protected float marginY = 4;
        public float MarginY {
            get { return marginY; }
            set {
                if (value!=marginY) {
                    marginY = value;
                    UpdatePositions ();
                }
            }
        }

        protected float spacing = 5;
        public float Spacing {
            get { return spacing; }
            set {
                if (value!=spacing) {
                    spacing = value;
                    UpdatePositions ();
                }
            }
        }

        public ClutterWidgetBar (Actor[] actors) : base ()
        {
            foreach (Actor actor in actors) {
                this.Add (actor);
            }
            text = new CairoTexture ((uint) Width,(uint) Height);
            this.Add (text);

            UpdatePositions ();

            this.ActorAdded += HandleActorAdded;
            this.ActorRemoved += HandleActorRemoved;
        }

        void HandleActorRemoved(object o, ActorRemovedArgs args)
        {
            UpdatePositions ();
        }

        void HandleActorAdded(object o, ActorAddedArgs args)
        {
            UpdatePositions ();
        }

        protected void UpdatePositions ()
        {
            /* TODO: variable heights need to be handled */
            float x = marginX; float y = marginY;
            text.Hide ();
            foreach (Actor actor in this) {
                if (actor!=text) {
                    actor.Hide ();
                    actor.SetPosition (x, y);
                    x += actor.Width + spacing;
                    actor.Show ();
                }
            }

            UpdateTexture ();
        }

        protected void UpdateTexture ()
        {
            text.SetSurfaceSize ((uint) (Width+MarginX),(uint) (Height+MarginY));
            text.Clear ();
            Cairo.Context context = text.Create ();

            double lwidth = 1;
            double hlwidth = lwidth*0.5;
            double width = Width - lwidth;
            double height = Height - lwidth;
            double radius = Math.Min(marginX, marginY)*0.75;

            if ((radius > height / 2) || (radius > width / 2))
                radius = Math.Min(height / 2, width / 2);

            context.MoveTo (hlwidth, hlwidth + radius);
            context.Arc (hlwidth + radius, hlwidth + radius, radius, Math.PI, -Math.PI / 2);
            context.LineTo (hlwidth + width - radius, hlwidth);
            context.Arc (hlwidth + width - radius, hlwidth + radius, radius, -Math.PI / 2, 0);
            context.LineTo (hlwidth + width, hlwidth + height - radius);
            context.Arc (hlwidth + width - radius, hlwidth + height - radius, radius, 0, Math.PI / 2);
            context.LineTo (hlwidth + radius, hlwidth + height);
            context.Arc (hlwidth + radius, hlwidth + height - radius, radius, Math.PI / 2, Math.PI);
            context.ClosePath ();

            context.LineWidth = lwidth;
            context.SetSourceRGB (1.0,1.0,1.0);
            context.Stroke ();

            ((IDisposable) context.Target).Dispose ();
            ((IDisposable) context).Dispose ();
        }

    }
}
