// 
// PreferenceView.cs
// 
// Author:
//   John Moore <jcwmoore@gmail.com>
// 
// Copyright (c) 2010 John Moore
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
namespace Banshee.Ampache
{
    [System.ComponentModel.ToolboxItem(true)]
    public class PreferenceView : Gtk.Bin
    {
        private Gtk.HBox hbox1;
        private Gtk.Table table1;
        private Gtk.Label lblUser;
        private Gtk.Label lblUrl;
        private Gtk.Label lblPassword;
        private Gtk.Entry entUser;
        private Gtk.Entry entUrl;
        private Gtk.Entry entPassword;
        private Gtk.VBox vbox1;
        private Gtk.Button btnSave;
        private Gtk.Button btnClear;


        protected virtual void Clean_OnClicked (object sender, System.EventArgs e)
        {
            entUrl.Text = string.Empty;
            entUser.Text = string.Empty;
            entPassword.Text = string.Empty;
            AmpacheSource.AmpacheRootAddress.Set(string.Empty);
            AmpacheSource.UserName.Set(string.Empty);
            AmpacheSource.UserPassword.Set(string.Empty);
        }

        public PreferenceView ()
        {
            vbox1 = new Gtk.VBox () { Spacing = 6 };
            table1 = new Gtk.Table (3, 2, true) { RowSpacing = 6, ColumnSpacing = 6 };
            entPassword = new Gtk.Entry () {
                CanFocus = true,
                IsEditable = true,
                Visibility = false,
                InvisibleChar = '●'
            };
            table1.Attach (entPassword, 1, 2, 2, 3);
            entUrl = new Gtk.Entry () { CanFocus = true, IsEditable = true };
            table1.Attach (entUrl, 1, 2, 0, 1);
            entUser = new Gtk.Entry () { CanFocus = true, IsEditable = true };
            table1.Attach (entUser, 1, 2, 1, 2);
            lblPassword = new Gtk.Label ("Password:");
            table1.Attach (lblPassword, 0, 1, 2, 3);
            lblUrl = new Gtk.Label ("Ampache Server Name:");
            table1.Attach (lblUrl, 0, 1, 0 ,1);
            lblUser = new Gtk.Label ("User Name:");
            table1.Attach (lblUser, 0, 1, 1, 2);
            vbox1.PackStart (table1, true, false, 0);

            hbox1 = new Gtk.HBox ();
            btnSave = new Gtk.Button ();
            btnSave.Label = "Save";
            btnSave.Clicked += Save_OnClicked;
            hbox1.PackStart (btnSave, false, false, 0);
            btnClear = new Gtk.Button ();
            btnClear.Label = "Clear";
            btnClear.Clicked += Clean_OnClicked;
            hbox1.PackStart (btnClear, true, false, 0);
            vbox1.PackStart (hbox1, false, false, 0);
            this.Add (vbox1);
            ShowAll ();

            entUrl.Text = AmpacheSource.AmpacheRootAddress.Get(AmpacheSource.AmpacheRootAddress.DefaultValue);
            entUser.Text = AmpacheSource.UserName.Get(AmpacheSource.UserName.DefaultValue);
            entPassword.Text = AmpacheSource.UserPassword.Get(AmpacheSource.UserPassword.DefaultValue);
        }

        protected virtual void Save_OnClicked (object sender, System.EventArgs e)
        {
            AmpacheSource.AmpacheRootAddress.Set(entUrl.Text);
            AmpacheSource.UserName.Set(entUser.Text);
            AmpacheSource.UserPassword.Set(entPassword.Text);
        }
    }
}