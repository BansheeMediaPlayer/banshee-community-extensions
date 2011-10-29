//
// RealRadiosPlugin.cs
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
using System.Xml;
using System.Collections.Generic;

using Banshee.Collection.Database;

using Hyena;
using Banshee.Configuration;
using System.Text.RegularExpressions;
using System.Net;
using System.IO;

namespace Banshee.LiveRadio.Plugins
{

    /// <summary>
    /// LiveRadio plugin for realradios.com
    ///
    /// This plugin is able to download a genre list upon initialize/refresh and execute live queries on the shoutcast directory
    /// </summary>
    public class RealRadiosPlugin : LiveRadioBasePlugin
    {

        private const string base_url = "http://www.realradios.com/";
        private const string play_url = "http://www.realradios.com/x/stream/";
        private const string station_url = "http://www.realradios.com/play/";
        private const string genre_list_request = "genres";
        private const string genre_url = "genres/";
        private const string freetext_url = "stations/search/";

        /// <summary>
        /// RealRadios plugin disabled for technical issues
        /// </summary>
        public override bool Active {
            get { return false; }
        }

        /// <summary>
        /// Constructor -- sets configuration entries
        /// </summary>
        public RealRadiosPlugin () : base ()
        {
            use_proxy = UseProxyEntry.Get ().Equals ("True") ? true : false;

            if (!Int32.TryParse(HttpTimeoutEntry.Get (), out http_timeout_seconds))
                http_timeout_seconds = 20;
            proxy_url = ProxyUrlEntry.Get ();
            SetWebIcon (base_url + "sites/default/files/favicon_0.ico");
        }

        /// <summary>
        /// Retrieve and parse genre list
        /// </summary>
        protected override void RetrieveGenres ()
        {
            ParseGenres(RetrieveHtml(base_url + genre_list_request));
        }

        /// <summary>
        /// Retrieve and parse a live query on the realradios directory
        /// </summary>
        /// <param name="request_type">
        /// A <see cref="LiveRadioRequestType"/> -- the request type
        /// </param>
        /// <param name="query">
        /// A <see cref="System.String"/> -- the freetext query or the genre name
        /// </param>
        protected override void RetrieveRequest (LiveRadioRequestType request_type, string query)
        {
            string key;
            if (request_type == LiveRadioRequestType.ByGenre) {
                key = "Genre:" + query;
            } else {
                key = query;
            }

            if (cached_results.ContainsKey (key)) {
                return;
            }

            string request;
            if (request_type == LiveRadioRequestType.ByGenre) {
                request = genres.Find (delegate (Genre g) {
                    return g.Name.Equals(query);
                }
                ).Hyperlink;
            } else {
                request = base_url + freetext_url + query;
            }
            string document = RetrieveHtml(request);
            if (document != null) ParseHtmlResponse(document, request_type, query);
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

            html = Regex.Replace (html, @"^.*<div class=""content""><div id=""genres-main"" class=""cols-4 round-3"">", "<xml>");
            html = Regex.Replace (html, @"<div id=""content-footer"" class=""clear-block"">.*$", "</xml>");
            html = Regex.Replace (html, @"<br>", "<br/>");
            html = Regex.Replace (html, @"<div[^>]*>", "<br/>");
            html = Regex.Replace (html, @"</div>", "<br/>");

            XmlDocument doc = new XmlDocument ();
            try
            {
                doc.LoadXml (html);
            }
            catch (Exception e)
            {
                Log.DebugFormat ("[RealRadiosPlugin]<ParseSearchResult> Parse Error: {0}", e.Message);
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
                    string name = node.InnerText;
                    bool is_genre = false;

                    if (Regex.IsMatch (link, @"[a-z_]*")) is_genre = true;

                    if (is_genre)
                    {
                        Genre genre = new Genre(name);
                        genre.Hyperlink = link;

                        if (!new_genres.Contains (genre)) {
                            new_genres.Add (genre);
                        }
                    }
                } catch (Exception ex) {
                    Log.Exception ("[RealRadiosPlugin] <ParseGenres> ERROR", ex);
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
        private void ParseHtmlResponse (string html, LiveRadioRequestType request_type, string query)
        {
            int pages = 1;
            string pagerequest = null;

            if (html.Contains ("\" title=\"Go to last page\""))
            {
                pagerequest = html.Remove (html.IndexOf ("\" title=\"Go to last page\""));
                pagerequest = pagerequest.Substring (pagerequest.LastIndexOf ("\"") + 1);

                string page = pagerequest.Substring (pagerequest.LastIndexOf ("=") + 1);
                pagerequest = pagerequest.Remove (pagerequest.IndexOf ("=") + 1);

                try
                {
                    pages = Int16.Parse (page);
                }
                catch {}
            }

            string full_html = null;

            for (int p = 1; p <= pages; p++)
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

                html = Regex.Replace (html, @"^.*<div class=""station-list"">", "<div>");
                html = Regex.Replace (html, @"<img[^>]*>", "<br/>");

                if (html.Contains ("ul class=\"pager\""))
                {
                    html = Regex.Replace (html, @"<div class=""item-list"">.*$", "<br/>");
                } else {
                    html = Regex.Replace (html, @"<div id=""content-footer"".*$", "<div/>");
                    html = Regex.Replace (html, @"> *<", "><");
                    html = Regex.Replace (html, @"</div></div></div><div/>", "<br/>");

                }
                html = Regex.Replace (html, @"<br>", "<br/>");
                full_html = full_html + html;


                if (p < pages)
                {
                    html = RetrieveHtml (pagerequest + (p + 1));
                }

            }

            html = "<xml>" + full_html + "</xml>";

            XmlDocument doc = new XmlDocument ();
            try
            {
                doc.LoadXml (html);
            }
            catch (Exception e)
            {
                Log.DebugFormat ("[RealRadiosPlugin]<ParseSearchResult> Parse Error: {0}", e.Message);
                RaiseErrorReturned ("General Error", e.Message);
                return;
            }

            string key;
            if (request_type == LiveRadioRequestType.ByGenre) {
                key = "Genre:" + query;
            } else {
                key = query;
            }

            cached_results[key] = new List<DatabaseTrackInfo> ();

            foreach (XmlNode snode in doc.GetElementsByTagName ("div"))
            {

                if (snode.Name.Equals ("div") && snode.Attributes != null && snode.Attributes.Count > 0)
                {
                    try
                    {

                        string location = null;
                        string frequency = null;
                        string title = null;
                        string preurl = null;

                        if (snode.Attributes.GetNamedItem ("class").InnerText.Equals ("station"))
                        {
                            preurl = snode.Attributes.GetNamedItem ("rel").InnerText.Replace ("station-","");

                            foreach (XmlNode node in snode.ChildNodes)
                            {

                                if (node.Attributes.GetNamedItem ("class").InnerText.Equals ("station-info"))
                                {
                                    foreach (XmlNode child in node.SelectNodes ("descendant::*"))
                                    {
                                        if (child.Name.Equals ("a"))
                                        {
                                            title = child.InnerText;
                                        }
                                        else if (child.Name.Equals ("span"))
                                        {
                                            if (child.Attributes.GetNamedItem ("class").InnerText.Equals ("frequency"))
                                            {
                                                frequency = child.InnerText;
                                            }
                                            else if (child.Attributes.GetNamedItem ("class").InnerText.Equals ("location"))
                                            {
                                                location = child.InnerText;
                                            }
                                        }
                                    }

                                    DatabaseTrackInfo new_station = new DatabaseTrackInfo ();

                                    new_station.ArtistName = location;
                                    new_station.Genre = query;
                                    new_station.Comment = frequency;
                                    new_station.TrackTitle = title;
                                    new_station.PrimarySource = source;
                                    new_station.Copyright = preurl;
                                    new_station.IsLive = true;

                                    cached_results[key].Add (new_station);
                                }
                            }
                        }
                    }
                    catch (Exception e) {
                        Log.DebugFormat ("[RealRadiosPlugin]<ParseSearchResult> XML Parse Error {0}", e.Message);
                        RaiseErrorReturned ("General Error", e.Message);
                        continue;
                    }
                }

            }

        }

        public override SafeUri RetrieveUrl (string baseurl)
        {
            string url = RetrieveHtml (station_url + baseurl);

            if (url.Contains ("\"format\": \"wm\""))
            {
                url = url.Substring (1, url.IndexOf ("\", \"format\": \"wm\"") - 1);
                url = play_url + url.Substring (url.LastIndexOf ("\"") + 1);
            } else {
                url = url.Substring (url.IndexOf ("document.rrData={0:{ \"id\": \"") + 28);
                url = url.Substring (0, url.IndexOf ("\""));
                Log.DebugFormat ("[RealRadiosPlugin] retrieving location: {0}", play_url + url);
                url = RetrieveHtml (play_url + url);
            }

            Log.DebugFormat ("[RealRadiosPlugin] final location: {0}", url);

            try
            {
                return new SafeUri (url);
            }
            catch (Exception e) {
                Log.DebugFormat ("[RealRadiosPlugin]<ParseSearchResult> URL Parse Error {0}", e.Message);
                RaiseErrorReturned ("General Error", e.Message);
                return null;
            }
        }

        /// <summary>
        /// Retrieves, reads and returns an HTML document from the specified query url using HTTP GET
        /// </summary>
        /// <param name="query">
        /// A <see cref="System.String"/> -- a URL with full query parameters
        /// </param>
        /// <returns>
        /// A <see cref="System.String"/> -- the retrieved and loaded XML document
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
                Log.DebugFormat ("[RealRadiosPlugin\"{0}\"] <RetrieveHtml> Error: {1} END", Name, e.Message);
                RaiseErrorReturned ("General Error", e.Message);
            }
            return null;
        }

        /// <summary>
        /// The name of the plugin -- used as identifier and as label for the source header
        /// </summary>
        public override string Name {
            get { return "RealRadios.com"; }
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
        "plugins.liveradio.realradios" , "use_proxy", "", "whether to use proxy for HTTP", "whether to use proxy for HTTP");

        public static readonly SchemaEntry<string> ProxyUrlEntry = new SchemaEntry<string> (
        "plugins.liveradio.realradios", "proxy_url", "", "HTTP proxy url", "HTTP proxy url");

        public static readonly SchemaEntry<string> HttpTimeoutEntry = new SchemaEntry<string> (
        "plugins.liveradio.realradios", "http_timeout_seconds", "", "HTTP timeout", "HTTP timeout");


    }

}
