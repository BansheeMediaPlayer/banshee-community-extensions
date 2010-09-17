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
using Mono.Addins;

using Banshee.Gui.Dialogs;

namespace Banshee.AlarmClock
{
    public class AlarmConfigDialog : BansheeDialog
    {
        private AlarmClockService service;
        private SpinButton spbHour;
        private SpinButton spbMinute;
        private CheckButton isEnabled;

        public AlarmConfigDialog (AlarmClockService service) : base (AddinManager.CurrentLocalizer.GetString ("Alarm Time"))
        {
            this.service = service;

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

            isEnabled = new CheckButton (AddinManager.CurrentLocalizer.GetString ("Enable Alarm"));

            HBox time_box = new HBox ();
            time_box.PackStart (new Label (AddinManager.CurrentLocalizer.GetString ("Set Time: ")));
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
            spbHour.Value = service.AlarmHour;
            spbMinute.Value = service.AlarmMinute;
            isEnabled.Active = service.AlarmEnabled;

            isEnabled.Toggled += new EventHandler (AlarmEnabled_Changed);
            spbHour.ValueChanged += new EventHandler (AlarmHour_Changed);
            spbMinute.ValueChanged += new EventHandler (AlarmMinute_Changed);
        }

        private void OnOKClicked (object o, EventArgs e)
        {
            // The alarm has to be reset to take into account the new alarm time
            service.ResetAlarm ();
            Destroy ();
        }

        private void AlarmEnabled_Changed (object source, System.EventArgs args)
        {
            service.AlarmEnabled = isEnabled.Active;
        }

        private void AlarmHour_Changed (object source, System.EventArgs args)
        {
            service.AlarmHour = (ushort) spbHour.Value;
        }

        private void AlarmMinute_Changed (object source, System.EventArgs args)
        {
            service.AlarmMinute = (ushort) spbMinute.Value;
        }
    }
}
