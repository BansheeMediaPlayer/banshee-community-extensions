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
using Banshee.MediaEngine;
using Banshee.ServiceStack;

namespace Banshee.AlarmClock
{
    public class AlarmThread
    {
        AlarmClockService service;

        public AlarmThread (AlarmClockService service)
        {
            this.service = service;
        }

        public void MainLoop ()
        {
            while (!service.Disposing) {
                uint delay = (uint)TimeUntilAlarm ().TotalMilliseconds;

                uint timeout_id = GLib.Timeout.Add (delay, StartPlaying);
                service.AlarmResetEvent.WaitOne ();
                GLib.Source.Remove (timeout_id);
            }
        }

        private bool StartPlaying ()
        {
            service.AlarmResetEvent.Set ();

            if (!service.AlarmEnabled) {
                return false;
            }
            if (ServiceManager.PlayerEngine.CurrentState == PlayerState.Playing) {
                return false;
            }

            Log.Debug ("Alarm Plugin: Start playing ");

            if (this.service.FadeDuration > 0) {
                ServiceManager.PlayerEngine.Volume = service.FadeStartVolume;
                new VolumeFade (service.FadeStartVolume, service.FadeEndVolume, service.FadeDuration);
            }
            // PlayerEngine.Play () only works if we are paused in a track
            // PlayerEngine.TogglePlaying () starts the first track if we're not paused in a track
            ServiceManager.PlayerEngine.TogglePlaying ();

            if (!String.IsNullOrEmpty (service.AlarmCommand)) {
                System.Diagnostics.Process.Start (service.AlarmCommand);
            }

            return false;
        }

        private TimeSpan TimeUntilAlarm ()
        {
            DateTime now = DateTime.Now;
            DateTime alarmTime = new DateTime (now.Year, now.Month, now.Day, service.AlarmHour, service.AlarmMinute, 0);

            TimeSpan delay = alarmTime - now;
            if (delay < TimeSpan.Zero) {
                alarmTime = alarmTime.AddDays (1);
                delay = alarmTime - now;
            }
            Log.DebugFormat ("Time until alarm is {0}", delay);
            return delay;
        }
    }
}
