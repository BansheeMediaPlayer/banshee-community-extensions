
using System;
using System.Collections.Generic;

using Mirage;

namespace Banshee.Mirage
{
    public abstract class BaseSeed : IDisposable
    {
        private int seed_id;
        protected ScmsConfiguration Config = new ScmsConfiguration (Analyzer.MFCC_COEFFICIENTS);

        protected float min_distance = Single.MaxValue, max_distance = 0;

        public int Id { get { return seed_id; } }

        // Scms object that can be reused when testing various tracks sequentially,
        // avoiding creating new arrays for each.
        public Scms TestScms = new Scms (Analyzer.MFCC_COEFFICIENTS);

        public BaseSeed ()
        {
            seed_id = DistanceCalculator.AddSeed (this);
            DistanceCalculator.total_count = 0;
            DistanceCalculator.total_ms = 0;
            DistanceCalculator.total_read_ms = 0;
        }

        public abstract IEnumerable<float> Distance (Scms from);

        public void Dispose ()
        {
            DistanceCalculator.RemoveSeed (seed_id);
            Console.WriteLine (">>>>>>>>>>>>>> Total ms spent in Distance func: {0} ms - spent reading: {1} ms; total calls: {2}",
                               DistanceCalculator.total_ms, DistanceCalculator.total_read_ms, DistanceCalculator.total_count);
            Console.WriteLine (">>>>>>>>>>>>>> Distance [min, max] = [{0}, {1}]", min_distance, max_distance);
        }
    }
}