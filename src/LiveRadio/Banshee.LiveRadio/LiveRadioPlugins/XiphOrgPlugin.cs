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

namespace Banshee.LiveRadio.Plugins
{

    public class XiphOrgPlugin : LiveRadioBasePlugin
    {
        private const string base_url = "http://dir.xiph.org";
        private const string catalog_url = "/yp.xml";

        public XiphOrgPlugin () : base ()
        {
            use_proxy = true;
            proxy_url = "http://213.203.241.210:80";
        }

        protected override void RetrieveGenres ()
        {
            RetrieveCatalog ();
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

        protected void RetrieveCatalog ()
        {
            WebProxy proxy;
            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create (base_url + catalog_url);
            request.Method = "GET";
            request.ContentType = "HTTP/1.0";
            request.Timeout = 60 * 1000;
            if (use_proxy) {
                proxy = new WebProxy (proxy_url, true);
                request.Proxy = proxy;
            }
            
            try {
                Log.Debug ("[XiphOrgPlugin] <RetrieveCatalog> pulling catalog");
                
                Stream response = request.GetResponse ().GetResponseStream ();
                StreamReader reader = new StreamReader (response);
                
                XmlDocument xml_response = new XmlDocument ();
                xml_response.LoadXml (reader.ReadToEnd ());
                
                Log.Debug ("[XiphOrgPlugin] <RetrieveCatalog> catalog retrieved");
                
                ParseCatalog (xml_response);
            } finally {
                Log.Debug ("[XiphOrgPlugin] <RetrieveCatalog> End");
            }
        }
        
    }
    
}
