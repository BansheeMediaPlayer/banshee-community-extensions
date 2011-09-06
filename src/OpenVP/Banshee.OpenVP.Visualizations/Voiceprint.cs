//
// Voiceprint.cs
//
// Author:
//       Chris Howie <cdhowie@gmail.com>
//       Nicholas Parker <nickbp@gmail.com>
//
// Copyright (c) 2009 Chris Howie
// Copyright (c) 2011 Nicholas Parker
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
            if (buffer == null) {
                buffer = new TextureHandle(w, h);
            } else {
                buffer.SetTextureSize(w, h);
            }
        }

        public void Render(IController controller)
        {
            EnsureBufferLength(controller.Height);

            controller.PlayerData.GetSpectrum(spectrumBuffer);

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

            ResizeTexture(controller.Width, controller.Height);

            Gl.glBindTexture(Gl.GL_TEXTURE_2D, buffer.TextureId);
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
            for (int i = 0; i < spectrumBuffer.Length; i++) {
                float v = spectrumBuffer[i];

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

        public void Dispose()
        {
            if (buffer != null) {
                buffer.Dispose();
                buffer = null;
            }
        }
    }

    public class ScaleVoiceprint : IRenderer, IDisposable
    {
        private const double scale = 0.5;
        private float[] spectrumBuffer,
            spectrumScaling;//for each index in spectrumBuffer, how wide (in px) that index should be
        private int spectrumViewLength = -1;//last value

        private TextureHandle buffer;

        public ScaleVoiceprint()
        {
        }

        private void EnsureBufferLengths(int dataLength, int viewLength)
        {
            bool rescale = false;
            if (spectrumBuffer == null || spectrumBuffer.Length != dataLength) {
                spectrumBuffer = new float[dataLength];
                spectrumScaling = new float[dataLength];
                rescale = true;
            }
            if (viewLength != spectrumViewLength) {
                spectrumViewLength = viewLength;
                rescale = true;
            }
            if (rescale) {
                /*
                  dataLen: size of the spectrum data
                  viewLen: height of the display
                  i: range from 0 to dataLen
                  viewI: view width of data pixel i (what we're calculating)
                  dataI: data at data pixel i

                  Start off with this formula:
                  viewI = (dataLen - dataI)^scale / dataLen^scale

                  Get the integral of the above formula from 0 to dataLen to
                  calculate the sum view widths the above formula would produce:

                  integral(viewI) = (dataI - dataLen) * dataLen^-scale * (dataLen - dataI)^s / (s + 1)
                  integral(viewI)[0,dataLen] = 0 - (-dataLen / (scale + 1))
                    = dataLen / (scale + 1) = sum(viewI)

                  Multiply the viewI formula by (viewLen/sum(viewI)) to get
                  things scaled to the length of the view:

                  viewLenIScaled = viewLenI * viewLen / sum(viewI)
                    = [(dataLen - dataI)^scale / dataLen^scale] * viewLen / [dataLen / (scale + 1)]
                    = (dataLen - dataI)^scale * viewLen * (scale + 1) / dataLen^(scale+1)

                  So now we have a formula which calculates, for each value in
                  the spectrum, how wide that value should appear in the display.
                  The scaled formula also pre-scales those widths to match
                  the width of the display.
                */
                double multiplier = viewLength * (scale + 1) / Math.Pow(dataLength, scale + 1);
                for (int i = 0; i < dataLength; ++i) {
                    spectrumScaling[i] = (float)(Math.Pow(dataLength-i, scale) * multiplier);
                }
            }
        }

        private void ResizeTexture(int w, int h)
        {
            if (buffer == null) {
                buffer = new TextureHandle(w, h);
            } else {
                buffer.SetTextureSize(w, h);
            }
        }

        public void Render(IController controller)
        {
            EnsureBufferLengths(controller.PlayerData.NativeSpectrumLength, controller.Height);

            controller.PlayerData.GetSpectrum(spectrumBuffer);

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

            ResizeTexture(controller.Width, controller.Height);

            Gl.glBindTexture(Gl.GL_TEXTURE_2D, buffer.TextureId);
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

            Gl.glBegin(Gl.GL_QUADS);
            float y = 0;
            for (int i = 0; i < controller.PlayerData.NativeSpectrumLength; ++i) {
                float v = spectrumBuffer[i];

                Color.FromHSL(120 * (1 - v), 1, Math.Min(0.5f, v)).Use();
                //Gl.glColor3f(v, v, v);

                Gl.glVertex2f(controller.Width - 1, y);
                Gl.glVertex2f(controller.Width, y);
                y += spectrumScaling[i];
                Gl.glVertex2f(controller.Width, y);
                Gl.glVertex2f(controller.Width - 1, y);
            }
            Gl.glEnd();

            Gl.glMatrixMode(Gl.GL_PROJECTION);
            Gl.glPopMatrix();

            Gl.glMatrixMode(Gl.GL_TEXTURE);
            Gl.glPopMatrix();

            Gl.glPopAttrib();
        }

        public void Dispose()
        {
            if (buffer != null) {
                buffer.Dispose();
                buffer = null;
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
            if (buffer == null) {
                buffer = new TextureHandle(w, h);
            } else {
                buffer.SetTextureSize(w, h);
            }
        }

        public void Render(IController controller)
        {
            int w = controller.Width / 2;
            int h = controller.Height;

            EnsureBufferLength(controller.Height / 2);

            controller.PlayerData.GetSpectrum(spectrumBuffer);

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

            ResizeTexture(w, h);

            Gl.glBindTexture(Gl.GL_TEXTURE_2D, buffer.TextureId);
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
            for (int i = 0; i < spectrumBuffer.Length; i++) {
                float v = spectrumBuffer[spectrumBuffer.Length - i - 1];

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

        public void Dispose()
        {
            if (buffer != null) {
                buffer.Dispose();
                buffer = null;
            }
        }
    }
}
