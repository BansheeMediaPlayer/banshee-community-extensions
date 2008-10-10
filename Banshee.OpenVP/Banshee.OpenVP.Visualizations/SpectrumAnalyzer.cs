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
        private int spectrumLength = -1;
        
        private float spacing;

        private float[] spectrum;

        private float[] newspec;
        
        public SpectrumAnalyzer()
        {
        }

        private void UpdateSpectrumLength(int length)
        {
            if (this.spectrumLength == length)
                return;

            this.spacing = 2f / length;
            this.spectrum = new float[length];
            this.newspec = new float[length];
            this.spectrumLength = length;
        }

        private void MergeSpectrum()
        {
            for (int i = 0; i < this.spectrumLength; i++) {
                this.spectrum[i] = Math.Max(this.newspec[i], this.spectrum[i] / 1.25f);
            }
        }

        public void Render (IController controller)
        {
            gl.glClearColor(0, 0, 0, 1);
            gl.glClear(gl.GL_COLOR_BUFFER_BIT);

            this.UpdateSpectrumLength(controller.PlayerData.NativeSpectrumLength);
            controller.PlayerData.GetSpectrum(this.newspec);
            this.MergeSpectrum();
            
            gl.glBegin(gl.GL_QUADS);
            
            for (int i = 0; i < this.spectrumLength; i++) {
                Color color = Color.FromHSL(120 * (1 - this.spectrum[i]), 1, 0.5f);
                
                float x1 = -1 + this.spacing * i;
                float x2 = -1 + this.spacing * (i + 1);

                float v = this.spectrum[i] * 2 - 1;

                color.Use();
                gl.glVertex2f(x1, v);
                gl.glVertex2f(x2, v);
                
                gl.glVertex2f(x2, -1);
                gl.glVertex2f(x1, -1);
            }

            gl.glEnd();
        }
    }
}
