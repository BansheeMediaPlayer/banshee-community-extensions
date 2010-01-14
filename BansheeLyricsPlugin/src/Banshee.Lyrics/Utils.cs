
using System;
using System.Text.RegularExpressions;

namespace Banshee.Lyrics
{


    public static class Utils
    {

        public static string ToHtml (string lyric)
        {
            if (lyric == null) {
                return null;
            }
            if (IsHtml (lyric)) {
                return lyric;
            }
            return lyric.Replace("\n","<br/>");
        }

        public static bool IsHtml (string lyric)
        {
            Match m = Regex.Match(lyric,@"<(.|\n)*?>");
            return m.Success;
        }

        public static string ToNormalString (string html_lyric)
        {
            if (html_lyric == null) {
                return null;
            }
            string l = Regex.Replace(html_lyric, @"<br\b[^>]*>", "\n");
            l = Regex.Replace(l, @"<(.|\n)*?>", string.Empty);
            l = Regex.Replace(l, @"\n\s*\n", "\n");
            l = Regex.Replace(l, "[\r\t]", String.Empty);
            return Regex.Replace(l, "[\n]+"," \n");
        }
    }
}
