
using System;
using System.Linq;
using System.Collections.Generic;

using Mirage;

namespace Banshee.Mirage
{
    public class SingleSeed : BaseSeed
    {
        private List<Scms> seeds = new List<Scms> ();
        private List<Scms> anti_seeds = new List<Scms> ();

        public SingleSeed (Scms seed)
        {
            seeds.Add (seed);
        }

        public void AddSeed (Scms seed)
        {
            seeds.Add (seed);
        }

        public void AddAntiSeed (Scms anti_seed)
        {
            anti_seeds.Add (anti_seed);
        }

        public override IEnumerable<float> Distance (Scms from)
        {
            bool any = false;

            foreach (var ret in seeds.Select (seed => Scms.Distance (seed, from, Config)).Where (v => v > 0)) {
                if (ret < min_distance) {
                    min_distance = ret;
                } else if (ret > max_distance) {
                    max_distance = ret;
                }

                yield return ret;
                any = true;
            }

            if (!any) {
                yield return 0f;
            }
        }
    }
}