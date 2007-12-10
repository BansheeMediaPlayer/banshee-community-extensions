// ClearScreen.cs
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
using OpenVP;
using Tao.OpenGl;

namespace OpenVP.Core {
	[Serializable, EffectTitle("Clear screen")]
	public sealed class ClearScreen : Effect {
		public Color ClearColor = new Color(0, 0, 0);
		
		public ClearScreen() {
		}
		
		public override void NextFrame(Controller controller) {
		}
		
		public override void RenderFrame(Controller controller) {
			Gl.glClearColor(this.ClearColor.Red,
			                this.ClearColor.Green,
			                this.ClearColor.Blue,
			                this.ClearColor.Alpha);
			
			Gl.glClear(Gl.GL_COLOR_BUFFER_BIT);
		}
	}
}
