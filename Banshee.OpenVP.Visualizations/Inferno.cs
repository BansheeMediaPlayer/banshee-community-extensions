// Inferno.cs created with MonoDevelop
// User: chris at 12:31 AM 8/24/2008
//
// To change standard headers go to Edit->Preferences->Coding->Standard Headers
//

using System;
using OpenVP;
using OpenVP.Core;
using gl = Tao.OpenGl.Gl;

namespace Banshee.OpenVP.Visualizations
{
    public class Inferno : LinearPreset
    {
        public Inferno()
        {
            ClearScreen clear = new ClearScreen();
            clear.ClearColor = new Color(0, 0, 0, 0.035f);
            this.Effects.Add(clear);

            InfernoMovement movement = new InfernoMovement();
            this.Effects.Add(movement);

            Laser laser = new Laser();
            laser.Count = 50;
            laser.StartColor = new Color(0, 0, 0, 0.01f);
            laser.EndColor = new Color(0, 0, 0, 0.2f);
            laser.MaxSpeed = 2.5f;
            laser.MinSpeed = 0.5f;
            laser.Random = false;
            laser.Width = 0.05f;
            this.Effects.Add(laser);

            InfernoScope scope = new InfernoScope();
            this.Effects.Add(scope);
        }

        private class InfernoScope : ScopeBase
        {
            public InfernoScope()
            {
                this.LineWidth = 5;
            }
            
            protected override void PlotVertex(ScopeData data)
            {
                float r = data.FractionalI * 2 * (float) Math.PI;

                float v = Math.Abs(data.Value) * 0.75f;

                data.X = (float) Math.Sin(r) * v;
                data.Y = (float) Math.Cos(r) * v;

                data.Red = 1;
                data.Green = Math.Min(Math.Abs(data.Value), 0.5f);
                data.Blue = 0;
            }
        }

        private class InfernoMovement : MovementBase
        {
            public InfernoMovement()
            {
                this.XResolution = 32;
                this.YResolution = 32;
            }
            
            private static Random rand = new Random();
            
            protected override void PlotVertex(MovementData data)
            {
                data.Method = MovementMethod.Polar;

                data.Distance -= (float) (rand.NextDouble() * 0.03);
                data.Rotation += (float) (rand.NextDouble() * 0.025 - 0.0125);
            }
        }
    }
}
