//
// JamendoView.cs
//
// Authors:
//   Janez Troha <janez.troha@gmail.com>
//
// Copyright (C) 2010 Janez Troha
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
using System.Text.RegularExpressions;
using System.Threading;
using Banshee.Collection;
using Banshee.MediaEngine;
using Banshee.ServiceStack;
using Banshee.Streaming;
using Banshee.WebBrowser;
using Banshee.WebSource;
using Hyena;
using Hyena.Json;

namespace Banshee.Jamendo
{
    public class JamendoView : Banshee.WebSource.WebView, IDisposable
    {
        public JamendoView ()
        {
            CanSearch = true;
            FullReload ();
        }
        protected override OssiferNavigationResponse OnNavigationPolicyDecisionRequested (string uri)
        {
            return base.OnNavigationPolicyDecisionRequested (uri);
        }
        protected override string OnResourceRequestStarting (string old_uri)
        {
            if (old_uri.Contains ("http://8tracks.com/javascripts/player_packaged.js")) {
                return "about:blank";
            } else if (old_uri.Contains ("http://8tracks.com/stylesheets/universal_packaged.css")){
                return "http://dl.dropbox.com/u/302704/8tracks/universal_packaged.css";
            } else if (old_uri.Contains ("facebook.com") || old_uri.Contains ("twitter.com")){
                return "about:blank";
            }
            else {
                return base.OnResourceRequestStarting (old_uri);
            }

        }

        public override void GoHome ()
        {
            LoadUri (GetActionUrl (""));
        }

        public override void GoSearch (string query)
        {
            LoadUri (new Uri (GetActionUrl ("en/search/all/" + new SafeUri(query))).AbsoluteUri);
        }

        public string GetActionUrl (string action)
        {
            return "http://jamendo.com/" + action;
        }


    }
}

