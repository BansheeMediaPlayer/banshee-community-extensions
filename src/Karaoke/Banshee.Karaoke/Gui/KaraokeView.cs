// 
// KaraokeView.cs
// 
// Author:
//   Frank Ziegler <funtastix@googlemail.com>
// 
// Copyright (c) 2011 Frank Ziegler
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

using Hyena;
using Banshee.WebSource;
using Banshee.WebBrowser;
using Banshee.Collection;

namespace Banshee.Karaoke.Gui
{
    public class KaraokeView : Banshee.WebSource.WebView, IDisposable
    {
        public KaraokeView ()
        {
            CanSearch = false;
            FullReload ();
        }

        protected override string OnDownloadRequested (string mimetype, string uri, string suggestedFilename)
        {
            return null;
        }

        protected override OssiferNavigationResponse OnNavigationPolicyDecisionRequested (string uri)
        {
            if (uri.StartsWith ("http://youtubelyric.com/lyric")) {
                return OssiferNavigationResponse.Accept;
            }
            return OssiferNavigationResponse.Ignore;
        }

        public override void GoHome ()
        {
            //LoadUri ("http://youtubelyric.com/");
            return;
        }

        public override void GoSearch (string query)
        {
            //LoadUri ("http://youtubelyric.com/lyric/showlyric.php?" + query);
            return;
        }

        public void LoadLyrics (TrackInfo track)
        {
            LoadUri (String.Format ("http://youtubelyric.com/lyric/showlyric.php?artist={0}&song={1}",
                track.DisplayArtistName, track.DisplayTrackTitle));
        }
    }
}

