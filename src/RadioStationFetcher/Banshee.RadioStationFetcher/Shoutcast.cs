//
// Shoutcast.cs
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

using Banshee.Collection.Database;
using Banshee.Sources;

using Hyena;

namespace Banshee.RadioStationFetcher
{
    public class Shoutcast : FetcherDialog, IGenreSearchable, IFreetextSearchable
    {
        public Shoutcast ()
        {
            source_name = "www.shoutcast.com";
            InitializeDialog ();
        }

        public override void FillGenreList ()
        {
            genre_list.Add ("Blues");
            genre_list.Add ("Classic Rock");
            genre_list.Add ("Country");
            genre_list.Add ("Dance");
            genre_list.Add ("Disco");
            genre_list.Add ("Funk");
            genre_list.Add ("Grunge");
            genre_list.Add ("Hip-Hop");
            genre_list.Add ("Jazz");
            genre_list.Add ("Metal");
            genre_list.Add ("New Age");
            genre_list.Add ("Oldies");
            genre_list.Add ("Other");
            genre_list.Add ("Pop");
            genre_list.Add ("R&B");
            genre_list.Add ("Rap");
            genre_list.Add ("Reggae");
            genre_list.Add ("Rock");
            genre_list.Add ("Techno");
            genre_list.Add ("Industrial");
            genre_list.Add ("Alternative");
            genre_list.Add ("Ska");
            genre_list.Add ("Death Metal");
            genre_list.Add ("Pranks");
            genre_list.Add ("Soundtrack");
            genre_list.Add ("Euro-Techno");
            genre_list.Add ("Ambient");
            genre_list.Add ("Trip-Hop");
            genre_list.Add ("Vocal");
            genre_list.Add ("Jazz+Funk");
            genre_list.Add ("Fusion");
            genre_list.Add ("Trance");
            genre_list.Add ("Classical");
            genre_list.Add ("Instrumental");
            genre_list.Add ("Acid");
            genre_list.Add ("House");
            genre_list.Add ("Game");
            genre_list.Add ("Sound Clip");
            genre_list.Add ("Gospel");
            genre_list.Add ("Noise");
            genre_list.Add ("AlternRock");
            genre_list.Add ("Bass");
            genre_list.Add ("Soul");
            genre_list.Add ("Punk");
            genre_list.Add ("Space");
            genre_list.Add ("Meditative");
            genre_list.Add ("Instrumental Pop");
            genre_list.Add ("Instrumental Rock");
            genre_list.Add ("Ethnic");
            genre_list.Add ("Gothic");
            genre_list.Add ("Darkwave");
            genre_list.Add ("Techno-Industrial");
            genre_list.Add ("Electronic");
            genre_list.Add ("Pop-Folk");
            genre_list.Add ("Eurodance");
            genre_list.Add ("Dream");
            genre_list.Add ("Southern Rock");
            genre_list.Add ("Comedy");
            genre_list.Add ("Cult");
            genre_list.Add ("Gangsta");
            genre_list.Add ("Top 40");
            genre_list.Add ("Christian Rap");
            genre_list.Add ("Pop/Funk");
            genre_list.Add ("Jungle");
            genre_list.Add ("Native American");
            genre_list.Add ("Cabaret");
            genre_list.Add ("New Wave");
            genre_list.Add ("Psychedelic");
            genre_list.Add ("Rave");
            genre_list.Add ("Showtunes");
            genre_list.Add ("Trailer");
            genre_list.Add ("Lo-Fi");
            genre_list.Add ("Tribal");
            genre_list.Add ("Acid Punk");
            genre_list.Add ("Acid Jazz");
            genre_list.Add ("Polka");
            genre_list.Add ("Retro");
            genre_list.Add ("Musical");
            genre_list.Add ("Rock & Roll");
            genre_list.Add ("Hard Rock");
            genre_list.Add ("Folk");
            genre_list.Add ("Folk-Rock");
            genre_list.Add ("National Folk");
            genre_list.Add ("Swing");
            genre_list.Add ("Fast Fusion");
            genre_list.Add ("Bebob");
            genre_list.Add ("Latin");
            genre_list.Add ("Revival");
            genre_list.Add ("Celtic");
            genre_list.Add ("Bluegrass");
            genre_list.Add ("Avantgarde");
            genre_list.Add ("Gothic Rock");
            genre_list.Add ("Progressive Rock");
            genre_list.Add ("Psychedelic Rock");
            genre_list.Add ("Symphonic Rock");
            genre_list.Add ("Slow Rock");
            genre_list.Add ("Big Band");
            genre_list.Add ("Chorus");
            genre_list.Add ("Easy Listening");
            genre_list.Add ("Acoustic");
            genre_list.Add ("Humour");
            genre_list.Add ("Speech");
            genre_list.Add ("Chanson");
            genre_list.Add ("Opera");
            genre_list.Add ("Chamber Music");
            genre_list.Add ("Sonata");
            genre_list.Add ("Symphony");
            genre_list.Add ("Booty Bass");
            genre_list.Add ("Primus");
            genre_list.Add ("Porn Groove");
            genre_list.Add ("Satire");
            genre_list.Add ("Slow Jam");
            genre_list.Add ("Club");
            genre_list.Add ("Tango");
            genre_list.Add ("Samba");
            genre_list.Add ("Folklore");
            genre_list.Add ("Ballad");
            genre_list.Add ("Power Ballad");
            genre_list.Add ("Rhythmic Soul");
            genre_list.Add ("Freestyle");
            genre_list.Add ("Duet");
            genre_list.Add ("Punk Rock");
            genre_list.Add ("Drum Solo");
            genre_list.Add ("A capella");
            genre_list.Add ("Euro-House");
            genre_list.Add ("Dance Hall");

            genre_list.Sort ();
        }

        public List<DatabaseTrackInfo> FetchStationsByGenre (string genre)
        {
            Log.Debug ("[Shoutcast] <FetchStationsByGenre> Start");

            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.
                Create ("http://207.200.98.1/sbin/newxml.phtml?genre="+genre);
            request.Method = "GET";
            request.ContentType = "HTTP/1.0";
            request.Timeout = 10 * 1000; // 10 seconds

            try
            {
                Log.DebugFormat ("[Shoutcast] <FetchStationsByGenre> Querying genre \"{0}\" ...", Genre);

                Stream response = request.GetResponse ().GetResponseStream ();
                StreamReader reader = new StreamReader (response);

                XmlDocument xml_response = new XmlDocument ();
                xml_response.LoadXml (reader.ReadToEnd ());

                Log.Debug ("[Shoutcast] <FetchStationsByGenre> Query done");

                return ParseQuery (xml_response);
            }
            finally {
                Log.Debug ("[Shoutcast] <FetchStationsByGenre> End");
            }
        }

        public List<DatabaseTrackInfo> FetchStationsByFreetext (string text)
        {
            Log.Debug ("[Shoutcast] <FetchStationsByFreetext> Start");

            HttpWebRequest request = (HttpWebRequest) HttpWebRequest.
                Create ("http://207.200.98.1/sbin/newxml.phtml?search="+text);
            request.Method = "GET";
            request.ContentType = "HTTP/1.0";
            request.Timeout = 10 * 1000; // 10 seconds

            try
            {
                Log.DebugFormat ("[Shoutcast] <FetchStationsByFreetext> Querying freetext \"{0}\" ...", Freetext);

                Stream response = request.GetResponse ().GetResponseStream ();
                StreamReader reader = new StreamReader (response);

                XmlDocument xml_response = new XmlDocument();
                xml_response.LoadXml (reader.ReadToEnd ());

                Log.Debug ("[Shoutcast] <FetchStationsByFreetext> Query done");

                return ParseQuery (xml_response);
            }
            finally {
                Log.Debug ("[Shoutcast] <FetchStationsByFreetext> End");
            }
        }


        public List<DatabaseTrackInfo> ParseQuery (XmlDocument xml_response)
        {
            Log.Debug ("[Shoutcast] <ParseQuery> Start");

            List<DatabaseTrackInfo> station_list;
            XmlNodeList XML_station_nodes = xml_response.GetElementsByTagName ("station");

            station_list = new List<DatabaseTrackInfo> (XML_station_nodes.Count);

            PrimarySource source = GetInternetRadioSource (); // If not found, throws exception caught in upper level.

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

                    new_station.Uri = new SafeUri ("http://207.200.98.1/sbin/tunein-station.pls?id="+id);
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

                    Log.DebugFormat ("[Shoutcast] <ParseQuery> Station found! Name: {0} URL: {1}",
                        name, new_station.Uri.ToString ());

                    station_list.Add (new_station);
                }
                catch (Exception e) {
                    Log.Exception ("[Shoutcast] <ParseQuery> ERROR: ", e);
                    continue;
                }
            }

            Log.Debug ("[Shoutcast] <ParseQuery> End");
            return station_list;
        }


    }
}
