
using System;
using Cairo;

using Clutter;
using ClutterFlow;

namespace Banshee.ClutterFlow
{
	
	public class FullscreenButton : ClutterToggleButton
	{
		
		public FullscreenButton() : base (25, 25, 0)
		{
		}

		#region Rendering
		private void Render(Clutter.CairoTexture texture, byte with_state, bool outwards)
		{
			
			texture.Clear();
			Cairo.Context context = texture.Create();
			
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
			PointD center = new PointD(texture.Width*0.5, texture.Height*0.5);
			PointD transl = new PointD(center.X - margin, -(center.Y - margin));
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
		
		protected override void CreatePassiveTexture (Clutter.CairoTexture texture, byte with_state)
		{
			Render (texture, with_state, true);
		}

		protected override void CreateActiveTexture (Clutter.CairoTexture texture, byte with_state)
		{
			Render (texture, with_state, false);
		}
		#endregion
	}
}
