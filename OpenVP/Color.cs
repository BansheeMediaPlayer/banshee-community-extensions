// Color.cs
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

namespace OpenVP {
	/// <summary>
	/// Represents a color consisting of red, green, blue, and alpha components.
	/// </summary>
	/// <remarks>
	/// Color components are not clamped to any range.
	/// </remarks>
	[Serializable]
	public struct Color {
		private float[] mComponents;
		
		/// <summary>
		/// The red component.
		/// </summary>
		/// <value>
		/// The red component.
		/// </value>
		public float Red {
			get {
				return this.mComponents[0];
			}
			set {
				this.mComponents[0] = value;
			}
		}
		
		/// <summary>
		/// The green component.
		/// </summary>
		/// <value>
		/// The green component.
		/// </value>
		public float Green {
			get {
				return this.mComponents[1];
			}
			set {
				this.mComponents[1] = value;
			}
		}
		
		/// <summary>
		/// The blue component.
		/// </summary>
		/// <value>
		/// The blue component.
		/// </value>
		public float Blue {
			get {
				return this.mComponents[2];
			}
			set {
				this.mComponents[2] = value;
			}
		}
		
		/// <summary>
		/// The alpha component.
		/// </summary>
		/// <value>
		/// The alpha component.
		/// </value>
		public float Alpha {
			get {
				return this.mComponents[3];
			}
			set {
				this.mComponents[3] = value;
			}
		}
		
		/// <summary>
		/// Creates a new color using the specified components.
		/// </summary>
		/// <param name="r">
		/// The red component.
		/// </param>
		/// <param name="g">
		/// The green component.
		/// </param>
		/// <param name="b">
		/// The blue component.
		/// </param>
		/// <param name="a">
		/// The alpha component.
		/// </param>
		public Color(float r, float g, float b, float a) {
			this.mComponents = new float[] { r, g, b, a };
		}
		
		/// <summary>
		/// Creates a new color using the specified components and an alpha
		/// component value of 1.
		/// </summary>
		/// <param name="r">
		/// The red component.
		/// </param>
		/// <param name="g">
		/// The green component.
		/// </param>
		/// <param name="b">
		/// The blue component.
		/// </param>
		public Color(float r, float g, float b) : this(r, g, b, 1) {
		}
		
		/// <summary>
		/// Issues glColor using the component values.
		/// </summary>
		public void Use() {
			Gl.glColor4fv(this.mComponents);
		}
	}
}
