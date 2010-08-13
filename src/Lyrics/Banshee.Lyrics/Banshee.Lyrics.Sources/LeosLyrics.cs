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
using System.Xml;
using System.Web;

using Banshee.Lyrics.Network;

namespace Banshee.Lyrics.Sources
{
    public class LeosLyrics:ILyricsSource
    {
        public string Name {
            get { return "Leo's lyrics"; }
        }

        public string Credits {
            get{ return string.Format ("Powered by {0} ({1})", Name, "http://www.leoslyrics.com"); }
        }

        public string GetLyrics (string artist, string title)
        {
            /*search using leo lyrics api */
            string results_xml = GetSearchResults (UrlEncode (artist), UrlEncode (title));

            /*extract hid from search results xml */
            string hid = GetHid (results_xml);
            if (hid == null) {
                return null;
            }

            return GetLyrics (hid);
        }

        public string GetSuggestions (string artist, string title)
        {
            return null;
        }

        private string GetSearchResults (string artist, string title)
        {
            string search_url =
                string.Format ("http://api.leoslyrics.com/api_search.php?auth=duane&artist={0}&songtitle={1}", artist,
                               title);
            string xml = HttpUtils.ReadHtmlContent (search_url, null);
            return xml;
        }

        private string GetLyrics (string hid)
        {
            /*query for the lyric xml */
            string lyric_url = "http://api.leoslyrics.com/api_lyrics.php?auth=duane&hid=" + hid;
            string lyric_xml = HttpUtils.ReadHtmlContent (lyric_url, null);
            if (lyric_xml == null) {
                return null;
            }

			/*get the lyric from the xml */
            XmlDocument xDoc = new XmlDocument ();
            xDoc.LoadXml (lyric_xml);
            XmlNodeList textList = xDoc.GetElementsByTagName ("text");
            if (textList == null || textList.Count == 0) {
                return null;
            }
			
            string lyric = textList.Item (0).InnerText;
            return lyric;
        }

        private string GetHid (string xml)
        {
            if (xml == null) {
                return null;
            }

            XmlDocument xDoc = new XmlDocument ();
            xDoc.LoadXml (xml);
            XmlNodeList results = xDoc.GetElementsByTagName ("result");

            if (results == null) {
                return null;
            }
            string hid = null;
            for (int i = 0; i < results.Count; i++) {
                XmlNode result = results.Item (i);
                string matchAttrValue = result.Attributes.Item (2).Value;
                if (matchAttrValue.Equals ("true")) {
                    hid = result.Attributes.Item (1).Value;
                    break;
                }
            }
            return hid;
        }

        private string UrlEncode (string str)
        {
            string retval = "";
            if (str == null || str.Length == 0) {
                return null;
            }
            str = HttpUtility.UrlEncode (str);
            string[] splitted_strings = str.Split ('+');
            foreach (string substring in splitted_strings) {
                if (substring.Length == 0) {
                    continue;
                }
                char first_char = substring.ToCharArray ()[0];
                string new_substring =
                    first_char.ToString ().ToUpper () + substring.Substring (1, substring.Length - 1);
                retval += new_substring + "_";
            }
            return retval.Substring (0, retval.Length - 1);
        }
    }
}
