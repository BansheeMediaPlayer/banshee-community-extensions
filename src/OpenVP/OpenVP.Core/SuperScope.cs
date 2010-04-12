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
	[Serializable, Browsable(true), DisplayName("SuperScope"),
	 Category("Render"), Description("Powerful scriptable scope."),
	 Author("Chris Howie")]
	public class SuperScope : Effect {
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
		 Description("This script is executed for each vertex on the scope.")]
		public AffeScript VertexScript {
			get {
				return this.mVertexScript;
			}
		}
		
		[NonSerialized]
		private ScriptHost mScriptHost;
		
		[NonSerialized]
		private float[] mScopeData = null;
		
		public SuperScope() {
			this.InitializeScriptObjects();
		}
		
        protected override void OnDeserialization(object sender) {
            base.OnDeserialization(sender);
            
			this.InitializeScriptObjects();
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
			
			this.mInitScript.MadeDirty += this.OnInitMadeDirty;
		}
		
		private void OnInitMadeDirty(object o, EventArgs e) {
			this.mNeedInit = true;
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
		
		private void UpdateVariables(IController c) {
			this.mScriptHost.Width = c.Width;
			this.mScriptHost.Height = c.Height;
			
			this.mScriptHost.Beat = c.BeatDetector.IsBeat ? 1 : 0;
			
			this.mScriptHost.NativeN = c.PlayerData.NativePCMLength;
		}
		
		public override void NextFrame(IController controller) {
			if (this.mNeedInit) {
				this.mNeedInit = false;
				
				this.UpdateVariables(controller);
				this.mScriptHost.NumPoints = this.mScriptHost.NativeN;
				RunScript(this.InitScript, "initialization");
			}
			
			if (this.mScriptHost.NumPoints > 0) {
				this.UpdateVariables(controller);
				RunScript(this.FrameScript, "frame");
				
				this.UpdateVariables(controller);
				if (controller.BeatDetector.IsBeat)
					RunScript(this.BeatScript, "beat");
			}
		}
		
		private void EnsureDataArrayLength(int length) {
			if (this.mScopeData == null || this.mScopeData.Length != length)
				this.mScopeData = new float[length];
		}
		
		public override void RenderFrame(IController controller) {
			int points = unchecked((int) this.mScriptHost.NumPoints);
			
			if (points <= 0)
				return;
			
			this.EnsureDataArrayLength(points);
			controller.PlayerData.GetPCM(this.mScopeData);
			
			Gl.glLineWidth(this.mScriptHost.LineWidth);
			
			try {
				Gl.glBegin(Gl.GL_LINE_STRIP);
				
				for (int i = 0; i < points; i++) {
					this.mScriptHost.I = (float) i / (points - 1);
					this.mScriptHost.Value = this.mScopeData[i];
					if (!RunScript(this.mVertexScript, "vertex"))
						break;
					
					Gl.glColor4f(this.mScriptHost.Red,
					             this.mScriptHost.Green,
					             this.mScriptHost.Blue,
					             this.mScriptHost.Alpha);
					
					Gl.glVertex2f(this.mScriptHost.X,
					              this.mScriptHost.Y);
				}
			} finally {
				Gl.glEnd();
			}
		}
		
		[Serializable]
		private class ScriptHost : ISerializable {
			[AffeBound]
			public ScriptState State = new ScriptState();
			
			[AffeBound("red")]
			public float Red = 1;
			
			[AffeBound("green")]
			public float Green = 1;
			
			[AffeBound("blue")]
			public float Blue = 1;
			
			[AffeBound("alpha")]
			public float Alpha = 1;
			
			[AffeBound("n")]
			public float NumPoints = 0;
			
			[AffeBound("x")]
			public float X = 0;
			
			[AffeBound("y")]
			public float Y = 0;
			
			[AffeBound("px")]
			public float PX = 0;
			
			[AffeBound("py")]
			public float PY = 0;
			
			[AffeBound("palpha")]
			public float PAlpha = 0.1f;
			
			[AffeBound("w")]
			public float Width = 0;
			
			[AffeBound("h")]
			public float Height = 0;
			
			[AffeBound("b")]
			public float Beat = 0;
			
			[AffeBound("i")]
			public float I = 0;
			
			[AffeBound("v")]
			public float Value = 0;
			
			[AffeBound("nativen")]
			public float NativeN = 0;
			
			[AffeBound("linewidth")]
			public float LineWidth = 1;
			
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
