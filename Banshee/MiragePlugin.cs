/*
 * Mirage - High Performance Music Similarity and Automatic Playlist Generator
 * http://hop.at/mirage
 * 
 * Copyright (C) 2007 Dominik Schnitzer <dominik@schnitzer.at>
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

using Banshee.Base;
using Banshee.Kernel;
using Banshee.Sources;
using Banshee.Configuration;
using Banshee.Widgets;

using System;
using System.IO;
using System.Collections;
using System.Threading;
using System.Data;
using System.Text;

using Mono.Unix;

using Mirage;


public static class PluginModuleEntry
{
    public static Type [] GetTypes()
    {
        return new Type [] {
            typeof(Banshee.Plugins.Mirage.MiragePlugin)
        };
    }
}


namespace Banshee.Plugins.Mirage
{

    public class MiragePlugin : Banshee.Plugins.Plugin
    {
        Db db;
        ContinuousGeneratorSource continuousPlaylist;

        Queue jobQueue;
        Thread jobThread;
        int jobsScheduled;

        bool processing;
        object processingMutex;

        bool rescanFailed;
        Thread scanThread;

        ActionGroup actions;
        ActiveUserEvent userEvent;
        uint uiManagerId;
        
        protected override string ConfigurationName {
            get {
                return "Mirage";
            }
        }
        
        public override string DisplayName
        {
            get {
                return Catalog.GetString("Automatic Playlist Generator");
            }
        }

        public override string Description
        {
            get {
                return String.Format ("{0}\n\n{1}",
                    Catalog.GetString ("Drag a song on the automatic playlist generator, "+
                            "Mirage will then try to automatically generate a playlist of "+
                            "similar songs.\nMirage only looks at the audio signal!"),
                    "http://hop.at/mirage/");
            }
        }

        public override string [] Authors
        {
            get {
                return new string [] { "Dominik Schnitzer" };
            }
        }
        
        protected override void PluginInitialize()
        {
            Catalog.Init("Mirage", Config.LocaleDir);

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

            Globals.Library.Db.Execute(
                    "CREATE TABLE IF NOT EXISTS MirageProcessed"
                    + " (TrackID INTEGER PRIMARY KEY, Status INTEGER)");

            if(Globals.Library.IsLoaded) {
                OnLibraryReloaded(null, EventArgs.Empty);
            } else {
                Globals.Library.Reloaded += OnLibraryReloaded;
            }
            Globals.Library.TrackRemoved += OnLibraryTrackRemoved;
            
            continuousPlaylist =
                    new ContinuousGeneratorSource("Playlist Generator", new Db(dbfile));
            LibrarySource.Instance.AddChildSource(continuousPlaylist);

            if (!Globals.UIManager.IsInitialized) {
                Globals.UIManager.Initialized += OnInterfaceInitialized;
            } else {
                OnInterfaceInitialized(null, null);
            }
        }
        
        protected override void PluginDispose()
        {
            Globals.Library.Reloaded -= OnLibraryReloaded;
            Globals.Library.TrackAdded -= OnLibraryTrackAdded;
            Globals.Library.TrackRemoved -= OnLibraryTrackRemoved;

            // cancel analysis, everything
            lock(jobQueue) {
                jobQueue.Clear();
            }
            try {
                Mir.CancelAnalyze();
            } catch (Exception) {
            }

            LibrarySource.Instance.RemoveChildSource(continuousPlaylist);

            Globals.ActionManager.UI.RemoveUi(uiManagerId);
            Globals.ActionManager.UI.RemoveActionGroup(actions);
        }

        private void OnInterfaceInitialized(object o, EventArgs args)
        {
            ScanLibrary();
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
            Dbg.WriteLine("Mirage: Scanning library for tracks to update");

            // TODO: Eliminate this...
            System.Threading.Thread.Sleep(5000);

            String query;
            if (rescanFailed) {
                query = "SELECT TrackID FROM Tracks WHERE TrackID NOT IN"
                    + " (SELECT Tracks.TrackID FROM MirageProcessed, Tracks"
                    + " WHERE (Tracks.TrackID = MirageProcessed.TrackID) AND"
                    + " (MirageProcessed.Status = 0))"
                    + " ORDER BY Rating DESC, NumberOfPlays DESC";
            } else {
                query = "SELECT TrackID FROM Tracks WHERE TrackID NOT IN"
                    + " (SELECT Tracks.TrackID FROM MirageProcessed, Tracks"
                    + " WHERE (Tracks.TrackID = MirageProcessed.TrackID))"
                    + " ORDER BY Rating DESC, NumberOfPlays DESC";
            }

            IDataReader reader = Globals.Library.Db.Query(query);

            lock(jobQueue) {
                jobsScheduled = 0;
                while(reader.Read()) {
                    int trackId = Convert.ToInt32(reader["TrackID"]);
                    jobQueue.Enqueue(trackId);
                    jobsScheduled++;
                }
            }

            reader.Dispose();
            Dbg.WriteLine("Mirage: Done scanning library");

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
            lock(jobQueue) {
                if (jobQueue.Count <= 0) {
                    lock (processingMutex) {
                        processing = false;
                    }
                    return;
                }
            }

            // Banshee user event
            userEvent = new ActiveUserEvent("Mirage");
            userEvent.Header = Catalog.GetString("Mirage: Analyzing Songs");
            userEvent.CancelMessage = Catalog.GetString(
                "Are you sure you want to stop Mirage. "
                + "Automatic Playlist Generation will only work for the tracks which are already analyzed. "
                + "The operation can be resumed at any time from the <i>Tools</i> menu.");
            userEvent.Icon = IconThemeUtils.LoadIcon(22, "audio-x-generic");
            userEvent.CancelRequested += OnUserEventCancelRequested;
            userEvent.Progress = 0;

            do {
                lock(jobQueue) {
                    trackId = (int)jobQueue.Dequeue();
                    queueLength = jobQueue.Count;
                }

                TrackInfo track = Globals.Library.GetTrack(trackId);

                if (track == null) {
                    lock (processingMutex) {
                        processing = false;
                    }
                    return;
                }

                if (track.Uri.IsLocalPath) {
                    int status = 0;
                    try {
                        Dbg.WriteLine("Mirage: processing " + track.TrackId + "/" + track.Artist + "/" + track.Title);

                        userEvent.Message = String.Format("{0} - {1}", track.Artist, track.Title);
                        Scms scms = Mir.Analyze(track.Uri.LocalPath);
                        db.AddTrack(track.TrackId, scms);
                    } catch (DbFailureException) {
                        status = -2;
                    } catch (MirAnalysisImpossibleException) {
                        status = -1;
                    } finally {
                        // Banshee user event
                        userEvent.Progress = 1 - (double)queueLength/(double)jobsScheduled;

                        Globals.Library.Db.Execute("INSERT INTO MirageProcessed"
                                + " (TrackID, Status) VALUES (" + track.TrackId + ", "
                                + status + ")");
                    }

                }

            } while (jobQueue.Count > 0);

            jobsScheduled = 0;

            if (userEvent != null) {
                userEvent.CancelRequested -= OnUserEventCancelRequested;
                userEvent.Dispose();
                userEvent = null;
            }

            lock (processingMutex) {
                processing = false;
            }
        }

        protected override void InterfaceInitialize()
        {
            InstallInterfaceActions();
        }

        private void InstallInterfaceActions()
        {
            actions = new ActionGroup("Mirage Playlist Generator");

            // Pixbufs in 'PodcastPixbufs' should be registered with the StockManager and used here.
            actions.Add(new ActionEntry [] {
                    new ActionEntry ("MirageAction", null,
                        Catalog.GetString ("Mirage Playlist Generator"), null,
                        Catalog.GetString ("Manage the Mirage plugin"), null),

                    new ActionEntry("MirageRescanMusicAction", Stock.Refresh,
                        Catalog.GetString("Rescan the Music Collection"), null,
                        Catalog.GetString("Rescans the Music Collection for new Songs"),
                        OnMirageRescanMusicHandler),

                    new ActionEntry("MirageResetAction", Stock.Clear,
                        Catalog.GetString("Reset Mirage"), null,
                        Catalog.GetString("Resets the Mirage Playlist Generation Plugin. "+
                            "All songs have to be analyzed again to use Automatic Playlist Generation."),
                        OnMirageResetHandler),
                    });

            Globals.ActionManager.UI.InsertActionGroup(actions, 0);
            uiManagerId = Globals.ActionManager.UI.AddUiFromResource("MirageMenu.xml");
        }

        private void OnMirageRescanMusicHandler(object sender, EventArgs args)
        {
            if (((jobThread == null) && (scanThread == null)) ||
                (!jobThread.IsAlive && !scanThread.IsAlive)) {
                Dbg.WriteLine("Mirage: Rescan");
                rescanFailed = true;
                ScanLibrary();
            }
        }

        private void OnMirageResetHandler(object sender, EventArgs args)
        {
            MessageDialog md = new MessageDialog (null, DialogFlags.Modal, MessageType.Question,
                    ButtonsType.Cancel, Catalog.GetString("Do you really want to reset the Mirage Automatic Playlist Generation Plugin. "+
                    "All extracted information will be lost. Your music will have to be re-analyzed to use Mirage again."));
            md.AddButton(Catalog.GetString("Reset Mirage"), ResponseType.Yes);
            ResponseType result = (ResponseType)md.Run();
            md.Destroy();

            if (result == ResponseType.Yes) {
                Dbg.WriteLine("Mirage: Reset");
                if (userEvent != null) {
                    userEvent.CancelRequested -= OnUserEventCancelRequested;
                    userEvent.Dispose();
                    userEvent = null;
                }
                lock(jobQueue) {
                    jobQueue.Clear();
                }

                try {
                    Mir.CancelAnalyze();
                    Globals.Library.Db.Execute("DELETE FROM MirageProcessed");
                    db.Reset();

                    md = new MessageDialog(null, DialogFlags.Modal, MessageType.Info, ButtonsType.Ok,
                            Catalog.GetString("Mirage was reset. Your music will have to be re-analyzed to use Mirage again."));
                    md.Run();
                    md.Destroy();
                } catch (Exception) {
                    Dbg.WriteLine("Mirage: Error resetting Mirage.");
                }

            }

        }

        private void OnLibraryReloaded(object o, EventArgs args)
        {
            Globals.Library.TrackAdded += OnLibraryTrackAdded;
        }

        private void OnLibraryTrackAdded(object o, LibraryTrackAddedArgs args)
        {
            lock(jobQueue) {
                jobQueue.Enqueue(args.Track.TrackId);
                jobsScheduled++;
            }
            if (((jobThread == null) && (scanThread == null)) ||
                (!jobThread.IsAlive && !scanThread.IsAlive)) {
                ProcessQueue();
            }
        }

        private void OnLibraryTrackRemoved(object o, LibraryTrackRemovedArgs args)
        {
            Dbg.WriteLine("Mirage: Deleted track.");

            int[] trackids = new int[args.Tracks.Count];
            int i = 0;
            foreach(TrackInfo track in args.Tracks) {
                trackids[i] = track.TrackId;
                i++;
            }
            try {
                db.RemoveTracks(trackids);
            } catch (Exception) {
            }

            StringBuilder removeSql = new StringBuilder("DELETE FROM MirageProcessed WHERE TrackID IN (");
            removeSql.Append(trackids[0].ToString());
            for (i = 1; i < trackids.Length; i++) {
                removeSql.Append("," + trackids[i]);
            }
            removeSql.Append(")");
            Globals.Library.Db.Execute(removeSql.ToString());
        }

        private void OnUserEventCancelRequested(object o, EventArgs args)
        {
            if (userEvent != null) {
                userEvent.CancelRequested -= OnUserEventCancelRequested;
                userEvent.Dispose();
                userEvent = null;
            }

            lock(jobQueue) {
                jobQueue.Clear();
            }
        }

        public static readonly SchemaEntry<bool> EnabledSchema = new SchemaEntry<bool>(
            "plugins.mirage", "enabled",
            true,
            "Plugin enabled",
            "Playlist Generation plugin enabled"
        );

    }
}
