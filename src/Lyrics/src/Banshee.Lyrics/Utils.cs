
using System;
using System.Text.RegularExpressions;

namespace Banshee.Lyrics
{
    public static class Utils
    {

        public static string ToHtml (string lyrics)
        {
            if (lyrics == null) {
                return null;
            }

            if (IsHtml (lyrics)) {
                return lyrics;
            }

            return lyrics.Replace("\n","<br/>");
        }

        public static bool IsHtml (string lyrics)
        {
            if (lyrics == null) {
                return false;
            }

            Match m = Regex.Match(lyrics, @"<(.|\n)*?>");

            return m.Success;
        }

        public static string ToNormalString (string html_lyrics)
        {
            if (html_lyrics == null) {
                return null;
            }

            string l = Regex.Replace(html_lyrics, @"<br\b[^>]*>", "\n");
            l = Regex.Replace(l, @"<(.|\n)*?>", string.Empty);
            l = Regex.Replace(l, @"\n\s+\n", "\n");
            l = Regex.Replace(l, "[\r\t]", String.Empty);

            return l;
        }
    }
}
