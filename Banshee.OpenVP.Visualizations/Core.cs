// Core.cs
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
using Tao.OpenGl;

using Buffer = OpenVP.Core.Buffer;

namespace Banshee.OpenVP.Visualizations
{
    public class Core : LinearPreset
    {
        public Core()
        {
            TextureHandle bufferTexture = new TextureHandle();
            
            SingleBuffer buffer = new SingleBuffer();
            buffer.Load = true;
            buffer.Texture = bufferTexture;
            this.Effects.Add(buffer);
            
            ClearScreen clear = new ClearScreen();
            clear.ClearColor = new Color(0, 0, 0, 0.085f);
            this.Effects.Add(clear);

            this.Effects.Add(new RandomMovement());

            this.Effects.Add(new CustomScope());

            Scope scope = new Scope();
            scope.Color = new Color(1, 0.5f, 0, 1);
            scope.Circular = true;
            scope.LineWidth = 3;
            this.Effects.Add(scope);

            buffer = new SingleBuffer();
            buffer.Load = false;
            buffer.Texture = bufferTexture;
            this.Effects.Add(buffer);
            
            this.Effects.Add(new CircularMovement());
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

        private class CustomScope : ScopeBase
        {
            private float green;

            public override void RenderFrame(IController controller)
            {
                float[] pcm = new float[controller.PlayerData.NativePCMLength];
                controller.PlayerData.GetPCM(pcm);

                float avg = 0;
                for (int i = 0; i < pcm.Length; i++)
                    avg += Math.Abs(pcm[i]);

                avg /= pcm.Length;

                green = Math.Min(2 * avg, 0.75f);

                this.LineWidth = Math.Max(8 * avg, 0.01f);
                
                base.RenderFrame(controller);
            }

            protected override void PlotVertex(ScopeBase.ScopeData data)
            {
                data.X = 2 * data.FractionalI - 1;
                data.Y = data.Value;
                
                data.Red = 0;
                data.Green = green;
                data.Blue = 1;
            }
        }

        private class CircularMovement : MovementBase
        {
            private float rr = 0;

            public CircularMovement()
            {
                this.XResolution = 63;
                this.YResolution = 63;
                this.Wrap = true;
            }

            protected override void OnRenderFrame()
            {
                rr += 0.05f;
            }

            protected override void PlotVertex (MovementBase.MovementData data)
            {
                data.Method = MovementMethod.Polar;
                data.Rotation = (float) Math.Cos(data.Rotation * 4 - rr / 4) + rr;
                data.Distance *= 0.75f;
            }
        }

        private class SingleBuffer : Effect
        {
            public bool Load { get; set; }

            public TextureHandle Texture { get; set; }

            public override void NextFrame(IController controller)
            {
            }
            
            public override void RenderFrame(IController controller)
            {
                Gl.glPushAttrib(Gl.GL_ENABLE_BIT);
                Gl.glEnable(Gl.GL_TEXTURE_2D);
                Gl.glDisable(Gl.GL_DEPTH_TEST);
                Gl.glTexEnvf(Gl.GL_TEXTURE_ENV, Gl.GL_TEXTURE_ENV_MODE, Gl.GL_DECAL);
                
                Texture.SetTextureSize(controller.Width,
                                       controller.Height);
                
                Gl.glBindTexture(Gl.GL_TEXTURE_2D, Texture.TextureId);
                
                Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_WRAP_S,
                                   Gl.GL_CLAMP);
                
                Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_WRAP_T,
                                   Gl.GL_CLAMP);

                if (this.Load) {
                        Gl.glColor4f(1, 1, 1, 1);
                        Gl.glBegin(Gl.GL_QUADS);
                        
                        Gl.glTexCoord2f(0, 0);
                        Gl.glVertex2f(-1, -1);
                        
                        Gl.glTexCoord2f(0, 1);
                        Gl.glVertex2f(-1,  1);
                        
                        Gl.glTexCoord2f(1, 1);
                        Gl.glVertex2f( 1,  1);
                        
                        Gl.glTexCoord2f(1, 0);
                        Gl.glVertex2f( 1, -1);
                        
                        Gl.glEnd();
                } else {
                        Gl.glCopyTexImage2D(Gl.GL_TEXTURE_2D, 0, Gl.GL_RGB, 0, 0,
                                            controller.Width,
                                            controller.Height, 0);
                }
                
                Gl.glPopAttrib();
            }

            public override void Dispose()
            {
                if (this.Texture != null) {
                    this.Texture.Dispose();
                    this.Texture = null;
                }

                base.Dispose();
            }
        }
    }
}
