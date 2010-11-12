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
using System.Collections.Generic;
using System.Linq;

using Banshee.Collection.Database;
using Banshee.Collection;
using Banshee.PlaybackController;
using Banshee.ServiceStack;
using Mono.Addins;
using Mirage;

namespace Banshee.Mirage
{
    public class RandomBySimilar : RandomBy
    {
        // At a high level, what we're trying to do is get the most similar track
        // to the seed tracks, excluding played/skipped ones[, and ones too similar to skipped ones].
        //
        // The RANDOM_CONDITION will exclude the played/skipped items for us, but we need to
        // add the Similarity-based selection[, exclusion], and ordering.

        public RandomBySimilar () : base ("mirage_similar")
        {
            MiragePlugin.Init ();

            Label = AddinManager.CurrentLocalizer.GetString ("Shuffle by Similar");
            Adverb = AddinManager.CurrentLocalizer.GetString ("by similar");
            Description = AddinManager.CurrentLocalizer.GetString ("Play songs similar to those already played");

            Select = ", HYENA_BINARY_FUNCTION ('MIRAGE_DISTANCE', ?, mirage.ScmsData) as Distance";
            From = "LEFT OUTER JOIN MirageTrackAnalysis mirage ON (mirage.TrackID = CoreTracks.TrackID) ";
            Condition = "mirage.Status = 0 AND CoreTracks.ArtistID NOT IN (?) AND Distance > 0";
            OrderBy = "Distance ASC, RANDOM ()";
        }

        public override bool Next (DateTime after)
        {
            return true;
        }

        protected override RandomBy.QueryContext GetQueryContext (DateTime after)
        {
            return GetSimilarityContext (after, Shuffler == Shuffler.Playback);
        }

        private SimilarityContext GetSimilarityContext (DateTime after, bool playback)
        {
            var context = new SimilarityContext ();

            if (!playback) {
                // Manually added songs are the strongest postive signal for what we want
                context.AddSeeds (GetSeeds (
                    "d.ModificationType = 1 AND d.LastModifiedAt IS NOT NULL AND d.LastModifiedAt > ? ORDER BY d.LastModifiedAt DESC",
                    after, 4, SimilarityContext.SelectedWeight
                ));
            }

            // Played songs are the next strongest postive signal for what we want
            context.AddSeeds (GetSeeds (
                "t.LastPlayedStamp IS NOT NULL AND t.LastPlayedStamp > MAX (?, coalesce(d.LastModifiedAt, 0), coalesce(t.LastSkippedStamp, 0)) ORDER BY t.LastPlayedStamp DESC",
                after, playback ? 4 : 2, SimilarityContext.PlayedWeight
            ));

            if (!playback) {
                // Shuffled songs that the user hasn't removed are a decent, positive signal
                context.AddSeeds (GetSeeds (
                    "s.LastShuffledAt IS NOT NULL AND s.LastShuffledAt > MAX (?, coalesce(t.LastPlayedStamp, 0), coalesce(d.LastModifiedAt, 0), coalesce(t.LastSkippedStamp, 0)) ORDER BY s.LastShuffledAt DESC",
                    after, 2, SimilarityContext.ShuffledWeight
                ));

                // Discarded songs are a strong negative signal for what we want
                context.AddSeeds (GetSeeds (
                    "d.ModificationType = 0 AND d.LastModifiedAt IS NOT NULL AND d.LastModifiedAt > ? ORDER BY d.LastModifiedAt DESC",
                    after, 3, SimilarityContext.DiscardedWeight
                ));
            }

            // Skipped songs are also a strong negative signal for what we want
            context.AddSeeds (GetSeeds (
                "t.LastSkippedStamp IS NOT NULL AND t.LastSkippedStamp > ? ORDER BY t.LastSkippedStamp DESC",
                after, playback ? 4 : 2, SimilarityContext.SkippedWeight
            ));

            return context;
        }

        private IEnumerable<Seed> GetSeeds (string query, DateTime after, int limit, float weight)
        {
            var reader = ServiceManager.DbConnection.Query (
                String.Format (similarity_query, query),
                Shuffler.DbId, Shuffler.DbId, after, limit // these args assume the query string has exactly one ? meant for the after date
            );

            using (reader) {
                while (reader.Read ()) {
                    yield return new Seed () {
                        TrackId = Convert.ToInt32 (reader[0]),
                        Scms = Scms.FromBytes (reader[1] as byte[]),
                        Weight = weight,
                        ArtistId = Convert.ToInt32 (reader[2]),
                        Uri = reader[3] as string
                    };
                }
            }
        }

        const string similarity_query =
            @"SELECT a.TrackID, a.ScmsData, t.ArtistId, t.Uri FROM MirageTrackAnalysis a, CoreTracks t
                 WHERE a.Status = 0 AND a.TrackID = t.TrackID AND
                 a.TrackID IN (SELECT t.TrackID FROM
                        CoreTracks t LEFT OUTER JOIN
                        CoreShuffles s ON (s.ShufflerID = ? AND s.TrackID = t.TrackID) LEFT OUTER JOIN
                        CoreShuffleModifications d ON (d.ShufflerID = ? AND t.TrackID = d.TrackID)
                    WHERE {0} LIMIT ?)";
    }
}
