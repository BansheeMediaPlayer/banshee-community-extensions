//
// VolumeFade.cs
//
// Authors:
//   Bertrand Lorentz <bertrand.lorentz@gmail.com>
//   Patrick van Staveren  <trick@vanstaveren.us>
//
// Copyright (C) 2008-2009 Bertrand Lorentz and Patrick van Staveren.
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
using System.Threading;

using Hyena;
using Banshee.Base;
using Banshee.MediaEngine;
using Banshee.ServiceStack;

namespace Banshee.AlarmClock
{
    public class VolumeFade
    {
        float sleep;
        ushort endVolume;
        int increment;
        ushort curVolume;

        public VolumeFade (ushort start, ushort end, ushort duration)
        {
            sleep = ((float) duration / (float) Math.Abs (end - start)) * 1000;
            increment = start < end ? 1 : -1;
            endVolume = end;
            curVolume = start;
            GLib.Timeout.Add ((uint) sleep, VolumeFadeTick);
        }
        
        private bool VolumeFadeTick ()
        {
            if (curVolume == endVolume) {
                Log.Debug("Volume Fade: Done.");
                return false;
            }
            
            if (increment == 1) {
                curVolume++;
            } else {
                curVolume--;
            }
            
            ServiceManager.PlayerEngine.Volume = curVolume;
            Log.DebugFormat ("Volume Fade: Fading a notch. Vol={0}, curVol={1}, End={2}, inc={3}, TickTime={4}ms",
                ServiceManager.PlayerEngine.Volume, curVolume, endVolume, increment, sleep);

            return true;
        }
    }
}
