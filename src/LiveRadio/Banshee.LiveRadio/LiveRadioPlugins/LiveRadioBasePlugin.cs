
using System;
using System.Net;
using System.Xml;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;

using Banshee.Collection.Database;
using Banshee.Sources;

using Hyena;

namespace Banshee.LiveRadio.Plugins
{

    public enum LiveRadioRequestType
    {
        ByGenre,
        ByFreetext
    }

    public delegate void GenreListLoadedEventHandler(object sender, List<string> genres);
    public delegate void RequestResultRetrievedEventHandler(object sender,
                                                            string request,
                                                            LiveRadioRequestType request_type,
                                                            List<DatabaseTrackInfo> result);

    public abstract class LiveRadioBasePlugin : ILiveRadioPlugin
    {

        protected List<string> genres;
        protected Dictionary<string, List<DatabaseTrackInfo>> cached_results;
        protected LiveRadioPluginSource source;
        protected bool use_proxy;
        protected string proxy_url;

        public event GenreListLoadedEventHandler GenreListLoaded;
        public event RequestResultRetrievedEventHandler RequestResultRetrieved;
        public event RequestResultRetrievedEventHandler RequestResultRefreshRetrieved;

        public LiveRadioBasePlugin ()
        {
            genres = new List<string> ();
            cached_results = new Dictionary<string, List<DatabaseTrackInfo>> ();
            //stations = new List<DatabaseTrackInfo> ();
            //request_thread = new BackgroundWorker ();

        }

        public void Initialize ()
        {
            RetrieveGenreList ();
        }

        protected abstract void RetrieveGenres ();

        protected abstract void RetrieveRequest (LiveRadioRequestType request_type, string query);


        private class LiveRadioRequestObject : Object
        {
            public string query;
            public LiveRadioRequestType request_type;

            public LiveRadioRequestObject(LiveRadioRequestType request_type, string query) : base ()
            {
                this.query = query;
                this.request_type = request_type;
            }
        }

        public void ExecuteRequest(LiveRadioRequestType request_type, string query)
        {
            BackgroundWorker request_thread = new BackgroundWorker();
            request_thread.DoWork += DoExecuteRequest;
            request_thread.RunWorkerCompleted += OnDoExecuteRequestFinished;
            LiveRadioRequestObject request_object = new LiveRadioRequestObject (request_type, query);
            request_thread.RunWorkerAsync(request_object);
        }

        void DoExecuteRequest(object sender, DoWorkEventArgs e)
        {
            LiveRadioRequestObject request_object = e.Argument as LiveRadioRequestObject;
            RetrieveRequest (request_object.request_type, request_object.query);
            e.Result = request_object;
        }

        public void RetrieveGenreList()
        {
            BackgroundWorker request_thread = new BackgroundWorker();
            request_thread.DoWork += DoRetrieveGenreList;
            request_thread.RunWorkerCompleted += OnDoRetrieveGenreListFinished;
            request_thread.RunWorkerAsync();
        }

        void DoRetrieveGenreList(object sender, DoWorkEventArgs e)
        {
            lock(this);
            RetrieveGenres ();
        }

        void OnDoRetrieveGenreListFinished (object sender, RunWorkerCompletedEventArgs e)
        {
            Hyena.Log.DebugFormat("[LiveRadioBasePlugin\"{0}\"]<OnDoRetrieveGenreListFinished> START", Name);
            BackgroundWorker request_thread = sender as BackgroundWorker;
            request_thread.DoWork -= DoRetrieveGenreList;
            request_thread.RunWorkerCompleted -= OnDoRetrieveGenreListFinished;
            RaiseGenreListLoaded();
            Hyena.Log.DebugFormat("[LiveRadioBasePlugin\"{0}\"]<OnDoRetrieveGenreListFinished> END", Name);
        }

        void OnDoExecuteRequestFinished (object sender, RunWorkerCompletedEventArgs e)
        {
            Hyena.Log.DebugFormat("[LiveRadioBasePlugin\"{0}\"]<OnDoExecuteRequestFinished> START", Name);
            BackgroundWorker request_thread = sender as BackgroundWorker;
            request_thread.DoWork -= DoExecuteRequest;
            request_thread.RunWorkerCompleted -= OnDoExecuteRequestFinished;
            LiveRadioRequestObject request_object = e.Result as LiveRadioRequestObject;
            RaiseRequestResultRetrieved (request_object.request_type, request_object.query);
            Hyena.Log.DebugFormat("[LiveRadioBasePlugin\"{0}\"]<OnDoExecuteRequestFinished> END", Name);
        }

        public void SetLiveRadioPluginSource(LiveRadioPluginSource source)
        {
            this.source = source;
        }

        public string GetName ()
        {
            return Name;
        }

        public List<string> GetGenres ()
        {
            return genres;
        }

        public LiveRadioPluginSource GetLiveRadioPluginSource ()
        {
            return source;
        }

        abstract public string Name
        {
            get;
        }

        public override string ToString ()
        {
            return Name;
        }

        protected virtual void OnGenreListLoaded ()
        {
            GenreListLoadedEventHandler handler = GenreListLoaded;
            if(handler != null) {
                handler(this, genres);
            }
        }

        protected virtual void OnRequestResultRetrieved (LiveRadioRequestType request_type, string query, List<DatabaseTrackInfo> result)
        {
            RequestResultRetrievedEventHandler handler = RequestResultRetrieved;
            if(handler != null) {
                handler(this, query, request_type, result);
            }
        }

        protected virtual void OnRequestResultRefreshRetrieved (LiveRadioRequestType request_type, string query, List<DatabaseTrackInfo> result)
        {
            RequestResultRetrievedEventHandler handler = RequestResultRefreshRetrieved;
            if(handler != null) {
                handler(this, query, request_type, result);
            }
        }

        public void RaiseRequestResultRetrieved (LiveRadioRequestType request_type, string query)
        {
            List<DatabaseTrackInfo> result = cached_results[query];
            Hyena.Log.DebugFormat("[LiveRadioBasePlugin\"{0}\"]<RaiseRequestResultRetrieved> result contains {1} entries for query {2}", Name, result.Count, query);
            OnRequestResultRetrieved (request_type, query, result);
        }

        public void RaiseRequestResultRefreshRetrieved (LiveRadioRequestType request_type, string query)
        {
            List<DatabaseTrackInfo> result = cached_results[query];
            OnRequestResultRefreshRetrieved (request_type, query, result);
        }

        public void RaiseGenreListLoaded ()
        {
            OnGenreListLoaded ();
        }
    }
}
