
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
