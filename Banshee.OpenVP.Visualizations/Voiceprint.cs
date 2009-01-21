// Voiceprint.cs
//
//  Copyright (C) 2008 Chris Howie
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

namespace Banshee.OpenVP.Visualizations
{
    public class Voiceprint : IRenderer, IDisposable
    {
        private float[] spectrumBuffer;

        private TextureHandle buffer;
        
        public Voiceprint()
        {
        }

        private void EnsureBufferLength(int length)
        {
            if (spectrumBuffer == null || spectrumBuffer.Length != length)
                spectrumBuffer = new float[length];
        }

        private void ResizeTexture(int w, int h)
        {
            if (this.buffer == null) {
                this.buffer = new TextureHandle(w, h);
            } else {
                this.buffer.SetTextureSize(w, h);
            }
        }

        public void Render(IController controller)
        {
            this.EnsureBufferLength(controller.Height);

            controller.PlayerData.GetSpectrum(this.spectrumBuffer);

            Gl.glPushAttrib(Gl.GL_ENABLE_BIT);
            Gl.glDisable(Gl.GL_DEPTH_TEST);
            Gl.glTexEnvf(Gl.GL_TEXTURE_ENV, Gl.GL_TEXTURE_ENV_MODE, Gl.GL_DECAL);
            
            Gl.glMatrixMode(Gl.GL_PROJECTION);
            Gl.glPushMatrix();
            
            Glu.gluOrtho2D(0, controller.Width, 0, controller.Height);

            this.ResizeTexture(controller.Width - 1, controller.Height);

            Gl.glBindTexture(Gl.GL_TEXTURE_2D, this.buffer.TextureId);
            Gl.glCopyTexImage2D(Gl.GL_TEXTURE_2D, 0, Gl.GL_RGB, 1, 0,
                                controller.Width - 1, controller.Height, 0);

            Gl.glEnable(Gl.GL_TEXTURE_2D);
            Gl.glBegin(Gl.GL_QUADS);
            Gl.glTexCoord2f(0, 0);
            Gl.glVertex2f(0, 0);
            Gl.glTexCoord2f(0, 1);
            Gl.glVertex2f(0, controller.Height);
            Gl.glTexCoord2f(1, 1);
            Gl.glVertex2f(controller.Width - 1, controller.Height);
            Gl.glTexCoord2f(1, 0);
            Gl.glVertex2f(controller.Width - 1, 0);
            Gl.glEnd();
            Gl.glDisable(Gl.GL_TEXTURE_2D);

            Gl.glBegin(Gl.GL_POINTS);
            for (int i = 0; i < this.spectrumBuffer.Length; i++) {
                float v = this.spectrumBuffer[i];
                
                Color.FromHSL(120 * (1 - v), 1, Math.Min(0.5f, v)).Use();
                //Gl.glColor3f(v, v, v);

                Gl.glVertex2i(controller.Width - 1, i);
            }
            Gl.glEnd();

            Gl.glMatrixMode(Gl.GL_PROJECTION);
            Gl.glPopMatrix();

            Gl.glPopAttrib();
        }

        public void Dispose ()
        {
            if (this.buffer != null) {
                this.buffer.Dispose();
                this.buffer = null;
            }
        }
    }

    public class MirrorVoiceprint : IRenderer, IDisposable
    {
        private float[] spectrumBuffer;

        private TextureHandle buffer;
        
        public MirrorVoiceprint()
        {
        }

        private void EnsureBufferLength(int length)
        {
            if (spectrumBuffer == null || spectrumBuffer.Length != length)
                spectrumBuffer = new float[length];
        }

        private void ResizeTexture(int w, int h)
        {
            if (this.buffer == null) {
                this.buffer = new TextureHandle(w, h);
            } else {
                this.buffer.SetTextureSize(w, h);
            }
        }

        public void Render(IController controller)
        {
            int w = controller.Width / 2;
            int h = controller.Height;
            
            this.EnsureBufferLength(controller.Height / 2);

            controller.PlayerData.GetSpectrum(this.spectrumBuffer);

            Gl.glPushAttrib(Gl.GL_ENABLE_BIT);
            Gl.glDisable(Gl.GL_DEPTH_TEST);
            Gl.glTexEnvf(Gl.GL_TEXTURE_ENV, Gl.GL_TEXTURE_ENV_MODE, Gl.GL_DECAL);
            
            Gl.glMatrixMode(Gl.GL_PROJECTION);
            Gl.glPushMatrix();
            
            Glu.gluOrtho2D(0, w * 2, 0, h);

            this.ResizeTexture(w - 1, h);

            Gl.glBindTexture(Gl.GL_TEXTURE_2D, this.buffer.TextureId);
            Gl.glCopyTexImage2D(Gl.GL_TEXTURE_2D, 0, Gl.GL_RGB, 1, 0,
                                w - 1, h, 0);

            Gl.glEnable(Gl.GL_TEXTURE_2D);
            Gl.glBegin(Gl.GL_QUADS);
            
            Gl.glTexCoord2f(0, 0);
            Gl.glVertex2f(-5, 0);
            
            Gl.glTexCoord2f(0, 1);
            Gl.glVertex2f(-5, h);
            
            Gl.glTexCoord2f(1, 1);
            Gl.glVertex2f(w - 1, h);
            
            Gl.glTexCoord2f(1, 0);
            Gl.glVertex2f(w - 1, 0);
            
            Gl.glTexCoord2f(0, 0);
            Gl.glVertex2f(w * 2 + 5, 0);
            
            Gl.glTexCoord2f(0, 1);
            Gl.glVertex2f(w * 2 + 5, h);
            
            Gl.glTexCoord2f(1, 1);
            Gl.glVertex2f(w - 1, h);
            
            Gl.glTexCoord2f(1, 0);
            Gl.glVertex2f(w - 1, 0);
            
            Gl.glEnd();
            Gl.glDisable(Gl.GL_TEXTURE_2D);

            Gl.glBegin(Gl.GL_POINTS);
            for (int i = 0; i < this.spectrumBuffer.Length; i++) {
                float v = this.spectrumBuffer[this.spectrumBuffer.Length - i - 1];
                
                Color.FromHSL(120 * (1 - v), 1, Math.Min(0.5f, v)).Use();
                //Gl.glColor3f(v, v, v);

                Gl.glVertex2i(w - 1, i + 1);
                Gl.glVertex2i(w - 1, h - i - 1);
            }
            Gl.glEnd();

            Gl.glMatrixMode(Gl.GL_PROJECTION);
            Gl.glPopMatrix();

            Gl.glPopAttrib();
        }

        public void Dispose ()
        {
            if (this.buffer != null) {
                this.buffer.Dispose();
                this.buffer = null;
            }
        }
    }
}
