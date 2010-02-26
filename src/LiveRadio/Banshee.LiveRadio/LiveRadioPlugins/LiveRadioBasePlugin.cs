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

using Banshee.Collection.Database;


namespace Banshee.LiveRadio.Plugins
{

    public enum LiveRadioRequestType
    {
        ByGenre,
        ByFreetext
    }

    public delegate void GenreListLoadedEventHandler (object sender, List<Genre> genres);
    public delegate void RequestResultRetrievedEventHandler (object sender, string request, LiveRadioRequestType request_type, List<DatabaseTrackInfo> result);

    public abstract class LiveRadioBasePlugin : ILiveRadioPlugin
    {

        protected List<Genre> genres;
        protected Dictionary<string, List<DatabaseTrackInfo>> cached_results;
        protected LiveRadioPluginSource source;
        protected bool use_proxy;
        protected string proxy_url;

        public event GenreListLoadedEventHandler GenreListLoaded;
        public event RequestResultRetrievedEventHandler RequestResultRetrieved;

        public LiveRadioBasePlugin ()
        {
            genres = new List<Genre> ();
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

            public LiveRadioRequestObject (LiveRadioRequestType request_type, string query) : base()
            {
                this.query = query;
                this.request_type = request_type;
            }
        }

        public void ExecuteRequest (LiveRadioRequestType request_type, string query)
        {
            BackgroundWorker request_thread = new BackgroundWorker ();
            request_thread.DoWork += DoExecuteRequest;
            request_thread.RunWorkerCompleted += OnDoExecuteRequestFinished;
            LiveRadioRequestObject request_object = new LiveRadioRequestObject (request_type, query);
            request_thread.RunWorkerAsync (request_object);
        }

        void DoExecuteRequest (object sender, DoWorkEventArgs e)
        {
            LiveRadioRequestObject request_object = e.Argument as LiveRadioRequestObject;
            e.Result = request_object;
            RetrieveRequest (request_object.request_type, request_object.query);
        }

        public void RetrieveGenreList ()
        {
            BackgroundWorker request_thread = new BackgroundWorker ();
            request_thread.DoWork += DoRetrieveGenreList;
            request_thread.RunWorkerCompleted += OnDoRetrieveGenreListFinished;
            request_thread.RunWorkerAsync ();
        }

        void DoRetrieveGenreList (object sender, DoWorkEventArgs e)
        {
            RetrieveGenres ();
        }

        void OnDoRetrieveGenreListFinished (object sender, RunWorkerCompletedEventArgs e)
        {
            Hyena.Log.DebugFormat ("[LiveRadioBasePlugin\"{0}\"]<OnDoRetrieveGenreListFinished> START", Name);
            BackgroundWorker request_thread = sender as BackgroundWorker;
            request_thread.DoWork -= DoRetrieveGenreList;
            request_thread.RunWorkerCompleted -= OnDoRetrieveGenreListFinished;
            RaiseGenreListLoaded ();
            Hyena.Log.DebugFormat ("[LiveRadioBasePlugin\"{0}\"]<OnDoRetrieveGenreListFinished> ({1}) END", Name, genres.Count);
        }

        void OnDoExecuteRequestFinished (object sender, RunWorkerCompletedEventArgs e)
        {
            Hyena.Log.DebugFormat ("[LiveRadioBasePlugin\"{0}\"]<OnDoExecuteRequestFinished> START", Name);
            BackgroundWorker request_thread = sender as BackgroundWorker;
            LiveRadioRequestObject request_object = e.Result as LiveRadioRequestObject;
            Hyena.Log.DebugFormat ("[LiveRadioBasePlugin\"{0}\"]<OnDoExecuteRequestFinished> raising", Name);
            RaiseRequestResultRetrieved (request_object.request_type, request_object.query);
            request_thread.DoWork -= DoExecuteRequest;
            request_thread.RunWorkerCompleted -= OnDoExecuteRequestFinished;
            Hyena.Log.DebugFormat ("[LiveRadioBasePlugin\"{0}\"]<OnDoExecuteRequestFinished> END", Name);
        }

        public void SetLiveRadioPluginSource (LiveRadioPluginSource source)
        {
            this.source = source;
        }

        public string GetName ()
        {
            return Name;
        }

        public List<Genre> GetGenres ()
        {
            return genres;
        }

        public LiveRadioPluginSource GetLiveRadioPluginSource ()
        {
            return source;
        }

        public abstract string Name { get; }

        public override string ToString ()
        {
            return Name;
        }

        protected virtual void OnGenreListLoaded ()
        {
            GenreListLoadedEventHandler handler = GenreListLoaded;
            if (handler != null) {
                handler (this, genres);
            }
        }

        protected virtual void OnRequestResultRetrieved (LiveRadioRequestType request_type, string query, List<DatabaseTrackInfo> result)
        {
            RequestResultRetrievedEventHandler handler = RequestResultRetrieved;
            if (handler != null) {
                handler (this, query, request_type, result);
            }
        }

        public void RaiseRequestResultRetrieved (LiveRadioRequestType request_type, string query)
        {
            Hyena.Log.DebugFormat ("[LiveRadioBasePlugin\"{0}\"]<RaiseRequestResultRetrieved> START query: {0}", query);
            List<DatabaseTrackInfo> result;
            try {
                if (request_type == LiveRadioRequestType.ByGenre) {
                    result = cached_results["Genre:" + query];
                } else {
                    result = cached_results[query];
                }
            } catch (Exception e) {
                result = new List<DatabaseTrackInfo> ();
                Hyena.Log.DebugFormat ("[LiveRadioBasePlugin\"{0}\"]<RaiseRequestResultRetrieved> error {0}", e.Message);
            }
            Hyena.Log.DebugFormat ("[LiveRadioBasePlugin\"{0}\"]<RaiseRequestResultRetrieved> result contains {1} entries for query {2}", Name, result.Count, query);
            OnRequestResultRetrieved (request_type, query, result);
        }

        public void RaiseGenreListLoaded ()
        {
            OnGenreListLoaded ();
        }
    }
}
