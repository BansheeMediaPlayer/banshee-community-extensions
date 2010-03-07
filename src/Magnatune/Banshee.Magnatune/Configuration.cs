/*
Magnatune Plugin for Banshee. Configuration screen.

Copyright 2008 Max Battcher <me@worldmaker.net>.

Permission is hereby granted, free of charge, to any person obtaining
a copy of this software and associated documentation files (the
"Software"), to deal in the Software without restriction, including
without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to
permit persons to whom the Software is furnished to do so, subject to
the following conditions:

The above copyright notice and this permission notice shall be
included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
using System;

namespace Banshee.Magnatune
{
    public partial class Configuration : Gtk.Dialog
    {
        public static readonly string[] membershipTypes = { "", "streaming", "download" };

        public Configuration (string type, string user, string pass)
        {
            this.Build ();
            username.Text = user;
            password.Text = pass;
            switch (type) {
            case "streaming":
                membershipType.Active = 1;
                break;
            case "download":
                membershipType.Active = 2;
                break;
            default:
                membershipType.Active = 0;
                break;
            }
        }

        protected virtual void OnButtonOkPressed (object sender, System.EventArgs e)
        {
            RadioSource.MembershipTypeSchema.Set (membershipTypes[membershipType.Active]);
            RadioSource.UsernameSchema.Set (username.Text);
            RadioSource.PasswordSchema.Set (password.Text);
        }

        protected virtual void OnButtonCancelPressed (object sender, System.EventArgs e)
        {
            this.Hide ();
        }
    }
}
