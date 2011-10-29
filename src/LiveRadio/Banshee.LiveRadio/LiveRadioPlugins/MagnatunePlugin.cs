//
// MagnatunePlugin.cs
//
// Authors:
//   Frank Ziegler <funtastix@googlemail.com>
//
// Copyright (C) 2010 Frank Ziegler
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
using System.IO;
using System.Xml;
using System.Collections.Generic;

using Banshee.Collection.Database;

using Hyena;
using Banshee.Configuration;
using System.Text.RegularExpressions;
using System.Net;

namespace Banshee.LiveRadio.Plugins
{

    /// <summary>
    /// LiveRadio plugin for magnatune.com
    ///
    /// This plugin is able to download a genre list upon initialize/refresh and execute live queries on the shoutcast directory
    /// </summary>
    public class MagnatunePlugin : LiveRadioBasePlugin
    {

        private const string base_url = "http://magnatune.com";
        private const string search_url = "http://my.magnatune.com";
        private const string genre_url = "/collections/";
        private const string freetext_request = "/search_one?c=songnames&t=m&w=";
        private const string album_postfix = "hifi.xspf";

        /// <summary>
        /// Constructor -- sets configuration entries
        /// </summary>
        public MagnatunePlugin () : base ()
        {
            use_proxy = UseProxyEntry.Get ().Equals ("True") ? true : false;

            if (!Int32.TryParse(HttpTimeoutEntry.Get (), out http_timeout_seconds))
                http_timeout_seconds = 20;
            proxy_url = ProxyUrlEntry.Get ();
            SetWebIcon ("http://my.magnatune.com/favicon.ico");
        }

        /// <summary>
        /// Magnatune plugin disabled for technical issues
        /// </summary>
        public override bool Active {
            get { return false; }
        }

        /// <summary>
        /// Retrieve and parse genre list
        /// </summary>
        protected override void RetrieveGenres ()
        {
            ParseGenres(RetrieveHtml(base_url + genre_url));
        }

        /// <summary>
        /// Retrieve and parse a live query on the shoutcast directory
        /// </summary>
        /// <param name="request_type">
        /// A <see cref="LiveRadioRequestType"/> -- the request type
        /// </param>
        /// <param name="query">
        /// A <see cref="System.String"/> -- the freetext query or the genre name
        /// </param>
        protected override void RetrieveRequest (LiveRadioRequestType request_type, string query)
        {
            if (request_type == LiveRadioRequestType.ByGenre) {
                XmlDocument document = RetrieveXml (base_url + genre_url + query + ".xspf");
                if (document != null) ParseXspf (document, new Genre(query));
            } else {
                if (cached_results.ContainsKey (query)) return;
                string doc = RetrieveHtml (search_url + freetext_request + query);
                if (!String.IsNullOrEmpty(doc)) ParseSearchResult (doc, query);
            }
        }

        /// <summary>
        /// Parses and sorts an XML genre catalog and fills the plugins genre list
        /// </summary>
        /// <param name="doc">
        /// A <see cref="XmlDocument"/> -- the XML document containing the genre catalog
        /// </param>
        private void ParseGenres(string html)
        {
            List<char> badchars = new List<char> ();
            foreach (char c in html)
            {
                if (char.IsControl (c) || char.IsSeparator (c) || char.IsWhiteSpace (c) || char.GetNumericValue (c) == 13 || char.GetNumericValue (c) == 12)
                    if (!badchars.Contains (c))
                        badchars.Add (c);
            }
            foreach (Char c in badchars)
                html = html.Replace (c, ' ');

            html = Regex.Replace (html, @"^.*<IMG SRC=""http://he3.magnatune.com/img/grey_dot.gif"" HEIGHT=""1"" WIDTH=""420""><p><a href=""""></a>", "<xml>");
            html = Regex.Replace (html, @"</FONT>.*$", "</xml>");
            html = Regex.Replace (html, @"<br>", "<br/>");

            XmlDocument doc = new XmlDocument ();
            try
            {
                doc.LoadXml (html);
            }
            catch (Exception e)
            {
                Log.DebugFormat ("[MagnatunePlugin]<ParseSearchResult> Parse Error: {0}", e.Message);
                RaiseErrorReturned ("General Error", e.Message);
                return;
            }

            XmlNodeList XML_genre_nodes = doc.GetElementsByTagName ("a");

            List<Genre> new_genres = new List<Genre> ();

            foreach (XmlNode node in XML_genre_nodes) {
                XmlAttributeCollection xml_attributes = node.Attributes;

                try
                {
                    string link = xml_attributes.GetNamedItem ("href").InnerText;
                    bool is_genre = false;

                    if (Regex.IsMatch (link, @"[a-z_]*")) is_genre = true;

                    if (is_genre)
                    {
                        Genre genre = new Genre(link);

                        if (!new_genres.Contains (genre)) {
                            new_genres.Add (genre);
                        }
                    }
                } catch (Exception ex) {
                    Log.Exception ("[MagnatunePlugin] <ParseGenres> ERROR", ex);
                    RaiseErrorReturned ("XML Parse Error", ex.Message);
                    continue;
                }

            }

            new_genres.Sort ();
            genres = new_genres;
        }

        /// <summary>
        /// Parses the response to a query request and fills the results cache
        /// </summary>
        /// <param name="xml_response">
        /// A <see cref="XmlDocument"/> -- the XML document containing the response to the query request
        /// </param>
        /// <param name="request_type">
        /// A <see cref="LiveRadioRequestType"/> -- the type of the request
        /// </param>
        /// <param name="query">
        /// A <see cref="System.String"/> -- the requested query, freetext or the genre name
        /// </param>
        private void ParseSearchResult (string html, string query)
        {
            List<char> badchars = new List<char> ();
            foreach (char c in html)
            {
                if (char.IsControl (c) || char.IsSeparator (c) || char.IsWhiteSpace (c) || char.GetNumericValue (c) == 13 || char.GetNumericValue (c) == 12)
                    if (!badchars.Contains (c))
                        badchars.Add (c);
            }
            foreach (Char c in badchars)
                html = html.Replace (c, ' ');

            html = Regex.Replace (html, @"^.*songs:</b><br>", "<xml>");
            html = Regex.Replace (html, @"<p>.*$", "</xml>");
            html = Regex.Replace (html, @"<br>", "</li>");
            html = Regex.Replace (html, @"a>""", "a>");
            html = Regex.Replace (html, @"""<", "<");

            if (!html.StartsWith ("<xml>"))
                html = "<xml></xml>";

            XmlDocument xml_response = new XmlDocument ();
            try
            {
                xml_response.LoadXml (html);
            }
            catch (Exception e)
            {
                Log.DebugFormat ("[MagnatunePlugin]<ParseSearchResult> Parse Error: {0}", e.Message);
                RaiseErrorReturned ("General Error", e.Message);
                return;
            }

            XmlNodeList XML_song_nodes = xml_response.GetElementsByTagName ("li");

            if (!cached_results.ContainsKey (query)) {
                cached_results[query] = new List<DatabaseTrackInfo> (XML_song_nodes.Count);
            }

            cached_results[query].Clear ();

            foreach (XmlNode node in XML_song_nodes)
            {
                string inner_text = node.InnerText;
                string song_name = Regex.Replace (inner_text, @"^""([^""]*)"".*$", "$1");
                string artist_name= Regex.Replace (inner_text, @"^.* by ", "");
                string album_link = null;
                string album_name = null;

                foreach (XmlNode child in node.ChildNodes)
                {
                    if (child.Name.Equals ("a"))
                    {
                        try
                        {
                            string link = child.Attributes.GetNamedItem ("href").InnerText;
                            album_name = child.InnerText;
                            if (link.Contains ("artists/albums")) album_link = link;

                        }
                        catch (Exception e) {
                            Log.DebugFormat ("[MagnatunePlugin]<ParseSearchResult> XML Parse Error {0}", e.Message);
                            RaiseErrorReturned ("General Error", e.Message);
                            continue;
                        }
                    }
                }

                if (!String.IsNullOrEmpty (album_link))
                {
                    XmlDocument album_playlist = RetrieveXml (album_link + album_postfix);
                    if (album_playlist != null)
                    {
                        try
                        {
                            foreach (XmlNode track_node in album_playlist.GetElementsByTagName ("track"))
                            {
                                bool found_track = false;
                                string location = null;

                                foreach (XmlNode track_attribute_node in track_node.ChildNodes)
                                {
                                    if (track_attribute_node.Name.Equals ("annotation")
                                        && track_attribute_node.InnerText.Contains (song_name))
                                    {
                                        found_track = true;
                                    }
                                    if (track_attribute_node.Name.Equals ("location"))
                                        location = track_attribute_node.InnerText;
                                    //if (track_attribute_node.Name.Equals ("image"))
                                    //{
                                    //    artist_name = Regex.Replace (track_attribute_node.InnerText, @"music/([^/]*)/", "$1");
                                    //    artist_name = System.Web.HttpUtility.UrlDecode (artist_name);
                                    //}
                                }
                                if (found_track)
                                {
                                    DatabaseTrackInfo new_station = new DatabaseTrackInfo ();

                                    new_station.Uri = new SafeUri (location);
                                    new_station.ArtistName = artist_name;
                                    new_station.TrackTitle = song_name;
                                    new_station.Comment = "buy it at www.magnatune.com";
                                    new_station.AlbumTitle = album_name;
                                    new_station.PrimarySource = source;
                                    new_station.IsLive = true;

                                    cached_results[query].Add (new_station);
                                    break;
                                }
                            }
                        }
                        catch (Exception e) {
                            Log.Exception ("[MagnatunePlugin] <ParseXmlResponse> ERROR: ", e);
                            RaiseErrorReturned ("XML Parse Error", e.Message);
                            continue;
                        }
                    }
                }
            }
        }

        private void ParseXspf (XmlDocument document, Genre genre)
        {
            if (!cached_results.ContainsKey (genre.GenreKey))
                cached_results.Add (genre.GenreKey, new List<DatabaseTrackInfo> ());
            else
                cached_results.Clear ();
            try
            {
                foreach (XmlNode track_node in document.GetElementsByTagName ("track"))
                {
                    string location = null;
                    string artist_name = null;
                    string album_name = null;
                    string song_name = null;
                    string duration = null;
                    foreach (XmlNode track_attribute_node in track_node.ChildNodes)
                    {
                        if (track_attribute_node.Name.Equals ("annotation"))
                        {
                            string name_duration = track_attribute_node.InnerText.Split ('[')[0];
                            string extra_info = track_attribute_node.InnerText.Split ('[')[1].Replace ("]","");
                            song_name = Regex.Replace (name_duration, @"\([0-9]+:[0-9]+\)", "");
                            duration = Regex.Replace (name_duration, @" (\([0-9]+:[0-9]+\)) \[", "$1");
                            artist_name = Regex.Replace (extra_info, @"\Artist: ""(.*)"", Album", "$1");
                            album_name = Regex.Replace (extra_info, @"Album: ""(.*)""", "$1");
                        }
                        if (track_attribute_node.Name.Equals ("location"))
                            location = track_attribute_node.InnerText;
                    }
                    if (!String.IsNullOrEmpty (location))
                    {
                        DatabaseTrackInfo new_station = new DatabaseTrackInfo ();

                        new_station.Uri = new SafeUri (location);
                        int min = 0;
                        Int32.TryParse (duration.Split (':')[0], out min);
                        int sec = 0;
                        Int32.TryParse (duration.Split (':')[1], out sec);
                        new_station.Duration = new TimeSpan (0,min, sec);
                        new_station.ArtistName = artist_name;
                        new_station.TrackTitle = song_name;
                        new_station.Genre = genre.Name;
                        new_station.Comment = "www.magnatune.com";
                        new_station.AlbumTitle = album_name;
                        new_station.PrimarySource = source;
                        new_station.IsLive = true;

                        cached_results[genre.GenreKey].Add (new_station);
                    }
                }
            }
            catch (Exception e) {
                Log.Exception ("[MagnatunePlugin] <ParseXmlResponse> ERROR: ", e);
                RaiseErrorReturned ("XML Parse Error", e.Message);
            }
        }

        /// <summary>
        /// Retrieves, reads and returns an HTML document from the specified query url using HTTP GET
        /// </summary>
        /// <param name="query">
        /// A <see cref="System.String"/> -- a URL with full query parameters
        /// </param>
        /// <returns>
        /// A <see cref="XmlDocument"/> -- the retrieved and loaded XML document
        /// </returns>
        protected string RetrieveHtml(string query)
        {
            WebProxy proxy;
            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create (query);
            request.Method = "GET";
            request.ContentType = "HTTP/1.0";
            request.Timeout = http_timeout_seconds * 1000;

            if (use_proxy) {
                proxy = new WebProxy (proxy_url, true);
                request.Proxy = proxy;
            }

            try
            {
                Stream response = request.GetResponse ().GetResponseStream ();
                StreamReader reader = new StreamReader (response);

                string html = reader.ReadToEnd ();
                return html;
            }
            catch (Exception e) {
                Log.DebugFormat ("[MagnatunePlugin\"{0}\"] <RetrieveHtml> Error: {1} END", Name, e.Message);
                RaiseErrorReturned ("General Error", e.Message);
            }
            return null;
        }


        /// <summary>
        /// The name of the plugin -- used as identifier and as label for the source header
        /// </summary>
        public override string Name {
            get { return "magnatune.com"; }
        }

        /// <summary>
        /// Version of this plugin code
        /// </summary>
        public override string Version {
            get { return "0.1"; }
        }

        /// <summary>
        /// Saves the configuration for this plugin
        /// </summary>
        public override void SaveConfiguration ()
        {
            if (configuration_widget == null) return;
            http_timeout_seconds = configuration_widget.HttpTimeout;
            proxy_url = configuration_widget.ProxyUrl;
            use_proxy = configuration_widget.UseProxy;
            HttpTimeoutEntry.Set (http_timeout_seconds.ToString ());
            ProxyUrlEntry.Set (proxy_url);
            UseProxyEntry.Set (use_proxy.ToString ());
        }

        public static readonly SchemaEntry<string> UseProxyEntry = new SchemaEntry<string> (
        "plugins.liveradio.shoutcast" , "use_proxy", "", "whether to use proxy for HTTP", "whether to use proxy for HTTP");

        public static readonly SchemaEntry<string> ProxyUrlEntry = new SchemaEntry<string> (
        "plugins.liveradio.shoutcast", "proxy_url", "", "HTTP proxy url", "HTTP proxy url");

        public static readonly SchemaEntry<string> HttpTimeoutEntry = new SchemaEntry<string> (
        "plugins.liveradio.shoutcast", "http_timeout_seconds", "", "HTTP timeout", "HTTP timeout");


    }

}
