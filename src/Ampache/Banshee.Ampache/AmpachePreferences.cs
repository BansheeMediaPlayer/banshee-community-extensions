// 
// AmpacheSource.cs
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
using Banshee.Configuration;
using Banshee.Collection;
using Banshee.Gui;
using Banshee.Base;
using Banshee.MediaEngine;
using Banshee.ServiceStack;
using Banshee.Sources;
using Banshee.Sources.Gui;
using Banshee.Preferences;
using Gdk;

namespace Banshee.Ampache
{
    internal class AmpachePreferences : IDisposable
    {
        private SourcePage source_page;
        private Section account_section;

        public AmpachePreferences (AmpacheSource source)
        {
            var service = ServiceManager.Get<PreferenceService> ();
            if (service == null) {
                return;
            }
            source_page = new SourcePage(source);
            account_section = new Section("ampache-account", "Account", 20);
            //account_section.Add(AmpacheSource.AmpacheRootAddress);
            //account_section.Add(AmpacheSource.UserName);
            //account_section.Add(AmpacheSource.UserPassword);
            source_page.Add(account_section);
            //account_section["ampache-account"].DisplayWidget = new PreferenceView();
            var view = new PreferenceView();
            source_page.DisplayWidget = view;// = new PreferenceView();
        }

        public string PageId {
            get { return source_page.Id; }
        }
        #region IDisposable implementation
        public void Dispose ()
        {
            //throw new NotImplementedException ();
        }
        #endregion
    }
}
