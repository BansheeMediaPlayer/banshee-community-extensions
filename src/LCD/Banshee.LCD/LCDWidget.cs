//
// LCDWidget.cs
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

namespace Banshee.LCD
{
    public abstract class LCDWidget
    {
        public string name;
        public string typename;

        public LCDWidget(string name, string typename)
        {
            this.name = name;
            this.typename = typename;
        }

        public abstract string GetSetString(LCDParser parser);
    }

    public class LCDWidgetString : LCDWidget
    {
        public int x, y;
        public string format;

        public LCDWidgetString(string name, int x, int y, string format) : base(name, "string")
        {
            this.x = x;
            this.y = y;
            this.format = format;
        }

        public override string GetSetString (LCDParser parser)
        {
            string dispstr=parser.Parse(format);
            return x.ToString()+" "+y.ToString()+" {"+dispstr+"}";
        }

    }

    public class LCDWidgetScroller : LCDWidget
    {
        public enum Direction
        {
            Horizontal,
            Vertical,
            Marquee
        };
        public int x, y, w, h;
        public Direction dir;
        public int speed;
        public string format;

        public LCDWidgetScroller(string name, int x, int y, int w, int h, Direction dir, int speed, string format) : base(name, "scroller")
        {
            this.x = x;
            this.y = y;
            this.w = w;
            this.h = h;
            this.dir = dir;
            this.speed = speed;
            this.format = format;
        }

        public override string GetSetString (LCDParser parser)
        {
            string dispstr=parser.Parse(format);
            string dirstr="unknown";
            switch(dir) {
            case Direction.Horizontal:
                dirstr="h";
                break;
            case Direction.Marquee:
                dirstr="m";
                break;
            case Direction.Vertical:
                dirstr="v";
                break;
            }
            int xend=x+w-1;
            int yend=y+h-1;
            return x.ToString()+" "+y.ToString()+" "
                    +xend.ToString()+" "+yend.ToString()+" "
                    +dirstr+" "+speed.ToString()+" {"+dispstr+"}";
        }
    }

    public class LCDWidgetTitle : LCDWidget
    {
        public string format;

        public LCDWidgetTitle(string name, string format) : base(name, "title")
        {
            this.format = format;
        }

        public override string GetSetString (LCDParser parser)
        {
            string dispstr=parser.Parse(format);
            return "{"+dispstr+"}";
        }

    }
}