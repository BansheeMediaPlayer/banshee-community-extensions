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
using Tao.OpenGl;

namespace Test {
	class MainClass {
		public static void Main(string[] args) {
			UDPPlayerData udp = new UDPPlayerData();
			
			Controller c = new Controller();
			
			c.Renderer = new TestEffect();
			c.Initialize();
			c.PlayerData = udp;
			
			bool run = true;
			
			c.WindowClosed += delegate {
				run = false;
			};
			
			while (run) {
				udp.Update();
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
			this.mR += 0.05f;
		}
		
		public override void RenderFrame(Controller controller) {
			Gl.glClear(Gl.GL_COLOR_BUFFER_BIT);
			
			Gl.glColor3f(1, 1, 1);
			Gl.glBegin(Gl.GL_LINES);
			
			Gl.glVertex2f(0, 0);
			Gl.glVertex2d(Math.Sin(this.mR), Math.Cos(this.mR));
			
			Gl.glEnd();
			
			controller.PlayerData.GetPCM(this.mPcm);
			
			Gl.glBegin(Gl.GL_LINE_STRIP);
			
			for (int i = 0; i < this.mPcm.Length; i++)
				Gl.glVertex2f(((float) i / this.mPcm.Length) * 2 - 1,
				              this.mPcm[i]);
			
			Gl.glEnd();
		}
	}
}
