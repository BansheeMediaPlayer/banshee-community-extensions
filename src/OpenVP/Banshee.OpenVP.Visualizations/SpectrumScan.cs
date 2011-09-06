//
// SpectrumScan.cs
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
    // Horizontal, right -> left
    public class SpectrumScan : IRenderer, IDisposable
    {
        // what percent of the screen's width should the analyzer take up
        private const double analyzerWidthPct = 0.15,
            analyzerWidthOrtho = 2 / analyzerWidthPct;

        private const double scale = 0.5;
        private int bufferLength = -1, voiceprintWidthPx, viewHeightPx = -1;
        private float[] spectrumBuffer, smoothBuffer,
            bufferScaling;//for each index in *Buffer, how wide (in px) that index should be

        private TextureHandle voiceprintBuffer;

        public SpectrumScan()
        {
        }

        private void GetSpectrum(IController controller)
        {
            //initialize/populate things that depend on spectrum length
            bool rescale = false;
            if (controller.PlayerData.NativeSpectrumLength != bufferLength) {
                bufferLength = controller.PlayerData.NativeSpectrumLength;

                spectrumBuffer = new float[bufferLength];
                smoothBuffer = new float[bufferLength];
                bufferScaling = new float[bufferLength];
                rescale = true;
            }

            controller.PlayerData.GetSpectrum(spectrumBuffer);

            for (int i = 0; i < bufferLength; i++) {
                // go with a linear decrease - avoids appearance of
                // disconnectedness between analyzer and voiceprint, without
                // making analyzer look too jittery
                smoothBuffer[i] =
                    Math.Max(spectrumBuffer[i], smoothBuffer[i] - 0.05f);
            }

            //initialize/populate things that depend on view size
            voiceprintWidthPx = (int)(controller.Width * (1 - analyzerWidthPct));
            if (viewHeightPx != controller.Height) {
                viewHeightPx = controller.Height;
                rescale = true;
            }

            if (rescale) {
                //formula: pxlen = (dataLen - dataI)^scale / dataLen^scale
                //integrate over dataI from 0 to dataLen: sum(pxlen) = dataLen / (scale + 1)
                //scaled formula: pxlen = (dataLen - dataI)^scale * viewLen * (scale + 1) / dataLen^(scale + 1)
                double multiplier = viewHeightPx * (scale + 1) / Math.Pow(bufferLength, scale + 1);
                for (int i = 0; i < bufferLength; ++i) {
                    bufferScaling[i] = (float)(Math.Pow(bufferLength-i, scale) * multiplier);
                }
            }
        }

        public void Render(IController controller)
        {
            GetSpectrum(controller);

            Gl.glPushAttrib(Gl.GL_ENABLE_BIT);
            Gl.glDisable(Gl.GL_DEPTH_TEST);
            Gl.glTexEnvf(Gl.GL_TEXTURE_ENV, Gl.GL_TEXTURE_ENV_MODE, Gl.GL_DECAL);

            // voiceprint

            Gl.glMatrixMode(Gl.GL_PROJECTION);
            Gl.glPushMatrix();
            Gl.glLoadIdentity();

            Glu.gluOrtho2D(0, controller.Width, 0, controller.Height);

            Gl.glMatrixMode(Gl.GL_TEXTURE);
            Gl.glPushMatrix();
            Gl.glLoadIdentity();

            Gl.glScalef(1f / controller.Width, 1f / controller.Height, 1f);

            if (voiceprintBuffer == null) {
                voiceprintBuffer = new TextureHandle(controller.Width, controller.Height);
            } else {
                voiceprintBuffer.SetTextureSize(controller.Width, controller.Height);
            }

            Gl.glBindTexture(Gl.GL_TEXTURE_2D, voiceprintBuffer.TextureId);
            // OPTIMIZATION: could just copy texture of width voiceprintWidthPx
            // but then the coordinates get tricky
            Gl.glCopyTexImage2D(Gl.GL_TEXTURE_2D, 0, Gl.GL_RGB, 0, 0,
                    controller.Width, controller.Height, 0);

            // clean palette
            Gl.glClearColor(0, 0, 0, 1);
            Gl.glClear(Gl.GL_COLOR_BUFFER_BIT);

            // paste voiceprint region of copied texture, shifted left 1px
            Gl.glEnable(Gl.GL_TEXTURE_2D);
            Gl.glBegin(Gl.GL_QUADS);

            Gl.glTexCoord2f(1, 0);
            Gl.glVertex2f(0, 0);

            Gl.glTexCoord2f(1, controller.Height);
            Gl.glVertex2f(0, controller.Height);

            Gl.glTexCoord2f(voiceprintWidthPx, controller.Height);
            Gl.glVertex2f(voiceprintWidthPx - 1, controller.Height);

            Gl.glTexCoord2f(voiceprintWidthPx, 0);
            Gl.glVertex2f(voiceprintWidthPx - 1, 0);

            Gl.glEnd();
            Gl.glDisable(Gl.GL_TEXTURE_2D);

            Gl.glBegin(Gl.GL_QUADS);
            // instead of using a smoothed buffer like with the analyzer,
            // use the direct buffer for nicer looking output (less blurry)
            float y = 0;
            for (int i = 0; i < bufferLength; i++) {
                float v = spectrumBuffer[i];
                Color.FromHSL(120 * (1 - v), 1, Math.Min(0.5f, v)).Use();

                // draw a little beyond the edge to ensure no visible seams
                Gl.glVertex2f(voiceprintWidthPx-1, y);
                Gl.glVertex2f(voiceprintWidthPx+1, y);
                y += bufferScaling[i];
                Gl.glVertex2f(voiceprintWidthPx+1, y);
                Gl.glVertex2f(voiceprintWidthPx-1, y);
            }
            Gl.glEnd();

            Gl.glMatrixMode(Gl.GL_TEXTURE);
            Gl.glPopMatrix();

            Gl.glMatrixMode(Gl.GL_PROJECTION);
            Gl.glPopMatrix();

            // analyzer

            Gl.glMatrixMode(Gl.GL_PROJECTION);
            Gl.glPushMatrix();
            Gl.glLoadIdentity();

            // analyzer at right side of screen
            Glu.gluOrtho2D(1 - analyzerWidthOrtho, 1, -1, 1);

            RenderAnalyzer();// coordinates: [-1,+1] in x,y

            Gl.glMatrixMode(Gl.GL_PROJECTION);
            Gl.glPopMatrix();

            Gl.glPopAttrib();
        }

        private void RenderAnalyzer()
        {
            Gl.glBegin(Gl.GL_QUADS);

            // instead of directly using the raw buffer like with voiceprint,
            // use a smoothed buffer for nicer looking output (less jittery)
            float y = -1;
            float analyzerConv = 2f / viewHeightPx;//px -> [-1,1]
            for (int i = 0; i < bufferLength; i++) {
                float v = smoothBuffer[i];
                Color.FromHSL(120 * (1 - v), 1, Math.Min(0.5f, v)).Use();
                //Color.FromHSL(120 * (1 - v), 1, 0.5f).Use();

                Gl.glVertex2f(-1, y);
                Gl.glVertex2f(2*v - 1, y);
                y += analyzerConv * bufferScaling[i];
                Gl.glVertex2f(2*v - 1, y);
                Gl.glVertex2f(-1, y);
            }

            Gl.glEnd();
        }

        public void Dispose()
        {
            if (voiceprintBuffer != null) {
                voiceprintBuffer.Dispose();
                voiceprintBuffer = null;
            }
        }
    }


    // Vertical, top -> bottom
    public class SpectrumRain : IRenderer, IDisposable
    {
        // what percent of the screen's height should the analyzer take up
        private const double analyzerHeightPct = 0.25,
            analyzerHeightOrtho = 2 / analyzerHeightPct;

        private const double scale = 0.5;
        private int bufferLength = -1, voiceprintHeightPx, viewWidthPx = -1;
        private float[] spectrumBuffer, smoothBuffer,
            bufferScaling;//for each index in *Buffer, how wide (in px) that index should be

        private TextureHandle voiceprintBuffer;

        public SpectrumRain()
        {
        }

        private void GetSpectrum(IController controller)
        {
            //initialize/populate things that depend on spectrum length
            bool rescale = false;
            if (controller.PlayerData.NativeSpectrumLength != bufferLength) {
                bufferLength = controller.PlayerData.NativeSpectrumLength;

                spectrumBuffer = new float[bufferLength];
                smoothBuffer = new float[bufferLength];
                bufferScaling = new float[bufferLength];
                rescale = true;
            }

            controller.PlayerData.GetSpectrum(spectrumBuffer);

            for (int i = 0; i < bufferLength; i++) {
                // go with a linear decrease - avoids appearance of
                // disconnectedness between analyzer and voiceprint, without
                // making analyzer look too jittery
                smoothBuffer[i] =
                    Math.Max(spectrumBuffer[i], smoothBuffer[i] - 0.05f);
            }

            voiceprintHeightPx = (int)(controller.Height * (1 - analyzerHeightPct));
            if (viewWidthPx != controller.Width) {
                viewWidthPx = controller.Width;
                rescale = true;
            }

            if (rescale) {
                //formula: pxlen = (dataLen - dataI)^scale / dataLen^scale
                //integrate over dataI from 0 to dataLen: sum(pxlen) = dataLen / (scale + 1)
                //scaled formula: pxlen = (dataLen - dataI)^scale * viewLen * (scale + 1) / dataLen^(scale + 1)
                double multiplier = viewWidthPx * (scale + 1) / Math.Pow(bufferLength, scale + 1);
                for (int i = 0; i < bufferLength; ++i) {
                    bufferScaling[i] = (float)(Math.Pow(bufferLength-i, scale) * multiplier);
                }
            }
        }

        public void Render(IController controller)
        {
            GetSpectrum(controller);

            Gl.glPushAttrib(Gl.GL_ENABLE_BIT);
            Gl.glDisable(Gl.GL_DEPTH_TEST);
            Gl.glTexEnvf(Gl.GL_TEXTURE_ENV, Gl.GL_TEXTURE_ENV_MODE, Gl.GL_DECAL);

            // voiceprint

            Gl.glMatrixMode(Gl.GL_PROJECTION);
            Gl.glPushMatrix();
            Gl.glLoadIdentity();

            Glu.gluOrtho2D(0, controller.Width, 0, controller.Height);

            Gl.glMatrixMode(Gl.GL_TEXTURE);
            Gl.glPushMatrix();
            Gl.glLoadIdentity();

            Gl.glScalef(1f / controller.Width, 1f / controller.Height, 1f);

            if (voiceprintBuffer == null) {
                voiceprintBuffer = new TextureHandle(controller.Width, controller.Height);
            } else {
                voiceprintBuffer.SetTextureSize(controller.Width, controller.Height);
            }

            Gl.glBindTexture(Gl.GL_TEXTURE_2D, voiceprintBuffer.TextureId);
            // OPTIMIZATION: could just copy texture of width voiceprintHeightPx
            // but then the coordinates get tricky
            Gl.glCopyTexImage2D(Gl.GL_TEXTURE_2D, 0, Gl.GL_RGB, 0, 0,
                    controller.Width, controller.Height, 0);

            // clean palette
            Gl.glClearColor(0, 0, 0, 1);
            Gl.glClear(Gl.GL_COLOR_BUFFER_BIT);

            // paste voiceprint region of copied texture, shifted down 1px
            Gl.glEnable(Gl.GL_TEXTURE_2D);
            Gl.glBegin(Gl.GL_QUADS);

            Gl.glTexCoord2f(0, 1);
            Gl.glVertex2f(0, 0);

            Gl.glTexCoord2f(0, voiceprintHeightPx);
            Gl.glVertex2f(0, voiceprintHeightPx - 1);

            Gl.glTexCoord2f(controller.Width, voiceprintHeightPx);
            Gl.glVertex2f(controller.Width, voiceprintHeightPx - 1);

            Gl.glTexCoord2f(controller.Width, 1);
            Gl.glVertex2f(controller.Width, 0);

            Gl.glEnd();
            Gl.glDisable(Gl.GL_TEXTURE_2D);

            Gl.glBegin(Gl.GL_QUADS);
            // instead of using a smoothed buffer like with the analyzer,
            // use the direct buffer for nicer looking output (less blurry)
            float x = 0;
            for (int i = 0; i < bufferLength; i++) {
                float v = spectrumBuffer[i];
                Color.FromHSL(120 * (1 - v), 1, Math.Min(0.5f, v)).Use();

                // draw a little beyond the edge to ensure no visible seams
                Gl.glVertex2f(x, voiceprintHeightPx-1);
                Gl.glVertex2f(x, voiceprintHeightPx+1);
                x += bufferScaling[i];
                Gl.glVertex2f(x, voiceprintHeightPx+1);
                Gl.glVertex2f(x, voiceprintHeightPx-1);
            }
            Gl.glEnd();

            Gl.glMatrixMode(Gl.GL_TEXTURE);
            Gl.glPopMatrix();

            Gl.glMatrixMode(Gl.GL_PROJECTION);
            Gl.glPopMatrix();

            // analyzer

            Gl.glMatrixMode(Gl.GL_PROJECTION);
            Gl.glPushMatrix();
            Gl.glLoadIdentity();

            // analyzer at top side of screen
            Glu.gluOrtho2D(-1, 1, 1 - analyzerHeightOrtho, 1);

            RenderAnalyzer();// coordinates: [-1,+1] in x,y

            Gl.glMatrixMode(Gl.GL_PROJECTION);
            Gl.glPopMatrix();

            Gl.glPopAttrib();
        }

        private void RenderAnalyzer()
        {
            Gl.glBegin(Gl.GL_QUADS);

            // instead of directly using the raw buffer like with voiceprint,
            // use a smoothed buffer for nicer looking output (less jittery)
            float x = -1;
            float analyzerConv = 2f / viewWidthPx;//px -> [-1,1]
            for (int i = 0; i < bufferLength; i++) {
                float v = smoothBuffer[i];
                Color.FromHSL(120 * (1 - v), 1, Math.Min(0.5f, v)).Use();
                //Color.FromHSL(120 * (1 - v), 1, 0.5f).Use();

                Gl.glVertex2f(x, -1);
                Gl.glVertex2f(x, 2*v - 1);
                x += analyzerConv * bufferScaling[i];
                Gl.glVertex2f(x, 2*v - 1);
                Gl.glVertex2f(x, -1);
            }

            Gl.glEnd();
        }

        public void Dispose()
        {
            if (voiceprintBuffer != null) {
                voiceprintBuffer.Dispose();
                voiceprintBuffer = null;
            }
        }
    }
}
