// SharedTextureHandle.cs
//
//  Copyright (C) 2008 Chris Howie
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

namespace OpenVP.Core {
	public class SharedTextureHandle : TextureHandle {
		public SharedTextureHandle() : base() {
		}
		
		private int mReferences = 0;
		
		public void AddReference() {
			this.mReferences++;
		}
		
		public void RemoveReference() {
			if (this.mReferences > 0) {
				if (--this.mReferences == 0) {
					base.Dispose();
				}
			}
		}
		
		public override void Dispose() {
			this.RemoveReference();
		}
	}
}
