//
// GreenFlower.cs
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
    public class GreenFlower : LinearPreset
    {
        public GreenFlower()
        {
            Effects.Add(new RandomMovement());

            ClearScreen clear = new ClearScreen();
            clear.ClearColor = new Color(0, 0, 0, 0.14f);
            Effects.Add(clear);

            Scope scope = new Scope();
            scope.Color = new Color(0, 1, 0.06f, 0.5f);
            scope.LineWidth = 5;
            scope.Circular = true;
            Effects.Add(scope);

            Effects.Add(new DiscMovement());

            BurstScope bscope = new BurstScope();
            bscope.Rays = 128;
            bscope.Mode = BurstScope.ColorMode.RayRandom;
            bscope.Sensitivity = 0.5f;
            bscope.MinRaySpeed = 0.1f;
            bscope.MaxRaySpeed = 0.15f;
            bscope.Wander = 5;
            bscope.Rotate = 0;
            Effects.Add(bscope);

            Mirror mirror = new Mirror();
            mirror.HorizontalMirror = Mirror.HorizontalMirrorType.RightToLeft;
            mirror.VerticalMirror = Mirror.VerticalMirrorType.BottomToTop;
            Effects.Add(mirror);
        }

        private class RandomMovement : MovementBase
        {
            private Random random = new Random();

            public RandomMovement()
            {
                XResolution = 64;
                YResolution = 64;
                Wrap = true;
            }

            protected override void PlotVertex(MovementData data)
            {
                data.Method = MovementMethod.Rectangular;
                data.X += (float) ((random.NextDouble() - 0.5) / 32.0);
                data.Y += (float) ((random.NextDouble() - 0.5) / 32.0);
                data.Alpha = 0.5f;
            }
        }

        private class DiscMovement : MovementBase
        {
            public DiscMovement()
            {
                XResolution = 64;
                YResolution = 64;
                Wrap = true;
                Static = true;
            }

            protected override void PlotVertex(MovementBase.MovementData data)
            {
                data.Method = MovementMethod.Polar;

                data.Distance -= (float) (Math.Cos(data.Distance * 4 * Math.PI +
                                                   Math.PI / 2) / 50);
                data.Rotation += (float) (Math.Sin(4 * data.Rotation) / 50);
            }
        }
    }
}
