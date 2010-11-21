//
// Author:
//   Christian Martellini <christian.martellini@gmail.com>
//
// Copyright (C) 2009 Christian Martellini
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

using Banshee.ServiceStack;

namespace Banshee.Lyrics.Sources
{
    public class LyricWiki:LyricsWebSource
    {
        public LyricWiki ()
        {
            base.regexLyric =
                new Regex ("&lt;lyrics&gt;(.*?)&lt;/lyrics&gt;",
                           RegexOptions.Multiline | RegexOptions.IgnoreCase | RegexOptions.Singleline);
        }
        public override string Name {
            get { return "LyricWiki";}
        }

        public override string Url {
            get { return "http://lyrics.wikia.com/api.php?action=query&prop=revisions&rvprop=content&format=xml&titles="; }
        }

        protected override string GetSuggestionUrl (string artist, string title)
        {
            return null;
        }

        protected override string GetLyricUrl (string artist, string title)
        {
            string url_artist = System.Web.HttpUtility.UrlEncode (base.CleanArtistName (artist));
            string url_song_title = System.Web.HttpUtility.UrlEncode (base.CleanSongTitle (title));
            string relative_url = string.Format ("{0}:{1}", url_artist, url_song_title);
            string[]splitted_strings = relative_url.Split ('+');

            string lyricwiki_url = this.Url;
            /*make first character of each word upper and separate each word with '_' */
            foreach (string str in splitted_strings) {
                if (str.Length == 0) {
                    continue;
                }
                char first_char = str.ToCharArray ()[0];
                string new_str = first_char.ToString ().ToUpper () + str.Substring (1, str.Length - 1);
                lyricwiki_url += new_str + "_";
            }

            return lyricwiki_url.Substring (0, lyricwiki_url.Length - 1);
        }
    }
}
