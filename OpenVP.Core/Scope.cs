// Scope.cs
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
using System.ComponentModel;

using Tao.OpenGl;

namespace OpenVP.Core {
	[Serializable, Browsable(true), DisplayName("Basic scope"),
	 Description("Draws a scope with limited customizability.")]
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
		 Description("Whether to draw the scope from right to left (off) or in a circle (on)."),
		 DefaultValue(false)]
		public bool Circular {
			get {
				return this.mCircular;
			}
			set {
				this.mCircular = value;
			}
		}
		
		public Scope() {
		}
		
		public override void NextFrame(Controller controller) {
		}
		
		public override void RenderFrame(Controller controller) {
			float[] pcm = new float[controller.PlayerData.NativePCMLength];
			
			controller.PlayerData.GetPCM(pcm);
			
			this.mColor.Use();
			
			Gl.glLineWidth(this.mLineWidth);
			
			if (this.mCircular) {
				Gl.glBegin(Gl.GL_LINE_LOOP);
				
				double rc = 2 * Math.PI / pcm.Length;
				double r = 0;
				
				for (int i = 0; i < pcm.Length; i++) {
					Gl.glVertex2d(Math.Cos(r) * (pcm[i] / 2 + 0.5),
					              Math.Sin(r) * (pcm[i] / 2 + 0.5));
					r += rc;
				}
				
				Gl.glEnd();
			} else {
				Gl.glMatrixMode(Gl.GL_MODELVIEW);
				Gl.glPushMatrix();
				
				Gl.glTranslatef(-1, 0, 0);
				Gl.glScalef(2, 1, 1);
				
				Gl.glBegin(Gl.GL_LINE_STRIP);
				
				for (int i = 0; i < pcm.Length; i++) {
					Gl.glVertex2f((float) i / (pcm.Length - 1), pcm[i]);
				}
				
				Gl.glEnd();
				
				Gl.glPopMatrix();
			}
		}
	}
}
