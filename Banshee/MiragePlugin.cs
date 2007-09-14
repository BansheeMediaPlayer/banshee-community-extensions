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

    public interface IMirageScanJob : IJob
    {
        TrackInfo Track { get; }
    }
    
    public class MirageScanJob : IMirageScanJob
    {
        TrackInfo track;
        Db db;

        public MirageScanJob(int trackId, Db db)
        {
            this.track = Globals.Library.GetTrack(trackId);
            this.db = db;
        }

        public TrackInfo Track
        {
            get {
                return track;
            }
        }

        public void Run()
        {
            // Process a track
            if (track == null)
                return;

            if (track.Uri.IsLocalPath) {
                int status = -1;
                Dbg.WriteLine("Mirage: processing " + track.TrackId + "/" + track.Artist + "/" + track.Title);

                Scms scms = Mir.Analyze(track.Uri.LocalPath);
                if (scms != null) {
                    status = 0;
                    db.AddTrack(track.TrackId, scms);
                }

                Globals.Library.Db.Execute("INSERT INTO MirageProcessed"
                        + " (TrackID, Status) VALUES (" + track.TrackId + ", "
                        + status + ")");
            }
        }
    }

    public class MiragePlugin : Banshee.Plugins.Plugin
    {
        Db db;
        ContinuousGeneratorSource continuousPlaylist = null;

        int mirage_jobs_scheduled = 0;

        public event EventHandler ScanStartStop;
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
            mirage_jobs_scheduled = 0;

            Globals.Library.Db.Execute(
                    "CREATE TABLE IF NOT EXISTS MirageProcessed"
                    + " (TrackID INTEGER PRIMARY KEY, Status INTEGER)");

            Scheduler.JobStarted += OnJobStarted;
            Scheduler.JobScheduled += OnJobScheduled;
            Scheduler.JobFinished += OnJobUnscheduled;
            Scheduler.JobUnscheduled += OnJobUnscheduled;

            if(Globals.Library.IsLoaded) {
                OnLibraryReloaded(null, EventArgs.Empty);
            } else {
                Globals.Library.Reloaded += OnLibraryReloaded;
            }
            Globals.Library.TrackAdded += OnLibraryTrackAdded;
            Globals.Library.TrackRemoved += OnLibraryTrackRemoved;
            Globals.ShutdownRequested += OnShutdownRequested;
            
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
            db = null;

            Globals.Library.Reloaded += OnLibraryReloaded;
            Globals.Library.TrackAdded += OnLibraryTrackAdded;
            Globals.Library.TrackRemoved += OnLibraryTrackRemoved;

            Scheduler.JobStarted -= OnJobStarted;
            Scheduler.JobScheduled -= OnJobScheduled;
            Scheduler.JobFinished -= OnJobUnscheduled;
            Scheduler.JobUnscheduled -= OnJobUnscheduled;
        }

        private bool OnShutdownRequested()
        {
            Dbg.WriteLine("Shutdown Requested");
            return true;
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

            IDataReader reader = Globals.Library.Db.Query(
                    "SELECT TrackID FROM Tracks WHERE TrackID NOT IN"
                    + " (SELECT Tracks.TrackID FROM MirageProcessed, Tracks"
                    + " WHERE Tracks.TrackID = MirageProcessed.TrackID)");

            while(reader.Read()) {
                int trackId = Convert.ToInt32(reader["TrackID"]);
                Scheduler.Schedule(new MirageScanJob(trackId, db), JobPriority.BelowNormal);
            }

            reader.Dispose();

            Dbg.WriteLine("Mirage: Done scanning library");
        }

        protected override void InterfaceInitialize()
        {
            //TODO: add Menu-Items to Banshee
        }

        public bool IsScanning {
            get {
                return mirage_jobs_scheduled > 1;
            }
        }

        private void OnLibraryReloaded(object o, EventArgs args)
        {
            Globals.Library.TrackAdded += OnLibraryTrackAdded;
        }

        private void OnLibraryTrackAdded(object o, LibraryTrackAddedArgs args)
        {
            Dbg.WriteLine("Do Stuff");
        }

        private void OnJobScheduled(IJob job)
        {
            if(job is IMirageScanJob) {
                bool previous = IsScanning;

                mirage_jobs_scheduled++;

                if(IsScanning != previous) {
                    OnScanStartStop();
                }
            }
        }

        private void OnJobStarted(IJob job)
        {
            lock(this) {
                if(job is IMirageScanJob) {
                    Dbg.WriteLine("Jobs: " + mirage_jobs_scheduled);
                    OnUpdateProgress(job as IMirageScanJob);
                }
            }
        }

        private void OnJobUnscheduled(IJob job)
        {
            if(job is IMirageScanJob) {
                bool previous = IsScanning;

                mirage_jobs_scheduled--;
                Dbg.WriteLine("Jobs-Unscheduled: " + mirage_jobs_scheduled);

                OnUpdateProgress(job as IMirageScanJob);

                if(IsScanning != previous) {
                    OnScanStartStop();
                }
            }
        }

        private void OnScanStartStop()
        {
            ThreadAssist.ProxyToMain(OnRaiseScanStartStop);
        }

        private void OnRaiseScanStartStop(object o, EventArgs args)
        {
            EventHandler handler = ScanStartStop;
            if(handler != null) {
                handler(this, EventArgs.Empty);
            }
        }

        private void CancelJobs()
        {
            Scheduler.Unschedule(typeof(IMirageScanJob));
        }
 

        private void OnLibraryTrackRemoved(object o, LibraryTrackRemovedArgs args)
        {
            foreach(TrackInfo track in args.Tracks) {
                db.RemoveTrack(track.TrackId);
                Globals.Library.Db.Execute("DELETE FROM MirageProcessed"
                    + " WHERE TrackID = " + track.TrackId);
            }
        }

        private void OnUpdateProgress(IMirageScanJob job)
        {
            lock(this) {
                try{
                    if(IsScanning && userEvent == null) {
                        userEvent = new ActiveUserEvent(Catalog.GetString("Download"));
                        userEvent.Header = Catalog.GetString("Analyzing your Music Collection");
                        userEvent.Message = Catalog.GetString("Analyzing");
                        userEvent.CancelMessage = Catalog.GetString(
                            "Are you sure you want to stop Mirage. "
                            + "Automatic Playlist Generation will only work for the tracks which are already analyzed. "
                            + "The operation can be resumed at any time from the <i>Tools</i> menu.");
                        userEvent.Icon = IconThemeUtils.LoadIcon(22, "document-save", Gtk.Stock.Save);
                        userEvent.CancelRequested += OnUserEventCancelRequested;
                    } else if(!IsScanning && userEvent != null) {
                        userEvent.Dispose();
                        userEvent = null;
                    } else if(userEvent != null) {
                        userEvent.Progress = 0.6;
                        userEvent.Message = String.Format("{0} - {1}", job.Track.Artist, job.Track.Album);
                    }
                } catch {
                }
            }
        }

        private void OnUserEventCancelRequested(object o, EventArgs args)
        {
            ThreadAssist.Spawn(CancelJobs);
        }

        public static readonly SchemaEntry<bool> EnabledSchema = new SchemaEntry<bool>(
            "plugins.mirage", "enabled",
            true,
            "Plugin enabled",
            "Playlist Generation plugin enabled"
        );

    }
}
