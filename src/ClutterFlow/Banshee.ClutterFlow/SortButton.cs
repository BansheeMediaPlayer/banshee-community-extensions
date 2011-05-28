

using System;

using Cairo;

using Clutter;

using ClutterFlow;
using ClutterFlow.Buttons;

namespace Banshee.ClutterFlow.Buttons
{

    public class SortButton : ClutterToggleButton
    {

        public SortButton (bool toggled) : base (25, 25, toggled)
        {
        }

        public SortButton () : this (false)
        {
        }

        #region Rendering
        private void Render(Clutter.CairoTexture texture, int with_state, bool outwards)
        {

            texture.Clear ();
            Cairo.Context context = texture.Create ();

            double alpha_f = with_state == 0 ? 0.5 : (with_state == 1 ? 0.8 : 1);

            double lwidth = 1;
            double hlwidth = lwidth*0.5;
            context.LineWidth = lwidth;

            if (outwards) {
                context.MoveTo (texture.Width*0.5-texture.Height*0.45, texture.Height*0.9);
                context.CurveTo (texture.Width*0.3, texture.Height, texture.Width*0.6, texture.Height, texture.Width*0.5+texture.Height*0.45, texture.Height*0.9);
                context.ArcNegative (texture.Width*0.5, texture.Height*0.9, texture.Height*0.45, 0, Math.PI);
                context.ClosePath ();
                Gradient g1 = new LinearGradient (0, texture.Height/2, 0, texture.Height);
                g1.AddColorStop (0, new Cairo.Color (0.6, 0.6, 0.6, 1.0*alpha_f));
                g1.AddColorStop (1.0, new Cairo.Color (1.0, 1.0, 1.0, 1.0*alpha_f));
                context.Pattern = g1;
                context.FillPreserve ();
                context.SetSourceRGBA (1.0, 1.0, 1.0, 1.0*alpha_f);
                context.Stroke ();
                ((IDisposable) g1).Dispose ();

                context.Arc (Width*0.5, Height*0.33+lwidth, Height*0.33, 0, Math.PI*2);
                context.ClosePath ();
                context.Operator = Operator.Source;
                Gradient g2 = new RadialGradient (texture.Width*0.5, texture.Height*0.25, 0, texture.Width*0.5, texture.Height*0.25, texture.Width*0.5);
                g2.AddColorStop (0, new Cairo.Color (1.0, 1.0, 1.0, 1.0*alpha_f));
                g2.AddColorStop (1.0, new Cairo.Color (0.6, 0.6, 0.6, 1.0*alpha_f));
                context.Pattern = g2;
                //context.SetSourceRGBA (1.0, 1.0, 1.0, 1.0*alpha_f);
                context.FillPreserve ();
                Gradient g3 = new LinearGradient (0, 0, 0, texture.Height*0.5);
                g3.AddColorStop (0, new Cairo.Color (1.0, 1.0, 1.0, 1.0*alpha_f));
                g3.AddColorStop (1.0, new Cairo.Color (0, 0, 0, 1.0*alpha_f));
                context.Pattern = g3;
                //context.SetSourceRGBA (1.0, 1.0, 1.0, 1.0*alpha_f);
                context.Stroke ();
                ((IDisposable) g2).Dispose ();
                ((IDisposable) g3).Dispose ();


            } else {
                Cairo.PointD c = new Cairo.PointD (texture.Width*0.5, texture.Height*0.5);
                double max_r = Math.Min (c.X, c.Y) - hlwidth;

                context.Arc (c.X, c.Y, max_r, 0, Math.PI*2);
                context.ArcNegative (c.X, c.Y, max_r*0.25, Math.PI*2, 0);
                context.ClosePath ();
                context.SetSourceRGBA (0.5, 0.5, 0.5, 1.0*alpha_f);
                context.StrokePreserve ();
                Gradient g1 = new LinearGradient (0, texture.Height, texture.Width, 0);
                g1.AddColorStop(0, new Cairo.Color (1.0, 1.0, 1.0, 1.0*alpha_f));
                g1.AddColorStop(0.5, new Cairo.Color (0.7, 0.7, 0.7, 1.0*alpha_f));
                g1.AddColorStop(1, new Cairo.Color (0.9, 0.9, 0.9, 1.0*alpha_f));
                context.Pattern = g1;
                context.Fill ();
                ((IDisposable) g1).Dispose ();

                context.ArcNegative (c.X, c.Y, max_r*0.25+lwidth, Math.PI*1.75, Math.PI*0.75);
                context.Arc (c.X, c.Y, max_r, Math.PI*0.75, Math.PI*1.75);
                context.ClosePath ();
                Gradient g2 = new LinearGradient (c.X, c.Y, c.X*0.35, c.Y*0.35);
                g2.AddColorStop(0, new Cairo.Color (1.0, 1.0, 1.0, 1.0*alpha_f));
                g2.AddColorStop(1, new Cairo.Color (1.0, 1.0, 1.0, 0.0));
                context.Pattern = g2;
                context.Fill ();
                ((IDisposable) g2).Dispose ();

                context.ArcNegative (c.X, c.Y, max_r*0.25+lwidth, Math.PI*2, 0);
                context.Arc (c.X, c.Y, max_r*0.45, 0, Math.PI*2);
                context.SetSourceRGBA (1.0, 1.0, 1.0, 0.8*alpha_f);
                context.Fill ();

                context.Arc (c.X, c.Y, max_r-lwidth, 0, Math.PI*2);
                Gradient g3 = new LinearGradient (0, texture.Height, texture.Width, 0);
                g3.AddColorStop(0, new Cairo.Color (1.0, 1.0, 1.0, 0.0));
                g3.AddColorStop(1, new Cairo.Color (0.9, 0.9, 0.9, 1.0*alpha_f));
                context.Pattern = g3;
                context.Stroke ();
                ((IDisposable) g3).Dispose ();
            }

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
