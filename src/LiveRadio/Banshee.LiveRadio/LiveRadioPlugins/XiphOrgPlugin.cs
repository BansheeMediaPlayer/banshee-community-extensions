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
using System.Net;
using System.IO;
using System.Xml;
using System.Collections.Generic;

using Banshee.Base;
using Banshee.Collection.Database;

using Hyena;
using System.Text;
using Banshee.Configuration;

namespace Banshee.LiveRadio.Plugins
{

    public class XiphOrgPlugin : LiveRadioBasePlugin
    {
        private const string base_url = "http://dir.xiph.org";
        private const string catalog_url = "/yp.xml";

        public XiphOrgPlugin () : base ()
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
            ParseCatalog (RetrieveXml(base_url + catalog_url));
        }

        protected override void RetrieveRequest (LiveRadioRequestType request_type, string query)
        {
            string key;
            if (request_type == LiveRadioRequestType.ByGenre) {
                key = "Genre:" + query;
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

        public override string Name {
            get { return "xiph.org"; }
        }

        protected void ParseCatalog (XmlDocument doc)
        {
            Log.Debug ("[XiphOrgPlugin] <ParseCatalog> START");
            
            XmlNodeList XML_station_nodes = doc.GetElementsByTagName ("entry");
            Log.DebugFormat ("[XiphOrgPlugin] <ParseCatalog> {0} nodes found", XML_station_nodes.Count);
            
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
                        cached_results.Add ("Genre:" + genre, new List<DatabaseTrackInfo> ());
                    }
                    cached_results["Genre:" + genre].Add (new_station);
                    
                } catch (Exception ex) {
                    Log.Exception ("[XiphOrgPlugin] <ParseCatalog> ERROR", ex);
                    continue;
                }
                
            }
            
            new_genres.Sort ();
            genres = new_genres;
            
            Log.DebugFormat ("[XiphOrgPlugin] <ParseCatalog> {0} genres found", genres.Count);
            
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
        "plugins.liveradio.xiph" , "use_proxy", "", "whether to use proxy for HTTP", "whether to use proxy for HTTP");

        public static readonly SchemaEntry<string> ProxyUrlEntry = new SchemaEntry<string> (
        "plugins.liveradio.xiph", "proxy_url", "", "HTTP proxy url", "HTTP proxy url");

        public static readonly SchemaEntry<string> UseCredentialsEntry = new SchemaEntry<string> (
        "plugins.liveradio.xiph", "use_credentials", "", "whether to use credentials authentification", "whether to use credentials authentification");

        public static readonly SchemaEntry<string> HttpUsernameEntry = new SchemaEntry<string> (
        "plugins.liveradio.xiph", "credentials_username", "", "HTTP username", "HTTP username");

        public static readonly SchemaEntry<string> HttpPasswordEntry = new SchemaEntry<string> (
        "plugins.liveradio.xiph", "credentials_password", "", "HTTP password", "HTTP password");

        public static readonly SchemaEntry<string> HttpTimeoutEntry = new SchemaEntry<string> (
        "plugins.liveradio.xiph", "http_timeout_seconds", "", "HTTP timeout", "HTTP timeout");

    }
    
}
