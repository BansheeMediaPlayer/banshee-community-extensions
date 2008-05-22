// DynamicMovement.cs
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
using Tao.OpenGl;
using Cdh.Affe;
using OpenVP.Scripting;
using OpenVP.Metadata;

namespace OpenVP.Core {
	[Serializable, DisplayName("Dynamic movement"), Category("Transform"),
	 Description("Applies a movement function to the buffer."),
	 Author("Chris Howie")]
	public class DynamicMovement : Effect {
		private AffeScript mInitScript = new AffeScript();
		
		[Browsable(true), DisplayName("Init"),
		 Category("Scripts"),
		 Description("This script is executed once, before any others.")]
		public AffeScript InitScript {
			get {
				return this.mInitScript;
			}
		}
		
		[NonSerialized]
		private bool mNeedInit = true;
		
		private AffeScript mFrameScript = new AffeScript();
		
		[Browsable(true), DisplayName("Frame"),
		 Category("Scripts"), Follows("InitScript"),
		 Description("This script is executed once each frame, before the vertex script.")]
		public AffeScript FrameScript {
			get {
				return this.mFrameScript;
			}
		}
		
		private AffeScript mBeatScript = new AffeScript();
		
		[Browsable(true), DisplayName("On beat"),
		 Category("Scripts"), Follows("FrameScript"),
		 Description("This script is executed after the frame script when a beat is detected.")]
		public AffeScript BeatScript {
			get {
				return this.mBeatScript;
			}
		}
		
		private AffeScript mVertexScript = new AffeScript();
		
		[Browsable(true), DisplayName("Vertex"),
		 Category("Scripts"), Follows("BeatScript"),
		 Description("This script is executed for each vertex in the grid.")]
		public AffeScript VertexScript {
			get {
				return this.mVertexScript;
			}
		}
		
		[NonSerialized]
		private ScriptHost mScriptHost;
		
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
		 Description("Whether to wrap when the scripts compute points that are off the screen.")]
		public bool Wrap {
			get {
				return this.mWrap;
			}
			set {
				this.mWrap = value;
			}
		}
		
		private bool mRectangular = false;
		
		[Browsable(true), DisplayName("Rectangular"), Category("Miscellaneous"),
		 Description("Whether to use rectangular coordinates instead of polar.")]
		public bool Rectangular {
			get {
				return this.mRectangular;
			}
			set {
				this.mRectangular = value;
				this.mStaticDirty = true;
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
		
		public DynamicMovement() {
			this.InitializeScriptObjects();
		}
		
		protected override void OnDeserialization(object sender) {
            base.OnDeserialization(sender);
            
			this.InitializeScriptObjects();
		}
		
		private void CreatePointDataArray() {
			this.mPointData = new PointData[this.mXResolution,
			                                this.mYResolution];
			this.mStaticDirty = true;
		}
		
		private void InitializeScriptObjects() {
			AffeCompiler compiler = new AffeCompiler(typeof(ScriptHost));
			
			ScriptingEnvironment.InstallBase(compiler);
			ScriptingEnvironment.InstallMath(compiler);
			
			this.mScriptHost = new ScriptHost();
			
			this.mInitScript.Compiler = compiler;
			this.mInitScript.TargetObject = this.mScriptHost;
			
			this.mFrameScript.Compiler = compiler;
			this.mFrameScript.TargetObject = this.mScriptHost;
			
			this.mBeatScript.Compiler = compiler;
			this.mBeatScript.TargetObject = this.mScriptHost;
			
			this.mVertexScript.Compiler = compiler;
			this.mVertexScript.TargetObject = this.mScriptHost;
			
			this.mNeedInit = true;
			this.mStaticDirty = true;
			
			this.mInitScript.MadeDirty += this.OnInitMadeDirty;
			this.mFrameScript.MadeDirty += this.OnOtherMadeDirty;
			this.mVertexScript.MadeDirty += this.OnOtherMadeDirty;
			
			this.CreatePointDataArray();
		}
		
		private void OnInitMadeDirty(object o, EventArgs e) {
			this.mNeedInit = true;
			this.mStaticDirty = true;
		}
		
		private void OnOtherMadeDirty(object o, EventArgs e) {
			this.mStaticDirty = true;
		}
		
		private static bool RunScript(UserScript script, string type) {
			try {
				ScriptCall call = script.Call;
				if (call == null)
					return false;
				
				call();
			} catch (Exception e) {
				Console.WriteLine("Exception executing the {0} script:", type);
				Console.WriteLine(e.ToString());
				return false;
			}
			
			return true;
		}
		
		public override void NextFrame(IController controller) {
			if (this.mNeedInit) {
				this.mNeedInit = false;
				RunScript(this.InitScript, "initialization");
			}
			
			if (!this.mStatic || this.mStaticDirty) {
				RunScript(this.FrameScript, "frame");
				
				if (controller.BeatDetector.IsBeat)
					RunScript(this.BeatScript, "beat");
			}
		}
		
		public override void RenderFrame(IController controller) {
			Gl.glMatrixMode(Gl.GL_PROJECTION);
			Gl.glPushMatrix();
			Gl.glLoadIdentity();
			
			Gl.glPushAttrib(Gl.GL_ENABLE_BIT);
			Gl.glEnable(Gl.GL_TEXTURE_2D);
			Gl.glDisable(Gl.GL_DEPTH_TEST);
			Gl.glTexEnvf(Gl.GL_TEXTURE_ENV, Gl.GL_TEXTURE_ENV_MODE, Gl.GL_DECAL);
			
			this.mTexture.SetTextureSize(controller.Width,
			                             controller.Height);
			
			Gl.glBindTexture(Gl.GL_TEXTURE_2D, this.mTexture.TextureId);
			
			Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_WRAP_S,
			                   this.Wrap ? Gl.GL_REPEAT : Gl.GL_CLAMP);
			
			Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_WRAP_T,
			                   this.Wrap ? Gl.GL_REPEAT : Gl.GL_CLAMP);
			
			Gl.glCopyTexImage2D(Gl.GL_TEXTURE_2D, 0, Gl.GL_RGB, 0, 0,
			                    controller.Width, controller.Height, 0);
			
			PointData pd;
			
			if (!this.mStatic || this.mStaticDirty) {
				for (int yi = 0; yi < this.YResolution; yi++) {
					for (int xi = 0; xi < this.XResolution; xi++) {
						this.mScriptHost.XI = (float) xi / (this.XResolution - 1);
						this.mScriptHost.YI = (float) yi / (this.YResolution - 1);
						this.mScriptHost.X = this.mScriptHost.XI;
						this.mScriptHost.Y = this.mScriptHost.YI;
						
						float xp = this.mScriptHost.X * 2 - 1;
						float yp = this.mScriptHost.Y * 2 - 1;
						
						this.mScriptHost.D = (float) Math.Sqrt((xp * xp) + (yp * yp));
						this.mScriptHost.R = (float) Math.Atan2(yp, xp);
						
						if (!RunScript(this.VertexScript, "vertex")) {
							// Force breaking out of the outer loop too.
							yi = this.YResolution;
							break;
						}
						
						if (this.Rectangular) {
							pd.XOffset = this.mScriptHost.X;
							pd.YOffset = this.mScriptHost.Y;
						} else {
							pd.XOffset = (this.mScriptHost.D * (float) Math.Cos(this.mScriptHost.R) + 1) / 2;
							pd.YOffset = (this.mScriptHost.D * (float) Math.Sin(this.mScriptHost.R) + 1) / 2;
						}
						
						pd.Alpha = this.mScriptHost.Alpha;
						
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
			
			Gl.glPopMatrix();
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
		
		~DynamicMovement() {
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
		
		private static SharedTextureHandle mTextureHandle = new SharedTextureHandle();
		
		private struct PointData {
			public float XOffset;
			
			public float YOffset;
			
			public float Alpha;
		}
		
		[Serializable]
		private class ScriptHost : ISerializable {
			[AffeBound]
			public ScriptState State = new ScriptState();
			
			[AffeBound("x")]
			public float X = 0;
			
			[AffeBound("y")]
			public float Y = 0;
			
			[AffeBound("xi")]
			public float XI = 0;
			
			[AffeBound("yi")]
			public float YI = 0;
			
			[AffeBound("alpha")]
			public float Alpha = 1;
			
			[AffeBound("d")]
			public float D = 0;
			
			[AffeBound("r")]
			public float R = 0;
			
			void ISerializable.GetObjectData(SerializationInfo info,
			                                 StreamingContext context) {
			}
			
			protected ScriptHost(SerializationInfo info,
			                     StreamingContext context) {
			}
			
			public ScriptHost() {
			}
		}
	}
}
