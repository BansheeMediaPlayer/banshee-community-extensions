//
// Grid.cs
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
using OpenVP.Core;

namespace Banshee.OpenVP.Visualizations
{
    public class SpinningGrid : LinearPreset
    {
        public SpinningGrid()
        {
            ClearScreen clear = new ClearScreen();
            clear.ClearColor = new Color(0, 0, 0, 0.075f);
            Effects.Add(clear);

            Effects.Add(new GridScope());

            Effects.Add(new GridMovement());
        }

        private class GridScope : ScopeBase
        {
            private static readonly Random rand = new Random();

            private float dx = 0;

            private float dy = 0;

            private float da = 0;

            public override void RenderFrame(IController controller)
            {
                dx = 0;
                dy = 0;

                float[] pcm = new float[controller.PlayerData.NativePCMLength];
                controller.PlayerData.GetPCM(pcm);

                float total = 0;
                for (int i = 0; i < pcm.Length; i++)
                    total += Math.Abs(pcm[i]);

                da = da / 2 + total / pcm.Length;
                LineWidth = 3 + da * 2;

                base.RenderFrame(controller);
            }

            protected override void PlotVertex(ScopeData data)
            {
                int dr = rand.Next(2) == 0 ? -1 : 1;

                if (rand.Next(2) == 0) {
                    dx += data.Value / 4 * dr;
                } else {
                    dy += data.Value / 4 * dr;
                }

                data.X = dx;
                data.Y = dy;

                data.Alpha = Math.Abs(data.Value) * 0.8f;
                data.Red = da;
                data.Green = data.Alpha;
                data.Blue = 1 - da;
            }
        }

        private class GridMovement : MovementBase
        {
            public GridMovement()
            {
                XResolution = 64;
                YResolution = 64;
            }

            public override void RenderFrame(IController controller)
            {
                float[] spectrum = new float[controller.PlayerData.NativeSpectrumLength];
                controller.PlayerData.GetSpectrum(spectrum);

                float channels = spectrum.Length / 64;

                float total = 0;
                for (int i = 0; i < channels; i++)
                    total += spectrum[i];

                factor = (factor * 0.4f) + (total / channels) * 2f;
                time += (total / channels) / 50;

                base.RenderFrame(controller);
            }

            private float factor = 0;
            private float time = 0;

            protected override void PlotVertex(MovementData data)
            {
                data.Method = MovementMethod.Polar;

                float d = data.Distance;

                data.Distance *= 0.995f - (0.015f * factor);
                data.Rotation += (float) Math.Sin(Math.PI * 2 + d * 8 + time + factor * 4) * factor / 50;
                data.Alpha = 0.75f - (d / 3);
            }
        }
    }
}
