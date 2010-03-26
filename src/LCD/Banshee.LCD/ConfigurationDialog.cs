//
// ConfigurationDialog.cs
//
// Authors:
//   André Gaul
//
// Copyright (C) 2010 André Gaul
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

namespace Banshee.LCD
{


    public class ConfigurationDialog : Gtk.Dialog
    {
        private LCDService plugin;
        private Entry host_entry;
        private SpinButton port_spin;

        public ConfigurationDialog (LCDService plugin)
        {
            this.plugin = plugin;
            Title = AddinManager.CurrentLocalizer.GetString ("LCD configuration");
            BorderWidth = 5;
            HasSeparator = false;
            Resizable = false;

            VBox lcdproc_box = new VBox ();

            HBox host_box = new HBox ();
            host_box.PackStart (new Label (AddinManager.CurrentLocalizer.GetString ("Hostname:")), false, false, 3);
            host_entry = new Entry ();
            host_box.PackStart (host_entry, true, true, 3);
            host_entry.Text = this.plugin.Host;
            host_entry.Changed += new EventHandler (Host_Changed);

            HBox port_box = new HBox ();
            port_box.PackStart (new Label (AddinManager.CurrentLocalizer.GetString ("Port:")), false, false, 3);
            port_spin = new SpinButton (1, 65535, 1);
            port_box.PackStart (port_spin, true, true, 3);
            port_spin.Value = this.plugin.Port;
            port_spin.Changed += new EventHandler (Port_Changed);

            Frame lcdproc_frame = new Frame (AddinManager.CurrentLocalizer.GetString ("LCDProc Daemon:"));
            lcdproc_box.PackStart (host_box);
            lcdproc_box.PackStart (port_box);
            lcdproc_frame.Add (lcdproc_box);
            lcdproc_frame.ShowAll ();

            VBox.PackStart (lcdproc_frame, false, false, 3);
            AddButton (Stock.Close, ResponseType.Close);
        }

        private void Host_Changed (object source, System.EventArgs args)
        {
            plugin.Host = host_entry.Text;
        }

        private void Port_Changed (object source, System.EventArgs args)
        {
            plugin.Port = (ushort)port_spin.Value;
        }
    }
}
