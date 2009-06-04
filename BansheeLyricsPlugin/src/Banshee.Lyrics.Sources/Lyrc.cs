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
using System.Web;
using System.Text.RegularExpressions;
using Mono.Unix;

namespace Banshee.Lyrics.Sources
{
    public class Lyrc:LyricWebSource
    {
    
        public Lyrc ()
        {
            base.regexLyric =
                new
                Regex
                ("<img src=\"img/pix_discontinua.gif\" width=\"400\" height=\"3\"></td>(.*)<td height=\"10\" align=\"center\" valign=\"top\" class=\"TEXTmagenta\">",
                 RegexOptions.Multiline | RegexOptions.IgnoreCase | RegexOptions.Singleline);
            base.regexSuggestion =
                new Regex ("Suggestions :(.*?)<br><br> If none is your song <br>",
                           RegexOptions.Multiline | RegexOptions.IgnoreCase | RegexOptions.Singleline);
        }
        
        public override string Name {
            get { return "Lyrc"; }
        }
        
        public override string Url {
            get { return "http://lyrc.com.ar"; }
        }
        
        public override string GetLyrics (string artist, string title)
        {
            string lyric = base.GetLyrics (artist, title);
            /*HACK: on Lyrc lyrics and suggestions share the same html code. 
               Sometimes text downloaded as a lyric could be a suggestion. */
            if (GetSuggestions (artist, title) == null) {
                return lyric;
            } else {
                return null;
            }
        }
        
        protected override string GetLyricUrl (string artist, string title)
        {
            string url_artist = HttpUtility.UrlEncode (base.cleanArtistName (artist));
            string url_song_title = HttpUtility.UrlEncode (base.cleanSongTitle (title));
            string url = string.Format (this.Url + "/tema1en.php?artist={0}&songname={1}", url_artist, url_song_title);
            
            return url;
        }
        
        protected override string GetSuggestionUrl (string artist, string title)
        {
            artist = artist == null ? "" : artist;
            return GetLyricUrl (artist, title);
        }
    }
}
