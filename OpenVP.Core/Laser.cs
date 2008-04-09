/*
 * Kaffeeklatsch core: Core effects for the Kaffeeklatsch Visualization Platform.
 * Copyright (C) 2006  Chris Howie
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
 */

using System;
using System.Drawing;
using Kaffeeklatsch.Platform;
using Tao.OpenGl;

namespace Kaffeeklatsch.Core {
	[EffectTitle("Laser")]
	public class Laser : BaseEffect {
		[Option] public float Width = 0.01f;
		[Option] public bool Random = false;
		[Option("Color")] public GlColor DrawColor = new GlColor(0, 255, 0);
		[Option] public uint Count = 50;
		[Option] public float MaxSpeed = 2f;
		
		private double[] Angle = new double[0];
		private double[] Speed = new double[0];
		
		private Random R = new Random();
		
		[Trigger]
		public void Randomize() {
			Angle = new double[0];
			this.BuildLists();
		}
		
		private void BuildLists() {
			if (Angle.Length == Count)
				return;
			
			lock (this.OptionLock) {
				Angle = new double[Count];
				Speed = new double[Count];
			}
			
			for (int i = 0; i < Angle.Length; i++) {
				Angle[i] = R.NextDouble() * 360;
				Speed[i] = (R.NextDouble() * MaxSpeed * 2) - MaxSpeed;
			}
		}
		
		protected override void NextFrame() {
			this.BuildLists();
			
			for (int i = 0; i < Angle.Length; i++)
				Angle[i] += Speed[i];
		}
		
		protected override void RenderImpl(VizData d) {
			Gl.glDisable(Gl.GL_DEPTH_TEST);
			Gl.glMatrixMode(Gl.GL_MODELVIEW);
			Gl.glPushMatrix();
			{
				Glu.gluLookAt(0, 0, 1, 0, 0, 0, 0, 1, 0);
				
				Gl.glColor3fv(DrawColor.Vector3);
				
				for (int i = 0; i < Angle.Length; i++) {
					Gl.glPushMatrix();
					
					Gl.glRotated(Random ? (R.NextDouble() * 360) : Angle[i], 0, 0, 1);
					
					if (Width == 0f) {
						Gl.glBegin(Gl.GL_LINES);
						Gl.glVertex2i(0, 0);
						Gl.glVertex2i(0, 2);
						Gl.glEnd();
					} else {
						Gl.glBegin(Gl.GL_POLYGON);
						Gl.glVertex2i(0, 0);
						Gl.glVertex2f(-Width, 2);
						Gl.glVertex2i(0, 2);
						Gl.glVertex2f(Width, 2);
						Gl.glEnd();
					}
					
					Gl.glPopMatrix();
				}
			}
			
			Gl.glPopMatrix();
			Gl.glEnable(Gl.GL_DEPTH_TEST);
		}
	}
}
