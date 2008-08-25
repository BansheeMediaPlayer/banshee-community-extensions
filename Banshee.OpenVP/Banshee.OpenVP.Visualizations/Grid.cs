// Grid.cs created with MonoDevelop
// User: chris at 10:43 AM 8/25/2008
//
// To change standard headers go to Edit->Preferences->Coding->Standard Headers
//

using System;
using OpenVP;
using OpenVP.Core;

namespace Banshee.OpenVP.Visualizations
{
	public class Grid : LinearPreset
	{
		public Grid()
		{
			ClearScreen clear = new ClearScreen();
			clear.ClearColor = new Color(0, 0, 0, 0.075f);
			this.Effects.Add(clear);

			Laser laser = new Laser();
			laser.Count = 50;
			laser.StartColor = new Color(0, 0, 0, 0);
			laser.EndColor = new Color(0, 0, 0, 0.5f);
			laser.MaxSpeed = 2;
			laser.MinSpeed = 0.5f;
			laser.Random = false;
			laser.Width = 0.05f;
			this.Effects.Add(laser);

			this.Effects.Add(new GridMovement());
			
			this.Effects.Add(new GridScope());
		}

		private class GridScope : ScopeBase
		{
			private static readonly Random rand = new Random();
			
			public GridScope()
			{
				this.Vertices = 512;
			}

			private float dx = 0;

			private float dy = 0;

			private float da = 0;

			public override void RenderFrame(IController controller)
			{
				this.dx = 0;
				this.dy = 0;

				float[] pcm = new float[controller.PlayerData.NativePCMLength];
				controller.PlayerData.GetPCM(pcm);

				float total = 0;
				for (int i = 0; i < pcm.Length; i++)
					total += Math.Abs(pcm[i]);

				this.da = this.da / 2 + total / pcm.Length;
				this.LineWidth = 3 + this.da * 2;

				base.RenderFrame(controller);
			}
			
			protected override void PlotVertex(ScopeData data)
			{
				int dr = rand.Next(2) == 0 ? -1 : 1;

				if (rand.Next(2) == 0) {
					this.dx += data.Value / 4 * dr;
				} else {
					this.dy += data.Value / 4 * dr;
				}

				data.X = this.dx;
				data.Y = this.dy;

				data.Alpha = Math.Abs(data.Value) * 0.8f;
				data.Red = this.da;
				data.Green = data.Alpha;
				data.Blue = 1 - this.da;
			}
		}

		private class GridMovement : MovementBase
		{
			public GridMovement()
			{
				this.XResolution = 3;
				this.YResolution = 3;
			}
			
			public override void RenderFrame(IController controller)
			{
				float[] spectrum = new float[controller.PlayerData.NativeSpectrumLength];
				controller.PlayerData.GetSpectrum(spectrum);

				float channels = spectrum.Length / 64;
				
				float total = 0;
				for (int i = 0; i < channels; i++)
					total += spectrum[i];

				this.factor = total / channels;

				base.RenderFrame(controller);
			}

			private float factor = 0;
			
			protected override void PlotVertex(MovementData data)
			{
				data.Method = MovementMethod.Polar;
				
				data.Distance *= 0.995f - (0.02f * this.factor);
			}
		}
	}
}
