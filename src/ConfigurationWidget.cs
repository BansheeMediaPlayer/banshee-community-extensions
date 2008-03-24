using System;
using Gtk;
using GConf;
using Mono.Unix;

using Banshee.Base;

namespace Banshee.AlarmClock 
{
    public class ConfigurationWidget : VBox
    {
        private AlarmClockService plugin;
        private Entry command_entry;
        private VScale fade_start;
        private VScale fade_end;
        private SpinButton fade_duration;
        int volumeSliderHeight = 120;

        public ConfigurationWidget(AlarmClockService plugin) : base()
        {
            this.plugin = plugin;
            BuildWidget();
        }

        private void BuildWidget()
        {
            fade_start = new VScale(0, 100, 1);
            fade_start.Inverted = true;
            fade_start.HeightRequest = volumeSliderHeight;
            fade_end = new VScale(0, 100, 1);
            fade_end.Inverted = true;
            fade_end.HeightRequest = volumeSliderHeight;
            fade_duration = new SpinButton(0, 65535, 1);
            fade_duration.WidthChars = 6;

            VBox fade_big_box = new VBox();

            VBox fade_start_box = new VBox();
            fade_start_box.PackEnd(new Label(Catalog.GetString("Start")));
            fade_start_box.PackStart(fade_start, false, false, 3);

            VBox fade_end_box = new VBox();
            fade_end_box.PackEnd(new Label(Catalog.GetString("End")));
            fade_end_box.PackStart(fade_end, false, false, 3);

            HBox fade_box_group = new HBox();
            fade_box_group.PackStart(fade_start_box);
            fade_box_group.PackStart(fade_end_box);

            Label volume_label = new Label(Catalog.GetString("<b>Volume</b>"));
            volume_label.UseMarkup = true;
            fade_big_box.PackStart(volume_label, false, true, 3);
            fade_big_box.PackStart(fade_box_group);
            Label duration_label = new Label("Duration:");
            Label duration_seconds_label = new Label(Catalog.GetString(" <i>(seconds)</i>"));
            duration_label.UseMarkup = true;
            duration_seconds_label.UseMarkup = true;
            HBox duration_box = new HBox();
            duration_box.PackStart(duration_label, false, false, 3);
            duration_box.PackStart(fade_duration, false, false, 3);
            duration_box.PackStart(duration_seconds_label, false, true, 3);
            fade_big_box.PackStart(duration_box);

            Frame alarm_fade_frame = new Frame(Catalog.GetString("Fade-In Adjustment"));
            alarm_fade_frame.Add(fade_big_box);

            HBox command_box = new HBox();
            command_box.PackStart(new Label(Catalog.GetString("Command:")), false, false, 3);
            command_entry = new Entry();
            command_box.PackStart(command_entry, true, true, 3);
            
            Frame alarm_misc_frame = new Frame(Catalog.GetString("Command To Execute:"));
            alarm_misc_frame.Add(command_box);

            VBox alarm_configs = new VBox();
            alarm_configs.PackStart(alarm_fade_frame, false, false, 3);
            alarm_configs.PackStart(alarm_misc_frame, false, false, 3);
            
            VBox alarm_frame = new VBox();
            Label alarm_frame_label = new Label(Catalog.GetString("<big><b>Alarm Configuration</b></big>"));
            alarm_frame_label.UseMarkup = true;
            alarm_frame_label.Xalign = 0.0f;
            alarm_frame.PackStart(alarm_frame_label, false, false, 3);
            alarm_frame.PackStart(alarm_configs);
            
            /*VBox sleep_frame = new VBox();
            Label sleep_frame_label = new Label("<big><b>Sleep Timer Configuration</b></big>");
            sleep_frame_label.UseMarkup = true;
            sleep_frame_label.Xalign = 0.0f;
            
            sleep_frame.PackStart(sleep_frame_label, false, false, 3);
            Label no_options_avail = new Label ("No configuration options available.");
            no_options_avail.Xalign = 0.0f;
            sleep_frame.PackStart(no_options_avail, true, true, 3);*/
            
            PackStart(alarm_frame, false, false, 3);
            //PackStart(sleep_frame, false, false, 3);

            // initialize values
            command_entry.Text = plugin.AlarmCommand;
            fade_start.Value = plugin.FadeStartVolume;
            fade_end.Value = plugin.FadeEndVolume;
            fade_duration.Value = plugin.FadeDuration;
            
            // attach change handlers
            command_entry.Changed += new EventHandler(AlarmCommand_Changed);
            fade_start.ValueChanged += new EventHandler(FadeStartVolume_Changed);
            fade_end.ValueChanged += new EventHandler(FadeEndVolume_Changed);
            fade_duration.ValueChanged += new EventHandler(FadeDuration_Changed);

            ShowAll();
        }

        private void AlarmCommand_Changed(object source, System.EventArgs args) {
            plugin.AlarmCommand = command_entry.Text;
        }

        private void FadeStartVolume_Changed(object source, System.EventArgs args) {
            plugin.FadeStartVolume = (ushort) fade_start.Value;
        }

        private void FadeEndVolume_Changed(object source, System.EventArgs args) {
            plugin.FadeEndVolume = (ushort) fade_end.Value;
        }

        private void FadeDuration_Changed(object source, System.EventArgs args) {
            plugin.FadeDuration = (ushort) fade_duration.Value;
        }

    }
}
