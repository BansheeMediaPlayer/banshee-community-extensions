// TextureHandle.cs
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
using System.Threading;

using Tao.OpenGl;

namespace OpenVP.Core {
	public class TextureHandle : IDisposable {
		private Thread mAllocatedBy = null;
		
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
			
			this.mAllocatedBy = Thread.CurrentThread;
			
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
		
		public virtual void Dispose() {
			if (this.mHaveTexture) {
				if (this.mAllocatedBy != Thread.CurrentThread) {
					throw new InvalidOperationException("Texture handle must be disposed of on the same thread it was allocated on.");
				}
				
				this.mHaveTexture = false;
				Gl.glDeleteTextures(1, new int[] { this.mTextureId });
			}
		}
		
		~TextureHandle() {
			this.Dispose();
		}
	}
}
