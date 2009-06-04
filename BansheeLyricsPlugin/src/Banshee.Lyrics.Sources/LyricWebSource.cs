
using System;
using Banshee.IO;
using System.Text.RegularExpressions;

using Mono.Unix;

using Banshee.Lyrics.Network;

namespace Banshee.Lyrics.Sources
{
    public abstract class LyricWebSource:ILyricSource
    {
        /*regex used to parse html content */
        protected Regex regexLyric;
        protected Regex regexSuggestion;
        
        public abstract string Name { get; }
        
        public abstract string Url { get; }
        
        public string Credits {
            get {
                return string.Format ("Powered by {0} ({1})", Name, Url);
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
            return ParseUrl (url, regexSuggestion);
        }
        
        protected abstract string GetLyricUrl (string artist, string title);
        
        protected abstract string GetSuggestionUrl (string artist, string title);
        
        protected string cleanArtistName (string artist)
        {
            return artist.EndsWith (" ") ? artist.Substring (0, artist.Length - 2) : artist;
        }
        
        protected string cleanSongTitle (string title)
        {
            return title.EndsWith (" ") ? title.Substring (0, title.Length - 1) : title;
        }
        
        /*parse the content of an url using a regular expression to filter the content */
        public string ParseUrl (string url, Regex r)
        {
            if (url == null || r == null) {
                return null;
            }
            
            string html = HttpUtils.ReadHtmlContent (url);
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
