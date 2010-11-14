//
// JamendoView.cs
//
// Authors:
//   Janez Troha <janez.troha@gmail.com>
//   Bertrand Lorentz <bertrand.lorentz@gmail.com>
//
// Copyright 2010 Janez Troha
// Copyright 2010 Bertrand Lorentz
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

using Hyena;

using Banshee.WebBrowser;
using Banshee.WebSource;

namespace Banshee.Jamendo
{
    public class JamendoView : Banshee.WebSource.WebView, IDisposable
    {
        public JamendoView ()
        {
            CanSearch = true;
            FixupJavascriptUrl = "http://integrated-services.banshee.fm/jamendo/jamendo-fixup.js";

            FullReload ();
        }

        private static bool IsPlaylistContentType (string contentType)
        {
            switch (contentType) {
                case "audio/x-mpegurl":
                case "application/xspf+xml":
                    return true;
            }

            return false;
        }

        private static bool IsDownloadContentType (string contentType)
        {
            switch (contentType) {
                // For single tracks
                case "application/octet-stream":
                // For album downloads
                case "application/zip":
                    return true;
            }

            return false;
        }

        protected override OssiferNavigationResponse OnNavigationPolicyDecisionRequested (string uri)
        {
            return base.OnNavigationPolicyDecisionRequested (uri);
        }

        protected override OssiferNavigationResponse OnMimeTypePolicyDecisionRequested (string mimetype)
        {
            // We only explicitly accept (render) text/html types, and only
            // download what we can import or stream.
            if (IsPlaylistContentType (mimetype) || IsDownloadContentType (mimetype)) {
                return OssiferNavigationResponse.Download;
            }

            return base.OnMimeTypePolicyDecisionRequested (mimetype);
        }

        protected override string OnDownloadRequested (string mimetype, string uri, string suggestedFilename)
        {
            if (IsPlaylistContentType (mimetype)) {
                Log.DebugFormat ("Streaming from Jamendo playlist : {0} ({1})", uri, mimetype);
                // FIXME: Works only for single track and only once
                Banshee.Streaming.RadioTrackInfo.OpenPlay (uri);
                Banshee.ServiceStack.ServiceManager.PlaybackController.StopWhenFinished = true;
                return null;
            } else if (IsDownloadContentType (mimetype)) {
                JamendoDownloadManager.Download (uri, mimetype);
                return null;
            }

            return null;
        }

        public override void GoHome ()
        {
            LoadUri (GetActionUrl (""));
        }

        public override void GoSearch (string query)
        {
            LoadUri (new Uri (GetActionUrl ("en/search/all/" + query)).AbsoluteUri);
        }

        public string GetActionUrl (string action)
        {
            return "http://jamendo.com/" + action;
        }
    }
}

