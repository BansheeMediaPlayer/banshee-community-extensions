
using System;
using Cairo;

using Clutter;

namespace ClutterFlow
{
	public delegate void CreateTextureMethod (CairoTexture texture, byte with_state);
	
	public class ClutterGenericButton : ClutterButton
	{
		CreateTextureMethod createTexture = null;
		
		public ClutterGenericButton(uint width, uint height, byte state, CreateTextureMethod createTexture) : base (width, height, state, false)
		{
			this.createTexture = createTexture;
			Initialise ();
		}

		protected override void CreateTexture (Clutter.CairoTexture texture, byte with_state)
		{
			if (createTexture!=null) createTexture(texture, with_state);
			else base.CreateTexture (texture, with_state);
		}

	}
}
