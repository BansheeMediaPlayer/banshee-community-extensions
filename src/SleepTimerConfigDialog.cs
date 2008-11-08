using System;
using Gtk;
using Mono.Unix;

using Banshee.Base;

namespace Banshee.AlarmClock 
{
    public class SleepTimerConfigDialog : Dialog
    {
        AlarmClockService plugin;
        
        private SpinButton sleepHour;
        private SpinButton sleepMin;        
        
        public SleepTimerConfigDialog (AlarmClockService plugin) : base ()
        {
            this.plugin = plugin;
            
            Title = Catalog.GetString ("Sleep Timer");
            WidthRequest = 250;
            HeightRequest = 150;
            VBox.Spacing = 10;
            
            BuildWidget ();
            ShowAll ();
        }
        
        private void BuildWidget ()
        {
            sleepHour = new SpinButton (0,23,1);
            sleepMin  = new SpinButton (0,59,1);
            
            sleepHour.Value = (int) plugin.GetSleepTimer () / 60 ;
            sleepMin.Value = plugin.GetSleepTimer () - (sleepHour.Value * 60);

            sleepHour.WidthChars = 2;
            sleepMin.WidthChars  = 2;

            Label prefix    = new Label (Catalog.GetString ("Sleep Timer :"));
            Label separator = new Label (":");
            Label comment   = new Label (Catalog.GetString ("<i>(set to 0:00 to disable)</i>"));
            comment.UseMarkup = true;

            Button OK = new Button (Gtk.Stock.Ok);
            OK.Clicked += new EventHandler (OnSleepTimerOK);

            HBox topbox     = new HBox (false, 10);

            topbox.PackStart (prefix);
            topbox.PackStart (sleepHour);
            topbox.PackStart (separator);
            topbox.PackStart (sleepMin);

            this.AddActionWidget (OK, 0);

            this.VBox.PackStart (topbox);
            this.VBox.PackStart (comment);
        }
        
        public void OnSleepTimerOK (object o, EventArgs a)
        {
            int timervalue = (int)sleepHour.Value * 60 + (int)sleepMin.Value;
            plugin.SetSleepTimer (timervalue);
            this.Destroy ();
        }
    }
}
