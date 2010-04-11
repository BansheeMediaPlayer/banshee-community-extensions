//
// SpectrumAnalyzer.cs
//
// Author:
//       Chris Howie <cdhowie@gmail.com>
//
// Copyright (c) 2009 Chris Howie
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

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
            if (spectrumLength == length)
                return;

            spacing = 2f / length;
            spectrum = new float[length];
            newspec = new float[length];
            spectrumLength = length;
        }

        private void MergeSpectrum()
        {
            for (int i = 0; i < spectrumLength; i++) {
                spectrum[i] = Math.Max(newspec[i], spectrum[i] / 1.25f);
            }
        }

        public void Render(IController controller)
        {
            gl.glClearColor(0, 0, 0, 1);
            gl.glClear(gl.GL_COLOR_BUFFER_BIT);

            UpdateSpectrumLength(controller.PlayerData.NativeSpectrumLength);
            controller.PlayerData.GetSpectrum(newspec);
            MergeSpectrum();

            gl.glBegin(gl.GL_QUADS);

            for (int i = 0; i < spectrumLength; i++) {
                Color color = Color.FromHSL(120 * (1 - spectrum[i]), 1, 0.5f);

                float x1 = -1 + spacing * i;
                float x2 = -1 + spacing * (i + 1);

                float v = spectrum[i] * 2 - 1;

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
