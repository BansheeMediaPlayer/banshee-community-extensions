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
    public partial class PreferenceView : Gtk.Bin
    {
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
            this.Build ();
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