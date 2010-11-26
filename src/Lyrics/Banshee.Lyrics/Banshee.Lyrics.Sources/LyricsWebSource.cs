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
using System.Text;
using System.Text.RegularExpressions;

using Mono.Addins;

using Banshee.IO;

using Banshee.Lyrics.Network;

namespace Banshee.Lyrics.Sources
{
    public abstract class LyricsWebSource: ILyricsSource
    {
        /*regex used to parse html content */
        protected Regex regexLyric;
        protected Regex regexSuggestion;

        public abstract string Name { get; }

        public abstract string Url { get; }

        public string Credits {
            get {
                return string.Format (AddinManager.CurrentLocalizer.GetString ("Powered by {0} ({1})"),
                                      Name, Url);
            }
        }

        public virtual string GetLyrics (string artist, string title)
        {
            return GetLyrics (GetLyricUrl (artist, title));
        }

        public virtual string GetSuggestions (string artist, string title)
        {
            return GetSuggestions (GetSuggestionUrl (artist, title));
        }

        public virtual string GetLyrics (string url)
        {
            return ParseUrl (url, regexLyric);
        }

        public virtual string GetSuggestions (string url)
        {
            string suggestion = ParseUrl (url, regexSuggestion);
            return CleanSuggestion (suggestion);
        }

        static string CleanSuggestion (string suggestion)
        {
            if (suggestion == null) {
                return null;
            }
            suggestion = suggestion.Replace ("color='white'", "");
            return suggestion;
        }

        protected abstract string GetLyricUrl (string artist, string title);

        protected abstract string GetSuggestionUrl (string artist, string title);

        protected string CleanArtistName (string artist)
        {
            return artist != null ? artist.Trim () : null;
        }

        protected string CleanSongTitle (string title)
        {
            return title != null ? title.Trim () : null;
        }

        protected virtual Encoding Encoding {
            get { return null; }
        }

        /*parse the content of an url using a regular expression to filter the content */
        public string ParseUrl (string url, Regex r)
        {
            if (url == null || r == null) {
                return null;
            }

            string html = HttpUtils.ReadHtmlContent (url, this.Encoding);
            if (html == null) {
                return null;
            }
            string parsed_html = null;
            if (r.IsMatch (html)) {
                Match m = r.Match (html);
                parsed_html = m.Groups[1].ToString ();
            }
            return parsed_html;
        }
    }
}
