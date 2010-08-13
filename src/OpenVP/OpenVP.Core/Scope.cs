// Scope.cs
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
	[Serializable, Browsable(true), DisplayName("Basic scope"),
	 Category("Render"),
	 Description("Draws a scope with limited customizability."),
	 Author("Chris Howie")]
	public class Scope : Effect {
		private Color mColor = new Color(1, 1, 1);
		
		[Browsable(true), DisplayName("Color"), Category("Display"),
		 Description("The color used to draw the scope.")]
		public Color Color {
			get {
				return this.mColor;
			}
			set {
				this.mColor = value;
			}
		}
		
		private float mLineWidth = 1;
		
		[Browsable(true), DisplayName("Line width"), Category("Display"),
		 Description("Scope line width."), DefaultValue(1f)]
		public float LineWidth {
			get {
				return this.mLineWidth;
			}
			set {
				this.mLineWidth = value;
			}
		}
		
		private bool mCircular = false;
		
		[Browsable(true), DisplayName("Circular"), Category("Display"),
		 Description("Whether to draw the scope from left to right (off) or in a circle (on)."),
		 DefaultValue(false)]
		public bool Circular {
			get {
				return this.mCircular;
			}
			set {
				this.mCircular = value;
			}
		}
		
		private bool mFrequency = false;
		
		[Browsable(true), DisplayName("Frequency"), Category("Display"),
		 Description("Whether to draw the scope using PCM (off) or frequency (on) data."),
		 DefaultValue(false)]
		public bool Frequency {
			get {
				return this.mFrequency;
			}
			set {
				this.mFrequency = value;
			}
		}
		
		public Scope() {
		}
		
		public override void NextFrame(IController controller) {
		}
		
		public override void RenderFrame(IController controller) {
			float[] data;
			
			if (this.mFrequency) {
				data = new float[controller.PlayerData.NativeSpectrumLength];
				controller.PlayerData.GetSpectrum(data);
			} else {
				data = new float[controller.PlayerData.NativePCMLength];
				controller.PlayerData.GetPCM(data);
			}
			
			this.mColor.Use();
			
			Gl.glLineWidth(this.mLineWidth);
			
			if (this.mCircular) {
				Gl.glBegin(Gl.GL_LINE_LOOP);
				
				double rc = 2 * Math.PI / data.Length;
				double r = 0;
				
				for (int i = 0; i < data.Length; i++) {
					Gl.glVertex2d(Math.Cos(r) * (data[i] / 2 + 0.5),
					              Math.Sin(r) * (data[i] / 2 + 0.5));
					r += rc;
				}
				
				Gl.glEnd();
			} else {
				Gl.glMatrixMode(Gl.GL_MODELVIEW);
				Gl.glPushMatrix();
				
				Gl.glTranslatef(-1, 0, 0);
				Gl.glScalef(2, 1, 1);
				
				Gl.glBegin(Gl.GL_LINE_STRIP);
				
				for (int i = 0; i < data.Length; i++) {
					Gl.glVertex2f((float) i / (data.Length - 1), data[i]);
				}
				
				Gl.glEnd();
				
				Gl.glPopMatrix();
			}
		}
	}
}
