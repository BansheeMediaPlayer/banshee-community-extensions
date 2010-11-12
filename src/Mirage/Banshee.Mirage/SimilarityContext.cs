
using System;
using System.Linq;
using System.Collections.Generic;

using Mirage;

namespace Banshee.Mirage
{
    public class SimilarityContext : BaseSimilarityContext
    {
        public const float SelectedWeight = 4.0f;
        public const float PlayedWeight = 2.0f;
        public const float ShuffledWeight = 1.0f;
        public const float DiscardedWeight = 1.0f / 5.0f;
        public const float SkippedWeight = 1.0f / 10.0f;

        private List<Seed> seeds = new List<Seed> ();

        // This is for testing/debugging use
        private Scms best_scms;
        private bool debug;

        private static bool static_avoid_artists;
        private bool avoid_artists;

        public SimilarityContext ()
        {
            avoid_artists = static_avoid_artists;
            static_avoid_artists = !static_avoid_artists;
        }

        public void AddSeeds (IEnumerable<Seed> seeds)
        {
            foreach (var seed in seeds) {
                //Console.WriteLine ("Adding seed track ({0}) with weight {1}", seed.Uri, seed.Weight);
                if (seed.Scms != null) {
                    this.seeds.Add (seed);
                }
            }

            IsEmpty = this.seeds.Count == 0;
        }

        public int [] AvoidArtistIds {
            get {
                if (avoid_artists) {
                    return seeds.Select (s => s.ArtistId).ToArray ();
                } else {
                    return new int [0];
                }
            }
        }

        public override IEnumerable<object> Parameters {
            get {
                yield return Id;
                yield return AvoidArtistIds;
            }
            set {}
        }

        protected override void DumpDebug ()
        {
            var avoid_ids = String.Join (", ", AvoidArtistIds.Select (id => id.ToString ()).ToArray ());
            Console.WriteLine ("  Avoided artist ids = {0}\n  Seed Distances:", avoid_ids);

            debug = true;
            Console.WriteLine ("  Average weighted distance: {0:N1}", Distance (best_scms).Average ());
            debug = false;

            base.DumpDebug ();
        }

        public override IEnumerable<float> Distance (Scms from)
        {
            if (from == null) {
                yield return float.MaxValue;
                yield break;
            }

            bool any_seeds = false;
            float last_weight = -99;

            foreach (var seed in seeds) {
                var distance = Scms.Distance (seed.Scms, from, Config);
                if (distance < 0) {
                    // Ignore negative distance values
                    continue;
                }

                if (distance < min_distance) {
                    min_distance = distance;
                    best_scms = from;
                } else if (distance > max_distance) {
                    max_distance = distance;
                }

                float weighted_distance = distance / seed.Weight;
                if (debug) {
                    if (seed.Weight != last_weight) {
                        last_weight = seed.Weight;
                        Console.WriteLine ("    {0} seeds (weight {1,3:N1})", last_weight == ShuffledWeight ? "Shuffled" : last_weight == PlayedWeight ? "Played" : last_weight == SelectedWeight ? "Manually Selected" : "Skipped/Discarded", last_weight);
                    }

                    Console.WriteLine ("      distance: {0,4:N1}, weighted: {1,4:N1} from artist_id {2,4}, uri {3}",
                                       distance, weighted_distance, seed.ArtistId, seed.Uri);
                }
                yield return weighted_distance;
                any_seeds = true;
            }

            if (!any_seeds) {
                // Return the highest distance possible
                yield return Single.MaxValue;
            }
        }
    }
}
