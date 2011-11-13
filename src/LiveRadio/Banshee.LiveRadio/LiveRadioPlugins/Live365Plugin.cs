//
// Live365Plugin.cs
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

using Timer = System.Timers.Timer;

namespace Banshee.LiveRadio.Plugins
{

    /// <summary>
    /// LiveRadio plugin for live365.com
    ///
    /// This plugin is able to download a genre list upon initialize/refresh and execute live queries on the live365 directory.
    /// The plugin can configure and handle user login data of registered live365 users. Once added to the internet radio
    /// source, the user specifics for a track are lost, so this makes only sense for public tracks available to non-registered
    /// users
    /// </summary>
    public class Live365Plugin : LiveRadioBasePlugin
    {

        /* API urls:
         * /cgi-bin/api_account.cgi
         * /cgi-bin/api_bcinfo.cgi
         * /cgi-bin/api_broadcast.cgi
         * /cgi-bin/api_featured_content.cgi
         * /cgi-bin/api_genres.cgi
         * /cgi-bin/api_get_playlist.cgi
         * /cgi-bin/api_live.cgi
         * /cgi-bin/api_login.cgi
         * /cgi-bin/api_meetings.cgi
         * /cgi-bin/api_presets.cgi
         * /cgi-bin/api_profile.cgi
         * /cgi-bin/api_set_playlist.cgi
         * /cgi-bin/api_station_status.cgi
         * /cgi-bin/api_subscription.cgi
         * /cgi-bin/api_track_stats.cgi
         * /cgi-bin/api_validate.cgi
        */

        private const string base_url = "http://www.live365.com";
        private const string request_url = "/cgi-bin/directory.cgi?site=xml&org=live365&access=all&version=4&rows=200&charset=UTF-8";
        private const string genre_request = "&genre=";
        private const string freetext_request = "&searchdesc=";
        private const string genre_url = "/cgi-bin/api_genres.cgi?site=xml&org=live365&access=all&version=4&charset=UTF-8";

        private const string test_session_url = "http://www.live365.com/cgi-bin/api_presets.cgi?action=get&org=live365&first=1&device_id=UNKNOWN&rows=";
        private const string login_url = "http://www.live365.com/cgi-bin/api_login.cgi?site=xml&remeber=Y&org=live365&access=all&version=4&charset=UTF-8&membername=%USERNAME%&password=%PASSWORD%";
        private const string playlist_url = "http://www.live365.com/play/";
        //private const string playlist_url = "http://www.live365.com/cgi-bin/play.pls?stationid=";
        private const string replace_url = "http://www.live365.com/play/";

        private string session_id = null;

        private Timer keepalive_timer = null;
        private const int keepalive_timeout = 600000;

        /// <summary>
        /// Constructor -- invokes the base constructor with has_login=true for handling user credentials.
        /// Sets configured Properties
        /// </summary>
        public Live365Plugin () : base (true)
        {
            use_proxy = UseProxyEntry.Get ().Equals ("True") ? true : false;
            use_credentials = UseCredentialsEntry.Get ().Equals ("True") ? true : false;

            if (!Int32.TryParse(HttpTimeoutEntry.Get (), out http_timeout_seconds))
                http_timeout_seconds = 20;
            credentials_username = HttpUsernameEntry.Get ();
            credentials_password = HttpPasswordEntry.Get ();
            proxy_url = ProxyUrlEntry.Get ();
            SetWebIcon ("http://www.live365.com/favicon.ico");
        }

        /// <summary>
        /// Retrieve and parse genre list
        /// </summary>
        protected override void RetrieveGenres ()
        {
            ParseGenres(RetrieveXml(base_url + genre_url));
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
        /// Checks if the memorized user session is still valid
        /// </summary>
        /// <returns>
        /// A <see cref="System.Boolean"/> -- true, if the session is still valid
        /// </returns>
        private bool TestSession ()
        {
            if (String.IsNullOrEmpty (session_id)) return false;
            XmlDocument test_xml = RetrieveXml (test_session_url + session_id.Replace(":", "%3A").Replace("session_id","sessionid"));
            XmlNodeList test_nodes = test_xml.GetElementsByTagName ("Code");
            if (test_nodes.Count > 0 && !test_nodes[0].InnerText.Equals ("0"))
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Creates a new user session and saves the result in the private session_id member.
        /// </summary>
        private void CreateSession ()
        {
            string login = login_url.Replace ("%USERNAME%", credentials_username)
                                    .Replace ("%PASSWORD%", credentials_password);

            XmlDocument login_xml = RetrieveXml (login);

            bool has_session = true;
            XmlNodeList session_nodes = null;
            XmlNodeList success_nodes = null;
            XmlNodeList code_nodes = null;
            string error = null;

            if (login_xml == null) {
                has_session = false;
            } else {
                session_nodes = login_xml.GetElementsByTagName ("Session_ID");
                success_nodes = login_xml.GetElementsByTagName ("Reason");
                code_nodes = login_xml.GetElementsByTagName ("Code");

                foreach (XmlNode code_node in code_nodes)
                {
                    if (!code_node.InnerText.Equals ("0")) has_session = false;
                }
                foreach (XmlNode success_node in success_nodes)
                {
                    if (!success_node.InnerText.Equals ("Success"))
                    {
                        has_session = false;
                        error = success_node.InnerText;
                    }
                }
            }

            if (has_session)
            {
                string new_session_id = null;
                foreach (XmlNode session_node in session_nodes)
                {
                    new_session_id = session_node.InnerText;
                }
                session_id = "&session_id=" + new_session_id;
                //init keepalive
                if (keepalive_timer == null)
                {
                    keepalive_timer = new Timer ();
                    keepalive_timer.Interval = keepalive_timeout;
                    keepalive_timer.Elapsed += OnKeepAliveElapsed;
                    keepalive_timer.Start ();
                }
            } else {
                RaiseErrorReturned ("Session Create Failure",
                                    (String.IsNullOrEmpty(error) ?
                                        "Failed to create a user session using credentials on live365" :
                                        error));
            }
            Log.DebugFormat ("[Live365Plugin] <CreateSession> session_id : {0}", session_id);
        }

        /// <summary>
        /// This function is periodically called by a timer, after a session has been established and tests session validity.
        /// The test is used as a keepalive signal for the session
        /// </summary>
        /// <param name="sender">
        /// A <see cref="System.Object"/> -- not used
        /// </param>
        /// <param name="e">
        /// A <see cref="System.Timers.ElapsedEventArgs"/> -- not used
        /// </param>
        void OnKeepAliveElapsed (object sender, System.Timers.ElapsedEventArgs e)
        {
            Log.DebugFormat ("[Live365Plugin] <OnKeepAliveElapsed> keepalive {0}", session_id);
            //just clear session if expired
            if (Banshee.ServiceStack.ServiceManager.PlaybackController.CurrentTrack != null
                && Banshee.ServiceStack.ServiceManager.PlaybackController.CurrentTrack.IsPlaying
                && Banshee.ServiceStack.ServiceManager.PlaybackController.CurrentTrack.Uri.ToString ().Contains (session_id)
                && Banshee.ServiceStack.ServiceManager.PlaybackController.CurrentTrack.Uri.ToString ().Contains (playlist_url))
                return;
            if (!TestSession ())
            {
                session_id = null;
                keepalive_timer.Stop ();
                RaiseErrorReturned ("User Session Lost", "The live365 user session has been lost");
            }
        }

        /// <summary>
        /// Get a user session identifier.
        /// If a session_id is already present, it tests the current session_id for validity
        /// first and will reuse it, if valid, otherwise a new session is created
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> -- the full parameter with session_id set ("&session_id=...")
        /// </returns>
        private string GetSession ()
        {
            if (!UseCredentials) return null;
            if (String.IsNullOrEmpty(session_id))
            {
                CreateSession ();
            } else {
                if (!TestSession ()) CreateSession ();
            }
            return session_id;
        }

        /// <summary>
        /// Parses and sorts an XML genre catalog and fills the plugins genre list
        /// </summary>
        /// <param name="doc">
        /// A <see cref="XmlDocument"/> -- the XML document containing the genre catalog
        /// </param>
        private void ParseGenres(XmlDocument doc)
        {
            XmlNodeList XML_genre_nodes = doc.GetElementsByTagName ("Genre");

            List<Genre> new_genres = new List<Genre> ();

            foreach (XmlNode node in XML_genre_nodes) {
                XmlNodeList data_nodes = node.ChildNodes;

                foreach (XmlNode data_node in data_nodes)
                {
                    try {
                        if (data_node.LocalName.Equals("Name"))
                        {
                            Genre genre = new Genre(data_node.InnerText);

                            if (!new_genres.Contains (genre))
                                new_genres.Add (genre);
                        }
                    } catch (Exception ex) {
                        Log.Exception ("[Live365Plugin] <ParseGenres> ERROR", ex);
                        RaiseErrorReturned ("XML Parse Error", ex.Message);
                        continue;
                    }
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
            string session = GetSession ();

            List<string> pages = new List<string> ();
            pages.Add(null);
            XmlNodeList pagination_nodes = xml_response.GetElementsByTagName ("PAGINATION_PAGES");

            foreach (XmlNode node in pagination_nodes)
            {
                string url = null;
                bool url_valid = false;
                XmlNodeList page_nodes = node.ChildNodes;
                foreach (XmlNode page_node in page_nodes)
                {
                    try {
                        if (page_node.LocalName.Equals("PAGINATION_DESC")
                            && !page_node.InnerText.Trim ().Equals ("1")
                            && !page_node.InnerText.Trim ().Equals ("Next"))
                            url_valid = true;

                        if (page_node.LocalName.Equals("PAGINATION_LINK"))
                            url = page_node.InnerText;

                    }
                    catch (Exception e) {
                        Log.Exception ("[Live365Plugin] <ParseXmlResponse> ERROR: ", e);
                        RaiseErrorReturned ("XML Parse Error", e.Message);
                        return;
                    }
                }
                if (!String.IsNullOrEmpty(url) && url_valid)
                        pages.Add(url);
            }

            string key;
            if (request_type == LiveRadioRequestType.ByGenre) {
                key = "Genre:" + query;
                if (!cached_results.ContainsKey (key)) {
                    cached_results[key] = new List<DatabaseTrackInfo> ();
                }
            } else {
                key = query;
                if (!cached_results.ContainsKey (key)) {
                    cached_results[key] = new List<DatabaseTrackInfo> ();
                }
            }

            cached_results[key].Clear ();

            foreach (string page in pages)
            {
                XmlDocument doc;
                if (String.IsNullOrEmpty(page)) {
                    doc = xml_response;
                } else {
                    doc = RetrieveXml(page);
                }

                XmlNodeList XML_station_nodes = doc.GetElementsByTagName ("LIVE365_STATION");

                foreach (XmlNode node in XML_station_nodes)
                {
                    string name = null;
                    string access = null;
                    string ex_id = null;
                    string url = null;
                    string broadcaster = null;
                    string description = null;
                    string keywords = null;
                    string genre = null;
                    string bitrate = null;
                    string rating = null;
                    string status = null;
                    string location = null;

                    XmlNodeList childnodes = node.ChildNodes;
                    foreach (XmlNode child in childnodes)
                    {

                        try {
                            if (child.LocalName.Equals ("STATION_ADDRESS"))
                                url = child.InnerText;
                            else if (child.LocalName.Equals ("STATION_ID"))
                                ex_id = child.InnerText;
                            else if (child.LocalName.Equals ("STATION_BROADCASTER"))
                                broadcaster = child.InnerText;
                            else if (child.LocalName.Equals ("STATION_TITLE"))
                                name = child.InnerText;
                            else if (child.LocalName.Equals ("STATION_DESCRIPTION"))
                                description = child.InnerText;
                            else if (child.LocalName.Equals ("STATION_KEYWORDS"))
                                keywords = child.InnerText;
                            else if (child.LocalName.Equals ("STATION_GENRE"))
                                genre = child.InnerText;
                            else if (child.LocalName.Equals ("STATION_CONNECTION"))
                                bitrate = child.InnerText;
                            else if (child.LocalName.Equals ("STATION_RATING"))
                                rating = child.InnerText;
                            else if (child.LocalName.Equals ("STATION_STATUS"))
                                status = child.InnerText;
                            else if (child.LocalName.Equals ("LISTENER_ACCESS"))
                                access = child.InnerText;
                            else if (child.LocalName.Equals ("STATION_LOCATION"))
                                location = child.InnerText;

                        }
                        catch (Exception e) {
                            Log.Exception ("[Live365Plugin] <ParseXmlResponse> ERROR: ", e);
                            RaiseErrorReturned ("General Error", e.Message);
                            continue;
                        }
                    }

                    int bitrate_int;
                    int id_int;
                    int rating_int;

                    if (!String.IsNullOrEmpty (access)
                        && !String.IsNullOrEmpty (status)
                        && (access.Equals ("PUBLIC") || UseCredentials)
                        && status.Equals ("OK"))
                    {

                        DatabaseTrackInfo new_station = new DatabaseTrackInfo ();

                        if (UseCredentials)
                        {
                            url = url.Replace (replace_url, playlist_url);
                            url += session;
                        }

                        new_station.Uri = new SafeUri (url);
                        new_station.ArtistName = broadcaster;
                        new_station.Genre = genre;
                        new_station.TrackTitle = name;
                        new_station.Comment = description + location + keywords;
                        Int32.TryParse (rating.Trim (), out rating_int);
                        new_station.Rating = rating_int;
                        Int32.TryParse (ex_id.Trim (), out id_int);
                        new_station.ExternalId = id_int;
                        new_station.PrimarySource = source;
                        new_station.IsLive = true;
                        Int32.TryParse (bitrate.Trim (), out bitrate_int);
                        new_station.BitRate = bitrate_int;
                        new_station.IsLive = true;

                        cached_results[key].Add (new_station);
                    }
                }
            }
        }

        /// <summary>
        /// The name of the plugin -- used as identifier and as label for the source header
        /// </summary>
        public override string Name {
            get { return "live365.com"; }
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

        /// <summary>
        /// Cleans up live365 session data from a track url
        /// </summary>
        /// <param name="url">
        /// A <see cref="SafeUri"/> -- the original url to be cleaned
        /// </param>
        /// <returns>
        /// A <see cref="SafeUri"/> -- the cleaned url
        /// </returns>
        public override SafeUri CleanUpUrl (SafeUri url)
        {
            if (url.ToString ().Contains ("session_id"))
            {
                int pos = url.ToString ().IndexOf ("session_id");
                string new_url = url.ToString ().Substring (0, pos - 1);
                return new SafeUri (new_url);
            }
            return url;
        }

        public static readonly SchemaEntry<string> UseProxyEntry = new SchemaEntry<string> (
        "plugins.liveradio.live365" , "use_proxy", "", "whether to use proxy for HTTP", "whether to use proxy for HTTP");

        public static readonly SchemaEntry<string> ProxyUrlEntry = new SchemaEntry<string> (
        "plugins.liveradio.live365", "proxy_url", "", "HTTP proxy url", "HTTP proxy url");

        public static readonly SchemaEntry<string> UseCredentialsEntry = new SchemaEntry<string> (
        "plugins.liveradio.live365", "use_credentials", "", "whether to use credentials authentification", "whether to use credentials authentification");

        public static readonly SchemaEntry<string> HttpUsernameEntry = new SchemaEntry<string> (
        "plugins.liveradio.live365", "credentials_username", "", "HTTP username", "HTTP username");

        public static readonly SchemaEntry<string> HttpPasswordEntry = new SchemaEntry<string> (
        "plugins.liveradio.live365", "credentials_password", "", "HTTP password", "HTTP password");

        public static readonly SchemaEntry<string> HttpTimeoutEntry = new SchemaEntry<string> (
        "plugins.liveradio.live365", "http_timeout_seconds", "", "HTTP timeout", "HTTP timeout");

    }

}
