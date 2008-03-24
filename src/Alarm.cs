using System;
using System.Threading;
using System.Diagnostics;

using Hyena;
using Banshee.Base;
using Banshee.MediaEngine;
using Banshee.ServiceStack;

namespace Banshee.AlarmClock
{
    public class AlarmThread
    {
        private AlarmClockService plugin;

        public AlarmThread(AlarmClockService plugin)
        {
            this.plugin = plugin;
        }

        public void MainLoop()
        {
            try
            {
                while(true)
                {
                    int delay = (int)TimeUntilAlarm().TotalMilliseconds;
                    bool thread_interrupted = false;
                    
                    try
                    {
                        Thread.Sleep(delay);
                    }
                    catch(ThreadInterruptedException)
                    {
                        Log.Debug("Alarm Plugin: sleep interrupted", "");
                        thread_interrupted = true;
                    }
                    
                    if (thread_interrupted)
                    {
                        // The alarm time was changed, we don't play and go back to sleep
                        thread_interrupted = false;
                    }
                    else
                    {
                        StartPlaying();
                    }
                }
            }
            catch (ThreadAbortException)
            {
                Log.Debug("Alarm Plugin: Alarm main loop aborted", "");
            }
        }

        private void StartPlaying()
        {
            if (ServiceManager.PlayerEngine.CurrentState == PlayerEngineState.Playing)
            {
                return;
            }

            Log.Debug("Alarm Plugin: Start playing ", "");

            if (this.plugin.FadeDuration > 0) {
                ServiceManager.PlayerEngine.Volume = plugin.FadeStartVolume;
                new VolumeFade(plugin.FadeStartVolume, plugin.FadeEndVolume, plugin.FadeDuration);
            }
            // PlayerEngineCore.Play() only worked if we were paused in a track
            // TODO : Check if it now works OK.
            ServiceManager.PlayerEngine.Play();
            
            if(plugin.AlarmCommand != null && plugin.AlarmCommand.Trim() != "")
                Process.Start(plugin.AlarmCommand);
        }
        
        private TimeSpan TimeUntilAlarm()
        {
            DateTime now = DateTime.Now;
            DateTime alarmTime = new DateTime(now.Year, now.Month, now.Day, plugin.AlarmHour, plugin.AlarmMinute, 0);
            
            TimeSpan delay = alarmTime - now;
            if (delay < TimeSpan.Zero)
            {
                alarmTime = alarmTime.AddDays(1);
                delay = alarmTime - now;
            }
            Log.Debug("Time until alarm is " + delay.ToString(), "");
            return delay;
        }
    }
}
