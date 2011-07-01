/*
 * Mirage - High Performance Music Similarity and Automatic Playlist Generator
 * http://hop.at/mirage
 *
 * Copyright (C) 2007 Dominik Schnitzer <dominik@schnitzer.at>
 *           (C) 2008 Bertrand Lorentz <bertrand.lorentz@gmail.com>
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin Street, Fifth Floor,
 * Boston, MA  02110-1301, USA.
 */

using Gtk;

using System;
using System.Threading;

using Mono.Addins;

using Hyena;

using Banshee.Collection.Database;
using Banshee.ServiceStack;
using Banshee.Sources;
using Banshee.Gui;
using Banshee.Playlist;

using Mirage;

namespace Banshee.Mirage
{
    public class MiragePlugin : IExtensionService, IDelayedInitializeService, IDisposable
    {
        AnalyzeLibraryJob analysis_job;
        Thread dupesearchThread;
        ActionGroup actions;
        uint uiManagerId;
        InterfaceActionService action_service;

        internal static bool Debug;

        static bool initialized = false;

        static MiragePlugin instance = null;

        void IExtensionService.Initialize ()
        {
        }

        public void DelayedInitialize ()
        {
            if (instance != null)
                throw new InvalidOperationException ("A MiragePlugin instance is already in use");

            Init ();

            action_service = ServiceManager.Get<InterfaceActionService> ();

            TrackAnalysis.Init ();
            MigrateLegacyDb ();
            DistanceCalculator.Init ();

            InstallInterfaceActions ();

            if (!ServiceStartup ()) {
                ServiceManager.SourceManager.SourceAdded += OnSourceAdded;
            }

            instance = this;
        }

        internal static void Init ()
        {
            if (initialized)
                return;

            Debug = ApplicationContext.CommandLine.Contains ("debug-mirage");

            Analyzer.Init ();

            initialized = true;
        }

        private void OnSourceAdded (SourceAddedArgs args)
        {
            if (ServiceStartup ()) {
                ServiceManager.SourceManager.SourceAdded -= OnSourceAdded;
            }
        }

        private bool ServiceStartup ()
        {
            var music_library = ServiceManager.SourceManager.MusicLibrary;
            if (music_library == null) {
                return false;
            }

            music_library.TracksAdded += OnLibraryTracksAdded;
            music_library.TracksDeleted += OnLibraryTracksDeleted;
            ScanLibrary ();

            return true;
        }

        public void Dispose ()
        {
            ServiceManager.SourceManager.MusicLibrary.TracksAdded -= OnLibraryTracksAdded;
            ServiceManager.SourceManager.MusicLibrary.TracksDeleted -= OnLibraryTracksDeleted;

            if (analysis_job != null) {
                ServiceManager.JobScheduler.Cancel (analysis_job);
                analysis_job = null;
            }

            DistanceCalculator.Dispose ();

            try {
                Analyzer.CancelAnalyze ();
            } catch (Exception) {
            }

            action_service.UIManager.RemoveUi (uiManagerId);
            action_service.UIManager.RemoveActionGroup (actions);

            instance = null;
        }

        private void ScanLibrary ()
        {
            if (analysis_job == null) {
                analysis_job = new AnalyzeLibraryJob ();
                analysis_job.Finished += delegate { analysis_job = null; };
            }
        }

        private void DuplicateSearch ()
        {
            dupesearchThread = new Thread (DuplicateSearchThread);
            dupesearchThread.IsBackground = true;
            dupesearchThread.Priority = ThreadPriority.Lowest;
            dupesearchThread.Start();
        }

        private class DupePlaylistSource : PlaylistSource {

            public DupePlaylistSource() :
                base(AddinManager.CurrentLocalizer.GetString ("Mirage Duplicates"), ServiceManager.SourceManager.MusicLibrary)
            {
            }

            public void Add(DatabaseTrackInfo track)
            {
                AddTrack(track);
            }
        }

        private void DuplicateSearchThread()
        {
            /*Log.Debug("Mirage - Scanning library for duplicate tracks");

            UserJob userJob = new UserJob("Mirage",
                AddinManager.CurrentLocalizer.GetString (@"Mirage: Duplicate Search"),
                "audio-x-generic");
            userJob.CancelMessage = AddinManager.CurrentLocalizer.GetString (
                @"Are you sure you want to stop the Duplicate Search? ");
            userJob.CanCancel = true;
            userJob.Progress = 0;
            userJob.Register ();

            Dictionary<int, Scms> loadedDb = Analyzer.LoadLibrary (ref db);
            List<int> dupes = new List<int> ();
            Log.Debug ("Mirage - Database fully loaded!");


            DupePlaylistSource dupePlaylist = new DupePlaylistSource ();
            dupePlaylist.Save ();
            dupePlaylist.PrimarySource.AddChildSource (dupePlaylist);
            // FIXME : There's probably a better way to keep the same track order in the playlist
            dupePlaylist.DatabaseTrackModel.ForcedSortQuery = "CorePlaylistEntries.ViewOrder ASC, CorePlaylistEntries.EntryID ASC";

            int tracksProcessed = 0;

            foreach (int trackId in loadedDb.Keys) {

                userJob.Progress = (double)tracksProcessed / (double)loadedDb.Count;
                tracksProcessed++;

                // cancel if requested
                if (userJob.IsCancelRequested) {
                    break;
                }

                // skip if we have already identified the track as a dupe of another
                // track.
                if (dupes.Contains (trackId)) {
                    continue;
                }

                List<int> currentDupes = Analyzer.DuplicateSearch(loadedDb, trackId, 1);
                dupes.AddRange(currentDupes);

                DatabaseTrackInfo track = DatabaseTrackInfo.Provider.FetchSingle(trackId);
                if (track == null) {
                    continue;
                }
                Log.DebugFormat ("Mirage - Processing {0}-{1}-{2}", track.TrackId, track.ArtistName, track.TrackTitle);
                userJob.Status = String.Format("{0} - {1}", track.ArtistName, track.TrackTitle);

                if (currentDupes.Count > 0) {
                    dupePlaylist.Add(track);
                    foreach (int dupeId in currentDupes) {
                        DatabaseTrackInfo qtrack = DatabaseTrackInfo.Provider.FetchSingle(dupeId);
                        if (qtrack != null) {
                            Log.DebugFormat ("Mirage - Duplicate: {0} - {1} : {2}", qtrack.ArtistName, qtrack.TrackTitle, qtrack.Uri);
                            dupePlaylist.Add(qtrack);
                        }
                    }
                    dupePlaylist.NotifyUser ();
                }
            }
            dupePlaylist.DatabaseTrackModel.Reload ();
            dupePlaylist.DatabaseTrackModel.ForcedSortQuery = null;

            userJob.Finish();

            Gtk.Application.Invoke(delegate {
                HigMessageDialog.RunHigMessageDialog (null, DialogFlags.Modal,
                        MessageType.Info, ButtonsType.Ok, "Duplicate Search finished.",
                        AddinManager.CurrentLocalizer.GetString (
                            "The Mirage Duplicate Search finished. Check the newly created <i>Mirage Duplicates</i> playlist for possible duplicates."));
            });*/
        }

        private void InstallInterfaceActions()
        {
            actions = new ActionGroup("Mirage Playlist Generator");

            actions.Add(new ActionEntry [] {
                    new ActionEntry ("MirageAction", null,
                        AddinManager.CurrentLocalizer.GetString ("Mirage Playlist Generator"), null,
                        AddinManager.CurrentLocalizer.GetString ("Manage the Mirage extension"), null),

                    new ActionEntry("MirageRescanMusicAction", null,
                        AddinManager.CurrentLocalizer.GetString ("Rescan the Music Library"), null,
                        AddinManager.CurrentLocalizer.GetString ("Rescans the Music Library for new songs"),
                        OnMirageRescanMusicHandler),

                    new ActionEntry("MirageDuplicateSearchAction", null,
                        AddinManager.CurrentLocalizer.GetString ("Duplicate Search (Experimental)"), null,
                        AddinManager.CurrentLocalizer.GetString ("Searches your Music Library for possible duplicates"),
                        OnMirageDuplicateSearchHandler),

                    new ActionEntry("MirageResetAction", null,
                        AddinManager.CurrentLocalizer.GetString ("Reset Mirage"), null,
                        AddinManager.CurrentLocalizer.GetString ("Resets the Mirage Playlist Generation Plugin. "+
                            "All songs have to be analyzed again to use Automatic Playlist Generation."),
                        OnMirageResetHandler),
                    });

            action_service.UIManager.InsertActionGroup(actions, 0);
            uiManagerId = action_service.UIManager.AddUiFromResource("MirageMenu.xml");
        }

        private void OnMirageDuplicateSearchHandler(object sender, EventArgs args)
        {
            MessageDialog md = new MessageDialog (null, DialogFlags.Modal, MessageType.Question,
                    ButtonsType.Cancel, AddinManager.CurrentLocalizer.GetString (
                        "<b>Mirage can search your music library for duplicate music pieces.</b>\n\n"+
                        "· To do so, your music library needs to be analyzed completely from Mirage.\n" +
                        "· This process will take a long time depending on the size of your library."));
            md.AddButton (AddinManager.CurrentLocalizer.GetString ("Scan for Duplicates"), ResponseType.Yes);
            ResponseType result = (ResponseType)md.Run();
            md.Destroy();

            if (result == ResponseType.Yes) {
                try {
                    DuplicateSearch();
                } catch (Exception) {
                    Log.Warning("Mirage - Error scanning for duplicates.");
                }
            }
        }

        private void OnMirageRescanMusicHandler(object sender, EventArgs args)
        {
            Log.Debug("Mirage - Rescan");
            ScanLibrary();
        }

        private void OnMirageResetHandler(object sender, EventArgs args)
        {
            MessageDialog md = new MessageDialog (null, DialogFlags.Modal, MessageType.Question,
                    ButtonsType.Cancel, AddinManager.CurrentLocalizer.GetString (
                        "Do you really want to reset the Mirage Extension?\n" +
                        "All extracted information will be lost. Your music will have to be re-analyzed to use Mirage again."));
            md.AddButton (AddinManager.CurrentLocalizer.GetString ("Reset Mirage"), ResponseType.Yes);
            ResponseType result = (ResponseType)md.Run();
            md.Destroy();

            if (result == ResponseType.Yes) {
                try {
                    Analyzer.CancelAnalyze();

                    TrackAnalysis.Provider.Delete ("1");

                    md = new MessageDialog(null, DialogFlags.Modal, MessageType.Info, ButtonsType.Ok,
                            AddinManager.CurrentLocalizer.GetString ("Mirage was reset. Your music will have to be re-analyzed to use Mirage again."));
                    md.Run();
                    md.Destroy();
                } catch (Exception) {
                    Log.Warning("Mirage - Error resetting Mirage.");
                }
            }
        }

        private void OnLibraryTracksAdded (object o, TrackEventArgs args)
        {
            ScanLibrary ();
        }

        private void OnLibraryTracksDeleted (object o, TrackEventArgs args)
        {
            TrackAnalysis.Provider.Delete (
                "TrackID NOT IN (SELECT TrackID from CoreTracks WHERE PrimarySourceID = ?)",
                ServiceManager.SourceManager.MusicLibrary.DbId
            );
        }

        string IService.ServiceName {
            get { return "MirageService"; }
        }

        private void MigrateLegacyDb ()
        {
            string db_path = Paths.Combine (XdgBaseDirectorySpec.GetUserDirectory ("XDG_CACHE_HOME", ".cache"), "banshee-mirage", "mirage.db");
            var db_uri = new SafeUri (db_path);
            if (!Banshee.IO.File.Exists (db_uri)) {
                return;
            }

            long analysis_count = ServiceManager.DbConnection.Query<long> (String.Format ("SELECT COUNT(*) FROM {0}", TrackAnalysis.Provider.TableName));
            if (analysis_count > 0) {
                return;
            }

            try {
                // Copy the external db's data into the main banshee.db
                var db = ServiceManager.DbConnection;
                db.Execute ("ATTACH DATABASE ? AS Mirage", db_path);
                db.Execute (String.Format ("INSERT INTO {0} (TrackID, ScmsData, Status) SELECT trackid, scms, 0 FROM Mirage.mirage", TrackAnalysis.Provider.TableName));
                db.Execute ("DETACH DATABASE Mirage");
                Banshee.IO.Utilities.DeleteFileTrimmingParentDirectories (db_uri);

                // Migrate the status info from the already-local MirageProcessed table
                if (db.TableExists ("MirageProcessed")) {
                    db.Execute (String.Format (
                        @"INSERT OR IGNORE INTO {0} (TrackID, ScmsData, Status) SELECT TrackID, NULL,
                        CASE Status WHEN 0 THEN 0 WHEN -1 THEN {1} WHEN -2 THEN {2} END FROM MirageProcessed WHERE Status != 0",
                        TrackAnalysis.Provider.TableName, (int)AnalysisStatus.Failed, (int)AnalysisStatus.UnknownFailure
                    ));
                    db.Execute ("DROP TABLE MirageProcessed");
                }
            } catch (Exception e) {
                Log.Exception ("Failed to migrate old Mirage database", e);
            }
        }
    }
}
