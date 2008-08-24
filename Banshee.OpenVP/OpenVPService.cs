// OpenVPService.cs
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
using System.IO;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;

using Banshee.MediaEngine;
using Banshee.ServiceStack;

using OpenVP;
using OpenVP.Core;

namespace Banshee.OpenVP
{
	public class OpenVPService : IExtensionService, IDisposable
	{
		private SDLController Controller = null;
		
		public OpenVPService()
		{
		}
		
		void IExtensionService.Initialize() {
			this.Controller = new SDLController();
			this.mRunning = true;
			
			new Thread(this.ControllerLoop).Start();
		}
		
		string IService.ServiceName {
			get { return "OpenVP Visualizer"; }
		}
		
		private volatile bool mRunning = false;
		
		private void ControllerLoop() {
			Thread.Sleep(1000);
			BansheePlayerData data = new BansheePlayerData(ServiceManager.PlayerEngine.ActiveEngine);
			this.Controller.PlayerData = data;
			this.Controller.WindowTitle = "Banshee - OpenVP Visualizer";
			
			this.Controller.Initialize();
			
			/* LinearPreset preset = new LinearPreset();
			
			ClearScreen clear = new ClearScreen();
			clear.ClearColor = new Color(0, 0, 0, 0.075f);
			
			preset.Effects.Add(clear);
			
			preset.Effects.Add(new TestMovement());
			
			Scope scope = new Scope();
			scope.LineWidth = 2;
			scope.Color = new Color(0, 0.5f, 1);
            scope.Frequency = true;
			
			preset.Effects.Add(scope); */
			
			IRenderer preset = LoadPreset("stargrid");
			
			this.Controller.Renderer = preset;
			
			while (this.mRunning) {
				if (data.Update(500))
					this.Controller.RenderFrame();
			}
			
			this.Controller.Destroy();
			if (preset is IDisposable)
				((IDisposable) preset).Dispose();
            
            data.Dispose();
		}
		
		void IDisposable.Dispose() {
			this.mRunning = false;
            
            LoadPreset("inferno");
		}
		
		private static IRenderer LoadPreset(string name) {
			BinaryFormatter bf = new BinaryFormatter();
			
			using (Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream(name + ".ovpb")) {
				return (IRenderer) bf.Deserialize(s);
			}
		}
		
		private class TestMovement : MovementBase {
			public TestMovement() {
				this.XResolution = 2;
				this.YResolution = 3;
				this.Static = true;
			}
			
			protected override void PlotVertex(MovementData data) {
				data.Method = MovementMethod.Rectangular;
				
				data.Y = (float) ((data.Y - 0.5) * 0.98 + 0.5);
			}
		}
	}
}
