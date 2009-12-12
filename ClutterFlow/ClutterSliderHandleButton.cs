using System;
using Clutter;
using Cairo;

namespace ClutterFlow
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
		
		public ClutterSliderHandleButton(uint width, uint height, byte state) : base(width, height, state) {
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

		
		protected override void CreateTexture (Clutter.CairoTexture texture, byte with_state) {
			texture.Clear();
			Cairo.Context context = texture.Create();
			
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
		
		protected override void HandleButtonPressEvent (object o, Clutter.ButtonPressEventArgs args)
		{
			base.HandleButtonPressEvent (o, args);
			args.RetVal = false;
		}

	}
}