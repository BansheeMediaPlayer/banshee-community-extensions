//
// LiveRadioBasePlugin.cs
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
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Xml;

using Mono.Addins;

using Banshee.Collection.Database;
using Hyena;
using Gtk;

namespace Banshee.LiveRadio.Plugins
{

    /// <summary>
    /// Enumeration of RequestTypes for LiveRadio
    /// </summary>
    public enum LiveRadioRequestType
    {
        ByGenre,
        ByFreetext
    }

    /// <summary>
    /// EventHandler for the event a genre list has been retrieved by the plugin
    /// </summary>
    public delegate void GenreListLoadedEventHandler (object sender, List<Genre> genres);
    /// <summary>
    /// EventHandler for the event a query result has been retrieved by the plugin
    /// </summary>
    public delegate void RequestResultRetrievedEventHandler (object sender, string request, LiveRadioRequestType request_type, List<DatabaseTrackInfo> result);
    /// <summary>
    /// EventHandler for the event an error is returned during a plugin operation
    /// </summary>
    public delegate void ErrorReturnedEventHandler (ILiveRadioPlugin plugin, LiveRadioPluginError error);

    /// <summary>
    /// An abstact base plugin for LiveRadio plugins. Should normally be the parent class of all plugins. Implements the ILiveRadioPlugin
    /// interface and provides a lot of basic functionality for plugins:
    /// - background-threaded processing of requests
    /// - basic protected configuration members
    /// - XML document retrieval
    /// - event handling
    ///
    /// derived classes must implement the following abstract members:
    /// - RetrieveGenres ()
    /// - RetrieveRequest (...)
    /// - Name
    /// - SaveConfiguration ()
    ///
    /// derived classes may override the following virtual members:
    /// - Initialize ()
    /// - SetLiveRadioPluginSource (...)
    /// - ConfigurationWidget
    /// - CleanUpUrl (...)
    ///
    /// Errors should be reported through the RaiseErrorReturned () method.
    /// </summary>
    public abstract class LiveRadioBasePlugin : ILiveRadioPlugin
    {

        protected List<Genre> genres;
        protected Dictionary<string, List<DatabaseTrackInfo>> cached_results;
        protected LiveRadioPluginSource source;
        protected bool has_login;
        protected bool use_proxy;
        protected bool use_credentials;
        private bool enabled;
        protected int http_timeout_seconds;
        protected string proxy_url;
        protected string credentials_username;
        protected string credentials_password;
        protected LiveRadioPluginConfigurationWidget configuration_widget;

        public event GenreListLoadedEventHandler GenreListLoaded;
        public event RequestResultRetrievedEventHandler RequestResultRetrieved;
        public event ErrorReturnedEventHandler ErrorReturned;


        /// <summary>
        /// Parameterless Constructor, assumes that it will handle an internet radio directory without
        /// user credentials processing
        /// </summary>
        public LiveRadioBasePlugin () : this (false) {}

        /// <summary>
        /// Full Constructor -- creates the genres list object and the cached_results dictionary
        /// </summary>
        /// <param name="has_login">
        /// A <see cref="System.Boolean"/> -- whether or not to use login with user credentials, implemented
        /// in the derived class
        /// </param>
        public LiveRadioBasePlugin (bool has_login)
        {
            this.has_login = has_login;
            genres = new List<Genre> ();
            cached_results = new Dictionary<string, List<DatabaseTrackInfo>> ();
            enabled = false;
        }

        /// <summary>
        /// Initializes the plugin by retrieving its genre list
        /// </summary>
        public virtual void Initialize ()
        {
            RetrieveGenreList ();
            enabled = true;
        }

        public virtual void Disable ()
        {
            SetLiveRadioPluginSource (null);
            enabled = false;
        }

        /// <summary>
        /// Actual method that does the work of retrieving the list of genres from an outside source.
        /// This method must take care of retrieving the data (possibly using the provided RetrieveXML
        /// method), parsing it, and filling the
        ///
        /// List<Genres> genres
        ///
        /// member of this class.
        /// </summary>
        protected abstract void RetrieveGenres ();

        /// <summary>
        /// Actual method that does the work of retrieving the results of a user query from an outside
        /// source. This method must take care of retrieving the data (possibly using the provided
        /// RetrieveXML method). parsing it, and filling the
        ///
        /// Dictionary<string key, List<DatabaseTrackInfo> track> cached_results
        ///
        /// member of this class with the result.
        /// </summary>
        /// <param name="request_type">
        /// A <see cref="LiveRadioRequestType"/> -- the type of the request
        /// </param>
        /// <param name="query">
        /// A <see cref="System.String"/> -- the freetext query or the genre name
        /// </param>
        protected abstract void RetrieveRequest (LiveRadioRequestType request_type, string query);

        /// <summary>
        /// Internal object to capsule request type and actual query
        /// </summary>
        private class LiveRadioRequestObject
        {
            public string query;
            public LiveRadioRequestType request_type;

            public LiveRadioRequestObject (LiveRadioRequestType request_type, string query) : base()
            {
                this.query = query;
                this.request_type = request_type;
            }
        }

        /// <summary>
        /// Method capsuling the actual RetrieveRequest worker method with a background worker thread
        /// </summary>
        /// <param name="request_type">
        /// A <see cref="LiveRadioRequestType"/> -- the type of the request
        /// </param>
        /// <param name="query">
        /// A <see cref="System.String"/> -- the freetext query or the genre name
        /// </param>
        public void ExecuteRequest (LiveRadioRequestType request_type, string query)
        {
            BackgroundWorker request_thread = new BackgroundWorker ();
            request_thread.DoWork += DoExecuteRequest;
            request_thread.RunWorkerCompleted += OnDoExecuteRequestFinished;
            LiveRadioRequestObject request_object = new LiveRadioRequestObject (request_type, query);
            request_thread.RunWorkerAsync (request_object);
        }

        /// <summary>
        /// The background worker asynchronous thread method calling the worker method
        /// </summary>
        /// <param name="sender">
        /// A <see cref="System.Object"/> -- the sending object
        /// </param>
        /// <param name="e">
        /// A <see cref="DoWorkEventArgs"/> -- containing the arguments (a LiveRadioRequestObject)
        /// and the result
        /// </param>
        void DoExecuteRequest (object sender, DoWorkEventArgs e)
        {
            LiveRadioRequestObject request_object = e.Argument as LiveRadioRequestObject;
            e.Result = request_object;
            RetrieveRequest (request_object.request_type, request_object.query);
        }

        /// <summary>
        /// Method capsuling the actual RetrieveGenres worker method with a background worker thread
        /// </summary>
        public void RetrieveGenreList ()
        {
            BackgroundWorker request_thread = new BackgroundWorker ();
            request_thread.DoWork += DoRetrieveGenreList;
            request_thread.RunWorkerCompleted += OnDoRetrieveGenreListFinished;
            request_thread.RunWorkerAsync ();
        }

        /// <summary>
        /// The background worker asynchronous thread method calling the worker method
        /// </summary>
        /// <param name="sender">
        /// A <see cref="System.Object"/> -- the sending object
        /// </param>
        /// <param name="e">
        /// A <see cref="DoWorkEventArgs"/> -- containing the arguments (a LiveRadioRequestObject)
        /// and the result
        /// </param>
        void DoRetrieveGenreList (object sender, DoWorkEventArgs e)
        {
            RetrieveGenres ();
        }

        /// <summary>
        /// Method executed upon the completion of the background worker method
        /// </summary>
        /// <param name="sender">
        /// A <see cref="System.Object"/> -- the sending object
        /// </param>
        /// <param name="e">
        /// A <see cref="RunWorkerCompletedEventArgs"/> -- containing the Result object
        /// (a LiveRadioRequestObject) with the original request type and query
        /// </param>
        void OnDoRetrieveGenreListFinished (object sender, RunWorkerCompletedEventArgs e)
        {
            BackgroundWorker request_thread = sender as BackgroundWorker;
            request_thread.DoWork -= DoRetrieveGenreList;
            request_thread.RunWorkerCompleted -= OnDoRetrieveGenreListFinished;
            RaiseGenreListLoaded ();
        }

        /// <summary>
        /// Method executed upon the completion of the background worker method
        /// </summary>
        /// <param name="sender">
        /// A <see cref="System.Object"/> -- the sending object
        /// </param>
        /// <param name="e">
        /// A <see cref="RunWorkerCompletedEventArgs"/> -- not used
        /// </param>
        void OnDoExecuteRequestFinished (object sender, RunWorkerCompletedEventArgs e)
        {
            BackgroundWorker request_thread = sender as BackgroundWorker;
            LiveRadioRequestObject request_object = e.Result as LiveRadioRequestObject;
            RaiseRequestResultRetrieved (request_object.request_type, request_object.query);
            request_thread.DoWork -= DoExecuteRequest;
            request_thread.RunWorkerCompleted -= OnDoExecuteRequestFinished;
        }

        /// <summary>
        /// Set the LiveRadioPluginSource of the plugin
        /// </summary>
        /// <param name="source">
        /// A <see cref="LiveRadioPluginSource"/>
        /// </param>
        public virtual void SetLiveRadioPluginSource (LiveRadioPluginSource source)
        {
            this.source = source;
        }

        public List<Genre> Genres
        {
            get { return genres; }
        }

        public LiveRadioPluginSource PluginSource
        {
            get { return source; }
        }

        public abstract string Name { get; }

        public override string ToString ()
        {
            return Name;
        }

        /// <summary>
        /// Actual method that does the work of saving the configuration for this plugin. For examples see derived
        /// classes. There are no predefined SchemaEntry objects, the derived class needs to take care of those.
        /// </summary>
        public abstract void SaveConfiguration ();

        /// <summary>
        /// Always returns a new standard Configration Widget with all base Properties set
        /// </summary>
        public virtual Widget ConfigurationWidget
        {
            get {
                configuration_widget = new LiveRadioPluginConfigurationWidget (has_login);
                configuration_widget.HttpTimeout = http_timeout_seconds;
                configuration_widget.HttpPassword = credentials_password;
                configuration_widget.HttpUsername = credentials_username;
                configuration_widget.ProxyUrl = proxy_url;
                configuration_widget.UseCredentials = use_credentials;
                configuration_widget.UseProxy = use_proxy;

                return configuration_widget; }
        }

        /// <summary>
        /// Method to invoke the GenreListLoaded event
        /// </summary>
        protected void OnGenreListLoaded ()
        {
            GenreListLoadedEventHandler handler = GenreListLoaded;
            if (handler != null) {
                handler (this, genres);
            }
        }

        /// <summary>
        /// Method to invoke the ErrorReturned event
        /// </summary>
        /// <param name="short_message">
        /// A <see cref="System.String"/> -- the short description of the error
        /// </param>
        /// <param name="long_message">
        /// A <see cref="System.String"/> -- the detailed description of the error
        /// </param>
        protected void OnErrorReturned (string short_message, string long_message)
        {
            ErrorReturnedEventHandler handler = ErrorReturned;
            if (handler != null) {
                handler (this, new LiveRadioPluginError (short_message, long_message));
            }
        }

        /// <summary>
        /// Method to invoke the RequestResultRetrieved event
        /// </summary>
        /// <param name="request_type">
        /// A <see cref="LiveRadioRequestType"/> -- the original request type
        /// </param>
        /// <param name="query">
        /// A <see cref="System.String"/> -- the original freetext query or the original genre name
        /// </param>
        /// <param name="result">
        /// A <see cref="List<DatabaseTrackInfo>"/> -- the resulting list of DatabaseTrackInfo objects for the query
        /// </param>
        protected void OnRequestResultRetrieved (LiveRadioRequestType request_type, string query, List<DatabaseTrackInfo> result)
        {
            RequestResultRetrievedEventHandler handler = RequestResultRetrieved;
            if (handler != null) {
                handler (this, query, request_type, result);
            }
        }

        /// <summary>
        /// Raises the ResultsRetrieved event
        /// </summary>
        /// <param name="request_type">
        /// A <see cref="LiveRadioRequestType"/> -- the original request type
        /// </param>
        /// <param name="query">
        /// A <see cref="System.String"/> -- the original freetext query or the original genre name
        /// </param>
        public void RaiseRequestResultRetrieved (LiveRadioRequestType request_type, string query)
        {
            List<DatabaseTrackInfo> result;
            try {
                if (request_type == LiveRadioRequestType.ByGenre) {
                    result = new List<DatabaseTrackInfo> (cached_results["Genre:" + query].ToArray ());
                } else {
                    result = new List<DatabaseTrackInfo> (cached_results[query].ToArray ());
                }
                foreach (DatabaseTrackInfo track in result)
                    result[result.IndexOf (track)] = new DatabaseTrackInfo (track);
            } catch (Exception e) {
                result = null;
                Hyena.Log.DebugFormat ("[LiveRadioBasePlugin\"{0}\"]<RaiseRequestResultRetrieved> error {1}", Name, e.Message);
                RaiseErrorReturned ("General Error", e.Message);
            }
            OnRequestResultRetrieved (request_type, query, result);
        }

        /// <summary>
        /// Raises the GenreListLoaded event
        /// </summary>
        public void RaiseGenreListLoaded ()
        {
            OnGenreListLoaded ();
        }

        /// <summary>
        /// Raises the ErrorReturned event
        /// </summary>
        /// <param name="short_message">
        /// A <see cref="System.String"/> -- the short description of the error
        /// </param>
        /// <param name="long_message">
        /// A <see cref="System.String"/> -- the detailed description of the error
        /// </param>
        public void RaiseErrorReturned (string short_message, string long_message)
        {
            OnErrorReturned (short_message, long_message);
        }

        /// <summary>
        /// Retrieves, reads and returns an XML document from the specified query url using HTTP GET
        /// </summary>
        /// <param name="query">
        /// A <see cref="System.String"/> -- a URL with full query parameters
        /// </param>
        /// <returns>
        /// A <see cref="XmlDocument"/> -- the retrieved and loaded XML document
        /// </returns>
        protected XmlDocument RetrieveXml(string query)
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

                XmlDocument xml_response = new XmlDocument ();
                xml_response.LoadXml (reader.ReadToEnd ());

                return xml_response;
            }
            catch (Exception e) {
                Log.DebugFormat ("[LiveRadioBasePlugin\"{0}\"] <RetrieveXml> Error: {1} END", Name, e.Message);
                RaiseErrorReturned ("General Error", e.Message);
            }
            return null;
        }

        protected void SetWebIcon (string uri)
        {
            BackgroundWorker request_thread = new BackgroundWorker ();
            request_thread.DoWork += DoRetrieveWebIcon;;
            request_thread.RunWorkerCompleted += OnDoRetrieveWebIconFinished;;
            request_thread.RunWorkerAsync (uri);
        }

        void OnDoRetrieveWebIconFinished (object sender, RunWorkerCompletedEventArgs e)
        {
            // First, handle the case where an exception was thrown.
            if (e.Error != null)
            {
                Log.DebugFormat ("[LiveRadioBasePlugin\"{0}\"] <OnDoRetrieveWebIconFinished> Error: {1} END", Name, e.Error.Message);
            }
            else if (e.Cancelled)
            {
                // Next, handle the case where the user canceled
                // the operation.
                Log.DebugFormat ("[LiveRadioBasePlugin\"{0}\"] <OnDoRetrieveWebIconFinished> Cancelled. END", Name);
            }
            else
            {
                // Finally, handle the case where the operation
                // succeeded.
                Gdk.Pixbuf icon = e.Result as Gdk.Pixbuf;
                if (icon != null && icon is Gdk.Pixbuf && source != null)
                {
                    source.SetIcon (icon);
                }
            }
        }

        void DoRetrieveWebIcon (object sender, DoWorkEventArgs e)
        {
            string uri = e.Argument as string;
            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create (uri);
            request.Method = "GET";
            request.ContentType = "HTTP/1.0";
            request.Timeout = 10000;

            try
            {
                Stream response = request.GetResponse ().GetResponseStream ();
                Gdk.Pixbuf icon = new Gdk.Pixbuf (response);
                e.Result = icon;
            }
            catch (Exception ex) {
                Log.DebugFormat ("[LiveRadioBasePlugin\"{0}\"] <DoRetrieveWebIcon> Error: {1} END", Name, ex.Message);
            }
        }

        /// <summary>
        /// Cleans up any plugin specific data from a track url, such as session data or any other
        /// temporary parameters.
        /// </summary>
        /// <param name="url">
        /// A <see cref="SafeUri"/> -- the original url to be cleaned
        /// </param>
        /// <returns>
        /// A <see cref="SafeUri"/> -- the cleaned url
        /// </returns>
        public virtual SafeUri CleanUpUrl (SafeUri url)
        {
            return url;
        }

        public virtual SafeUri RetrieveUrl (string baseurl)
        {
            return new SafeUri (baseurl);
        }

        public bool UseProxy {
            get { return use_proxy; }
            set { use_proxy = value; }
        }

        public string ProxyUrl {
            get { return proxy_url; }
            set { proxy_url = value; }
        }

        public bool UseCredentials {
            get { return use_credentials; }
            set { use_credentials = value; }
        }

        public string HttpUsername {
            get { return credentials_username; }
            set { credentials_username = value; }
        }

        public string HttpPassword {
            get { return credentials_password; }
            set { credentials_password = value; }
        }

        public int HttpTimeout {
            get { return http_timeout_seconds; }
            set { http_timeout_seconds = value; }
        }

        public bool Enabled {
            get { return enabled; }
        }

        public string IsEnabled {
            get { return (enabled ? AddinManager.CurrentLocalizer.GetString ("Yes") : AddinManager.CurrentLocalizer.GetString ("No")); }
        }

        public abstract string Version
        {
            get ;
        }

        public virtual bool Active {
            get { return true; }
        }

    }
}
