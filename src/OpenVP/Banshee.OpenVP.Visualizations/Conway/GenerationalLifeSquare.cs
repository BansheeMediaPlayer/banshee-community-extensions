//
// GenerationalLifeSquare.cs
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
using System.Collections.Generic;

using OpenVP;

namespace Banshee.OpenVP.Visualizations.Conway
{
    public class GenerationalLifeSquare
    {
        public int X { get; private set; }
        public int Y { get; private set; }

        public int Generation { get; private set; }
        public bool Alive
        {
            get { return Generation != 0; }
        }

        public float Hue { get; set; }

        private int nextGeneration;
        private float nextHue;

        public GenerationalLifeSquare(int x, int y)
        {
            X = x;
            Y = y;
            Generation = 0;
        }

        public void NextGeneration(IEnumerable<GenerationalLifeSquare> neighbors)
        {
            int alive = 0;

            float[] aliveHues = new float[3];

            foreach (var i in neighbors) {
                if (i.Alive) {
                    if (alive == 3) {
                        nextGeneration = 0;
                        return;
                    }

                    aliveHues[alive++] = i.Hue;
                }
            }

            if (alive < 2) {
                nextGeneration = 0;
            } else if (!Alive && alive == 3) {
                nextGeneration = 1;
                nextHue = MixHues(aliveHues);
            } else if (Alive) {
                nextGeneration = Generation + 1;
                nextHue = Hue;
            }
        }

        private float MixHues(float[] hues)
        {
            int north = 0;
            int south = 0;

            foreach (var i in hues) {
                if (i < 45 || i >= 225)
                    north++;
                else
                    south++;
            }

            float avg = 0;

            bool translate = north > south;

            foreach (var i in hues) {
                avg += i;

                if (translate && i > 180)
                    avg -= 360;
            }

            return avg / hues.Length;
        }

        public void Birth(float hue)
        {
            nextGeneration = 1;
            nextHue = hue;
        }

        public void Kill()
        {
            nextGeneration = 0;
        }

        public void Commit()
        {
            Generation = nextGeneration;
            Hue = nextHue % 360;
        }
    }
}
