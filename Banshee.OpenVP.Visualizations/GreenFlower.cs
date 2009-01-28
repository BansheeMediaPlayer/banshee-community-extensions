// GreenFlower.cs
//
//  Copyright (C) 2009 Chris Howie
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
using OpenVP;
using OpenVP.Core;

namespace Banshee.OpenVP.Visualizations
{
    public class GreenFlower : LinearPreset
    {
        public GreenFlower()
        {
            this.Effects.Add(new RandomMovement());
            
            ClearScreen clear = new ClearScreen();
            clear.ClearColor = new Color(0, 0, 0, 0.14f);
            this.Effects.Add(clear);

            Scope scope = new Scope();
            scope.Color = new Color(0, 1, 0.06f, 0.5f);
            scope.LineWidth = 5;
            scope.Circular = true;
            this.Effects.Add(scope);

            this.Effects.Add(new DiscMovement());

            BurstScope bscope = new BurstScope();
            bscope.Rays = 128;
            bscope.Mode = BurstScope.ColorMode.RayRandom;
            bscope.Sensitivity = 0.5f;
            bscope.MinRaySpeed = 0.1f;
            bscope.MaxRaySpeed = 0.15f;
            bscope.Wander = 5;
            bscope.Rotate = 0;
            this.Effects.Add(bscope);

            Mirror mirror = new Mirror();
            mirror.HorizontalMirror = Mirror.HorizontalMirrorType.RightToLeft;
            mirror.VerticalMirror = Mirror.VerticalMirrorType.BottomToTop;
            this.Effects.Add(mirror);
        }

        private class RandomMovement : MovementBase
        {
            private Random random = new Random();
            
            public RandomMovement()
            {
                this.XResolution = 64;
                this.YResolution = 64;
                this.Wrap = true;
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
                this.XResolution = 64;
                this.YResolution = 64;
                this.Wrap = true;
                this.Static = true;
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
