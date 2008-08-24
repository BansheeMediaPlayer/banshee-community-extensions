// SpectrumAnalyzer.cs created with MonoDevelop
// User: chris at 2:42 AM 8/24/2008
//
// To change standard headers go to Edit->Preferences->Coding->Standard Headers
//

using System;
using OpenVP;
using gl = Tao.OpenGl.Gl;

namespace Banshee.OpenVP.Visualizations
{
	public class SpectrumAnalyzer : IRenderer
	{
		private const int FREQUENCY_COUNT = 64;

		private const float SPACING = 2f / FREQUENCY_COUNT;

		private float[] spectrum = new float[FREQUENCY_COUNT];
		
		public SpectrumAnalyzer()
		{
		}

		private void MergeSpectrum(float[] newspec)
		{
			for (int i = 0; i < FREQUENCY_COUNT; i++) {
				spectrum[i] = Math.Max(newspec[i], spectrum[i] / 1.25f);
			}
		}

		public void Render (IController controller)
		{
			gl.glClearColor(0, 0, 0, 1);
			gl.glClear(gl.GL_COLOR_BUFFER_BIT);

			float[] newspec = new float[FREQUENCY_COUNT];
			controller.PlayerData.GetSpectrum(newspec);
			this.MergeSpectrum(newspec);
			
			gl.glBegin(gl.GL_QUADS);
			Color green = new Color(0, 1, 0);
			
			for (int i = 0; i < FREQUENCY_COUNT; i++) {
				Color color = Color.FromHSL(120 * (1 - this.spectrum[i]), 1, 0.5f);
				
				float x1 = -1 + SPACING * i;
				float x2 = -1 + SPACING * (i + 1);

				float v = this.spectrum[i] * 2 - 1;

				color.Use();
				gl.glVertex2f(x1, v);
				gl.glVertex2f(x2, v);
				
				green.Use();
				gl.glVertex2f(x2, -1);
				gl.glVertex2f(x1, -1);
			}

			gl.glEnd();
		}
	}
}
