// 
// Voiceprint.cs
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
            Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_WRAP_S, Gl.GL_CLAMP);
            Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_WRAP_T, Gl.GL_CLAMP);
            
            Gl.glMatrixMode(Gl.GL_PROJECTION);
            Gl.glPushMatrix();
            Gl.glLoadIdentity();
            
            Glu.gluOrtho2D(0, controller.Width, 0, controller.Height);
            
            Gl.glMatrixMode(Gl.GL_TEXTURE);
            Gl.glPushMatrix();
            Gl.glLoadIdentity();
            
            Gl.glScalef(1f / controller.Width, 1f / controller.Height, 1f);

            this.ResizeTexture(controller.Width, controller.Height);

            Gl.glBindTexture(Gl.GL_TEXTURE_2D, this.buffer.TextureId);
            Gl.glCopyTexImage2D(Gl.GL_TEXTURE_2D, 0, Gl.GL_RGB, 0, 0,
                                controller.Width, controller.Height, 0);
            
            Gl.glColor4f(1, 1, 1, 1);
            
            Gl.glEnable(Gl.GL_TEXTURE_2D);
            Gl.glBegin(Gl.GL_QUADS);
            
            Gl.glTexCoord2f(1, 0);
            Gl.glVertex2f(0, 0);
            
            Gl.glTexCoord2f(1, controller.Height);
            Gl.glVertex2f(0, controller.Height);
            
            Gl.glTexCoord2f(controller.Width, controller.Height);
            Gl.glVertex2f(controller.Width - 1, controller.Height);
            
            Gl.glTexCoord2f(controller.Width, 0);
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
            
            Gl.glMatrixMode(Gl.GL_TEXTURE);
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
            Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_WRAP_S, Gl.GL_CLAMP);
            Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_WRAP_T, Gl.GL_CLAMP);
            
            Gl.glMatrixMode(Gl.GL_PROJECTION);
            Gl.glPushMatrix();
            
            Glu.gluOrtho2D(0, w * 2, 0, h);
            
            Gl.glMatrixMode(Gl.GL_TEXTURE);
            Gl.glPushMatrix();
            Gl.glLoadIdentity();
            
            Gl.glScalef(1f / w, 1f / h, 1f);

            this.ResizeTexture(w, h);

            Gl.glBindTexture(Gl.GL_TEXTURE_2D, this.buffer.TextureId);
            Gl.glCopyTexImage2D(Gl.GL_TEXTURE_2D, 0, Gl.GL_RGB, 0, 0,
                                w, h, 0);

            Gl.glEnable(Gl.GL_TEXTURE_2D);
            Gl.glBegin(Gl.GL_QUADS);
            
            Gl.glTexCoord2f(1, 0);
            Gl.glVertex2f(-5, 0);
            
            Gl.glTexCoord2f(1, h);
            Gl.glVertex2f(-5, h);
            
            Gl.glTexCoord2f(w, h);
            Gl.glVertex2f(w - 1, h);
            
            Gl.glTexCoord2f(w, 0);
            Gl.glVertex2f(w - 1, 0);
            
            Gl.glTexCoord2f(1, 0);
            Gl.glVertex2f(w * 2 + 5, 0);
            
            Gl.glTexCoord2f(1, h);
            Gl.glVertex2f(w * 2 + 5, h);
            
            Gl.glTexCoord2f(w, h);
            Gl.glVertex2f(w - 1, h);
            
            Gl.glTexCoord2f(w, 0);
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

            Gl.glMatrixMode(Gl.GL_TEXTURE);
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
