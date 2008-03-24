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

        public VolumeFade(ushort start, ushort end, ushort duration)
        {
            sleep = ((float) duration / (float) Math.Abs(end - start)) * 1000;
            increment = start < end ? 1 : -1;
            endVolume = end;
            curVolume = start;
            GLib.Timeout.Add((uint) sleep, VolumeFadeTick);
        }
        
        private bool VolumeFadeTick(){
            if(curVolume == endVolume){
                Log.Debug("Volume Fade: Done.","");
                return false;
            }
            
            if(increment == 1)
                curVolume++;
            else
                curVolume--;
            
            ServiceManager.PlayerEngine.Volume = curVolume;
            Log.Debug("Volume Fade: Fading a notch...",
                    String.Format("Vol={0}, curVol={1}, End={2}, inc={3}, TickTime={4}ms",
                        ServiceManager.PlayerEngine.Volume,
                        curVolume,
                        endVolume,
                        increment,
                        sleep
                    ));

            return true;
        }
        
    }
}
