using System;
using Gtk;

using Banshee.Base;

namespace Banshee.Plugins.Alarm 
{
    public class SleepTimerConfigDialog : Dialog
    {
        AlarmPlugin plugin;
        
        public SleepTimerConfigDialog(AlarmPlugin plugin) : base()
        {
            this.plugin = plugin;
            
            Title = "Sleep Timer";
            IconThemeUtils.SetWindowIcon(this);
            WidthRequest = 250;
            HeightRequest = 150;
            VBox.Spacing = 10;
            DeleteEvent += new DeleteEventHandler(OnSleepTimerDialogDestroy);
            
            BuildWidget();
            ShowAll();
        }
        
        private void BuildWidget()
        {
            
            plugin.sleepHour.Value = (int) plugin.timervalue / 60 ;
            plugin.sleepMin.Value = plugin.timervalue - (plugin.sleepHour.Value * 60);

            plugin.sleepHour.WidthChars = 2;
            plugin.sleepMin.WidthChars  = 2;

            Label prefix    = new Label("Sleep Timer :");
            Label separator = new Label(":");
            Label comment   = new Label("<i>(set to 0:00 to disable)</i>");
            comment.UseMarkup = true;

            Button OK = new Button(Gtk.Stock.Ok);
            OK.Clicked += new EventHandler(OnSleepTimerOK);

            HBox topbox     = new HBox(false, 10);

            topbox.PackStart(prefix);
            topbox.PackStart(plugin.sleepHour);
            topbox.PackStart(separator);
            topbox.PackStart(plugin.sleepMin);

            this.AddActionWidget(OK, 0);

            this.VBox.PackStart(topbox);
            this.VBox.PackStart(comment);
        }
        
        private void OnSleepTimerDialogDestroy(object o, DeleteEventArgs a){
            plugin.SetSleepTimer();
        }

        public void OnSleepTimerOK(object o, EventArgs a)
        {
            this.Destroy();
            plugin.SetSleepTimer();
        }
    }
}
