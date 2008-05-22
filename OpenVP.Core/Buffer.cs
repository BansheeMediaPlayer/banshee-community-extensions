// Buffer.cs
//
//  Copyright (C) 2007-2008 Chris Howie
//
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 3 of the License, or (at your option) any later version.
//
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
// Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA 
//
//

using System;
using Tao.OpenGl;

using OpenVP.Metadata;

namespace OpenVP.Core {
	[Serializable, DisplayName("Buffer load/save"), Category("Miscellaneous"),
	 Description("Load a buffer to the screen or save the screen to a buffer."),
	 Author("Chris Howie")]
	public class Buffer : Effect {
		private const int BufferCount = 16;
		
		private static TextureHandle[] mTextures = null;
		
		private bool mLoad = false;
		
		[Browsable(true), DisplayName("Load"), Category("Buffer"),
		 Description("Whether to load from a buffer (on) or save to a buffer (off).")]
		public bool Load {
			get {
				return this.mLoad;
			}
			set {
				this.mLoad = value;
			}
		}
		
		private int mBufferId = 1;
		
		[Browsable(true), DisplayName("Buffer number"), Category("Buffer"),
		 Range(1, BufferCount), Description("The number of the buffer to use.")]
		public int BufferId {
			get {
				return this.mBufferId;
			}
			set {
				if (this.mBufferId < 1 || this.mBufferId > BufferCount)
					throw new ArgumentOutOfRangeException();
				
				this.mBufferId = value;
			}
		}
		
		public Buffer() {
		}
		
		private void InitTextures(IController controller) {
			if (mTextures == null) {
				mTextures = new TextureHandle[BufferCount];
				for (int i = 0; i < mTextures.Length; i++)
					mTextures[i] = new TextureHandle(controller.Width,
					                                 controller.Height);
				
				return;
			}
			
			foreach (TextureHandle i in mTextures)
				i.SetTextureSize(controller.Width,
				                 controller.Height);
		}
		
		public override void NextFrame(IController controller) {
		}
		
		public override void RenderFrame(IController controller) {
			this.InitTextures(controller);
			
			TextureHandle texture = mTextures[this.mBufferId - 1];
			
			Gl.glPushAttrib(Gl.GL_ENABLE_BIT);
			Gl.glEnable(Gl.GL_TEXTURE_2D);
			Gl.glDisable(Gl.GL_DEPTH_TEST);
			Gl.glTexEnvf(Gl.GL_TEXTURE_ENV, Gl.GL_TEXTURE_ENV_MODE, Gl.GL_DECAL);
			
			texture.SetTextureSize(controller.Width,
			                       controller.Height);
			
			Gl.glBindTexture(Gl.GL_TEXTURE_2D, texture.TextureId);
			
			Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_WRAP_S,
			                   Gl.GL_CLAMP);
			
			Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_WRAP_T,
			                   Gl.GL_CLAMP);
			
			if (this.Load) {
				Gl.glColor4f(1, 1, 1, 1);
				Gl.glBegin(Gl.GL_QUADS);
				
				Gl.glTexCoord2f(0, 0);
				Gl.glVertex2f(-1, -1);
				
				Gl.glTexCoord2f(0, 1);
				Gl.glVertex2f(-1,  1);
				
				Gl.glTexCoord2f(1, 1);
				Gl.glVertex2f( 1,  1);
				
				Gl.glTexCoord2f(1, 0);
				Gl.glVertex2f( 1, -1);
				
				Gl.glEnd();
			} else {
				Gl.glCopyTexImage2D(Gl.GL_TEXTURE_2D, 0, Gl.GL_RGB, 0, 0,
				                    controller.Width,
				                    controller.Height, 0);
			}
			
			Gl.glPopAttrib();
		}
	}
}
