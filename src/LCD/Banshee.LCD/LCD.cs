//
// LCD.cs
//
// Authors:
//   André Gaul
//
// Copyright (C) 2010 André Gaul
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using Hyena;

namespace Banshee.LCD
{
    public class LCD
    {
        //width, height, cellwidth, cellheight
        public int w;
        public int h;
        public int cw;
        public int ch;

        public LCD(int w, int h, int cw, int ch)
        {
            this.w=w;
            this.h=h;
            this.cw=cw;
            this.ch=ch;
            Hyena.Log.Debug("Created LCD (width: "+w.ToString()+", height: "+h.ToString()+", cellwidth: "+cw.ToString()+", cellheight: "+ch.ToString()+")");
        }
    }
}