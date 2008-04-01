// MovementBase.cs
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
using System.ComponentModel;

using OpenVP;

using Tao.OpenGl;

namespace OpenVP.Core {
	[Browsable(false)]
	public abstract class MovementBase : Effect {
		protected virtual void OnRenderFrame() {
		}
		
		protected virtual void OnBeat() {
		}
		
		protected abstract void PlotVertex(MovementData data);
		
		private int mXResolution = 16;
		
		[Browsable(true), DisplayName("X"), Category("Grid resolution"),
		 Range(2, 512),
		 Description("The number of verticies along the X axis.")]
		public int XResolution {
			get {
				return this.mXResolution;
			}
			set {
				if (value < 2)
					throw new ArgumentOutOfRangeException("value < 2");
				
				this.mXResolution = value;
				this.CreatePointDataArray();
			}
		}
		
		private int mYResolution = 16;
		
		[Browsable(true), DisplayName("Y"), Category("Grid resolution"),
		 Range(2, 512),
		 Description("The number of verticies along the Y axis.")]
		public int YResolution {
			get {
				return this.mYResolution;
			}
			set {
				if (value < 2)
					throw new ArgumentOutOfRangeException("value < 2");
				
				this.mYResolution = value;
				this.CreatePointDataArray();
			}
		}
		
		private bool mWrap = true;
		
		[Browsable(true), DisplayName("Wrap"), Category("Miscellaneous"),
		 Description("Whether to wrap when vertices are moved off the screen.")]
		public bool Wrap {
			get {
				return this.mWrap;
			}
			set {
				this.mWrap = value;
			}
		}
		
		private bool mStatic = false;
		
		[Browsable(true), DisplayName("Static motion"), Category("Miscellaneous"),
		 Description("Whether the motion can change over time (off) or is static (on).")]
		public bool Static {
			get {
				return this.mStatic;
			}
			set {
				this.mStatic = value;
			}
		}
		
		[NonSerialized]
		private bool mStaticDirty = true;
		
		[NonSerialized]
		private PointData[,] mPointData;
		
		private void CreatePointDataArray() {
			this.mPointData = new PointData[this.mXResolution,
			                                this.mYResolution];
			this.mStaticDirty = true;
		}
		
		public override void NextFrame(Controller controller) {
			if (!this.mStatic || this.mStaticDirty) {
				this.OnRenderFrame();
				
				if (controller.BeatDetector.IsBeat)
					this.OnBeat();
			}
		}
		
		public override void RenderFrame(Controller controller) {
			Gl.glMatrixMode(Gl.GL_PROJECTION);
			Gl.glPushMatrix();
			Gl.glLoadIdentity();
			
			Gl.glPushAttrib(Gl.GL_ENABLE_BIT);
			Gl.glEnable(Gl.GL_TEXTURE_2D);
			Gl.glDisable(Gl.GL_DEPTH_TEST);
			Gl.glTexEnvf(Gl.GL_TEXTURE_ENV, Gl.GL_TEXTURE_ENV_MODE, Gl.GL_DECAL);
			
			this.mTexture.SetTextureSize(controller.WindowWidth,
			                             controller.WindowHeight);
			
			Gl.glBindTexture(Gl.GL_TEXTURE_2D, this.mTexture.TextureId);
			
			Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_WRAP_S,
			                   this.Wrap ? Gl.GL_REPEAT : Gl.GL_CLAMP);
			
			Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_WRAP_T,
			                   this.Wrap ? Gl.GL_REPEAT : Gl.GL_CLAMP);
			
			Gl.glCopyTexImage2D(Gl.GL_TEXTURE_2D, 0, Gl.GL_RGB, 0, 0,
			                    controller.WindowWidth, controller.WindowHeight,
			                    0);
			
			PointData pd;
			
			MovementData data = new MovementData();
			
			if (!this.mStatic || this.mStaticDirty) {
				for (int yi = 0; yi < this.YResolution; yi++) {
					for (int xi = 0; xi < this.XResolution; xi++) {
						data.X = (float) xi / (this.XResolution - 1);
						data.Y = (float) yi / (this.YResolution - 1);
						
						float xp = data.X * 2 - 1;
						float yp = data.Y * 2 - 1;
						
						data.Distance = (float) Math.Sqrt((xp * xp) + (yp * yp));
						data.Rotation = (float) Math.Atan2(yp, xp);
						
						this.PlotVertex(data);
						
						if (data.Method == MovementMethod.Rectangular) {
							pd.XOffset = data.X;
							pd.YOffset = data.Y;
						} else {
							pd.XOffset = (data.Distance * (float) Math.Cos(data.Rotation) + 1) / 2;
							pd.YOffset = (data.Distance * (float) Math.Sin(data.Rotation) + 1) / 2;
						}
						
						pd.Alpha = data.Alpha;
						
						this.mPointData[xi, yi] = pd;
					}
				}
				
				this.mStaticDirty = false;
			}
			
			Gl.glColor4f(1, 1, 1, 1);
			Gl.glBegin(Gl.GL_QUADS);
			
			for (int yi = 0; yi < this.YResolution - 1; yi++) {
				for (int xi = 0; xi < this.XResolution - 1; xi++) {
					this.RenderVertex(xi,     yi    );
					this.RenderVertex(xi + 1, yi    );
					this.RenderVertex(xi + 1, yi + 1);
					this.RenderVertex(xi,     yi + 1);
				}
			}
			
			Gl.glEnd();
			
			Gl.glPopAttrib();
		}
		
		private void RenderVertex(int x, int y) {
			PointData pd = this.mPointData[x, y];
			
			Gl.glColor4f(1, 1, 1, pd.Alpha);
			
			Gl.glTexCoord2f(pd.XOffset, pd.YOffset);
			
			Gl.glVertex2f((float) x / (this.XResolution - 1) * 2 - 1,
						  (float) y / (this.YResolution - 1) * 2 - 1);
		}
		
		public override void Dispose() {
			if (mHasTextureRef) {
				mHasTextureRef = false;
				
				mTextureHandle.RemoveReference();
			}
		}
		
		~MovementBase() {
			this.Dispose();
		}
		
		[NonSerialized]
		private bool mHasTextureRef = false;
		
		private TextureHandle mTexture {
			get {
				if (!this.mHasTextureRef) {
					this.mHasTextureRef = true;
					
					mTextureHandle.AddReference();
				}
				
				return mTextureHandle;
			}
		}
		
		private static SharedTextureHandle mTextureHandle = null;
		
		private struct PointData {
			public float XOffset;
			
			public float YOffset;
			
			public float Alpha;
		}
		
		public enum MovementMethod : byte {
			Rectangular,
			Polar
		}
		
		public class MovementData {
			public MovementData() {
			}
			
			private MovementMethod mMethod = MovementMethod.Rectangular;
			
			public MovementMethod Method {
				get { return this.mMethod; }
				set { this.mMethod = value; }
			}
			
			private float mX;
			
			public float X {
				get { return this.mX; }
				set { this.mX = value; }
			}
			
			private float mY;
			
			public float Y {
				get { return this.mY; }
				set { this.mY = value; }
			}
			
			private float mAlpha;
			
			public float Alpha {
				get { return this.mAlpha; }
				set { this.mAlpha = value; }
			}
			
			private float mRotation;
			
			public float Rotation {
				get { return this.mRotation; }
				set { this.mRotation = value; }
			}
			
			private float mDistance;
			
			public float Distance {
				get { return this.mDistance; }
				set { this.mDistance = value; }
			}
		}
	}
}
