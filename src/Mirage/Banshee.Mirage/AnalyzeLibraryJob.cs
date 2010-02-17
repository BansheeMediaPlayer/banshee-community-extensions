
using System;

using Mono.Addins;

using Hyena;
using Hyena.Jobs;
using Hyena.Data.Sqlite;
using Banshee.ServiceStack;

using Mirage;
using Banshee.Collection.Database;

namespace Banshee.Mirage
{
    public class AnalyzeLibraryJob : DbIteratorJob
    {
        TrackAnalysis analysis = new TrackAnalysis ();

        public AnalyzeLibraryJob () : base (AddinManager.CurrentLocalizer.GetString ("Analyzing Song Similarity"))
        {
            IconNames = new string [] {"audio-x-generic"};
            IsBackground = true;
            SetResources (Resource.Cpu, Resource.Disk);
            PriorityHints = PriorityHints.LongRunning;

            var music_id = ServiceManager.SourceManager.MusicLibrary.DbId;

            CountCommand = new HyenaSqliteCommand (String.Format (
                @"SELECT COUNT(*)
                    FROM CoreTracks
                    WHERE PrimarySourceID IN ({0}) AND TrackID NOT IN
                        (SELECT TrackID FROM MirageTrackAnalysis)",
                music_id
            ));

            SelectCommand = new HyenaSqliteCommand (String.Format (@"
                SELECT TrackID
                    FROM CoreTracks
                    WHERE PrimarySourceID IN ({0}) AND TrackID NOT IN
                        (SELECT TrackID FROM MirageTrackAnalysis)
                    ORDER BY Rating DESC, PlayCount DESC LIMIT 1",
                music_id
            ));

            CancelMessage = AddinManager.CurrentLocalizer.GetString (
                "Are you sure you want to stop Mirage?\n" +
                "Shuffle by Similar will only work for the tracks which are already analyzed. " +
                "The operation can be resumed at any time from the <i>Tools</i> menu."
            );
            CanCancel = true;

            Register ();
        }

        protected override void IterateCore (HyenaDataReader reader)
        {
            var track = DatabaseTrackInfo.Provider.FetchSingle (reader.Get<long> (0));
            bool cancelled = false;
            if (track != null && track.Uri != null && track.Uri.IsLocalPath) {
                analysis.TrackId = track.TrackId;
                try {
                    if (Banshee.IO.File.Exists (track.Uri)) {
                        Log.DebugFormat ("Mirage - Processing {0}-{1}-{2}", track.TrackId, track.ArtistName, track.TrackTitle);
                        Status = String.Format("{0} - {1}", track.ArtistName, track.TrackTitle);
                        analysis.ScmsData = Analyzer.Analyze (track.Uri.LocalPath).ToBytes ();
                        analysis.Status = AnalysisStatus.Succeeded;
                    } else {
                        analysis.Status = AnalysisStatus.FileMissing;
                    }
                } catch (AudioDecoderCanceledException) {
                    cancelled = true;
                } catch (AudioDecoderErrorException) {
                    analysis.Status = AnalysisStatus.Failed;
                } catch (Exception e) {
                    analysis.Status = AnalysisStatus.UnknownFailure;
                    Log.Exception ("Unexpected exception doing Mirage analysis", e);
                } finally {
                    if (!cancelled) {
                        TrackAnalysis.Provider.Delete (analysis.TrackId);
                        // Manually do this query instead of using Save since we need to be able to specify
                        // the primarykey value
                        TrackAnalysis.Provider.Connection.Execute (
                            "INSERT INTO MirageTrackAnalysis (TrackID, ScmsData, Status) VALUES (?, ?, ?)",
                            analysis.TrackId, analysis.ScmsData, (int)analysis.Status
                        );
                    }
                }
            }
        }
    }
}