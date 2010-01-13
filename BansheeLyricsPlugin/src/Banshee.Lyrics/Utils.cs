
using System;
using System.Text.RegularExpressions;

namespace Banshee.Lyrics
{


    public static class Utils
    {

        public static string TagLyric (string lyric)
        {
            if (lyric == null) {
                return null;
            }
            return lyric.Replace("\n","<br/>");
        }

        public static string DeTagLyric (string lyric)
        {
            if (lyric == null) {
                return null;
            }
            string l = Regex.Replace(lyric,@"<br\b[^>]*>","\n");
            return Regex.Replace(l,@"<(.|\n)*?>",string.Empty);
        }
    }
}
