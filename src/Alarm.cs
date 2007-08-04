using System;
using System.Threading;
using System.Diagnostics;

using Banshee.Base;
using Banshee.MediaEngine;

namespace Banshee.Plugins.Alarm
{
    public class AlarmThread
    {
        private AlarmPlugin plugin;

        public AlarmThread(AlarmPlugin plugin)
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
                    Thread.Sleep(delay);
                    
                    if (plugin.alarmTimeChanged)
                    {
                        // The alarm time was changed, we don't play and go back to sleep
                        plugin.alarmTimeChanged = false;
                    }
                    else
                    {
                        StartPlaying();
                    }
                }
            }
            catch (ThreadAbortException)
            {
                LogCore.Instance.PushDebug("Alarm Plugin: Alarm main loop aborted", "");
            }
        }

        private void StartPlaying()
        {
            if (PlayerEngineCore.CurrentState == PlayerEngineState.Playing)
            {
                return;
            }

            LogCore.Instance.PushDebug("Alarm Plugin: Start playing ", "");

            if (this.plugin.FadeDuration > 0) {
                PlayerEngineCore.Volume = plugin.FadeStartVolume;
                new VolumeFade(plugin.FadeStartVolume, plugin.FadeEndVolume, plugin.FadeDuration);
            }
            // PlayerEngineCore.Play() only works if we're paused in a track
            Globals.ActionManager["PlayPauseAction"].Activate();
            
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
            LogCore.Instance.PushDebug("Time until alarm is " + delay.ToString(), "");
            return delay;
        }
    }
}
