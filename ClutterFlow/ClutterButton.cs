
using System;
using Clutter;

namespace ClutterFlow
{

	public abstract class ClutterButtonState : Group {
		
		protected bool bubble = false;
		public virtual bool BubbleEvents {
			get { return bubble; }
			set { bubble = value; }
		}

		protected virtual void Initialise () {
			IsReactive = true;
			ButtonPressEvent += HandleButtonPressEvent;
			ButtonReleaseEvent += HandleButtonReleaseEvent;
			EnterEvent += HandleEnterEvent;
			LeaveEvent += HandleLeaveEvent;
		}
		
		protected int state = 0;
		protected abstract int MaxState { get; }
		//// <value>
		/// State represents the toggle buttons state, the bits represent:
		/// 	1: mouse_over
		/// 	2: mouse_down
		/// Overriding classes might have more bits,, check MaxBits
		/// </value>
		public virtual int State {
			get { return state; }
			set {
				value &= MaxState; //block any other bits
				if (value!=state) {
					state = value;
					Update();
				}
			}
		}
		
		protected virtual void HandleEnterEvent(object o, EnterEventArgs args)
		{
			State |= 1;
			args.RetVal = !BubbleEvents;
		}

		protected virtual void HandleButtonPressEvent(object o, ButtonPressEventArgs args)
		{
			State |= 2;
			args.RetVal = !BubbleEvents;
		}

		protected virtual void HandleButtonReleaseEvent(object o, ButtonReleaseEventArgs args)
		{
			State &= ~2;
			args.RetVal = !BubbleEvents;
		}
		
		protected virtual void HandleLeaveEvent(object o, LeaveEventArgs args)
		{
			State &= ~1;
			args.RetVal = !BubbleEvents;
		}

		public abstract void Update();
	}
	
	public class ClutterButton : ClutterButtonState
	{

		#region Fields
		protected override int MaxState {
			get { return 3; }
		}
		
		protected CairoTexture[] textures;
		protected virtual int GetTextureIndex(int state) {
			return ((state == 3) ? 2 : state);
		}
		public virtual CairoTexture StateTexture {
			get { return textures[GetTextureIndex(state)]; }
		}
		#endregion

		#region Initialization
		protected ClutterButton (uint width, uint height, byte state, bool init) : base () 
		{
			this.state = state;
			this.SetSize (width, height);
			
			if (init) Initialise ();
		}
		
		public ClutterButton (uint width, uint height, byte state) : this (width, height, state, true)
		{
		}

		protected override void Initialise () {
			CreateTextures ();
			base.Initialise ();
		}

		protected virtual void CreateTextures() {
			if (textures==null||textures.Length==0) InitTextures();
			for (int i=0; i < textures.Length; i++) {
				if (textures[i]!=null) {
					if (textures[i].Parent!=null) ((Container) textures[i].Parent).Remove(textures[i]);
					textures[i].Dispose();
				}
				textures[i] = new Clutter.CairoTexture((uint) Width,(uint) Height);
				Add(textures[i]);
				CreateTexture(textures[i], (byte) i);
			}
		}
		
		protected virtual void InitTextures() {
			textures = new CairoTexture[3];
		}
		#endregion

		#region Rendering
		protected virtual void CreateTexture(CairoTexture texture, byte with_state) {
			throw new System.NotImplementedException ();
		}
		#endregion
		
		public override void Update() {
			HideAll();
			StateTexture.Show();
			Show();
		}
		
	}
}
