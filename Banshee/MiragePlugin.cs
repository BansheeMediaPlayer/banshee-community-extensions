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
using Banshee.Collection.Database;
using Banshee.Configuration;
using Banshee.ServiceStack;
using Banshee.Sources;
using Banshee.Gui;

using Mirage;

namespace Banshee.Mirage
{
    public class MiragePlugin : IExtensionService, IDisposable
    {
        Db db;
        PlaylistGeneratorSource continuousPlaylist;

        Queue jobQueue;
        Thread jobThread;
        int jobsScheduled;

        bool processing;
        object processingMutex;

        bool rescanFailed;
        Thread scanThread;

        ActionGroup actions;
        uint uiManagerId;
        
        InterfaceActionService action_service;
        
        void IExtensionService.Initialize ()
        {
            action_service = ServiceManager.Get<InterfaceActionService> ("InterfaceActionService");
            
            string xdgcachedir = Environment.GetEnvironmentVariable("XDG_CACHE_HOME");
            if (xdgcachedir == null) {
                xdgcachedir = Environment.GetFolderPath(Environment.SpecialFolder.Personal) + 
                    "/.cache";
            }
            string dbdir = xdgcachedir + "/banshee-mirage";
            if (!Directory.Exists(dbdir)) {
                Directory.CreateDirectory(dbdir);
            }
            string dbfile = dbdir + "/mirage.db";

            db = new Db(dbfile);
            
            jobsScheduled = 0;
            jobQueue = new Queue();
            rescanFailed = false;

            scanThread = null;
            jobThread = null;

            processingMutex = new object();
            processing = false;

            ServiceManager.DbConnection.Execute(
                    "CREATE TABLE IF NOT EXISTS MirageProcessed"
                    + " (TrackID INTEGER PRIMARY KEY, Status INTEGER)");
            
            if (db.WasReset) {
                ResetMirageProcessed();
            }
            
            InstallInterfaceActions();
            
            if (!ServiceStartup ()) {
                ServiceManager.SourceManager.SourceAdded += OnSourceAdded;
            }
            
            Log.Debug("Mirage - Initialized");
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
            
            if (continuousPlaylist == null) {
                ServiceManager.SourceManager.SourceAdded -= OnSourceAdded;

                ServiceManager.SourceManager.MusicLibrary.TracksAdded += OnLibraryTracksAdded;
                ServiceManager.SourceManager.MusicLibrary.TracksDeleted += OnLibraryTracksDeleted;
                
                continuousPlaylist = new PlaylistGeneratorSource(db);
                ServiceManager.SourceManager.AddSource (continuousPlaylist);
                //ScanLibrary();
            }
            
            return true;
        }
        
        public void Dispose ()
        {
            ServiceManager.SourceManager.MusicLibrary.TracksAdded -= OnLibraryTracksAdded;
            ServiceManager.SourceManager.MusicLibrary.TracksDeleted -= OnLibraryTracksDeleted;

            // cancel analysis, everything
            lock (jobQueue) {
                jobQueue.Clear();
            }
            try {
                Mir.CancelAnalyze();
            } catch (Exception) {
            }

            if (continuousPlaylist != null) {
                continuousPlaylist.Dispose ();
                ServiceManager.SourceManager.MusicLibrary.RemoveChildSource(continuousPlaylist);
            }

            action_service.UIManager.RemoveUi(uiManagerId);
            action_service.UIManager.RemoveActionGroup(actions);
        }

        private void ScanLibrary()
        {
            scanThread = new Thread(ScanLibraryThread);
            scanThread.IsBackground = true;
            scanThread.Priority = ThreadPriority.Lowest;
            scanThread.Start();
        }

        private void ScanLibraryThread()
        {
            Log.Debug("Mirage - Scanning library for tracks to update");
            
            String query;
            if (rescanFailed) {
                query = String.Format(
                    @"SELECT TrackID FROM CoreTracks 
                        WHERE PrimarySourceID = {0} AND TrackID NOT IN
                            (SELECT CoreTracks.TrackID FROM MirageProcessed, CoreTracks
                                WHERE CoreTracks.TrackID = MirageProcessed.TrackID AND
                                    MirageProcessed.Status = 0)
                        ORDER BY Rating DESC, PlayCount DESC",
                    ServiceManager.SourceManager.MusicLibrary.DbId);
            } else {
                query = String.Format(
                    @"SELECT TrackID FROM CoreTracks 
                        WHERE PrimarySourceID = {0} AND TrackID NOT IN
                            (SELECT CoreTracks.TrackID FROM MirageProcessed, CoreTracks
                                WHERE CoreTracks.TrackID = MirageProcessed.TrackID)
                        ORDER BY Rating DESC, PlayCount DESC",
                    ServiceManager.SourceManager.MusicLibrary.DbId);
            }

            IDataReader reader = ServiceManager.DbConnection.Query(query);

            lock (jobQueue) {
                jobsScheduled = 0;
                while(reader.Read()) {
                    int trackId = Convert.ToInt32(reader["TrackID"]);
                    jobQueue.Enqueue(trackId);
                    jobsScheduled++;
                }
            }

            reader.Dispose();
            Log.Debug("Mirage - Done scanning library");

            ProcessQueue();
        }


        private void ProcessQueue()
        {
            lock (processingMutex) {
                if (processing)
                    return;
                else
                    processing = true;
            }

            jobThread = new Thread(ProcessQueueThread);
            jobThread.IsBackground = true;
            jobThread.Priority = ThreadPriority.Lowest;
            jobThread.Start();
        }

        private void ProcessQueueThread()
        {
            int trackId = 0;
            int queueLength = 0;
            lock (jobQueue) {
                if (jobQueue.Count <= 0) {
                    lock (processingMutex) {
                        processing = false;
                    }
                    return;
                }
            }

            // Banshee user job
            UserJob userJob = new UserJob("Mirage", AddinManager.CurrentLocalizer.GetString ("Mirage: Analyzing Songs"), "audio-x-generic");
            userJob.CancelMessage = AddinManager.CurrentLocalizer.GetString (
                @"Are you sure you want to stop Mirage? 
                Automatic Playlist Generation will only work for the tracks which are already analyzed. 
                The operation can be resumed at any time from the <i>Tools</i> menu.");
            userJob.CanCancel = true;
            userJob.Progress = 0;
            userJob.Register();

            while (jobQueue.Count > 0 && !userJob.IsCancelRequested) {
                lock (jobQueue) {
                    trackId = (int)jobQueue.Dequeue();
                    queueLength = jobQueue.Count;
                }

                DatabaseTrackInfo track = DatabaseTrackInfo.Provider.FetchSingle(trackId);

                if (track == null) {
                    lock (processingMutex) {
                        processing = false;
                    }
                    break;
                }

                if (track.Uri != null && track.Uri.IsLocalPath) {
                    int status = 0;
                    try {
                        Log.DebugFormat ("Mirage - Processing {0}-{1}-{2}", track.TrackId, track.ArtistName, track.TrackTitle);

                        userJob.Status = String.Format("{0} - {1}", track.ArtistName, track.TrackTitle);
                        Scms scms = Mir.Analyze(track.Uri.LocalPath);
                        db.AddTrack(track.TrackId, scms);
                    } catch (DbFailureException) {
                        status = -2;
                    } catch (MirAnalysisImpossibleException) {
                        status = -1;
                    } finally {
                        // Banshee user job
                        userJob.Progress = 1 - (double)queueLength / (double)jobsScheduled;

                        ServiceManager.DbConnection.Execute (
                            @"DELETE FROM MirageProcessed WHERE TrackID = ?; 
                            INSERT INTO MirageProcessed (TrackID, Status) VALUES (?, ?)", 
                            track.TrackId, track.TrackId, status);
                    }
                }
            }

            jobsScheduled = 0;
            jobQueue.Clear();
            userJob.Finish();

            lock (processingMutex) {
                processing = false;
            }
        }

        private void InstallInterfaceActions()
        {
            actions = new ActionGroup("Mirage Playlist Generator");

            actions.Add(new ActionEntry [] {
                    new ActionEntry ("MirageAction", null,
                        AddinManager.CurrentLocalizer.GetString ("Mirage Playlist Generator"), null,
                        AddinManager.CurrentLocalizer.GetString ("Manage the Mirage plugin"), null),

                    new ActionEntry("MirageRescanMusicAction", Stock.Refresh,
                        AddinManager.CurrentLocalizer.GetString ("Rescan the Music Collection"), null,
                        AddinManager.CurrentLocalizer.GetString ("Rescans the Music Collection for new Songs"),
                        OnMirageRescanMusicHandler),

                    new ActionEntry("MirageResetAction", Stock.Clear,
                        AddinManager.CurrentLocalizer.GetString ("Reset Mirage"), null,
                        AddinManager.CurrentLocalizer.GetString ("Resets the Mirage Playlist Generation Plugin. "+
                            "All songs have to be analyzed again to use Automatic Playlist Generation."),
                        OnMirageResetHandler),
                    });

            action_service.UIManager.InsertActionGroup(actions, 0);
            uiManagerId = action_service.UIManager.AddUiFromResource("MirageMenu.xml");
        }

        private void OnMirageRescanMusicHandler(object sender, EventArgs args)
        {
            if (((jobThread == null) && (scanThread == null)) ||
                (!jobThread.IsAlive && !scanThread.IsAlive)) {
                Log.Debug("Mirage - Rescan");
                rescanFailed = true;
                ScanLibrary();
            }
        }

        private void OnMirageResetHandler(object sender, EventArgs args)
        {
            MessageDialog md = new MessageDialog (null, DialogFlags.Modal, MessageType.Question,
                    ButtonsType.Cancel, AddinManager.CurrentLocalizer.GetString (@"Do you really want to reset the Mirage Automatic Playlist Generation Extension? 
                    All extracted information will be lost. Your music will have to be re-analyzed to use Mirage again."));
            md.AddButton (AddinManager.CurrentLocalizer.GetString ("Reset Mirage"), ResponseType.Yes);
            ResponseType result = (ResponseType)md.Run();
            md.Destroy();

            if (result == ResponseType.Yes) {
                try {
                    Mir.CancelAnalyze();
                    ResetMirageProcessed();
                    db.Reset();

                    md = new MessageDialog(null, DialogFlags.Modal, MessageType.Info, ButtonsType.Ok,
                            AddinManager.CurrentLocalizer.GetString ("Mirage was reset. Your music will have to be re-analyzed to use Mirage again."));
                    md.Run();
                    md.Destroy();
                } catch (Exception) {
                    Log.Warning("Mirage - Error resetting Mirage.");
                }
            }

        }

        private void OnLibraryTracksAdded(object o, TrackEventArgs args)
        {
            // We have to scan the library to process new tracks
            ScanLibrary();
        }

        private void OnLibraryTracksDeleted(object o, TrackEventArgs args)
        {
            IDataReader reader = ServiceManager.DbConnection.Query (
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
            }
        }
        
        private void ResetMirageProcessed()
        {
            ServiceManager.DbConnection.Execute("DELETE FROM MirageProcessed");
        }

        string IService.ServiceName {
            get { return "MirageService"; }
        }
        
        public static readonly SchemaEntry<bool> ClearOnQuitSchema = new SchemaEntry<bool> (
            "plugins.mirage", "clear_on_quit",
            false,
            "Clear on Quit",
            "Clear the playlist when quitting"
        );

    }
}
