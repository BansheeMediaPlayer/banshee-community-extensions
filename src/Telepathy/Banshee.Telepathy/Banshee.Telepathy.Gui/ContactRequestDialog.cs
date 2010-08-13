//
// ContactRequestDialog.cs
//
// Authors:
//   Neil Loknath <neil.loknath@gmail.com>
//
// Copyright (C) 2009 Neil Loknath
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
using Mono.Addins;
using Gtk;

namespace Banshee.Telepathy.Gui
{
    public class ContactRequestDialog : Gtk.Dialog
    {
        private AccelGroup accel_group;
        private Label message;

        public ContactRequestDialog (string contact_name) : base ()
        {
            Title = AddinManager.CurrentLocalizer.GetString ("Contact Request");
            HasSeparator = false;
            BorderWidth = 5;

            IconName = "gtk-dialog-authentication";

            accel_group = new AccelGroup ();
            AddAccelGroup (accel_group);

            HBox hbox = new HBox (false, 12);
            VBox vbox = new VBox (false, 0);
            hbox.BorderWidth = 5;
            vbox.Spacing = 5;
            hbox.Show ();
            vbox.Show ();

            Image image = new Image ();
            image.Yalign = 0.0f;
            image.IconName = "gtk-dialog-authentication";
            image.IconSize = (int)IconSize.Dialog;
            image.Show ();

            hbox.PackStart (image, false, false, 0);
            hbox.PackStart (vbox, true, true, 0);

            message = new Label (String.Format (AddinManager.CurrentLocalizer.GetString ("{0} would like to browse your music library."), contact_name));
            message.Xalign = 0.0f;
            message.Show ();

            vbox.PackStart (message, false, false, 0);

            VBox.PackStart (hbox, true, true, 0);
            VBox.Remove (ActionArea);
            VBox.Spacing = 10;

            HBox bottom_box = new HBox ();
            bottom_box.PackStart (ActionArea, false, false, 0);
            bottom_box.ShowAll ();
            VBox.PackEnd (bottom_box, false, false, 0);

            Button accept_button = new Button ();
            accept_button.Label = AddinManager.CurrentLocalizer.GetString ("Accept");
            accept_button.ShowAll ();
            accept_button.Activated += delegate {
                //login_form.Save ();
            };
            accept_button.Clicked += delegate {
                //login_form.Save ();
            };
            AddActionWidget (accept_button, ResponseType.Accept);

            Button reject_button = new Button ();
            reject_button.Label = AddinManager.CurrentLocalizer.GetString ("Reject");
            reject_button.ShowAll ();
            reject_button.Activated += delegate {
                //login_form.Save ();
            };
            reject_button.Clicked += delegate {
                //login_form.Save ();
            };
            AddActionWidget (reject_button, ResponseType.Reject);
        }

        public void AddButton (string message, ResponseType response, bool isDefault)
        {
            Button button = (Button)AddButton (message, response);

            if (isDefault) {
                DefaultResponse = response;
                button.AddAccelerator ("activate", accel_group, (uint)Gdk.Key.Return,
                    0, Gtk.AccelFlags.Visible);
            }
        }

        public string Message {
            get { return message.Text; }
            set { message.Text = value; }
        }
    }
}