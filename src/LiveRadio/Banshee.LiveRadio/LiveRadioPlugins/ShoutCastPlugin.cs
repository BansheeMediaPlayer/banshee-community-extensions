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
using System.IO;
using System.Text;
using System.Xml;
using System.Collections.Generic;

using Banshee.Base;
using Banshee.Collection.Database;

using Hyena;
using Banshee.Configuration;

namespace Banshee.LiveRadio.Plugins
{


    public class ShoutCastPlugin : LiveRadioBasePlugin
    {

        private const string base_url = "http://www.shoutcast.com";
        private const string request_url = "/sbin/newxml.phtml";
        private const string genre_request = "?genre=";
        private const string freetext_request = "?search=";

        public ShoutCastPlugin () : base ()
        {
            use_proxy = UseProxyEntry.Get ().Equals ("True") ? true : false;
            use_credentials = UseCredentialsEntry.Get ().Equals ("True") ? true : false;

            if (!Int32.TryParse(HttpTimeoutEntry.Get (), out http_timeout_seconds))
                http_timeout_seconds = 20;
            credentials_username = HttpUsernameEntry.Get ();
            credentials_password = HttpPasswordEntry.Get ();
            proxy_url = ProxyUrlEntry.Get ();
        }

        protected override void RetrieveGenres ()
        {
            ParseGenres(RetrieveXml(base_url + request_url));
        }

        protected override void RetrieveRequest (LiveRadioRequestType request_type, string query)
        {
            string request;
            if (request_type == LiveRadioRequestType.ByGenre) {
                request = base_url + request_url + genre_request + query;
            } else {
                request = base_url + request_url + freetext_request + query;
            }
            XmlDocument document = RetrieveXml(request);
            Log.Debug ("[ShoutCastPlugin] <RetrieveRequest> Start Parsing");
            if (document != null) ParseXmlResponse(document, request_type, query);
        }

        private void ParseGenres(XmlDocument doc)
        {
            Log.Debug ("[ShoutCastPlugin] <ParseGenres> START");
            
            XmlNodeList XML_genre_nodes = doc.GetElementsByTagName ("genre");
            Log.DebugFormat ("[ShoutCastPlugin] <ParseGenres> {0} nodes found", XML_genre_nodes.Count);

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
                    continue;
                }
                
            }
            
            new_genres.Sort ();
            genres = new_genres;

            Log.DebugFormat ("[ShoutCastPlugin] <ParseGenres> {0} genres found", genres.Count);
        }

        private void ParseXmlResponse (XmlDocument xml_response, LiveRadioRequestType request_type, string query)
        {
            Log.Debug ("[ShoutCastPlugin] <ParseXmlResponse> Start");

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
                    return;
                }
            }

            Log.Debug ("[ShoutCastPlugin] <ParseXmlResponse> analyzing stations");

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
    
                    new_station.Uri = new SafeUri (base_url + tunein_url + "?" + id);
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

                    //Log.DebugFormat ("[ShoutCastPlugin] <ParseXmlResponse> Station found! Name: {0} URL: {1}",
                    //    name, new_station.Uri.ToString ());

                    cached_results[key].Add (new_station);
                }
                catch (Exception e) {
                    Log.Exception ("[ShoutCastPlugin] <ParseXmlResponse> ERROR: ", e);
                    continue;
                }
            }

            Log.Debug ("[ShoutCastPlugin] <ParseXmlResponse> End");
        }

        public override string Name {
            get { return "SHOUTcast.com"; }
        }
        public override void SaveConfiguration ()
        {
            if (configuration_widget == null) return;
            http_timeout_seconds = configuration_widget.HttpTimeout;
            credentials_password = configuration_widget.HttpPassword;
            credentials_username = configuration_widget.HttpUsername;
            proxy_url = configuration_widget.ProxyUrl;
            use_credentials = configuration_widget.UseCredentials;
            use_proxy = configuration_widget.UseProxy;
            HttpTimeoutEntry.Set (http_timeout_seconds.ToString ());
            HttpPasswordEntry.Set (credentials_password);
            HttpUsernameEntry.Set (credentials_username);
            ProxyUrlEntry.Set (proxy_url);
            UseCredentialsEntry.Set (use_credentials.ToString ());
            UseProxyEntry.Set (use_proxy.ToString ());
        }

        public static readonly SchemaEntry<string> UseProxyEntry = new SchemaEntry<string> (
        "plugins.liveradio.shoutcast" , "use_proxy", "", "whether to use proxy for HTTP", "whether to use proxy for HTTP");

        public static readonly SchemaEntry<string> ProxyUrlEntry = new SchemaEntry<string> (
        "plugins.liveradio.shoutcast", "proxy_url", "", "HTTP proxy url", "HTTP proxy url");

        public static readonly SchemaEntry<string> UseCredentialsEntry = new SchemaEntry<string> (
        "plugins.liveradio.shoutcast", "use_credentials", "", "whether to use credentials authentification", "whether to use credentials authentification");

        public static readonly SchemaEntry<string> HttpUsernameEntry = new SchemaEntry<string> (
        "plugins.liveradio.shoutcast", "credentials_username", "", "HTTP username", "HTTP username");

        public static readonly SchemaEntry<string> HttpPasswordEntry = new SchemaEntry<string> (
        "plugins.liveradio.shoutcast", "credentials_password", "", "HTTP password", "HTTP password");

        public static readonly SchemaEntry<string> HttpTimeoutEntry = new SchemaEntry<string> (
        "plugins.liveradio.shoutcast", "http_timeout_seconds", "", "HTTP timeout", "HTTP timeout");


    }
    
}
