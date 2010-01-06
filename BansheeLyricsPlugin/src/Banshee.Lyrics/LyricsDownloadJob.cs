
using System;

using Mono.Unix;

using Banshee.ServiceStack;

using Banshee.MediaEngine;

using Hyena.Jobs;
using Hyena;

using Banshee.Collection.Database;

using Banshee.Sources;

namespace Banshee.Lyrics
{


    public class LyricsDownloadJob : SimpleAsyncJob
    {

        private bool force_refresh;

        public LyricsDownloadJob (bool force_refresh)
        {
            base.Title = Catalog.GetString ("Downloading Lyrics");
            PriorityHints = PriorityHints.LongRunning;
            IsBackground = true;
            CanCancel = true;
        }

        protected override void Run ()
        {
            FetchLyrics ();
            OnFinished ();
        }

        private static void FetchLyrics ()
        {
            PrimarySource music_library = ServiceManager.SourceManager.MusicLibrary;
            CachedList<DatabaseTrackInfo> list = CachedList<DatabaseTrackInfo>.CreateFromSourceModel (music_library.DatabaseTrackModel);
            foreach (DatabaseTrackInfo track_info in list) {
                LyricsManager.Instance.GetLyrics (track_info.Artist.Name, track_info.TrackTitle, force_refresh);
            }
        }
    }
}
