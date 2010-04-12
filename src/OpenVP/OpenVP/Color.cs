// Color.cs
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

namespace OpenVP {
	/// <summary>
	/// Represents a color consisting of red, green, blue, and alpha components.
	/// </summary>
	/// <remarks>
	/// Color components are not clamped to any range.
	/// </remarks>
	[Serializable]
	public struct Color {
		private float mRed;
		private float mGreen;
		private float mBlue;
		private float mAlpha;
		
		/// <summary>
		/// The red component.
		/// </summary>
		/// <value>
		/// The red component.
		/// </value>
		public float Red {
			get {
				return this.mRed;
			}
			set {
				this.mRed = value;
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
				return this.mGreen;
			}
			set {
				this.mGreen = value;
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
				return this.mBlue;
			}
			set {
				this.mBlue = value;
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
				return this.mAlpha;
			}
			set {
				this.mAlpha = value;
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
			this.mRed = r;
			this.mGreen = g;
			this.mBlue = b;
			this.mAlpha = a;
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
			Gl.glColor4f(this.mRed, this.mGreen, this.mBlue, this.mAlpha);
		}
		
		/// <summary>
		/// Create a Color struct from an HSL tuple.
		/// </summary>
		/// <param name="h">
		/// Hue.
		/// </param>
		/// <param name="s">
		/// Saturation.
		/// </param>
		/// <param name="l">
		/// Luminance.
		/// </param>
		/// <returns>
		/// The Color struct representing the specified HSL tuple.
		/// </returns>
		public static Color FromHSL(float h, float s, float l) {
			byte r, g, b;
			
			if (s == 0) {
				unchecked {
					r = (byte) (l * 255);
					g = (byte) (l * 255);
					b = (byte) (l * 255);
				}
			} else {
				float temp2;
				if (l < 0.5)
					temp2 = l * (1 + s);
				else
					temp2 = (l + s) - (l * s);
				
				float temp1 = 2 * l - temp2;
				
				h /= 360;
				
				r = HSLPart2(temp1, temp2, h + 1/3f);
				g = HSLPart2(temp1, temp2, h);
				b = HSLPart2(temp1, temp2, h - 1/3f);
			}
			
			return new Color((float) r / 255, (float) g / 255, (float) b / 255);
		}
		
		private static byte HSLPart2(float temp1, float temp2, float temp3) {
			if (temp3 < 0)
				temp3 += 1;
			else if (temp3 > 1)
				temp3 -= 1;
			
			if (temp3 < 1/6f)
				temp1 = (temp1 + (temp2 - temp1) * 6 * temp3);
			else if (temp3 < 1/2f)
				temp1 = temp2;
			else if (temp3 < 2/3f)
				temp1 = (temp1 + ((temp2 - temp1) * (2/3f - temp3) * 6));
			
			return unchecked((byte) (255 * temp1));
		}
	}
}
