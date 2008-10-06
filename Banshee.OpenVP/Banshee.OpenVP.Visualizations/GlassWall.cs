// GlassWall.cs created with MonoDevelop
// User: chris at 10:28 AM 8/27/2008
//
// To change standard headers go to Edit->Preferences->Coding->Standard Headers
//

using System;
using OpenVP;
using OpenVP.Core;
using gl = Tao.OpenGl.Gl;

namespace Banshee.OpenVP.Visualizations
{
    public class GlassWall : LinearPreset
    {
        public GlassWall()
        {
            ClearScreen clear = new ClearScreen();
            clear.ClearColor = new Color(0, 0, 0, 0.075f);
            this.Effects.Add(clear);

            this.Effects.Add(new GlassWallScope());

            this.Effects.Add(new GlassWallMovement());
        }

        private class GlassWallMovement : MovementBase
        {
            private const int NUM_PANELS = 8;

            private const float SIN_BIAS = NUM_PANELS * 2 * (float) Math.PI;

            private const int SIN_DIVISOR = NUM_PANELS * 32;
            
            public GlassWallMovement()
            {
                this.XResolution = NUM_PANELS * 4;
                this.YResolution = NUM_PANELS * 4;
                this.Static = true;
            }
            
            protected override void PlotVertex (MovementData data)
            {
                data.Method = MovementMethod.Rectangular;
    
                data.X += (float) Math.Sin(data.X * SIN_BIAS) / SIN_DIVISOR;
                data.Y += (float) Math.Sin(data.Y * SIN_BIAS) / SIN_DIVISOR;
            }
        }

        private class GlassWallScope : Effect
        {
            private float rotation = 0;

            private static readonly float LINE_LENGTH = 1 / (float) Math.Sin(Math.PI / 4);
    
            public override void NextFrame (IController controller)
            {
                this.rotation = (this.rotation + 0.25f) % 360;
            }
            
            public override void RenderFrame (IController controller)
            {
                float[] pcm = new float[controller.PlayerData.NativePCMLength];
                controller.PlayerData.GetPCM(pcm);
    
                gl.glMatrixMode(gl.GL_MODELVIEW);
                gl.glPushMatrix();
                gl.glRotatef(this.rotation, 0, 0, -1);

                gl.glLineWidth(3);
    
                gl.glBegin(gl.GL_LINE_STRIP);
                for (int i = 0; i < pcm.Length; i++) {
                    float fi = ((float) i / pcm.Length) * 2 - 1;
                    fi *= LINE_LENGTH;
                    
                    float v = pcm[i];
                    float av = Math.Abs(v);
                    
                    gl.glColor4f(av, 0.5f + (0.5f * av), 1, 0.5f + (0.5f * av));
                    gl.glVertex2f(fi, v);
                }
                gl.glEnd();
                
                gl.glPopMatrix();
            }
        }
    }
}
