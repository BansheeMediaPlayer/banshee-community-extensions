
using System;
using System.Net;
using System.Xml;
using System.IO;
using System.Collections.Generic;

using Banshee.Base;
using Banshee.Database;
using Banshee.Collection.Database;
using Banshee.Collection;

using Hyena;
using System.Text;

namespace Banshee.LiveRadio.Plugins
{

    public class XiphOrgPlugin : LiveRadioBasePlugin
    {
        private const string base_url = "http://dir.xiph.org";
        //private const string base_url = "file:///home/dingsi";
        private const string catalog_url = "/yp.xml";
        //private List<DatabaseTrackInfo> stations;

        public XiphOrgPlugin ()
        {
            use_proxy = true;
            proxy_url = "http://213.203.241.210:80";
            //stations = new List<DatabaseTrackInfo> ();
        }

        protected override void RetrieveGenres ()
        {
            RetrieveCatalog ();
        }

        protected override void RetrieveRequest(LiveRadioRequestType request_type, string query)
        {
            string key;
            if (request_type == LiveRadioRequestType.ByGenre)
            {
                key = "Genre:" + query;
                if (!cached_results.ContainsKey(key))
                {
                    cached_results[key] = new List<DatabaseTrackInfo> ();
                }
            }
            if (request_type == LiveRadioRequestType.ByFreetext)
            {
                key = query;
                if (!cached_results.ContainsKey(key))
                {
                    List<DatabaseTrackInfo> newlist = new List<DatabaseTrackInfo> ();
                    foreach (KeyValuePair<string, List<DatabaseTrackInfo>> entry in cached_results)
                    {
                        newlist.AddRange(entry.Value.FindAll(delegate (DatabaseTrackInfo track) { return QueryString(track,query); }));
                    }
                    cached_results.Add(key, newlist);
                }
            }
        }

        private static bool QueryString(DatabaseTrackInfo track, string query)
        {
            StringBuilder sb = new StringBuilder ();
            sb.Append (track.TrackTitle);
            sb.Append (track.Genre);
            sb.Append (track.Comment);
            if (sb.ToString ().Contains (query))
                return true;
            return false;
        }

        public override string Name
        {
            get { return "xiph.org"; }
        }

        protected void ParseCatalog (XmlDocument doc)
        {
            Log.Debug ("[XiphOrgPlugin] <ParseCatalog> START");

            XmlNodeList XML_station_nodes = doc.GetElementsByTagName ("entry");
            Log.DebugFormat ("[XiphOrgPlugin] <ParseCatalog> {0} nodes found", XML_station_nodes.Count);

            List<string> new_genres = new List<string> ();

            foreach (XmlNode node in XML_station_nodes)
            {
                XmlNodeList xml_attributes = node.ChildNodes;

                try {
                    string name = "";
                    string URI = "";
                    string media_type = "";
                    string genre = "";
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
                            genre = station_attributes.InnerText;
                        else if (station_attributes.Name.Equals ("current_song"))
                            now_playing = station_attributes.InnerText;
                        else if (station_attributes.Name.Equals ("bitrate"))
                            bitrate = station_attributes.InnerText;
                    }                           
                    
                    DatabaseTrackInfo new_station = new DatabaseTrackInfo ();

                    new_station.Uri = new SafeUri (URI);
                    new_station.ArtistName = Name;
                    new_station.Genre = genre;
                    new_station.TrackTitle = name;
                    new_station.Comment = now_playing;
                    new_station.AlbumTitle = now_playing;
                    new_station.MimeType = media_type;
                    new_station.IsLive = true;
                    Int32.TryParse (bitrate.Trim (), out bitrate_int);
                    new_station.BitRate = bitrate_int;


                    if (!new_genres.Contains(genre))
                    {
                        new_genres.Add(genre);
                        cached_results.Add(genre, new List<DatabaseTrackInfo> ());
                    }
                    cached_results[genre].Add (new_station);

                }
                catch (Exception ex) {
                    Log.Exception ("[XiphOrgPlugin] <ParseCatalog> ERROR", ex);
                    continue;
                }

            }

            genres = new_genres;

            Log.DebugFormat ("[XiphOrgPlugin] <ParseCatalog> {0} genres found", genres.Count);

        }

        protected void RetrieveCatalog ()
        {
            WebProxy proxy;
            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create (base_url + catalog_url);
            request.Method = "GET";
            request.ContentType = "HTTP/1.0";
            request.Timeout = 60 * 1000; // 10 seconds
            if (use_proxy)
            {
                proxy = new WebProxy(proxy_url,true);
                request.Proxy = proxy;
            }

            try
            {
                Log.Debug ("[XiphOrgPlugin] <RetrieveCatalog> pulling catalog");

                Stream response = request.GetResponse().GetResponseStream ();
                StreamReader reader = new StreamReader (response);

                XmlDocument xml_response = new XmlDocument ();
                xml_response.LoadXml (reader.ReadToEnd ());

                Log.Debug ("[XiphOrgPlugin] <RetrieveCatalog> catalog retrieved");

                ParseCatalog(xml_response);
            }
            finally {
                Log.Debug ("[XiphOrgPlugin] <RetrieveCatalog> End");
            }
        }

    }

    /*public class Xiph : FetcherDialog, IFreetextSearchable, IGenreSearchable
    {
        List<DatabaseTrackInfo> station_list = new List<DatabaseTrackInfo>();
        bool stations_fetched = false;

        public Xiph ()
        {
            source_name = "www.xiph.org";
            InitializeDialog ();
        }
        
        public override void ShowDialog () 
        {
            Banshee.Kernel.Scheduler.Schedule (new DelegateJob (FetchStations));
            base.ShowDialog ();
        }
        
        public override void FillGenreList () 
        {
        }


        public List<DatabaseTrackInfo> FetchStationsByGenre (string genre)
        {
            if (!stations_fetched) {
                FetchStations ();
            }

            if (station_list == null) {
                return null;
            }
            
            return station_list.FindAll (delegate (DatabaseTrackInfo station) 
                {
                    if (station.Genre.ToLower ().Trim ().Equals (genre.ToLower ().Trim ()))
                        return true;
                    
                    return false;
                } );
        }

        public List<DatabaseTrackInfo> FetchStationsByFreetext (string text) 
        {
            if (!stations_fetched) {
                FetchStations ();
            }
            
            if (station_list == null) {
                return null;
            }
            
            return station_list.FindAll (delegate (DatabaseTrackInfo station) 
                {
                    if (station.ArtistName.ToLower ().Trim ().Contains (text.ToLower ().Trim ()))
                        return true;
                    if (station.Genre.ToLower ().Trim ().Contains (text.ToLower ().Trim ()))
                        return true;
                    if (station.TrackTitle.ToLower ().Trim ().Contains (text.ToLower ().Trim ()))
                        return true;
                    if (station.Comment.ToLower ().Trim ().Contains (text.ToLower ().Trim ()))
                        return true;
                    if (station.AlbumTitle.ToLower ().Trim ().Contains (text.ToLower ().Trim ()))
                        return true;
                    if (station.MimeType.ToLower ().Trim ().Contains (text.ToLower ().Trim ()))
                        return true;
                    
                    return false;
                } );
        }
         
        public void FetchStations () 
        {
            Log.Debug ("[Xiph] <FetchStations> Start");
        
            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create ("http://dir.xiph.org/yp.xml");
            request.Method = "GET";
            request.ContentType = "HTTP/1.0";
            request.Timeout = 10 * 1000; // 10 seconds
            
            try
            {
                if (GetInternetRadioSource () == null) {
                    throw new InternetRadioExtensionNotFoundException ();
                }

                Log.Debug ("[Xiph] <FetchStations> Querying");

                Stream response = request.GetResponse().GetResponseStream ();
                StreamReader reader = new StreamReader (response);

                XmlDocument xml_response = new XmlDocument ();
                xml_response.LoadXml (reader.ReadToEnd ());

                Log.Debug ("[Xiph] <FetchStations> Query done");

                ParseQuery (xml_response);
            }
            finally {
                Log.Debug ("[Xiph] <FetchStations> End");
            }
        }

        public void ParseQuery (XmlDocument xml_response)
        {
            Log.Debug ("[Xiph] <ParseQuery> Start");
            
            XmlNodeList XML_station_nodes = xml_response.GetElementsByTagName ("entry");
            Log.DebugFormat ("[Xiph] <ParseQuery> Num stations found: {0}", XML_station_nodes.Count);
            
            PrimarySource source = GetInternetRadioSource ();
            
            if (source == null) {
                throw new InternetRadioExtensionNotFoundException ();
            }
            
            foreach (XmlNode node in XML_station_nodes)
            {
                XmlNodeList xml_attributes = node.ChildNodes;

                try {
                    string name = "";
                    string URI = "";
                    string media_type = "";
                    string genre = "";
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
                            genre = station_attributes.InnerText;
                        else if (station_attributes.Name.Equals ("current_song"))
                            now_playing = station_attributes.InnerText;
                        else if (station_attributes.Name.Equals ("bitrate"))
                            bitrate = station_attributes.InnerText;
                    }                           
                    
                    DatabaseTrackInfo new_station = new DatabaseTrackInfo ();
    
                    new_station.Uri = new SafeUri (URI);
                    new_station.ArtistName = "www.xiph.org";
                    new_station.Genre = genre;
                    new_station.TrackTitle = name;
                    new_station.Comment = now_playing;
                    new_station.AlbumTitle = now_playing;
                    new_station.MimeType = media_type;
                    new_station.PrimarySource = source;
                    new_station.IsLive = true;
                    Int32.TryParse (bitrate.Trim (), out bitrate_int);                    
                    new_station.BitRate = bitrate_int;
                    
                    Log.DebugFormat ("[Xiph] <ParseQuery> Station found! Name: {0} URL: {1}",
                        name, new_station.Uri.ToString ());
                    
                    station_list.Add (new_station);
                }
                catch (Exception e) {
                    Log.Exception ("[Xiph] <ParseQuery> ERROR", e);
                    continue;
                }
            }
            
            Log.Debug ("[Xiph] <ParseQuery> END");
            
            SetStatusBarMessage (String.Format (AddinManager.CurrentLocalizer.GetString ("www.xiph.org {0} stations available."), 
                station_list.Count.ToString ()));
            stations_fetched = true;
        }
    }*/

}
