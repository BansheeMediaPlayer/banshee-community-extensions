//
// LiveRadioSource.cs
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
using System.Text;
using Banshee.Configuration;

namespace Banshee.LiveRadio.Plugins
{

    /// <summary>
    /// LiveRadio plugin to xiph.org internet radio directory
    ///
    /// This plugin downloads the catalog once when the genres are requested and builds a cache for all stations
    /// </summary>
    public class XiphOrgPlugin : LiveRadioBasePlugin
    {
        private const string base_url = "http://dir.xiph.org";
        private const string catalog_url = "/yp.xml";

        /// <summary>
        /// Constructor -- sets configuration entries
        /// </summary>
        public XiphOrgPlugin () : base ()
        {
            use_proxy = UseProxyEntry.Get ().Equals ("True") ? true : false;

            if (!Int32.TryParse(HttpTimeoutEntry.Get (), out http_timeout_seconds))
                http_timeout_seconds = 20;
            proxy_url = ProxyUrlEntry.Get ();
            SetWebIcon ("http://www.xiph.org/favicon.ico");
        }

        /// <summary>
        /// Retrieve and parse the catalog
        /// </summary>
        protected override void RetrieveGenres ()
        {
            ParseCatalog (RetrieveXml(base_url + catalog_url));
        }

        /// <summary>
        /// Lookup the query in the cached station track entries
        /// </summary>
        /// <param name="request_type">
        /// A <see cref="LiveRadioRequestType"/> -- the type of the request
        /// </param>
        /// <param name="query">
        /// A <see cref="System.String"/> -- the freetext query or the genre name
        /// </param>
        protected override void RetrieveRequest (LiveRadioRequestType request_type, string query)
        {
            string key;
            if (request_type == LiveRadioRequestType.ByGenre) {
                key = new Genre(query).GenreKey;
                if (!cached_results.ContainsKey (key)) {
                    cached_results.Add (key, new List<DatabaseTrackInfo> ());
                }
            }
            if (request_type == LiveRadioRequestType.ByFreetext) {
                key = query;
                if (!cached_results.ContainsKey (key)) {
                    List<DatabaseTrackInfo> newlist = new List<DatabaseTrackInfo> ();
                    foreach (KeyValuePair<string, List<DatabaseTrackInfo>> entry in cached_results) {
                        newlist.AddRange (entry.Value.FindAll (delegate (DatabaseTrackInfo track) { return QueryString (track, query); }));
                    }
                    cached_results.Add (key, newlist);
                }
            }
        }

        /// <summary>
        /// Checks if a track's metadata contains the user query
        /// </summary>
        /// <param name="track">
        /// A <see cref="DatabaseTrackInfo"/> -- the track to query
        /// </param>
        /// <param name="query">
        /// A <see cref="System.String"/> -- the user query
        /// </param>
        /// <returns>
        /// A <see cref="System.Boolean"/> -- the query result, true if the query is contained within the tracks metadata, false otherwise
        /// </returns>
        private static bool QueryString (DatabaseTrackInfo track, string query)
        {
            StringBuilder sb = new StringBuilder ();
            sb.Append (track.TrackTitle);
            sb.Append (" ");
            sb.Append (track.Genre);
            sb.Append (" ");
            sb.Append (track.Comment);
            if (sb.ToString ().ToLower ().Contains (query.ToLower ()))
                return true;
            return false;
        }

        /// <summary>
        /// The name of the plugin -- used as identifier and as label for the source header
        /// </summary>
        public override string Name {
            get { return "xiph.org"; }
        }

        /// <summary>
        /// Version of this plugin code
        /// </summary>
        public override string Version {
            get { return "0.1"; }
        }

        /// <summary>
        /// Parse the XML catalog and build the sorted genre list and track cache
        /// </summary>
        /// <param name="doc">
        /// A <see cref="XmlDocument"/> -- the XML document containing the xiph.org catalog
        /// </param>
        protected void ParseCatalog (XmlDocument doc)
        {
            XmlNodeList XML_station_nodes = doc.GetElementsByTagName ("entry");

            List<Genre> new_genres = new List<Genre> ();

            foreach (XmlNode node in XML_station_nodes) {
                XmlNodeList xml_attributes = node.ChildNodes;

                try {
                    string name = "";
                    string URI = "";
                    string media_type = "";
                    Genre genre = new Genre ();
                    string now_playing = "";
                    string bitrate = "";
                    int bitrate_int = 0;

                    foreach (XmlNode station_attributes in xml_attributes) {
                        if (station_attributes.Name.Equals ("server_name"))
                            name = station_attributes.InnerText;
                        else if (station_attributes.Name.Equals ("listen_url"))
                            URI = station_attributes.InnerText;
                        else if (station_attributes.Name.Equals ("server_type"))
                            media_type = station_attributes.InnerText;
                        else if (station_attributes.Name.Equals ("genre"))
                            genre.Name = station_attributes.InnerText;
                        else if (station_attributes.Name.Equals ("current_song"))
                            now_playing = station_attributes.InnerText;
                        else if (station_attributes.Name.Equals ("bitrate"))
                            bitrate = station_attributes.InnerText;
                   }

                    DatabaseTrackInfo new_station = new DatabaseTrackInfo ();

                    new_station.Uri = new SafeUri (URI);
                    new_station.ArtistName = Name;
                    new_station.Genre = genre.Name;
                    new_station.TrackTitle = name;
                    new_station.Comment = now_playing;
                    new_station.AlbumTitle = now_playing;
                    new_station.MimeType = media_type;
                    new_station.IsLive = true;
                    Int32.TryParse (bitrate.Trim (), out bitrate_int);
                    new_station.BitRate = bitrate_int;

                    if (!new_genres.Contains (genre)) {
                        new_genres.Add (genre);
                        if (cached_results.ContainsKey (genre.GenreKey))
                            cached_results[genre.GenreKey].Clear ();
                        else
                            cached_results.Add (genre.GenreKey, new List<DatabaseTrackInfo> ());
                    }
                    cached_results[genre.GenreKey].Add (new_station);
                } catch (Exception ex) {
                    Log.Exception ("[XiphOrgPlugin] <ParseCatalog> ERROR", ex);
                    RaiseErrorReturned ("XML Parse Error", ex.Message);
                    continue;
                }

            }

            new_genres.Sort ();
            genres = new_genres;
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
        "plugins.liveradio.xiph" , "use_proxy", "", "whether to use proxy for HTTP", "whether to use proxy for HTTP");

        public static readonly SchemaEntry<string> ProxyUrlEntry = new SchemaEntry<string> (
        "plugins.liveradio.xiph", "proxy_url", "", "HTTP proxy url", "HTTP proxy url");

        public static readonly SchemaEntry<string> HttpTimeoutEntry = new SchemaEntry<string> (
        "plugins.liveradio.xiph", "http_timeout_seconds", "", "HTTP timeout", "HTTP timeout");

    }

}
