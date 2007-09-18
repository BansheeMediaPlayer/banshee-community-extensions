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


using Banshee.Base;
using Banshee.Kernel;
using Banshee.Sources;
using Banshee.Configuration;
using Banshee.Widgets;
using Mono.Unix;
using System;
using System.Collections;
using System.Threading;
using System.Data;
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
        ContinuousGeneratorSource continuousPlaylist = null;

        Queue jobQueue;
        Thread jobThread;
        int jobsScheduled = 0;

        private ActiveUserEvent userEvent;
        
        protected override string ConfigurationName {
            get {
                return Catalog.GetString("Mirage");
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
                    Catalog.GetString ("http://hop.at/mirage/"));
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
            db = new Db();

            jobsScheduled = 0;
            jobQueue = new Queue();

            jobThread = new Thread(ProcessQueueThread);
            jobThread.IsBackground = true;
            jobThread.Priority = ThreadPriority.Lowest;

            Globals.Library.Db.Execute(
                    "CREATE TABLE IF NOT EXISTS MirageProcessed"
                    + " (TrackID INTEGER PRIMARY KEY, Status INTEGER)");

            if(Globals.Library.IsLoaded) {
                OnLibraryReloaded(null, EventArgs.Empty);
            } else {
                Globals.Library.Reloaded += OnLibraryReloaded;
            }
            Globals.Library.TrackAdded += OnLibraryTrackAdded;
            Globals.Library.TrackRemoved += OnLibraryTrackRemoved;
            
            continuousPlaylist =
                    new ContinuousGeneratorSource("Playlist Generator", new Db());
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

            // cancel analysis everything
            lock(jobQueue) {
                jobQueue.Clear();
            }
            Mir.CancelAnalyze();
        }

        private void OnInterfaceInitialized(object o, EventArgs args)
        {
            ScanLibrary();
        }

        private void ScanLibrary()
        {
            Thread thread = new Thread(ScanLibraryThread);
            thread.IsBackground = true;
            thread.Priority = ThreadPriority.Lowest;
            thread.Start();
        }

        private void ScanLibraryThread()
        {
            Dbg.WriteLine("Mirage: Scanning library for tracks to update");

            // TODO: Eliminate this...
            System.Threading.Thread.Sleep(5000);

            IDataReader reader = Globals.Library.Db.Query(
                    "SELECT TrackID FROM Tracks WHERE TrackID NOT IN"
                    + " (SELECT Tracks.TrackID FROM MirageProcessed, Tracks"
                    + " WHERE Tracks.TrackID = MirageProcessed.TrackID)");

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
            if (!jobThread.IsAlive)
                jobThread.Start();
        }

        private void ProcessQueueThread()
        {
            int trackId = 0;
            int queueLength = 0;
            lock(jobQueue) {
                queueLength = jobQueue.Count;
                if (queueLength > 0)
                    trackId = (int)jobQueue.Dequeue();
                else
                    return;
            }

            // Banshee user event
            userEvent = new ActiveUserEvent(Catalog.GetString("Mirage"));
            userEvent.Header = Catalog.GetString("Mirage: Analyzing Songs");
            userEvent.CancelMessage = Catalog.GetString(
                "Are you sure you want to stop Mirage. "
                + "Automatic Playlist Generation will only work for the tracks which are already analyzed. "
                + "The operation can be resumed at any time from the <i>Tools</i> menu.");
            userEvent.Icon = IconThemeUtils.LoadIcon(22, "document-save", Gtk.Stock.Save);
            userEvent.CancelRequested += OnUserEventCancelRequested;

            while (queueLength > 0) {

                TrackInfo track = Globals.Library.GetTrack(trackId);

                if (track == null)
                    return;

                if (track.Uri.IsLocalPath) {
                    int status = -1;
                    Dbg.WriteLine("Mirage: processing " + track.TrackId + "/" + track.Artist + "/" + track.Title);

                    // Banshee user event
                    userEvent.Progress = 1 - (double)queueLength/(double)jobsScheduled;
                    userEvent.Message = String.Format("{0} - {1}", track.Artist, track.Title);

                    Scms scms = Mir.Analyze(track.Uri.LocalPath);
                    if (scms != null) {
                        status = 0;
                        db.AddTrack(track.TrackId, scms);
                    }

                    Globals.Library.Db.Execute("INSERT INTO MirageProcessed"
                            + " (TrackID, Status) VALUES (" + track.TrackId + ", "
                            + status + ")");
                }

                lock(jobQueue) {
                    queueLength = jobQueue.Count;
                    if (queueLength > 0)
                        trackId = (int)jobQueue.Dequeue();
                }
            }

            if (userEvent != null) {
                userEvent.CancelRequested -= OnUserEventCancelRequested;
                userEvent.Dispose();
                userEvent = null;
            }
        }

        protected override void InterfaceInitialize()
        {
            //TODO: add Menu-Items to Banshee
        }

        private void OnLibraryReloaded(object o, EventArgs args)
        {
            Globals.Library.TrackAdded += OnLibraryTrackAdded;
        }

        private void OnLibraryTrackAdded(object o, LibraryTrackAddedArgs args)
        {
            //TODO: add to library
        }

        private void OnLibraryTrackRemoved(object o, LibraryTrackRemovedArgs args)
        {
            foreach(TrackInfo track in args.Tracks) {
                db.RemoveTrack(track.TrackId);
                Globals.Library.Db.Execute("DELETE FROM MirageProcessed"
                    + " WHERE TrackID = " + track.TrackId);
            }
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
