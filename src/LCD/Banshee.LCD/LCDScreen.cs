//
// LCDScreen.cs
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

using System.Collections.Generic;

namespace Banshee.LCD
{
    public class LCDScreen
    {
        public enum Prio {
            Hidden,
            Background,
            Info,
            Foreground,
            Alert,
            Input
        };

        public string name;
        public Prio prio;
        public int dur; //duration in 1/8 seconds

        public LCDScreen(string name, Prio prio, int dur)
        {
            this.name = name;
            this.prio = prio;
            this.dur = dur;
        }

        public string GetStringPrio()
        {
            switch(prio){
            case Prio.Hidden:
                return "hidden";
            case Prio.Background:
                return "background";
            case Prio.Info:
                return "info";
            case Prio.Foreground:
                return "foreground";
            case Prio.Alert:
                return "alert";
            case Prio.Input:
                return "input";
            }
            return "unknown";
        }

        public string GetStringAdd()
        {
            return "screen_add "+name+"\n";
        }

        public string GetStringSet()
        {
            return "screen_set "+name+" -name "+name+" -priority "+GetStringPrio()+" -heartbeat off -duration "+dur.ToString()+"\n";
        }
    }
}