//
// ShoutCastPlugin.cs
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

namespace Banshee.LiveRadio.Plugins
{

    /// <summary>
    /// LiveRadio plugin for shoutcast.com
    ///
    /// This plugin is able to download a genre list upon initialize/refresh and execute live queries on the shoutcast directory
    /// </summary>
    public class ShoutCastPlugin : LiveRadioBasePlugin
    {

        private const string base_url = "http://207.200.98.1";
        private const string play_url = "http://207.200.98.1";
        private const string request_url = "/sbin/newxml.phtml?";
        private const string genre_list_request = "genrelist";
        private const string genre_request = "&genre=";
        private const string freetext_request = "&search=";

        /// <summary>
        /// Shoutcast plugin disabled for legal issues, no developer key available
        /// </summary>
        public override bool Active {
            get { return true; }
        }

        /// <summary>
        /// Constructor -- sets configuration entries
        /// </summary>
        public ShoutCastPlugin () : base ()
        {
            use_proxy = UseProxyEntry.Get ().Equals ("True") ? true : false;

            if (!Int32.TryParse(HttpTimeoutEntry.Get (), out http_timeout_seconds))
                http_timeout_seconds = 20;
            proxy_url = ProxyUrlEntry.Get ();
            SetWebIcon ("http://o.aolcdn.com/shoutcast/images/sc_favicon.ico");
        }

        /// <summary>
        /// Retrieve and parse genre list
        /// </summary>
        protected override void RetrieveGenres ()
        {
            ParseGenres(RetrieveXml(base_url + request_url + genre_list_request));
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
            string request;
            if (request_type == LiveRadioRequestType.ByGenre) {
                request = base_url + request_url + genre_request + query;
            } else {
                request = base_url + request_url + freetext_request + query;
            }
            XmlDocument document = RetrieveXml(request);
            if (document != null) ParseXmlResponse(document, request_type, query);
        }

        /// <summary>
        /// Parses and sorts an XML genre catalog and fills the plugins genre list
        /// </summary>
        /// <param name="doc">
        /// A <see cref="XmlDocument"/> -- the XML document containing the genre catalog
        /// </param>
        private void ParseGenres(XmlDocument doc)
        {
            XmlNodeList XML_genre_nodes = doc.GetElementsByTagName ("genre");

            List<Genre> new_genres = new List<Genre> ();

            foreach (XmlNode node in XML_genre_nodes) {
                XmlAttributeCollection xml_attributes = node.Attributes;

                try {
                    Genre genre = new Genre(xml_attributes.GetNamedItem ("name").InnerText);

                    if (!new_genres.Contains (genre)) {
                        new_genres.Add (genre);
                    }
                } catch (Exception ex) {
                    Log.Exception ("[ShoutCastPlugin] <ParseGenres> ERROR", ex);
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
        private void ParseXmlResponse (XmlDocument xml_response, LiveRadioRequestType request_type, string query)
        {
            string tunein_url = "";

            XmlNodeList XML_tunein_nodes = xml_response.GetElementsByTagName ("tunein");

            foreach (XmlNode node in XML_tunein_nodes)
            {
                XmlAttributeCollection xml_attributes = node.Attributes;
                try {
                    tunein_url = xml_attributes.GetNamedItem("base").InnerText;
                    break;
                }
                catch (Exception e) {
                    Log.Exception ("[ShoutCastPlugin] <ParseXmlResponse> ERROR: ", e);
                    RaiseErrorReturned ("XML Parse Error", e.Message);
                    return;
                }
            }

            XmlNodeList XML_station_nodes = xml_response.GetElementsByTagName ("station");

            string key;
            if (request_type == LiveRadioRequestType.ByGenre) {
                key = "Genre:" + query;
                if (!cached_results.ContainsKey (key)) {
                    cached_results[key] = new List<DatabaseTrackInfo> (XML_station_nodes.Count);
                }
            } else {
                key = query;
                if (!cached_results.ContainsKey (key)) {
                    cached_results[key] = new List<DatabaseTrackInfo> (XML_station_nodes.Count);
                }
            }

            cached_results[key].Clear ();

            foreach (XmlNode node in XML_station_nodes)
            {
                XmlAttributeCollection xml_attributes = node.Attributes;

                try {
                    string name = xml_attributes.GetNamedItem ("name").InnerText;
                    string media_type = xml_attributes.GetNamedItem ("mt").InnerText;
                    string id = xml_attributes.GetNamedItem ("id").InnerText;
                    string genre = xml_attributes.GetNamedItem ("genre").InnerText;
                    string now_playing = xml_attributes.GetNamedItem ("ct").InnerText;
                    string bitrate = xml_attributes.GetNamedItem ("br").InnerText;
                    int id_int;
                    int bitrate_int;

                    if (!Int32.TryParse (id.Trim (), out id_int)) {
                        continue; //Something wrong with id, skip this
                    }

                    DatabaseTrackInfo new_station = new DatabaseTrackInfo ();

                    new_station.Uri = new SafeUri (play_url + tunein_url + "?id=" + id);
                    new_station.ArtistName = "www.shoutcast.com";
                    new_station.Genre = genre;
                    new_station.TrackTitle = name;
                    new_station.Comment = now_playing;
                    new_station.AlbumTitle = now_playing;
                    new_station.MimeType = media_type;
                    new_station.ExternalId = id_int;
                    new_station.PrimarySource = source;
                    new_station.IsLive = true;
                    Int32.TryParse (bitrate.Trim (), out bitrate_int);
                    new_station.BitRate = bitrate_int;
                    new_station.IsLive = true;

                    cached_results[key].Add (new_station);
                }
                catch (Exception e) {
                    Log.Exception ("[ShoutCastPlugin] <ParseXmlResponse> ERROR: ", e);
                    RaiseErrorReturned ("XML Parse Error", e.Message);
                    continue;
                }
            }
        }

        /// <summary>
        /// The name of the plugin -- used as identifier and as label for the source header
        /// </summary>
        public override string Name {
            get { return "SHOUTcast.com"; }
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
