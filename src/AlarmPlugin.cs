using System;
using System.Threading;
using Mono.Unix;
using Gtk;
 
using Banshee.Base;
using Banshee.MediaEngine;

public static class PluginModuleEntry
{
    public static Type [] GetTypes()
    {
        return new Type [] {
            typeof(Banshee.Plugins.Alarm.AlarmPlugin)
        };
    }
}

namespace Banshee.Plugins.Alarm
{
    public class AlarmPlugin : Banshee.Plugins.Plugin
    {
        private ActionGroup actions;
        private uint ui_manager_id;
        
        protected override string ConfigurationName { get { return "Alarm"; } }
        public override string DisplayName { get { return "Alarm & Sleep Timer"; } }

        public override string Description {
            get {
                return Catalog.GetString(
                    "Gives Banshee alarm clock type functions.  Provides an alarm " +
                    "which can start playback at a predefined time, " +
                    "and allows you to set a sleep timer to pause playback after a set delay."
                );
            }
        }

        public override string [] Authors {
            get {
                return new string [] {
                    "Bertrand Lorentz\n" +
                    "Patrick van Staveren"
                };
            }
        }

        // --------------------------------------------------------------- //

        private Thread alarmThread;
        private static AlarmPlugin thePlugin;
        public SpinButton sleepHour = new SpinButton(0,23,1);
        public SpinButton sleepMin  = new SpinButton(0,59,1);
        public Window alarmDialog;
        public int timervalue;
        uint sleep_timer_id;

        protected override void PluginInitialize()
        {
            LogCore.Instance.PushDebug("Initializing Alarm Plugin", "");

            AlarmPlugin.thePlugin = this;
            ThreadStart alarmThreadStart = new ThreadStart(AlarmPlugin.DoWait);
            alarmThread = new Thread(alarmThreadStart);
            alarmThread.Start();
        }

        protected override void InterfaceInitialize()
        {
            actions = new ActionGroup("Alarm");
            
            actions.Add(new ActionEntry [] {
                new ActionEntry("SetSleepTimerAction", null,
                    Catalog.GetString("Sleep Timer..."), null,
                    Catalog.GetString("Set the sleep timer value"), OnSetSleepTimer),
                
                new ActionEntry("SetAlarmAction", null,
                    Catalog.GetString("Alarm..."), null,
                    Catalog.GetString("Set the alarm time"), OnSetAlarm)
            });
            
            Globals.ActionManager.UI.InsertActionGroup(actions, 3);
            ui_manager_id = Globals.ActionManager.UI.AddUiFromResource("AlarmMenu.xml");
        }

        protected override void PluginDispose()
        {
            LogCore.Instance.PushDebug("Disposing Alarm Plugin", "");
            Globals.ActionManager.UI.RemoveUi(ui_manager_id);
            Globals.ActionManager.UI.RemoveActionGroup(actions);
            actions = null;
            
            if(sleep_timer_id > 0){
                GLib.Source.Remove(sleep_timer_id);
                LogCore.Instance.PushDebug("Disabling old sleep timer", "");
            }
            alarmThread.Abort();
        }

        public override Gtk.Widget GetConfigurationWidget()
        {
            return new ConfigurationWidget(this);
        }
        
        public static void DoWait()
        {
            LogCore.Instance.PushDebug("Alarm thread started", "");
            AlarmThread theAlarm = new AlarmThread(AlarmPlugin.thePlugin);
            theAlarm.MainLoop();
        }

        protected void OnSetAlarm(object o, EventArgs a)
        {
            new AlarmConfigDialog(this);
        }

        protected void OnSetSleepTimer(object o, EventArgs a)
        {
            if(sleep_timer_id > 0){
                GLib.Source.Remove(sleep_timer_id);
                LogCore.Instance.PushDebug("Disabling old sleep timer", "");
            }
            new SleepTimerConfigDialog(this);
        }
           
        public void SetSleepTimer()
        {
            timervalue = sleepHour.ValueAsInt * 60 + sleepMin.ValueAsInt;
            if(timervalue != 0) {
                Console.WriteLine("Sleep Timer set to {0}", timervalue);
                sleep_timer_id = GLib.Timeout.Add((uint) timervalue * 60 * 1000, onSleepTimerActivate);
            }
        }

        public bool onSleepTimerActivate()
        {
            if(PlayerEngineCore.CurrentState == PlayerEngineState.Playing){
                LogCore.Instance.PushDebug("Sleep Timer has gone off.  Fading out till end of song.", "");
                new VolumeFade(PlayerEngineCore.Volume, 0,
                        (ushort) (PlayerEngineCore.Length - PlayerEngineCore.Position));
                GLib.Timeout.Add((PlayerEngineCore.Length - PlayerEngineCore.Position) * 1000, delegate{
                    LogCore.Instance.PushDebug("Sleep Timer: Pausing.", "");
                    PlayerEngineCore.Pause();
                    return false;
                    }
                );
                
            }else{
                LogCore.Instance.PushDebug("Sleep Timer has gone off, but we're not playing.  Refusing to pause.", "");
            }
            return(false);
        }
        
        #region Configuration properties
        internal bool AlarmEnabled
        {
            get {
                try {
                    return GConfSchemas.IsEnabled.Get();
                } catch {
                    return false;
                }
            }

            set {
                GConfSchemas.IsEnabled.Set(value);
            }
        }

        internal ushort AlarmHour
        {
            get {
                try {
                    return (ushort)GConfSchemas.AlarmHour.Get();
                } catch {
                    return 0;
                }
            }

            set {
                GConfSchemas.AlarmHour.Set(value);
            }
        }

        internal ushort AlarmMinute
        {
            get {
                try {
                    return (ushort)GConfSchemas.AlarmMinute.Get();
                } catch {
                    return 0;
                }
            }

            set {
                GConfSchemas.AlarmMinute.Set(value);
            }
        }

        internal string AlarmCommand
        {
            get {
                try {
                    return GConfSchemas.AlarmCommand.Get();
                } catch {
                    return null;
                }
            }

            set {
                GConfSchemas.AlarmCommand.Set(value);
            }
        }

        internal ushort FadeStartVolume
        {
            get {
                try {
                    return (ushort)GConfSchemas.FadeStartVolume.Get();
                } catch {
                    return 0;
                }
            }

            set {
                GConfSchemas.FadeStartVolume.Set(value);
            }
        }

        internal ushort FadeEndVolume
        {
            get {
                try {
                    return (ushort)GConfSchemas.FadeEndVolume.Get();
                } catch {
                    return 100;
            }
        }

            set {
                GConfSchemas.FadeEndVolume.Set(value);
            }
        }

        internal ushort FadeDuration
        {
            get {
                try {
                    return (ushort)GConfSchemas.FadeDuration.Get();
                } catch {
                    return 0;
                }
            }

            set {
                GConfSchemas.FadeDuration.Set(value);
            }
        }
        #endregion
    }
}
