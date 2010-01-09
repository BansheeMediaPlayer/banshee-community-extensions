
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
            Title = Catalog.GetString ("Downloading Lyrics");
            PriorityHints = PriorityHints.LongRunning;
            IsBackground = true;
            CanCancel = true;
            DelayShow = true;

            this.force_refresh = force_refresh;
        }

        protected override void Run ()
        {
            FetchLyrics ();
            OnFinished ();
        }

        private void FetchLyrics ()
        {
            PrimarySource music_library = ServiceManager.SourceManager.MusicLibrary;
            CachedList<DatabaseTrackInfo> list = CachedList<DatabaseTrackInfo>.CreateFromSourceModel (music_library.DatabaseTrackModel);
            foreach (DatabaseTrackInfo track_info in list) {
                LyricsManager.Instance.DownloadLyrics (track_info.Artist.Name, track_info.TrackTitle, this.force_refresh);
            }
        }

        public void Stop ()
        {
            OnFinished ();
            AbortThread ();
        }
    }
}
