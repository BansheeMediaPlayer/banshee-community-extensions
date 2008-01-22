// Main.cs
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
using System.Runtime.Serialization.Formatters.Soap;
using OpenVP;
using OpenVP.Core;
using Tao.OpenGl;

namespace Test {
	class MainClass {
		public static void Main(string[] args) {
			UDPPlayerData udp = new UDPPlayerData();
			
			Controller c = new Controller();
			
//			LinearPreset preset = new LinearPreset();
//			
//			ClearScreen clear = new ClearScreen();
//			clear.ClearColor = new Color(0, 0, 0, 0.3f);
//			
//			preset.Effects.Add(clear);
//			Scope s = new Scope();
//			preset.Effects.Add(s);
//			s.Color = new Color(0, 1, 0);
//			s.Circular = true;
			
			TestTimedPreset preset = new TestTimedPreset();
			
			c.Renderer = preset;
			c.Initialize();
			c.PlayerData = udp;
			
			bool run = true;
			
			c.WindowClosed += delegate {
				run = false;
			};
			
			while (run) {
				udp.Update(-1);
				c.DrawFrame();
			}
			
			c.Destroy();
		}
	}
	
	[Serializable]
	public class TestEffect : Effect {
		[NonSerialized]
		private float mR = 0;
		
		[NonSerialized]
		private float[] mPcm = new float[512];
		
		public override void NextFrame(Controller controller) {
			this.mR += 1;
		}
		
		public override void RenderFrame(Controller controller) {
			Gl.glMatrixMode(Gl.GL_PROJECTION);
			Gl.glPushMatrix();
			
			Gl.glRotatef(this.mR, 0, 0, 1);
			
			Gl.glColor4f(1, 1, 1, 0.2f);
			Gl.glBegin(Gl.GL_LINES);
			
			Gl.glVertex2f(0, 0);
			Gl.glVertex2f(1, 0);
			
			Gl.glEnd();
			
			Gl.glPopMatrix();
			
			controller.PlayerData.GetPCM(this.mPcm);
			
			Gl.glPushMatrix();
			
			Gl.glTranslatef(-1, 0, 0);
			Gl.glScalef(2, 1, 1);
			
			Gl.glColor3f(1, 1, 1);
			Gl.glBegin(Gl.GL_LINE_STRIP);
			
			for (int i = 0; i < this.mPcm.Length; i++)
				Gl.glVertex2f((float) i / this.mPcm.Length,
				              this.mPcm[i]);
			
			Gl.glEnd();
			
			Gl.glPopMatrix();
		}
	}
	
	public class TestTimedPreset : TimedPresetBase {
		public TestTimedPreset() {
			this.mClear.ClearColor = new Color(0, 0, 0, 0.075f);
			this.mScope.Circular = true;
		}
		
		private ClearScreen mClear = new ClearScreen();
		
		private Scope mScope = new Scope();
		
		[Scene(0)]
		protected void MainScene(Controller controller) {
			this.mClear.Render(controller);
			this.mScope.Render(controller);
		}
		
		[Event(0)]
		protected void MakeRed(Controller controller) {
			this.mScope.Color = new Color(1, 0, 0);
		}
		
		[Event(5)]
		protected void MakeGreen(Controller controller) {
			this.mScope.Color = new Color(0, 1, 0);
		}
		
		[Event(10)]
		protected void MakeBlue(Controller controller) {
			this.mScope.Color = new Color(0, 0, 1);
			this.mScope.LineWidth = 3;
		}
	}
}
