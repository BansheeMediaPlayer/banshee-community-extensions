// SuperScope.cs
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
using System.Runtime.Serialization;
using OpenVP.Scripting;
using OpenVP.Metadata;
using Cdh.Affe;
using Tao.OpenGl;

namespace OpenVP.Core {
	[Serializable, Browsable(false)]
	public abstract class ScopeBase : Effect {
		[NonSerialized]
		private float[] mScopeData = null;

		private int mVertices = 0;

		protected int Vertices {
			get { return this.mVertices; }
			set {
				if (value < 0)
					throw new ArgumentOutOfRangeException("value < 0");

				this.mVertices = value;
			}
		}

		private float mLineWidth = 1;

		protected float LineWidth {
			get { return this.mLineWidth; }
			set {
				if (value <= 0)
					throw new ArgumentOutOfRangeException("value <= 0");

				this.mLineWidth = value;
			}
		}
		
		public ScopeBase() {
		}

		protected virtual void OnRenderFrame() {
		}

		protected virtual void OnBeat() {
		}

		protected abstract void PlotVertex(ScopeData data);
		
		public override void NextFrame(IController controller) {
			this.OnRenderFrame();

			if (controller.BeatDetector.IsBeat) {
				this.OnBeat();
			}
		}
		
		private void EnsureDataArrayLength(int length) {
			if (this.mScopeData == null || this.mScopeData.Length != length)
				this.mScopeData = new float[length];
		}
		
		public override void RenderFrame(IController controller) {
			int points = this.Vertices;
			
			if (points <= 1)
				points = controller.PlayerData.NativePCMLength;
			
			this.EnsureDataArrayLength(points);
			controller.PlayerData.GetPCM(this.mScopeData);
			
			Gl.glLineWidth(this.LineWidth);
			
			try {
				Gl.glBegin(Gl.GL_LINE_STRIP);

				ScopeData data = new ScopeData();
				
				for (int i = 0; i < points; i++) {
					data.FractionalI = (float) i / (points - 1);
					data.I = i;

					data.Value = this.mScopeData[i];

					this.PlotVertex(data);
					
					Gl.glColor4f(data.Red,
					             data.Green,
					             data.Blue,
					             data.Alpha);
					
					Gl.glVertex2f(data.X, data.Y);
				}
			} finally {
				Gl.glEnd();
			}
		}

		public class ScopeData {
			public ScopeData() {
			}

			private float mRed = 1;

			public float Red {
				get { return this.mRed; }
				set { this.mRed = value; }
			}

			private float mGreen = 1;

			public float Green {
				get { return this.mGreen; }
				set { this.mGreen = value; }
			}

			private float mBlue = 1;

			public float Blue {
				get { return this.mBlue; }
				set { this.mBlue = value; }
			}

			private float mAlpha = 1;

			public float Alpha {
				get { return this.mAlpha; }
				set { this.mAlpha = value; }
			}

			private float mX = 0;

			public float X {
				get { return this.mX; }
				set { this.mX = value; }
			}

			private float mY = 0;

			public float Y {
				get { return this.mY; }
				set { this.mY = value; }
			}

			private int mI = 0;

			public int I {
				get { return this.mI; }
				set { this.mI = value; }
			}

			private float mFractionalI = 0;

			public float FractionalI {
				get { return this.mFractionalI; }
				set { this.mFractionalI = value; }
			}

			private float mValue = 0;

			public float Value {
				get { return this.mValue; }
				set { this.mValue = value; }
			}
		}
	}
}
