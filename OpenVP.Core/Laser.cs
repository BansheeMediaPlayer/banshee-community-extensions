// Laser.cs
//
//  Copyright (C) 2008 Chris Howie
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

// This file is a port of Kaffeeklatsch's Laser class to OpenVP.

using System;
using System.Runtime.Serialization;
using Tao.OpenGl;
using OpenVP.Metadata;

namespace OpenVP.Core {
	[Serializable, Browsable(true), DisplayName("Laser"), Category("Render"),
	 Description("Draws lasers from the center of the screen."),
	 Author("Chris Howie")]
	public class Laser : Effect {
		private float mWidth = 0.01f;
		
		[Browsable(true), DisplayName("Width"), Category("Display"),
		 Description("Width of each laser beam."), Range(0, 2)]
		public float Width {
			get { return this.mWidth; }
			set { this.mWidth = value; }
		}
		
		private bool mRandom = false;
		
		[Browsable(true), DisplayName("Random"), Category("Movement"),
		 Description("Whether lasers will rotate (off) or move randomly (on).")]
		public bool Random {
			get { return this.mRandom; }
			set { this.mRandom = value; }
		}
		
		private Color mStartColor = new Color(0, 1, 0);
		
		[Browsable(true), DisplayName("Start Color"), Category("Display"),
		 Description("Laser beam color.")]
		public Color StartColor {
			get { return this.mStartColor; }
			set { this.mStartColor = value; }
		}
		
		private Color mEndColor = new Color(0, 1, 0, 0);
		
		[Browsable(true), DisplayName("End Color"), Category("Display"),
		 Description("Laser beam color."), Follows("StartColor")]
		public Color EndColor {
			get { return this.mEndColor; }
			set { this.mEndColor = value; }
		}
		
		private uint mCount = 50;
		
		[Browsable(true), DisplayName("Count"), Category("Display"),
		 Description("Number of laser beams.")]
		public uint Count {
			get { return this.mCount; }
			set {
				this.mCount = value;
				this.mNeedRebuild = true;
			}
		}
		
		private float mMaxSpeed = 2;
		
		[Browsable(true), DisplayName("Maximum speed"), Category("Movement"),
		 Description("Maximum rotation speed."), Follows("MinSpeed")]
		public float MaxSpeed {
			get { return this.mMaxSpeed; }
			set {
				this.mMaxSpeed = value;
				this.mNeedRebuild = true;
			}
		}
		
		private float mMinSpeed = 0.5f;
		
		[Browsable(true), DisplayName("Minimum speed"), Category("Movement"),
		 Description("Maximum rotation speed.")]
		public float MinSpeed {
			get { return this.mMinSpeed; }
			set {
				this.mMinSpeed = value;
				this.mNeedRebuild = true;
			}
		}
		
		[NonSerialized]
		private double[] Angle = null;
		
		[NonSerialized]
		private double[] Speed = null;
		
		private static Random R = new Random();
		
		[NonSerialized]
		private bool mNeedRebuild = true;
		
		private static readonly float SQRT2 = (float) Math.Sqrt(2);
		
		public void Randomize() {
			this.mNeedRebuild = true;
			this.BuildLists();
		}
		
		protected override void OnDeserialization(object sender) {
            base.OnDeserialization(sender);
            
			this.mNeedRebuild = true;
		}
		
		private void BuildLists() {
			if (!this.mNeedRebuild)
				return;
			
			this.mNeedRebuild = false;
			
			Angle = new double[Count];
			Speed = new double[Count];
			
			float srange = this.mMaxSpeed - this.mMinSpeed;
			
			for (int i = 0; i < Angle.Length; i++) {
				Angle[i] = R.NextDouble() * 360;
				
				int dir = R.Next(2) == 0 ? 1 : -1;
				
				double speed;
				if (this.mMaxSpeed == this.mMinSpeed)
					speed = this.mMaxSpeed;
				else
					speed = R.NextDouble() * srange + this.MinSpeed;
				
				speed *= dir;
				
				Speed[i] = speed;
			}
		}
		
		public override void NextFrame(IController controller) {
			this.BuildLists();
			
			for (int i = 0; i < Angle.Length; i++)
				Angle[i] += Speed[i];
		}
		
		public override void RenderFrame(IController controller) {
			Gl.glMatrixMode(Gl.GL_MODELVIEW);
			Gl.glPushMatrix();
			
			{
				for (int i = 0; i < Angle.Length; i++) {
					Gl.glPushMatrix();
					
					Gl.glRotated(Random ? (R.NextDouble() * 360) : Angle[i], 0, 0, 1);
					
					if (Width == 0f) {
						Gl.glLineWidth(1f);
						
						Gl.glBegin(Gl.GL_LINES);
						
						this.mStartColor.Use();
						Gl.glVertex2i(0, 0);
						
						this.mEndColor.Use();
						Gl.glVertex2f(0, SQRT2);
						
						Gl.glEnd();
					} else {
						Gl.glBegin(Gl.GL_POLYGON);
						
						this.mStartColor.Use();
						Gl.glVertex2i(0, 0);
						
						this.mEndColor.Use();
						Gl.glVertex2f(-Width, SQRT2);
						Gl.glVertex2f(Width, SQRT2);
						
						Gl.glEnd();
					}
					
					Gl.glPopMatrix();
				}
			}
			
			Gl.glPopMatrix();
		}
	}
}
