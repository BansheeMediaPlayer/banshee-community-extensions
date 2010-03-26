
using System;
using System.Linq;
using System.Collections.Generic;

using Mirage;
using Mono.Addins;

namespace Banshee.Mirage
{
    public class DistanceCalculator
    {
        const string FUNC_NAME = "MIRAGE_DISTANCE";

        public static void Init ()
        {
            Hyena.Data.Sqlite.BinaryFunction.Add (FUNC_NAME, Distance);
        }

        public static void Dispose ()
        {
            Hyena.Data.Sqlite.BinaryFunction.Remove (FUNC_NAME);
            seeds.Clear ();
        }

        private static Dictionary<int, BaseSimilarityContext> seeds = new Dictionary<int, BaseSimilarityContext> ();

        private static int seed_index;
        public static int AddSeed (BaseSimilarityContext seed)
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

        internal static string notify_string = AddinManager.CurrentLocalizer.GetString ("The Mirage extension is still analyzing your songs.  Until its finished, shuffle and fill by similar may not perform properly.");

        private static object Distance (object seed_id_obj, object scms_obj)
        {
            BaseSimilarityContext context;
            if (!seeds.TryGetValue (Convert.ToInt32 (seed_id_obj), out context))
                throw new ArgumentException ("seed_id not found", "seed_id_obj");

            var scms_bytes = scms_obj as byte[];
            if (scms_bytes == null) {
                // TODO raise an event to notify the user (one time only) that
                // there are un-analyzed tracks?
                // notify_string
                return Double.MaxValue;
            }

            var start = DateTime.Now;
            Scms.FromBytes (scms_bytes, context.ComparisonScms);
            total_read_ms += (DateTime.Now - start).TotalMilliseconds;

            float distance = context.Distance (context.ComparisonScms).Average ();
            total_ms += (DateTime.Now - start).TotalMilliseconds;
            total_count++;

            return distance;
        }
    }
}