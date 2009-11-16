
using System;
using Clutter;
using Cairo;

namespace Banshee.ClutterFlow
{
	
	
	public class ClutterSliderHandle : Group
	{
			
		public event EventHandler<System.EventArgs> SliderHasChanged;
		private void InvokeSliderHasChanged() {
			if (SliderHasChanged!=null) SliderHasChanged(this, System.EventArgs.Empty);
		}
		
		protected ClutterSliderHandleButton button;
			
		//position of the handle (as a fraction of width - handlewidth)
		protected float posval = 0.0f;
		public float Value {
			get { return posval; }
			set {
				if (value!=posval) {
					if (value>1) posval=1;
					else if (value<0) posval=0;
					else posval = value;
					UpdatePosition();
					InvokeSliderHasChanged();
				}
			}
		}
		private void UpdatePosition() {
			button.SetPosition(Value * (Width - Height), 0);
		}
		
		public ClutterSliderHandle(float x, float y, float width, float height, byte state) : base()
		{
			SetPosition(x,y);
			SetSize(width, height);

			button = new ClutterSliderHandleButton((uint) Height,(uint) Height,state);
			Add(button);
			button.Show();
			UpdatePosition();
			
			IsReactive = true;
			MotionEvent += HandleMotionEvent;
			ButtonPressEvent += HandleButtonPressEvent;
			ButtonReleaseEvent += HandleButtonReleaseEvent;
			EnterEvent += HandleEnterEvent;
			LeaveEvent += HandleLeaveEvent;
		}

		#region Event Handling
		protected virtual void HandleEnterEvent(object o, EnterEventArgs args)
		{
			button.State |= 1;
			args.RetVal = true;
		}

		protected virtual void HandleButtonPressEvent(object o, ButtonPressEventArgs args)
		{
			Clutter.Grab.Pointer(this);
			
			float x1; float y1; float x2;
			Clutter.EventHelper.GetCoords(args.Event, out x1, out y1);
			button.GetTransformedPosition(out x2, out y1);
			
			if ((x2 + button.Width) < x1) {
				//right side increase
				Value += 0.1f;
			} else if (x2 > x1) {
				//left side decrease
				Value -= 0.1f;
			}
			    
			
			args.RetVal = true;
		}

		protected virtual void HandleButtonReleaseEvent(object o, ButtonReleaseEventArgs args)
		{
			
			if (args.Event.Source!=this) button.State = 0;
			else button.State &= 1;
			Clutter.Ungrab.Pointer();
			args.RetVal = true;
		}
		
		protected virtual void HandleLeaveEvent(object o, LeaveEventArgs args)
		{
			button.State &= 2;
			args.RetVal = true;
		}
		
		private float mouseX;
		
		void HandleMotionEvent(object o, MotionEventArgs args)
		{
			float x1; float y1;
			Clutter.EventHelper.GetCoords(args.Event, out x1, out y1);
			
			float tx; float ty;
			GetTransformedPosition(out tx, out ty);
			
			if (x1 <=  tx+Width && x1 >= tx && (args.Event.ModifierState.value__ & ModifierType.Button1Mask.value__)!=0 && (button.State & 2)!=0) {
				float deltaX = (x1 - mouseX);
				Value += (deltaX / (Width - Height));
			}
			mouseX = x1;
			args.RetVal = true;
		}
		#endregion
		
		public void Update ()
		{
			button.Update();
		}

	}
}
