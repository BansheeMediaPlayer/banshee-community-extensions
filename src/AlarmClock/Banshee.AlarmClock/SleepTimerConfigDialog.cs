//
// SleepTimerConfigDialog.cs
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
    public class SleepTimerConfigDialog : BansheeDialog
    {
        AlarmClockService service;

        SpinButton sleepHour;
        SpinButton sleepMin;

        public SleepTimerConfigDialog (AlarmClockService service) : base (AddinManager.CurrentLocalizer.GetString ("Sleep Timer"))
        {
            this.service = service;

            WidthRequest = 250;
            HeightRequest = 150;

            BuildWidget ();
            ShowAll ();
        }

        private void BuildWidget ()
        {
            sleepHour = new SpinButton (0,23,1);
            sleepMin  = new SpinButton (0,59,1);

            sleepHour.Value = (int) service.GetSleepTimer () / 60 ;
            sleepMin.Value = service.GetSleepTimer () - (sleepHour.Value * 60);

            sleepHour.WidthChars = 2;
            sleepMin.WidthChars  = 2;

            Label prefix    = new Label (AddinManager.CurrentLocalizer.GetString ("Sleep Timer :"));
            Label separator = new Label (":");
            Label comment   = new Label (AddinManager.CurrentLocalizer.GetString ("<i>(set to 0:00 to disable)</i>"));
            comment.UseMarkup = true;

            Button OK = new Button (Gtk.Stock.Ok);
            OK.Clicked += new EventHandler (OnSleepTimerOK);

            HBox topbox     = new HBox (false, 10);

            topbox.PackStart (prefix);
            topbox.PackStart (sleepHour);
            topbox.PackStart (separator);
            topbox.PackStart (sleepMin);

            AddActionWidget (OK, 0);

            VBox.PackStart (topbox);
            VBox.PackStart (comment);
        }

        public void OnSleepTimerOK (object o, EventArgs a)
        {
            int timervalue = (int)sleepHour.Value * 60 + (int)sleepMin.Value;
            service.SetSleepTimer (timervalue);
            Destroy ();
        }
    }
}
