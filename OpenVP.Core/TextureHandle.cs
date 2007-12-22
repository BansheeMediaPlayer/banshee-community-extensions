// TextureHandle.cs
//
//  Copyright (C) 2007 Chris Howie
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
//
//

using System;
using Tao.OpenGl;

namespace OpenVP.Core {
	public class TextureHandle : IDisposable {
		private int mTextureId = -1;
		
		public int TextureId {
			get {
				return this.mTextureId;
			}
		}
		
		private int mTextureSize = -1;
		
		private bool mHaveTexture = false;
		
		public TextureHandle() {
		}
		
		public TextureHandle(int w, int h) {
			this.SetTextureSize(w, h);
		}
		
		public void SetTextureSize(int w, int h) {
			int[] tex;
			
			w = Math.Max(w, h);
			
			if (this.mHaveTexture) {
				if (this.mTextureSize >= w)
					return;
				
				tex = new int[] { this.mTextureId };
				
				Gl.glDeleteTextures(1, tex);
			} else {
				tex = new int[1];
			}
			
			Gl.glGetError();
			
			Gl.glGenTextures(1, tex);
			
			if (Gl.glGetError() != Gl.GL_NO_ERROR)
				throw new InvalidOperationException("Cannot create texture.");
			
			this.mTextureId = tex[0];
			this.mHaveTexture = true;
			
			int size = 1;
			
			while (size < w)
				size <<= 1;
			
			this.mTextureSize = size;
			
			Gl.glBindTexture(Gl.GL_TEXTURE_2D, this.mTextureId);
			
			byte[] mData = new byte[size * size * 3];
			
			Gl.glTexImage2D(Gl.GL_TEXTURE_2D, 0, Gl.GL_RGB, size, size, 0,
			                Gl.GL_RGB, Gl.GL_UNSIGNED_BYTE, mData);
			
			Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_MAG_FILTER, Gl.GL_LINEAR);
			Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_MIN_FILTER, Gl.GL_LINEAR);
		}
		
		public void Dispose() {
			if (this.mHaveTexture) {
				Gl.glDeleteTextures(1, new int[] { this.mTextureId });
				this.mHaveTexture = false;
			}
		}
	}
}
