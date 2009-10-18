using System;
using Clutter;
using Cairo;

namespace Banshee.ClutterFlow
{
	
	public class ClutterSliderHandleButton : ClutterButton
	{
		
		public ClutterSliderHandleButton(uint width, uint height, byte state) : base(width, height, state) {
		}
		
		#region Event Handling
		
		protected override void HandleEnterEvent (object o, Clutter.EnterEventArgs args)
		{
			args.RetVal = false; //pass it on to the parent
		}
		
		protected override void HandleButtonReleaseEvent (object o, Clutter.ButtonReleaseEventArgs args)
		{
			args.RetVal = false; //pass it on to the parent
		}
		
		protected override void HandleLeaveEvent (object o, Clutter.LeaveEventArgs args)
		{
			args.RetVal = false; //pass it on to the parent
		}
		#endregion

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
	}
}