using System;
using System.Threading;

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
            try {
                while(true) {
                    int delay = (int)TimeUntilAlarm().TotalMilliseconds;
                    bool thread_interrupted = false;
                    
                    try {
                        Thread.Sleep(delay);
                    } catch(ThreadInterruptedException) {
                        Log.Debug("Alarm Plugin: sleep interrupted");
                        thread_interrupted = true;
                    }
                    
                    if (thread_interrupted) {
                        // The alarm time was changed, we don't play and go back to sleep
                        thread_interrupted = false;
                    } else {
                        StartPlaying();
                    }
                }
            } catch (ThreadAbortException) {
                Log.Debug("Alarm Plugin: Alarm main loop aborted");
            }
        }

        private void StartPlaying()
        {
            if (ServiceManager.PlayerEngine.CurrentState == PlayerState.Playing) {
                return;
            }

            Log.Debug("Alarm Plugin: Start playing ");

            if (this.plugin.FadeDuration > 0) {
                ServiceManager.PlayerEngine.Volume = plugin.FadeStartVolume;
                new VolumeFade(plugin.FadeStartVolume, plugin.FadeEndVolume, plugin.FadeDuration);
            }
            // PlayerEngine.Play() only works if we are paused in a track
            // PlayerEngine.TogglePlaying() starts the first track if we're not paused in a track
            ServiceManager.PlayerEngine.TogglePlaying();
            
            if (!String.IsNullOrEmpty (plugin.AlarmCommand)) {
                System.Diagnostics.Process.Start(plugin.AlarmCommand);
            }
        }
        
        private TimeSpan TimeUntilAlarm()
        {
            DateTime now = DateTime.Now;
            DateTime alarmTime = new DateTime(now.Year, now.Month, now.Day, plugin.AlarmHour, plugin.AlarmMinute, 0);
            
            TimeSpan delay = alarmTime - now;
            if (delay < TimeSpan.Zero) {
                alarmTime = alarmTime.AddDays(1);
                delay = alarmTime - now;
            }
            Log.Debug("Time until alarm is " + delay.ToString());
            return delay;
        }
    }
}
