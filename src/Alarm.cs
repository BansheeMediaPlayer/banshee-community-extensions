//
// Alarm.cs
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
                bool thread_interrupted = false;
                while (true) {
                    int delay = (int)TimeUntilAlarm().TotalMilliseconds;
                    
                    try {
                        Thread.Sleep(delay);
                    } catch(ThreadInterruptedException) {
                        Log.Debug("Alarm Plugin: sleep interrupted");
                        thread_interrupted = true;
                    }
                    
                    if (thread_interrupted) {
                        // The alarm time was changed, we don't play and go back to sleep
                        thread_interrupted = false;
                    } else if (plugin.AlarmEnabled) {
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
