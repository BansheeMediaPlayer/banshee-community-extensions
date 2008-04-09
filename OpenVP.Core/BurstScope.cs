// BurstScope.cs
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

// This file is a port of Kaffeeklatsch's FountainScope class to OpenVP.

/*
 * The original idea here was a kind of "fountain" scope but turned into more
 * of a star/sun scope.
 */

using System;
using System.Runtime.Serialization;

using OpenVP;
using OpenVP.Metadata;

using Tao.OpenGl;

namespace Kaffeeklatsch.Core {
	[Serializable, Browsable(true), DisplayName("Burst Scope"),
	 Category("Render"), Description("A sunburst-like scope.")]
	public class BurstScope : Effect, IDeserializationCallback {
		public enum ColorMode : byte {
			[DisplayName("Use selected color")] Normal,
			[DisplayName("Rainbow")] Rainbow,
			[DisplayName("Random color per ray")] RayRandom,
			[DisplayName("Random color per frame")] BurstRandomFrame,
			[DisplayName("Random color per ray per frame")] RayRandomFrame,
			[DisplayName("Glow")] Glow
		}
		
		private Color mColor = new Color(1, 1, 1);
		
		[Browsable(true), DisplayName("Color"), Category("Appearance"),
		 Description("The color used to render the scope in basic mode.")]
		public Color Color {
			get { return this.mColor; }
			set { this.mColor = value; }
		}
		
		private int mRays = 512;
		
		[Browsable(true), DisplayName("Rays"), Category("Appearance"),
		 Description("The number of rays in the burst."),
		 Range(1, 1024)]
		public int Rays {
			get { return this.mRays; }
			// TODO: Validate.
			set { this.mRays = value; }
		}
		
		private ColorMode mMode = 0;
		
		[Browsable(true), DisplayName("Color mode"), Category("Appearance"),
		 Description("The mode used to determine colors."),
		 Range(0, 5)]
		public ColorMode Mode {
			get { return this.mMode; }
			set { this.mMode = value; }
		}
		
		private float mSensitivity = 0.5f;
		
		[Browsable(true), DisplayName("Sensitivity"), Category("Behavior"),
		 Description("Threshold of wave amplitude required to react."),
		 Range(0, 1)]
		public float Sensitivity {
			get { return this.mSensitivity; }
			// TODO: Validate.
			set { this.mSensitivity = value; }
		}
		
		private float mRaySpeed = 0.05f;
		
		[Browsable(true), DisplayName("Ray speed"), Category("Behavior"),
		 Description("Speed of each ray's expansion."),
		 Range(0.001, 1)]
		public float RaySpeed {
			get { return this.mRaySpeed; }
			// TODO: Validate.
			set { this.mRaySpeed = value; }
		}
		
		private int mRotateSpeed = 5;
		
		[Browsable(true), DisplayName("Rotate speed"), Category("Behavior"),
		 Description("Speed of rotation for the entire burst."),
		 Range(1, 359)]
		public int RotateSpeed {
			get { return this.mRotateSpeed; }
			// TODO: Validate.
			set { this.mRotateSpeed = value; }
		}
		
		private float mWander = 0f;
		
		[Browsable(true), DisplayName("Wander"), Category("Behavior"),
		 Description("Maximum random wander angle."),
		 Range(0, 259)]
		public float Wander {
			get { return this.mWander; }
			// TODO: Validate.
			set { this.mWander = value; }
		}
		
		[NonSerialized]
		private Dot[] Dots = new Dot[1024];
		
		private static Random Rand = new Random();
		
		[NonSerialized]
		private Color ModeColor = new Color(0, 0, 0);
		
		[NonSerialized]
		private int Angle = 0;
		
		private int mRotate = 0;
		
		[Browsable(true), DisplayName("Rotate direction"), Category("Behavior"),
		 Description("Direction of burst rotation."),
		 Range(-1, 1)]
		public int Rotate {
			get { return this.mRotate; }
			//TODO: Validate.
			set { this.mRotate = value; }
		}
		
		private struct Dot {
			public float Distance;
			public float MaxD;
			public float Rotation;
			public float RotationSpeed;
			public float Alpha;
			public Color Color;
			public bool Drawing;
			public float Pcm;
		}
		
		void IDeserializationCallback.OnDeserialization(object sender) {
			this.Dots = new Dot[1024];
		}
		
		public override void NextFrame(Controller controller) {
			for (int i = 0; i < mRays; i++) {
				if (Dots[i].Drawing) {
					Dots[i].Distance += this.mRaySpeed * Dots[i].MaxD;
					if (Dots[i].Distance >= Dots[i].MaxD)
						Dots[i].Drawing = false;
					else {
						Dots[i].Rotation += Dots[i].RotationSpeed;
					}
				}
			}
			
			Angle = (Angle + this.mRotateSpeed) % 360;
			
			if (mMode == ColorMode.BurstRandomFrame)
				ModeColor = Color.FromHSL((float) Rand.NextDouble() * 360, 1, 0.5f);
			else if (mMode == ColorMode.Glow) {
				ModeColor = Color.FromHSL(Angle, 1, 0.5f);
			}
		}
		
		public override void RenderFrame (Controller controller) {
			float[] pcm = new float[controller.PlayerData.NativePCMLength];
			
			controller.PlayerData.GetPCM(pcm);
			
			//Gl.glDisable(Gl.GL_DEPTH_TEST);
			Gl.glMatrixMode(Gl.GL_MODELVIEW);
			Gl.glPushMatrix();
			//Glu.gluLookAt(0, 0, 1.5, 0, 0, 0, 0, 1, 0);
			
			if (mRotate != 0)
				Gl.glRotatef(Angle * mRotate, 0, 0, 1);
			
			for (int i = 0; i < mRays; i++) {
				if (!Dots[i].Drawing && (Math.Abs(pcm[i]) > mSensitivity)) {
					Dots[i].Drawing = true;
					Dots[i].Distance = 0.05f;
					Dots[i].Rotation = 0f;
					Dots[i].RotationSpeed = (float) (Rand.NextDouble() * (this.mWander * 2) - this.mWander);
					Dots[i].Alpha = Dots[i].MaxD = Math.Abs(pcm[i]);
					Dots[i].Color = Color.FromHSL((float) Rand.NextDouble() * 360, 1, 0.5f);
					Dots[i].Pcm = pcm[i];
				} else if (Dots[i].Drawing && (pcm[i] > Dots[i].Pcm)) {
					Dots[i].Pcm = pcm[i];
					Dots[i].Alpha = Dots[i].MaxD = Math.Abs(pcm[i]);
				}
				
				if (Dots[i].Drawing) {
					Gl.glPushMatrix();
					
					Gl.glRotatef((i * 360 / mRays) + Dots[i].Rotation, 0, 0, 1);
					//Gl.glTranslatef(0, Dots[i].Distance, 0);
					
					float w = Dots[i].Distance * (float) Math.Sin(Math.PI / mRays);
					//float pw = (Dots[i].Distance - 0.05f) * (float) Math.Sin(Math.PI / DotCount);
					
					float block = (float) Math.Sqrt(Dots[i].Distance * Dots[i].Distance - w * w);
					//Gl.glTranslated(0, block, 0);
					//float w = (float) Math.PI * 2 * (Dots[i].Distance + 0.05f) / DotCount;
					//float pw = (float) Math.PI * 2 * (Dots[i].Distance) / DotCount;
					
					Color cl;
					switch (mMode) {
						case ColorMode.Normal:
							cl = mColor;
							break;
						
						case ColorMode.Rainbow:
							cl = Color.FromHSL(i * 360 / mRays, 1, 0.5f);
							break;
						
						case ColorMode.RayRandomFrame:
							cl = Color.FromHSL((float) Rand.NextDouble() * 360, 1, 0.5f);
							break;
						
						case ColorMode.BurstRandomFrame:
						case ColorMode.Glow:
							cl = ModeColor;
							break;
						
						case ColorMode.RayRandom:
							cl = Dots[i].Color;
							break;
						
						default:
							cl = mColor;
							break;
					}
					
					cl.Alpha = (1 - Dots[i].Distance) * Dots[i].Alpha;
					
					cl.Use();
					Gl.glBegin(Gl.GL_POLYGON);
					Gl.glVertex2f(- w,  block);
					Gl.glVertex2f(  w,  block);
					//Gl.glVertex2f( pw, 0);
					//Gl.glVertex2f(-pw, 0);
					Gl.glVertex2f( w/3, 0);
					Gl.glVertex2f(-w/3, 0);
					Gl.glEnd();
					
					Gl.glPopMatrix();
				}
			}
			
			Gl.glPopMatrix();
			//Gl.glEnable(Gl.GL_DEPTH_TEST);
		}
	}
}
