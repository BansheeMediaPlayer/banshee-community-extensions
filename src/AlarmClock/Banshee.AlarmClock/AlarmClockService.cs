//
// AlarmClockService.cs
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
using Gtk;
using Mono.Addins;

using Hyena;
using Banshee.MediaEngine;
using Banshee.ServiceStack;
using Banshee.Gui;

namespace Banshee.AlarmClock
{
    public class AlarmClockService : IExtensionService, IDisposable
    {
        static AlarmClockService alarm_service;
        Thread alarm_thread;
        ActionGroup actions;
        InterfaceActionService action_service;
        uint ui_manager_id;
        uint sleep_timer_id;

        public AlarmClockService ()
        {}

        void IExtensionService.Initialize ()
        {
            Log.Debug("Initializing Alarm Plugin");

            AlarmClockService.alarm_service = this;
            ThreadStart alarmThreadStart = new ThreadStart (AlarmClockService.DoWait);
            alarm_thread = new Thread (alarmThreadStart);
            alarm_thread.Start ();

            action_service = ServiceManager.Get<InterfaceActionService> ();

            actions = new ActionGroup ("AlarmClock");

            actions.Add (new ActionEntry [] {
                new ActionEntry ("AlarmClockAction", null,
                    AddinManager.CurrentLocalizer.GetString ("Alarm Clock"), null,
                    null, null),

                new ActionEntry ("SetSleepTimerAction", null,
                    AddinManager.CurrentLocalizer.GetString ("Sleep Timer..."), null,
                    AddinManager.CurrentLocalizer.GetString ("Set the sleep timer value"), OnSetSleepTimer),

                new ActionEntry ("SetAlarmAction", null,
                    AddinManager.CurrentLocalizer.GetString ("Alarm..."), null,
                    AddinManager.CurrentLocalizer.GetString ("Set the alarm time"), OnSetAlarm),

                new ActionEntry ("AlarmClockConfigureAction", Stock.Properties,
                    AddinManager.CurrentLocalizer.GetString ("_Configure..."), null,
                    AddinManager.CurrentLocalizer.GetString ("Configure the Alarm Clock plugin"), OnConfigure)
            });

            action_service.UIManager.InsertActionGroup (actions, 0);
            ui_manager_id = action_service.UIManager.AddUiFromResource ("AlarmMenu.xml");
        }

        private bool disposing = false;
        public bool Disposing {
            get { return disposing; }
        }

        public void Dispose ()
        {
            disposing = true;

            Log.Debug ("Disposing Alarm Plugin");
            action_service.UIManager.RemoveUi (ui_manager_id);
            action_service.UIManager.RemoveActionGroup (actions);
            actions = null;

            if (sleep_timer_id > 0) {
                GLib.Source.Remove (sleep_timer_id);
                Log.Debug ("Disabling old sleep timer");
            }

            ResetAlarm ();
            alarm_thread.Join ();
            alarm_thread = null;
        }

        public static void DoWait ()
        {
            Log.Debug ("Alarm thread started");
            AlarmThread theAlarm = new AlarmThread (AlarmClockService.alarm_service);
            theAlarm.MainLoop ();
        }

        protected void OnSetAlarm (object o, EventArgs a)
        {
            new AlarmConfigDialog (this);
        }

        private AutoResetEvent alarm_reset_event = new AutoResetEvent (false);
        public AutoResetEvent AlarmResetEvent {
            get { return alarm_reset_event; }
        }

        public void ResetAlarm ()
        {
            // The alarm has to be reset to take into account the new alarm time
            AlarmResetEvent.Set ();
        }

        protected void OnSetSleepTimer (object o, EventArgs a)
        {
            if (sleep_timer_id > 0) {
                GLib.Source.Remove(sleep_timer_id);
                Log.Debug("Disabling old sleep timer");
            }
            var dialog = new SleepTimerConfigDialog (this);
            dialog.Run ();
            dialog.Destroy ();
        }

        public void SetSleepTimer(int timervalue)
        {
            if (timervalue > 0) {
                Log.DebugFormat ("Sleep Timer set to {0}", timervalue);
                sleep_timer_id = GLib.Timeout.Add ((uint) timervalue * 60 * 1000, onSleepTimerActivate);
            }
        }

        public bool onSleepTimerActivate ()
        {
            var player_engine = ServiceManager.PlayerEngine;
            if (player_engine.CurrentState == PlayerState.Playing) {
                uint seconds_to_end = (ushort) ((player_engine.Length - player_engine.Position) / 1000);
                Log.Debug ("Sleep Timer has gone off.  Fading out till end of song.");
                new VolumeFade (ServiceManager.PlayerEngine.Volume, 0, seconds_to_end);
                GLib.Timeout.Add (seconds_to_end * 1000,
                    delegate {
                        Log.Debug ("Sleep Timer: Pausing.");
                        ServiceManager.PlayerEngine.Pause ();
                        return false;
                    }
                );
            } else {
                Log.Debug ("Sleep Timer has gone off, but we're not playing.  Refusing to pause.");
            }
            return false;
        }

        private void OnConfigure (object o, EventArgs args)
        {
            ConfigurationDialog dialog = new ConfigurationDialog (this);
            dialog.Run ();
            dialog.Destroy ();
        }

        #region Configuration properties
        internal bool AlarmEnabled
        {
            get { return ConfigurationSchema.IsEnabled.Get (); }
            set { ConfigurationSchema.IsEnabled.Set (value); }
        }

        internal ushort AlarmHour
        {
            get { return (ushort)ConfigurationSchema.AlarmHour.Get (); }
            set { ConfigurationSchema.AlarmHour.Set (value); }
        }

        internal ushort AlarmMinute
        {
            get { return (ushort)ConfigurationSchema.AlarmMinute.Get (); }
            set { ConfigurationSchema.AlarmMinute.Set (value); }
        }

        internal string AlarmCommand
        {
            get { return ConfigurationSchema.AlarmCommand.Get (); }
            set { ConfigurationSchema.AlarmCommand.Set (value); }
        }

        internal int SleepTimerDuration
        {
            get { return ConfigurationSchema.SleepTimerDuration.Get (); }
            set { ConfigurationSchema.SleepTimerDuration.Set (value); }
        }

        internal ushort FadeStartVolume
        {
            get { return (ushort)ConfigurationSchema.FadeStartVolume.Get (); }
            set { ConfigurationSchema.FadeStartVolume.Set (value); }
        }

        internal ushort FadeEndVolume
        {
            get { return (ushort)ConfigurationSchema.FadeEndVolume.Get (); }
            set { ConfigurationSchema.FadeEndVolume.Set (value); }
        }

        internal ushort FadeDuration
        {
            get { return (ushort)ConfigurationSchema.FadeDuration.Get (); }
            set { ConfigurationSchema.FadeDuration.Set (value); }
        }
        #endregion

        string IService.ServiceName {
            get { return "AlarmClockService"; }
        }
    }
}
