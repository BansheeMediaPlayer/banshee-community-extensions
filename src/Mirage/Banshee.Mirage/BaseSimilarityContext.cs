
using System;
using System.Collections.Generic;

using Mirage;

namespace Banshee.Mirage
{
    public abstract class BaseSimilarityContext : Banshee.Collection.Database.RandomBy.QueryContext
    {
        private int seed_id;
        protected ScmsConfiguration Config = new ScmsConfiguration (Analyzer.MFCC_COEFFICIENTS);

        protected float min_distance = Single.MaxValue, max_distance = 0;

        public int Id { get { return seed_id; } }

        public bool IsEmpty { get; protected set; }

        // Scms object that can be reused when testing various tracks sequentially,
        // avoiding creating new arrays for each.
        internal Scms ComparisonScms = new Scms (Analyzer.MFCC_COEFFICIENTS);

        public BaseSimilarityContext ()
        {
            seed_id = DistanceCalculator.AddSeed (this);
            IsEmpty = true;
            DistanceCalculator.total_count = 0;
            DistanceCalculator.total_ms = 0;
            DistanceCalculator.total_read_ms = 0;
        }

        public abstract IEnumerable<float> Distance (Scms from);

        public override void Dispose ()
        {
            DistanceCalculator.RemoveSeed (seed_id);

            if (MiragePlugin.Debug) {
                DumpDebug ();
            }

            base.Dispose ();
        }

        protected virtual void DumpDebug ()
        {
            Console.WriteLine (">>>>>>>>>>>>>> Total ms spent in Distance func: {0} ms - spent reading: {1} ms; total calls: {2}",
                               DistanceCalculator.total_ms, DistanceCalculator.total_read_ms, DistanceCalculator.total_count);
            Console.WriteLine (">>>>>>>>>>>>>> Distance [min, max] = [{0}, {1}]", min_distance, max_distance);
        }
    }
}
