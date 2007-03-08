using System;
using System.Threading;
using System.Diagnostics;

using Banshee.Base;
using Banshee.MediaEngine;

namespace Banshee.Plugins.Alarm
{
    public class AlarmThread
    {
        private bool isInAlarmMinute = false; // what a dirty hack :(
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
                    Thread.Sleep(10000);
                    DateTime now = DateTime.Now;

                    if (now.Hour == plugin.AlarmHour && now.Minute == plugin.AlarmMinute && plugin.AlarmEnabled)
                    {
                        this.StartPlaying();
                        isInAlarmMinute = true;
                    }else{
                        isInAlarmMinute = false;
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
            if (PlayerEngineCore.CurrentState == PlayerEngineState.Playing || isInAlarmMinute)
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
    }
}
