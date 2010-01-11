
using System;

using Mono.Unix;

using Banshee.Base;
using Banshee.ServiceStack;
using Banshee.Collection.Database;
using Banshee.Sources;
using Banshee.MediaEngine;

using Hyena.Jobs;
using Hyena;
using Hyena.Data.Sqlite;

namespace Banshee.Lyrics
{
    public class LyricsDownloadJob : DbIteratorJob
    {
        public LyricsDownloadJob (bool force) : base(Catalog.GetString ("Downloading Lyrics"))
        {
            PriorityHints = PriorityHints.LongRunning;
            IsBackground = true;
            CanCancel = true;
            DelayShow = true;
            SetResources (Resource.Database);

            if (force) {
                /*remove from Lyrics Downloads trakcs without lyrics */
                ServiceManager.DbConnection.Execute (new HyenaSqliteCommand (@"
                DELETE FROM LyricsDownloads WHERE Downloaded = 0"));
            }

            SelectCommand = new HyenaSqliteCommand (@"
                SELECT CoreTracks.TrackID, CoreArtists.Name, CoreTracks.Title, CoreTracks.Uri
                    FROM CoreTracks, CoreArtists
                    WHERE
                        CoreTracks.PrimarySourceID = ? AND
                        CoreTracks.ArtistID = CoreArtists.ArtistID AND
                        CoreTracks.TrackID NOT IN (
                            SELECT TrackID from LyricsDownloads)",
            ServiceManager.SourceManager.MusicLibrary.DbId);

            CountCommand = new HyenaSqliteCommand (@"
                SELECT count(CoreTracks.TrackID)
                    FROM CoreTracks
                    WHERE
                        CoreTracks.PrimarySourceID = ? AND
                        CoreTracks.TrackID NOT IN (
                            SELECT TrackID from LyricsDownloads)",
            ServiceManager.SourceManager.MusicLibrary.DbId);
        }

        protected override void IterateCore (HyenaDataReader reader)
        {
            var track = new LyricsTrackInfo() {
                ArtistName = reader.Get<string> (1),
                TrackTitle = reader.Get<string> (2),
                PrimarySource = ServiceManager.SourceManager.MusicLibrary,
                Uri = new SafeUri (reader.Get<string> (3)),
                DbId = reader.Get<int> (0),
            };

            Status = String.Format (Catalog.GetString ("{0} - {1}"), track.ArtistName, track.TrackTitle);
            DownloadLyric (track);
        }

        public void Start ()
        {
            Register ();
        }

        private void DownloadLyric (DatabaseTrackInfo track)
        {
            bool have_lyric = false;

            string lyric = null;
            try {
                lyric = LyricsManager.Instance.DownloadLyric (track);
            } catch (Exception e) {
                Log.Exception (e);
                return;
            }
            
            if (lyric != null) {
                have_lyric = true;
            }

            ServiceManager.DbConnection.Execute (
                "INSERT OR REPLACE INTO LyricsDownloads (TrackID, Downloaded) VALUES (?, ?)",
                track.TrackId, have_lyric);
        }

        protected override void OnCancelled ()
        {
            OnFinished ();
            AbortThread ();
        }

        private class LyricsTrackInfo : DatabaseTrackInfo
        {
            public int DbId {
                set { TrackId = value; }
            }
        }
    }
}
