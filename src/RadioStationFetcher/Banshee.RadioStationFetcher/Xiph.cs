//
// Xiph.cs
//
// Author:
//   Akseli Mantila <aksu@paju.oulu.fi>
//
// Copyright (C) 2009 Akseli Mantila
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
using System.Net;
using System.Collections.Generic;

using Mono.Addins;

using Banshee.Kernel;
using Banshee.Collection.Database;
using Banshee.Sources;

using Hyena;

namespace Banshee.RadioStationFetcher
{
    public class Xiph : FetcherDialog, IFreetextSearchable, IGenreSearchable
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
            genre_list.Add ("Alternative");
            genre_list.Add ("Indie");
            genre_list.Add ("Goth");
            genre_list.Add ("College");
            genre_list.Add ("Industrial");
            genre_list.Add ("Punk");
            genre_list.Add ("Hardcore");
            genre_list.Add ("Ska");
            genre_list.Add ("Classical");
            genre_list.Add ("Opera");
            genre_list.Add ("Symphonic");
            genre_list.Add ("Country");
            genre_list.Add ("Swing");
            genre_list.Add ("Electronic");
            genre_list.Add ("Ambient");
            genre_list.Add ("Drum&Bass"); // TODO: Test this one for the sake of '&'
            genre_list.Add ("Trance");
            genre_list.Add ("Techno");
            genre_list.Add ("House");
            genre_list.Add ("Downtempo");
            genre_list.Add ("Breakbeat");
            genre_list.Add ("Jungle");
            genre_list.Add ("Garage");
            genre_list.Add ("Jazz");
            genre_list.Add ("Swing");
            genre_list.Add ("Big");
            genre_list.Add ("Hip hop");
            genre_list.Add ("Rap");
            genre_list.Add ("Turntabl");
            genre_list.Add ("Old school");
            genre_list.Add ("New school");
            genre_list.Add ("Oldies");
            genre_list.Add ("Disco");
            genre_list.Add ("50s");
            genre_list.Add ("60s");
            genre_list.Add ("70s");
            genre_list.Add ("80s");
            genre_list.Add ("90s");
            genre_list.Add ("Pop");
            genre_list.Add ("Rock");
            genre_list.Add ("Top 40");
            genre_list.Add ("Metal");
            genre_list.Add ("Funk");
            genre_list.Add ("Soul");
            genre_list.Add ("Urban");
            genre_list.Add ("Spiritual");
            genre_list.Add ("Gospel");
            genre_list.Add ("Christian");
            genre_list.Add ("Muslim");
            genre_list.Add ("Jewish");
            genre_list.Add ("Religion");
            genre_list.Add ("Spoken");
            genre_list.Add ("Talk");
            genre_list.Add ("Comedy");
            genre_list.Add ("World");
            genre_list.Add ("Reggae");
            genre_list.Add ("Island");
            genre_list.Add ("African");
            genre_list.Add ("European");
            genre_list.Add ("Middle east");
            genre_list.Add ("Asia");
            genre_list.Add ("Various");
            genre_list.Add ("Mixed");
            genre_list.Add ("Misc");
            genre_list.Add ("Eclectic");
            genre_list.Add ("Film");
            genre_list.Add ("Show");
            genre_list.Add ("Instrumental");

            genre_list.Sort ();
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
    }
}
