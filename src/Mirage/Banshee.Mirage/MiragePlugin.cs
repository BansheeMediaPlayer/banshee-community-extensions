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
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Data;
using System.Text;

using Mono.Addins;

using Hyena;
using Hyena.Widgets;
using Banshee.Base;
using Banshee.Collection.Database;
using Banshee.ServiceStack;
using Banshee.Sources;
using Banshee.Gui;
using Banshee.Playlist;
using Banshee.Widgets;

using Mirage;

namespace Banshee.Mirage
{
    public class MiragePlugin : IExtensionService, IDisposable
    {
        //Db db;
        //PlaylistGeneratorSource continuousPlaylist;
        AnalyzeLibraryJob analysis_job;
        Thread dupesearchThread;
        ActionGroup actions;
        uint uiManagerId;
        InterfaceActionService action_service;
        
        void IExtensionService.Initialize ()
        {
            action_service = ServiceManager.Get<InterfaceActionService> ();

            string dbfile;

            // debugging option: check if we need to load a different database
            // file.
            if (ApplicationContext.CommandLine.Contains ("mirage-db")) {
                 dbfile = ApplicationContext.CommandLine["mirage-db"];
            } else {
                string xdgcachedir = Environment.GetEnvironmentVariable ("XDG_CACHE_HOME");
                if (xdgcachedir == null) {
                    xdgcachedir = Environment.GetFolderPath (Environment.SpecialFolder.Personal) +
                        "/.cache";
                }
                string dbdir = xdgcachedir + "/banshee-mirage";
                if (!Directory.Exists (dbdir)) {
                    Directory.CreateDirectory (dbdir);
                }
                dbfile = dbdir + "/mirage.db";
            }

            ServiceManager.DbConnection.Execute ("ATTACH DATABASE ? AS Mirage", dbfile);
            Hyena.Data.Sqlite.BinaryFunction.Add ("MIRAGE_DISTANCE", Distance);

            /*db = new Db (dbfile);
            Log.DebugFormat ("Mirage - Database Initialize (dbfile: {0})", dbfile);

            ServiceManager.DbConnection.Execute (
                    "CREATE TABLE IF NOT EXISTS MirageProcessed"
                    + " (TrackID INTEGER PRIMARY KEY, Status INTEGER)");
            
            if (db.WasReset) {
                ResetMirageProcessed ();
            }*/
            
            InstallInterfaceActions ();
            
            if (!ServiceStartup ()) {
                ServiceManager.SourceManager.SourceAdded += OnSourceAdded;
            }

            Log.Debug ("Mirage - Initialized");
        }

        internal static long total_count = 0;
        internal static double total_ms = 0;
        internal static double total_read_ms = 0;
        private static float min_distance = Single.MaxValue, max_distance = 0;
        private object Distance (object a_obj, object b_obj)
        {
            var start = DateTime.Now;
            var a = a_obj as byte[];
            var b = b_obj as byte[];
            if (a == null || b == null)
                return Double.MaxValue;

            var a_s = Scms.FromBytes (a);
            var b_s = Scms.FromBytes (b);
            total_read_ms += (DateTime.Now - start).TotalMilliseconds;
            var c = new ScmsConfiguration (Analyzer.MFCC_COEFFICIENTS);
            var ret = Scms.Distance (a_s, b_s, c);
            if (ret < min_distance) {
                min_distance = ret;
                Console.WriteLine ("New min distance: {0}", ret);
            }
            if (ret > max_distance) {
                max_distance = ret;
                Console.WriteLine ("New max distance: {0}", ret);
            }
            //Console.WriteLine ("Distance: {0}", ret);
            total_ms += (DateTime.Now - start).TotalMilliseconds;
            total_count++;
            return ret;
        }
        
        private void OnSourceAdded (SourceAddedArgs args)
        {
            if (ServiceStartup ()) {
                ServiceManager.SourceManager.SourceAdded -= OnSourceAdded;
            }
        }
        
        private bool ServiceStartup ()
        {
            if (ServiceManager.SourceManager.MusicLibrary == null)
                return false;

            //ScanLibrary ();
            
            //if (continuousPlaylist == null) {
                //ServiceManager.SourceManager.SourceAdded -= OnSourceAdded;

                //ServiceManager.SourceManager.MusicLibrary.TracksAdded += OnLibraryTracksAdded;
                //ServiceManager.SourceManager.MusicLibrary.TracksDeleted += OnLibraryTracksDeleted;
                
                //continuousPlaylist = new PlaylistGeneratorSource (db);
                //ServiceManager.SourceManager.AddSource (continuousPlaylist);
            //}
            
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

            try {
                Analyzer.CancelAnalyze ();
            } catch (Exception) {
            }

            /*if (continuousPlaylist != null) {
                continuousPlaylist.Dispose ();
                ServiceManager.SourceManager.MusicLibrary.RemoveChildSource (continuousPlaylist);
            }*/

            action_service.UIManager.RemoveUi (uiManagerId);
            action_service.UIManager.RemoveActionGroup (actions);
        }

        private void ScanLibrary ()
        {
            /*if (analysis_job == null) {
                analysis_job = new AnalyzeLibraryJob (db);
                analysis_job.Finished += delegate { analysis_job = null; };
            }*/
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
        }

        private void OnMirageResetHandler(object sender, EventArgs args)
        {
            /*MessageDialog md = new MessageDialog (null, DialogFlags.Modal, MessageType.Question,
                    ButtonsType.Cancel, AddinManager.CurrentLocalizer.GetString (
                        "Do you really want to reset the Mirage Extension?\n" +
                        "All extracted information will be lost. Your music will have to be re-analyzed to use Mirage again."));
            md.AddButton (AddinManager.CurrentLocalizer.GetString ("Reset Mirage"), ResponseType.Yes);
            ResponseType result = (ResponseType)md.Run();
            md.Destroy();

            if (result == ResponseType.Yes) {
                try {
                    Analyzer.CancelAnalyze();
                    ResetMirageProcessed();
                    db.Reset();

                    md = new MessageDialog(null, DialogFlags.Modal, MessageType.Info, ButtonsType.Ok,
                            AddinManager.CurrentLocalizer.GetString ("Mirage was reset. Your music will have to be re-analyzed to use Mirage again."));
                    md.Run();
                    md.Destroy();
                } catch (Exception) {
                    Log.Warning("Mirage - Error resetting Mirage.");
                }
            }*/

        }

        private void OnLibraryTracksAdded (object o, TrackEventArgs args)
        {
            ScanLibrary ();
        }

        private void OnLibraryTracksDeleted (object o, TrackEventArgs args)
        {
            /*IDataReader reader = ServiceManager.DbConnection.Query (
                @"SELECT TrackID FROM MirageProcessed WHERE TrackID NOT IN 
                    (SELECT TrackID from CoreTracks WHERE PrimarySourceID = ?)", 
                ServiceManager.SourceManager.MusicLibrary.DbId);
            
            List<int> track_ids = new List<int> ();
            while(reader.Read()) {
                track_ids.Add (Convert.ToInt32(reader["TrackID"]));
            }
            
            if (track_ids.Count > 0) {
                db.RemoveTracks (track_ids.ToArray());
                
                StringBuilder removeSql = new StringBuilder ("DELETE FROM MirageProcessed WHERE TrackID IN (");
                removeSql.Append (track_ids[0].ToString());
                for (int i = 1; i < track_ids.Count; i++) {
                    removeSql.AppendFormat(", {0}", track_ids[i]);
                }
                removeSql.Append (")");
                ServiceManager.DbConnection.Execute (removeSql.ToString());
            }*/
        }
        
        /*private void ResetMirageProcessed()
        {
            ServiceManager.DbConnection.Execute("DELETE FROM MirageProcessed");
        }*/

        string IService.ServiceName {
            get { return "MirageService"; }
        }
    }
}
