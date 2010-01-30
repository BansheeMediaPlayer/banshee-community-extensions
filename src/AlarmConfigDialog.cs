//
// AlarmConfigDialog.cs
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

namespace Banshee.AlarmClock
{
    public class AlarmConfigDialog : Dialog
    {
        private AlarmClockService plugin;
        private SpinButton spbHour;
        private SpinButton spbMinute;
        private CheckButton isEnabled;

        public AlarmConfigDialog (AlarmClockService plugin) : base ()
        {
            this.plugin = plugin;

            Title = "Alarm";
            WidthRequest = 250;
            HeightRequest = 150;

            BuildWidget ();
            ShowAll ();
        }

        private void BuildWidget ()
        {
            spbHour = new SpinButton (0, 23, 1);
            spbHour.WidthChars = 2;
            spbMinute = new SpinButton (0, 59, 1);
            spbMinute.WidthChars = 2;

            isEnabled = new CheckButton (Catalog.GetString ("Enable Alarm"));

            HBox time_box = new HBox ();
            time_box.PackStart (new Label (Catalog.GetString ("Set Time: ")));
            time_box.PackStart (spbHour);
            time_box.PackStart (new Label (" : "));
            time_box.PackStart (spbMinute);

            VBox time_box_outer = new VBox (false, 10);
            time_box_outer.PackStart (isEnabled);
            time_box_outer.PackStart (time_box);

            Button OK = new Button (Gtk.Stock.Ok);
            OK.Clicked += new EventHandler (OnOKClicked);

            AddActionWidget (OK, 0);
            VBox.PackStart (time_box_outer, true, false, 6);

            // Initialize with current values
            spbHour.Value = plugin.AlarmHour;
            spbMinute.Value = plugin.AlarmMinute;
            isEnabled.Active = plugin.AlarmEnabled;

            isEnabled.Toggled += new EventHandler (AlarmEnabled_Changed);
            spbHour.ValueChanged += new EventHandler (AlarmHour_Changed);
            spbMinute.ValueChanged += new EventHandler (AlarmMinute_Changed);
        }

        private void OnOKClicked (object o, EventArgs e)
        {
            // The alarm thread has to be re-initialized to take into account the new alarm time
            plugin.ReloadAlarm ();
            Destroy ();
        }

        private void AlarmEnabled_Changed (object source, System.EventArgs args)
        {
            plugin.AlarmEnabled = isEnabled.Active;
        }

        private void AlarmHour_Changed (object source, System.EventArgs args)
        {
            plugin.AlarmHour = (ushort) spbHour.Value;
        }

        private void AlarmMinute_Changed (object source, System.EventArgs args)
        {
            plugin.AlarmMinute = (ushort) spbMinute.Value;
        }
    }
}
