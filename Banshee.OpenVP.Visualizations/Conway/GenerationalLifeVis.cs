//
// GenerationalLifeVis.cs
//
// Author:
//       Chris Howie <cdhowie@gmail.com>
//
// Copyright (c) 2010 Chris Howie
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
using System.Linq;

using OpenVP;
using Tao.OpenGl;
using System.Collections.Generic;

namespace Banshee.OpenVP.Visualizations.Conway
{
    public class GenerationalLifeVis : IRenderer
    {
        private const float MAX_COVER = 0.75f;
        private const float COVER_TOLERANCE = 0.025f;

        private GenerationalLifeBoard board;
        private Random rand = new Random();

        public GenerationalLifeVis()
        {
            board = new GenerationalLifeBoard(100, 100);
            board.Wrap = true;
        }

        public void Render(IController controller)
        {
            ComputeState(controller);

            Gl.glClear(Gl.GL_COLOR_BUFFER_BIT);

            Gl.glMatrixMode(Gl.GL_MODELVIEW);
            Gl.glPushMatrix();
            Gl.glLoadIdentity();

            Gl.glTranslatef(-1, -1, 0);
            Gl.glScalef((float) 2 / board.Width, (float) 2 / board.Height, 1);

            Gl.glBegin(Gl.GL_QUADS);

            foreach (var i in board) {
                if (!i.Alive)
                    continue;

                Color.FromHSL(i.Hue, 1, 0.5f).Use();

                Gl.glVertex2i(i.X    , i.Y    );
                Gl.glVertex2i(i.X + 1, i.Y    );
                Gl.glVertex2i(i.X + 1, i.Y + 1);
                Gl.glVertex2i(i.X    , i.Y + 1);
            }

            Gl.glEnd();

            Gl.glPopMatrix();
        }

        private void ComputeState(IController controller)
        {
            float[] pcm = new float[controller.PlayerData.NativePCMLength];
            controller.PlayerData.GetPCM(pcm);

            float avg = AveragePcm(pcm) * MAX_COVER;

            List<GenerationalLifeSquare> aliveSquares = board.Where(i => i.Alive).ToList();

            float alivePct = (float) aliveSquares.Count / board.Count;

            if (alivePct < (avg - COVER_TOLERANCE) || alivePct > (avg + COVER_TOLERANCE)) {
                float diff = avg - alivePct;

                int diffCount = (int) (diff * board.Count);
                if (diffCount > 0)
                    Birth(diffCount);
                else
                    Kill(-diffCount);
            }

            board.NextGeneration();
            board.CommitGeneration();
        }

        private void Kill(int count)
        {
            var targets = from i in board
                where i.Alive
                    orderby rand.Next()
                    orderby i.Generation descending
                    select i;

            foreach (var i in targets.Take(count)) {
                i.Kill();
                i.Commit();
            }
        }

        private void Birth(int count)
        {
            while (count-- > 0) {
                GenerationalLifeSquare square;

                do {
                    int x = rand.Next(board.Width);
                    int y = rand.Next(board.Height);
                    square = board[x, y];
                } while (square.Alive);

                square.Birth((float) (rand.NextDouble() * 360));
                square.Commit();
            }
        }

        private float AveragePcm(float[] array)
        {
            if (array.Length == 0)
                return float.NaN;

            float total = 0;

            for (int i = 0; i < array.Length; i++)
                total += Math.Abs(array[i]);

            return total / array.Length;
        }
    }
}

