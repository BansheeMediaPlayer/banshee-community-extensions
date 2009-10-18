
using System;
using Clutter;

namespace Banshee.ClutterFlow
{
	
	
	public class ClutterButton : Group
	{
		protected CairoTexture[] textures;
		protected virtual int GetTextureIndex(byte state) {
			return ((state == 3) ? 2 : (int) state);
		}
		public virtual CairoTexture StateTexture {
			get { return textures[GetTextureIndex(state)]; }
		}
		
		protected byte state = 0;
		public virtual byte State {
			get { return state; }
			set {
				if (value!=state) {
					state = value;
					Update();
				}
			}
		}
		
		public ClutterButton(uint width, uint height, byte state) : base()
		{
			this.state = state;
			this.SetSize(width, height);
			
			CreateTextures();
			
			IsReactive = true;
			ButtonPressEvent += HandleButtonPressEvent;
			ButtonReleaseEvent += HandleButtonReleaseEvent;
			EnterEvent += HandleEnterEvent;
			LeaveEvent += HandleLeaveEvent;
		}
		
		#region Event Handling
		protected virtual void HandleEnterEvent(object o, EnterEventArgs args)
		{
			State |= 1;
		}

		protected virtual void HandleButtonPressEvent(object o, ButtonPressEventArgs args)
		{
			State |= 2;
		}

		protected virtual void HandleButtonReleaseEvent(object o, ButtonReleaseEventArgs args)
		{
			State &= 1;
		}
		
		protected virtual void HandleLeaveEvent(object o, LeaveEventArgs args)
		{
			State &= 2;
		}
		#endregion
		
		protected virtual void InitTextures() {
			textures = new CairoTexture[3];
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
		
		protected virtual void CreateTexture(CairoTexture texture, byte with_state) {
			throw new System.NotImplementedException ();
		}
		
		public virtual void Update() {
			HideAll();
			StateTexture.Show();
			Show();
		}
		
	}
}
