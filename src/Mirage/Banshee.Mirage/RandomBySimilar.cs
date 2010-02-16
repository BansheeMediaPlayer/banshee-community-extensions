//
// RandomBySimilar.cs
//
// Author:
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2010 Novell, Inc.
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

using Banshee.Collection.Database;
using Banshee.Collection;
using Banshee.PlaybackController;
using Banshee.ServiceStack;
using Mono.Unix;
using Mirage;

namespace Banshee.Mirage
{
    public class RandomBySimilar : RandomBy, IDisposable
    {
        // At a high level, what we're trying to do is get the most similar track
        // to the seed tracks, excluding played/skipped ones[, and ones too similar to skipped ones].
        //
        // The RANDOM_CONDITION will exclude the played/skipped items for us, but we need to
        // add the Similarity-based selection[, exclusion], and ordering.

        //private static string last_played_condition = String.Format ("AND CoreTracks.AlbumID = ? {0} ORDER BY Disc ASC, TrackNumber ASC", RANDOM_CONDITION);

        private long last_track_id;

        public RandomBySimilar (Shuffler shuffler) : base ("mirage_similar", shuffler)
        {
            Label = Catalog.GetString ("Shuffle by Similar");
            Adverb = Catalog.GetString ("by similar");
            Description = Catalog.GetString ("Play songs similar to those already played");

            // TODO Mirage's PlaylistGeneratorSource ensures no more than 50% of tracks are by same artist
            Condition = "1=1";
            From = "LEFT OUTER JOIN mirage ON (mirage.TrackID = CoreTracks.TrackID) ";
            Select = ", HYENA_BINARY_FUNCTION ('MIRAGE_DISTANCE', mirage.Scms, ?) as Distance";
            OrderBy = "Distance ASC, RANDOM ()";
        }

        public override void Reset ()
        {
            var track = ServiceManager.PlaybackController.CurrentTrack as DatabaseTrackInfo;
            if (track != null) {
                last_track_id = track.TrackId;
            } else {
                last_track_id = 0;
            }
        }

        public void Dispose ()
        {

        }

        public override bool Next (DateTime after)
        {
            return true;
        }

        public override TrackInfo GetPlaybackTrack (DateTime after)
        {
            // FIXME - hard to do - need to add mirage to FROM, etc
            //return Cache.GetSingle (track_condition, after, after);
            // last_track_id = track.TrackId;
            return null;
        }

        public override DatabaseTrackInfo GetShufflerTrack (DateTime after)
        {
            var seed_scms = ServiceManager.DbConnection.Query<byte[]> (String.Format (
                "SELECT Scms FROM mirage {0} LIMIT 1",
                last_track_id == 0
                    ? "ORDER BY RANDOM ()"
                    : String.Format ("WHERE TrackID = {0}", last_track_id)
            ));

            MiragePlugin.total_count = 0;
            MiragePlugin.total_ms = 0;
            MiragePlugin.total_read_ms = 0;
            var track = GetTrack (ShufflerQuery, seed_scms, after) as DatabaseTrackInfo;
            Console.WriteLine (">>>>>>>>>>>>>> Total ms spent in Distance func: {0} - ms spent reading: {1}; total calls: {2}", MiragePlugin.total_ms, MiragePlugin.total_read_ms, MiragePlugin.total_count);
            if (track != null) {
                last_track_id = track.TrackId;
            }
            return track;
        }
    }
}