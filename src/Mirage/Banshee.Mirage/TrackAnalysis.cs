
using System;
using Hyena.Data.Sqlite;
using Banshee.ServiceStack;

namespace Banshee.Mirage
{
    public enum AnalysisStatus
    {
        Succeeded = 0,
        Failed = 1,
        FileMissing = 2,
        UnknownFailure = 3
    }

    public class TrackAnalysis
    {
        public static readonly SqliteModelProvider<TrackAnalysis> Provider;

        [DatabaseColumn (Constraints = DatabaseColumnConstraints.PrimaryKey)]
        public long TrackId;

        [DatabaseColumn]
        public byte [] ScmsData;

        [DatabaseColumn]
        public AnalysisStatus Status;

        public TrackAnalysis ()
        {
        }

        public static void Init ()
        {
        }

        static TrackAnalysis ()
        {
            Provider = new SqliteModelProvider<TrackAnalysis> (ServiceManager.DbConnection, "MirageTrackAnalysis", true);
        }
    }
}