
using System;

using Mono.Data.Sqlite;

using Mirage;

namespace Banshee.Mirage
{
    // Example usage:
    // db.QueryEnumerable<long> (
    //     "SELECT TrackID FROM mirage WHERE BANSHEE_MIRAGE_DISTANCE (?, Scms) < ?",
    //     seed_scms, distance_threshold
    // );

    [SqliteFunction (Name = "BANSHEE_MIRAGE_DISTANCE", FuncType = FunctionType.Scalar, Arguments = 2)]
    internal class MirageDistanceFunction : SqliteFunction
    {
        public override object Invoke (object [] args)
        {
            var a = args[0] as byte[];
            var b = args[1] as byte[];
            return a == null || b == null ? Double.MaxValue : Scms.Distance (args[0] as byte[], args[1] as byte[]);
        }
    }
}