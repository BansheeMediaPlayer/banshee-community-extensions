
using System;

using Banshee.Collection.Database;

/*namespace Banshee.Mirage
{
    public class RandomByMirage : RandomBy
    {
        private static string last_played_condition = String.Format ("AND CoreTracks.AlbumID = ? {0} ORDER BY Disc ASC, TrackNumber ASC", RANDOM_CONDITION);

        private HyenaSqliteCommand album_query;

        public RandomByMirage (Shuffler shuffler) : base (PlaybackShuffleMode.Album, shuffler)
        {
            Condition = "CoreTracks.AlbumID = ?";
            OrderBy = "Disc ASC, TrackNumber ASC";
        }

        protected override void OnModelAndCacheUpdated ()
        {
            album_query = null;
        }

        public override void Reset ()
        {
            album_id = null;
        }

        public override bool IsReady { get { return album_id != null; } }

        public override bool Next (DateTime after)
        {
            Reset ();

            using (var reader = ServiceManager.DbConnection.Query (AlbumQuery, after, after)) {
                if (reader.Read ()) {
                    album_id = Convert.ToInt32 (reader[0]);
                }
            }

            return IsReady;
        }

        public override TrackInfo GetPlaybackTrack (DateTime after)
        {
            return album_id == null ? null : Cache.GetSingle (last_played_condition, (int)album_id, after, after);
        }

        public override DatabaseTrackInfo GetShufflerTrack (DateTime after)
        {
            if (album_id == null)
                return null;

            return GetTrack (ShufflerQuery, (int)album_id, after);
        }
    }
}*/