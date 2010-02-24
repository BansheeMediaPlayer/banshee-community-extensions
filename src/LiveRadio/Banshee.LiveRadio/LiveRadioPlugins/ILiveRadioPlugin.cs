
using System;
using System.Collections.Generic;

using Banshee.Sources;

namespace Banshee.LiveRadio.Plugins
{


    public interface ILiveRadioPlugin
    {
        event GenreListLoadedEventHandler GenreListLoaded;
        event RequestResultRetrievedEventHandler RequestResultRetrieved;
        event RequestResultRetrievedEventHandler RequestResultRefreshRetrieved;

        string GetName ();

        List<string> GetGenres();

        string ToString ();

        LiveRadioPluginSource GetLiveRadioPluginSource ();

        void Initialize ();

        void RetrieveGenreList ();

        void ExecuteRequest(LiveRadioRequestType request_type, string query);

        void SetLiveRadioPluginSource(LiveRadioPluginSource source);
    }
}
