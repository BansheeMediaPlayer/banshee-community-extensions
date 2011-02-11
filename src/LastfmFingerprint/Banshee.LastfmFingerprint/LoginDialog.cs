//
// AccountLoginDialog.cs
//
// Authors:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2006 Novell, Inc.
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
using Mono.Unix;
using Gtk;

using Lastfm;
using Banshee.Gui.Dialogs;

namespace Lastfm
{
    public class LoginDialog : BansheeDialog
    {
        private LoginForm login_form;
        private Label message;

        public LoginDialog (LastfmAccount account) : this (account, false)
        {
        }

        public LoginDialog (LastfmAccount account, bool addCloseButton) : base ()
        {
            Title = Catalog.GetString ("Log in to Last.fm");
            HasSeparator = false;

            IconName = "gtk-dialog-authentication";

            HBox hbox = new HBox (false, 12);
            VBox vbox = new VBox (false, 0);
            hbox.BorderWidth = 5;
            vbox.Spacing = 5;

            Image image = new Image ();
            image.Yalign = 0.0f;
            image.IconName = "gtk-dialog-authentication";
            image.IconSize = (int)IconSize.Dialog;

            hbox.PackStart (image, false, false, 0);
            hbox.PackStart (vbox, true, true, 0);

            Label header = new Label ();
            header.Xalign = 0.0f;
            header.Markup = String.Format ("<big><b>{0}</b></big>", Catalog.GetString ("Last.fm Account Login"));

            message = new Label (Catalog.GetString ("Please enter your Last.fm account credentials."));
            message.Xalign = 0.0f;

            vbox.PackStart (header, false, false, 0);
            vbox.PackStart (message, false, false, 0);

            login_form = new LoginForm (account);
            login_form.AddSignUpButton ();
            login_form.AddAuthorizeButton ();
            //TODO fix the verify user because always get bad pwd

            vbox.PackStart (login_form, true, true, 0);

            VBox.PackStart (hbox, true, true, 0);
            VBox.Spacing = 10;

            VBox.ShowAll ();

            if (addCloseButton) {
                AddStockButton (Stock.Cancel, ResponseType.Cancel);
                var button = AddStockButton (Stock.Ok, ResponseType.Ok, true);
                button.Label = Catalog.GetString ("Log In");
            }
        }

        private void AddSignUpButton ()
        {
            login_form.AddSignUpButton ();
        }

        private void AddAuthorizeButton ()
        {
            login_form.AddAuthorizeButton ();
        }

        protected override void OnResponse (ResponseType response)
        {
            if (response == ResponseType.Ok) {
                login_form.Save ();
            }
        }

        public string Message {
            get { return message.Text; }
            set { message.Text = value; }
        }

        public bool SaveOnEdit {
            get { return login_form.SaveOnEdit; }
            set { login_form.SaveOnEdit = value; }
        }

        public string Username {
            get { return login_form.Username; }
        }
        
        public string Password {
            get { return login_form.Password; }
        }
    }
}
