using System;
using Gtk;
using GConf;
using Mono.Unix;

using Banshee.Base;
using Banshee.Widgets;

namespace Banshee.Plugins.Alarm 
{
    public class AlarmConfigDialog : Dialog
    {
        private AlarmPlugin plugin;
        private SpinButton spbHour;
        private SpinButton spbMinute;
        private CheckButton isEnabled;
        
        public AlarmConfigDialog(AlarmPlugin plugin) : base()
        {
            this.plugin = plugin;
            
            Title = "Alarm";
            WidthRequest = 250;
            HeightRequest = 150;
            IconThemeUtils.SetWindowIcon(this);

            BuildWidget();
            ShowAll();
        }

        private void BuildWidget()
        {
            spbHour = new SpinButton(0, 23, 1);
            spbHour.WidthChars = 2;
            spbMinute = new SpinButton(0, 59, 1);
            spbMinute.WidthChars = 2;

            isEnabled = new CheckButton(Catalog.GetString("Enable Alarm"));

            HBox time_box = new HBox();
            time_box.PackStart(new Label(Catalog.GetString("Set Time: ")));
            time_box.PackStart(spbHour);
            time_box.PackStart(new Label(" : "));
            time_box.PackStart(spbMinute);

            VBox time_box_outer = new VBox(false, 10);
            time_box_outer.PackStart(isEnabled);
            time_box_outer.PackStart(time_box);

            Button OK = new Button(Gtk.Stock.Ok);
            OK.Clicked += new EventHandler(OnOKClicked);

            this.AddActionWidget(OK, 0);
            this.VBox.PackStart(time_box_outer, true, false, 6);

            #region Initialize with current values
            spbHour.Value = plugin.AlarmHour;
            spbMinute.Value = plugin.AlarmMinute;
            isEnabled.Active = plugin.AlarmEnabled;
            #endregion

            isEnabled.Toggled += new EventHandler(AlarmEnabled_Changed);
            spbHour.ValueChanged += new EventHandler(AlarmHour_Changed);
            spbMinute.ValueChanged += new EventHandler(AlarmMinute_Changed);
        }

        private void OnOKClicked(object o, EventArgs e) {
            this.Destroy();
        }

        private void AlarmEnabled_Changed(object source, System.EventArgs args) {
            plugin.AlarmEnabled = isEnabled.Active;
        }

        private void AlarmHour_Changed(object source, System.EventArgs args) {
            plugin.AlarmHour = (ushort) spbHour.Value;
        }

        private void AlarmMinute_Changed(object source, System.EventArgs args) {
            plugin.AlarmMinute = (ushort) spbMinute.Value;
        }
    }
}
