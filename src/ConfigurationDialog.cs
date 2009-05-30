//
// ConfigurationDialog.cs
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
using Gtk;
using Mono.Unix;

using Banshee.Base;

namespace Banshee.AlarmClock 
{
    public class ConfigurationDialog : Gtk.Dialog
    {
        private AlarmClockService plugin;
        private Entry command_entry;
        private VScale fade_start;
        private VScale fade_end;
        private SpinButton fade_duration;
        int volumeSliderHeight = 120;

        public ConfigurationDialog(AlarmClockService plugin) : base()
        {
            this.plugin = plugin;

            Title = Catalog.GetString ("Alarm Clock configuration");
            HasSeparator = false;
            BorderWidth = 5;
            
            fade_start = new VScale (0, 100, 1);
            fade_start.Inverted = true;
            fade_start.HeightRequest = volumeSliderHeight;
            fade_end = new VScale (0, 100, 1);
            fade_end.Inverted = true;
            fade_end.HeightRequest = volumeSliderHeight;
            fade_duration = new SpinButton (0, 65535, 1);
            fade_duration.WidthChars = 6;

            VBox fade_big_box = new VBox ();

            VBox fade_start_box = new VBox ();
            fade_start_box.PackEnd (new Label (Catalog.GetString ("Start")));
            fade_start_box.PackStart (fade_start, false, false, 3);

            VBox fade_end_box = new VBox ();
            fade_end_box.PackEnd (new Label (Catalog.GetString ("End")));
            fade_end_box.PackStart (fade_end, false, false, 3);

            HBox fade_box_group = new HBox ();
            fade_box_group.PackStart (fade_start_box);
            fade_box_group.PackStart (fade_end_box);

            Label volume_label = new Label (Catalog.GetString ("<b>Volume</b>"));
            volume_label.UseMarkup = true;
            fade_big_box.PackStart (volume_label, false, true, 3);
            fade_big_box.PackStart (fade_box_group);
            Label duration_label = new Label (Catalog.GetString ("Duration:"));
            Label duration_seconds_label = new Label (Catalog.GetString (" <i>(seconds)</i>"));
            duration_label.UseMarkup = true;
            duration_seconds_label.UseMarkup = true;
            HBox duration_box = new HBox ();
            duration_box.PackStart (duration_label, false, false, 3);
            duration_box.PackStart (fade_duration, false, false, 3);
            duration_box.PackStart (duration_seconds_label, false, true, 3);
            fade_big_box.PackStart (duration_box);

            Frame alarm_fade_frame = new Frame (Catalog.GetString ("Fade-In Adjustment"));
            alarm_fade_frame.Add (fade_big_box);
            alarm_fade_frame.ShowAll ();

            HBox command_box = new HBox ();
            command_box.PackStart (new Label (Catalog.GetString ("Command:")), false, false, 3);
            command_entry = new Entry ();
            command_box.PackStart (command_entry, true, true, 3);
            
            Frame alarm_misc_frame = new Frame (Catalog.GetString ("Command To Execute:"));
            alarm_misc_frame.Add (command_box);
            alarm_misc_frame.ShowAll ();

            VBox.PackStart (alarm_fade_frame, false, false, 3);
            VBox.PackStart (alarm_misc_frame, false, false, 3);
            
            AddButton (Stock.Close, ResponseType.Close);
            
            // initialize values
            command_entry.Text = plugin.AlarmCommand;
            fade_start.Value = plugin.FadeStartVolume;
            fade_end.Value = plugin.FadeEndVolume;
            fade_duration.Value = plugin.FadeDuration;
            
            // attach change handlers
            command_entry.Changed += new EventHandler (AlarmCommand_Changed);
            fade_start.ValueChanged += new EventHandler (FadeStartVolume_Changed);
            fade_end.ValueChanged += new EventHandler (FadeEndVolume_Changed);
            fade_duration.ValueChanged += new EventHandler (FadeDuration_Changed);
        }

        private void AlarmCommand_Changed (object source, System.EventArgs args)
        {
            plugin.AlarmCommand = command_entry.Text;
        }

        private void FadeStartVolume_Changed (object source, System.EventArgs args)
        {
            plugin.FadeStartVolume = (ushort) fade_start.Value;
        }

        private void FadeEndVolume_Changed (object source, System.EventArgs args)
        {
            plugin.FadeEndVolume = (ushort) fade_end.Value;
        }

        private void FadeDuration_Changed (object source, System.EventArgs args)
        {
            plugin.FadeDuration = (ushort) fade_duration.Value;
        }
    }
}
