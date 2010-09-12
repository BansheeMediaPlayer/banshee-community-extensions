//
// Author:
//   Alexander Kojevnikov <alexander@kojevnikov.com>
//
// Copyright (C) 2010 Alexander Kojevnikov
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
using System.Web;
using System.Text;
using System.Text.RegularExpressions;

namespace Banshee.Lyrics.Sources
{
    public class LyricsPlugin : LyricsWebSource
    {
        public LyricsPlugin ()
        {
            base.regexLyric = new Regex (
                @"<div id=""lyrics"">(.*?)</div>",
                RegexOptions.Multiline | RegexOptions.IgnoreCase | RegexOptions.Singleline);
        }

        public override string Name {
            get { return "LyricsPlugin"; }
        }

        public override string Url {
            get { return "http://www.lyricsplugin.com"; }
        }

        public override string GetLyrics (string artist, string title)
        {
            string lyrics = base.GetLyrics (artist, title);

            if (lyrics == null || lyrics.Trim().Length == 0) {
                return null;
            }
            // We sometimes just get a link when no lyrics are found
            if (lyrics.Trim() == "<a href=\"http://www.lyricsvip.com/\" target=\"_blank\">http://www.lyricsvip.com</a>") {
                return null;
            }

            return lyrics;
        }

        protected override string GetLyricUrl (string artist, string title)
        {
            string url_artist = HttpUtility.UrlEncode (CleanArtistName (artist));
            string url_title = HttpUtility.UrlEncode (CleanSongTitle (title));
            return String.Format ("{0}/plugin/?title={1}&artist={2}", Url, url_title, url_artist);
        }

        protected override string GetSuggestionUrl (string artist, string title)
        {
            return null;
        }

        protected override Encoding Encoding {
            get { return Encoding.UTF8; }
        }

    }
}