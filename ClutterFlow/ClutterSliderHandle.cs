//
// ClutterSliderHandle.cs
//
// Author:
//   Mathijs Dumon <mathijsken@hotmail.com>
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

//
// The ClutterSliderHandle is actually a wrapper around the ClutterSliderHandleButton.
// It restricts it's position to certain bounds and enables dragging of the button,
// and making the slider jump when clicking before & after the button.
//

using System;
using Clutter;
using Cairo;

namespace ClutterFlow
{
	
	
	public class ClutterSliderHandle : Group
	{

		#region Events
		public event EventHandler<System.EventArgs> SliderHasMoved;
		private void InvokeSliderHasMoved ()
		{
			if (SliderHasMoved!=null) SliderHasMoved (this, System.EventArgs.Empty);
		}		
		
		public event EventHandler<System.EventArgs> SliderHasChanged;
		private void InvokeSliderHasChanged ()
		{
			if (SliderHasChanged!=null) SliderHasChanged (this, System.EventArgs.Empty);
		}
		#endregion
		
		#region Fields
		
		protected ClutterSliderHandleButton button;
		public ClutterSliderHandleButton Button {
			get { return button; }
		}
			
		//position of the handle (as a fraction of width - handlewidth)
		protected float posval = 0.0f;
		public float Value {
			get { return posval; }
			set {
				if (value!=posval) {
					SetValueSilently (value);
					InvokeSliderHasChanged();
				}
			}
		}
		public void SetValueSilently (float value) {
			if (value!=posval) {
				if (value>1) posval=1;
				else if (value<0 || float.IsNaN(value) || float.IsInfinity(value)) posval=0;
				else posval = value;
				UpdatePosition();
				InvokeSliderHasMoved();
			}
		}

		protected float width; protected float height;
		protected float mouseX;

		#endregion
		
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

		private void UpdatePosition ()
		{
			if (!float.IsNaN(Value))
				button.SetPosition(Value * (width - height), 0);
			else
				button.SetPosition(0, 0);
		}
		
		#region Event Handling
		protected virtual void HandleEnterEvent (object o, EnterEventArgs args)
		{
			button.State |= 1;
			args.RetVal = true;
		}

		public new void SetSize (float width, float height)
		{
			this.width = width;
			this.height = height;
			base.SetSize (width, height);
		}

		protected virtual void HandleButtonPressEvent (object o, ButtonPressEventArgs args)
		{
			Clutter.Grab.Pointer(this);
			
			float x1; float y1; float x2;
			Clutter.EventHelper.GetCoords (args.Event, out x1, out y1);
			button.GetTransformedPosition (out x2, out y1);
			
			if ((x2 + button.Width) < x1) {
				//right side increase
				Value += 0.1f;
			} else if (x2 > x1) {
				//left side decrease
				Value -= 0.1f;
			}

			args.RetVal = true;
		}

		protected virtual void HandleButtonReleaseEvent (object o, ButtonReleaseEventArgs args)
		{
			if (args.Event.Source!=this) button.State = 0;
			else button.State &= 1;
			Clutter.Ungrab.Pointer ();
			InvokeSliderHasChanged ();
			args.RetVal = true;
		}
		
		protected virtual void HandleLeaveEvent (object o, LeaveEventArgs args)
		{
			button.State &= 2;
			args.RetVal = true;
		}
		
		protected void HandleMotionEvent (object o, MotionEventArgs args)
		{
			float x1; float y1;
			Clutter.EventHelper.GetCoords (args.Event, out x1, out y1);
			
			float tx; float ty;
			GetTransformedPosition (out tx, out ty);
			
			if (x1 <=  tx+Width && x1 >= tx && (args.Event.ModifierState.value__ & ModifierType.Button1Mask.value__)!=0 && (button.State & 2)!=0) {
				float deltaX = (x1 - mouseX);
				SetValueSilently (posval + (deltaX / (width - height)));
			}
			mouseX = x1;
			args.RetVal = true;
		}
		#endregion
		
		public void Update ()
		{
			button.Update ();
		}
	}
}
