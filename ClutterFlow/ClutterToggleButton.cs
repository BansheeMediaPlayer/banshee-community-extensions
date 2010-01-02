
using System;
using Cairo;

using Clutter;


namespace ClutterFlow
{
	
	
	public abstract class ClutterToggleButton : ClutterButtonState
	{

		public event EventHandler Toggled;
		protected void InvokeToggled () {
			if (Toggled!=null) Toggled (this, EventArgs.Empty);
		}
		
		//// <value>
		/// MaxBits represents the toggle buttons maximal bit-states to be set:
		/// 	1: mouse_over
		/// 	2: mouse_down
		/// 	4: toggled
		/// </value>
		protected override int MaxState {
			get { return 7; }
		}

		private bool freeze_state = false;
		public virtual bool IsActive {
			get { return (state & 4) > 0;	}
			set {
				if (!freeze_state) {
					if (value && (state & 4)==0) {
						State |= 4;
						InvokeToggled ();
					} else if (!value && (state & 4)>0) {
						State &= ~4;
						InvokeToggled ();
					}
				}
			}
		}
		
		public virtual CairoTexture StateTexture {
			get { return (IsActive) ? active_button.StateTexture : passive_button.StateTexture; }
		}
		
		protected ClutterGenericButton passive_button;
		protected ClutterGenericButton active_button;
		
		public ClutterToggleButton(uint width, uint height, byte state) : base () 
		{
			this.SetSize (width, height);
			this.state = state;
			
			passive_button = new ClutterGenericButton(width, height, state, CreatePassiveTexture);
			passive_button.BubbleEvents = true;
			active_button  = new ClutterGenericButton(width, height, state, CreateActiveTexture);
			active_button.BubbleEvents = true;
			
			Initialise ();
		}

		protected override void Initialise () {
			Add (passive_button);
			Add (active_button);
			
			base.Initialise ();

			active_button.Update ();
			passive_button.Update ();
			Update ();
			Show ();
		}
		
		#region Event Handling
		protected override void HandleButtonReleaseEvent(object o, ButtonReleaseEventArgs args)
		{
			State = (state^4) & ~2;
			freeze_state = true; //freeze the state untill after the event has finished handling
			InvokeToggled ();
			freeze_state = false; //thaw the state
			args.RetVal = !bubble;
		}
		#endregion
		
		public override void Update () {
			if ((state & 4) > 0) {
				passive_button.Hide ();
				active_button.State = state;
				active_button.Show ();
			} else {
				active_button.Hide ();
				passive_button.State = state;
				passive_button.Show ();
			}
			Show ();
		}

		protected abstract void CreatePassiveTexture (Clutter.CairoTexture texture, byte with_state);
		protected abstract void CreateActiveTexture (Clutter.CairoTexture texture, byte with_state);
	}
}
