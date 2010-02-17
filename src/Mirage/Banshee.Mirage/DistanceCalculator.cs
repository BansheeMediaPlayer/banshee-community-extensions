
using System;
using System.Linq;
using System.Collections.Generic;

using Mirage;

namespace Banshee.Mirage
{
    public class DistanceCalculator
    {
        const string FUNC_NAME = "MIRAGE_DISTANCE";

        public static void Init ()
        {
        }

        static DistanceCalculator ()
        {
            Hyena.Data.Sqlite.BinaryFunction.Add (FUNC_NAME, Distance);
        }

        public static void Dispose ()
        {
            Hyena.Data.Sqlite.BinaryFunction.Add (FUNC_NAME, Distance);
        }

        private static Dictionary<int, BaseSeed> seeds = new Dictionary<int, BaseSeed> ();

        private static int seed_index;
        public static int AddSeed (BaseSeed seed)
        {
            lock (seeds) {
                seeds[seed_index] = seed;
                return seed_index++;
            }
        }

        public static void RemoveSeed (int seed_id)
        {
            lock (seeds) {
                seeds.Remove (seed_id);
            }
        }

        internal static long total_count = 0;
        internal static double total_ms = 0;
        internal static double total_read_ms = 0;

        private static object Distance (object seed_id_obj, object scms_obj)
        {
            var seed = seeds[(int) seed_id_obj];
            var scms_bytes = scms_obj as byte[];
            if (seed == null || scms_bytes == null)
                return Double.MaxValue;

            var start = DateTime.Now;
            Scms.FromBytes (scms_bytes, seed.TestScms);
            total_read_ms += (DateTime.Now - start).TotalMilliseconds;

            float distance = seed.Distance (seed.TestScms).Average ();
            total_ms += (DateTime.Now - start).TotalMilliseconds;
            total_count++;

            return distance;
        }
    }
}