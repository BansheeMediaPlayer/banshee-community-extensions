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
using Banshee.Sources;
using Banshee.Configuration;
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
        bool isScanning;
        bool processingQueue;
        Queue scanQueue;
        Db db;
        ContinuousGeneratorSource continuousPlaylist = null;
        
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
            scanQueue = new Queue();
            db = new Db();
            
            Globals.Library.Db.Execute(
                    "CREATE TABLE IF NOT EXISTS MirageProcessed"
                    + " (TrackID INTEGER PRIMARY KEY, Status INTEGER)");

            if(Globals.Library.IsLoaded) {
                ScanLibrary();
            }

            Globals.Library.Reloaded += OnLibraryReloaded;
            Globals.Library.TrackAdded += OnLibraryTrackAdded;
            Globals.Library.TrackRemoved += OnLibraryTrackRemoved;
            
            continuousPlaylist =
                    new ContinuousGeneratorSource("Playlist Generator", new Db());
            LibrarySource.Instance.AddChildSource(continuousPlaylist);
        }
        
        protected override void PluginDispose()
        {
            db = null;
        }

        
        private void ScanLibrary()
        {
            lock (this) {
                if (isScanning) {
                    return;
                }
                isScanning = true;
                Thread thread = new Thread(ScanLibraryThread);
                thread.IsBackground = true;
                thread.Priority = ThreadPriority.Lowest;
                thread.Start();
            }
        }
        
        private void ScanLibraryThread()
        {
            Dbg.WriteLine("Mirage: Scanning library for tracks to update");
            while (Globals.StartupInitializer.IsRunFinished != true) {
                System.Threading.Thread.Sleep(1000);
                Dbg.WriteLine("Mirage: Waiting for Banshee to startup");
            }
                
            
            IDataReader reader = Globals.Library.Db.Query(
                    "SELECT TrackID FROM Tracks WHERE TrackID NOT IN"
                    + " (SELECT Tracks.TrackID FROM MirageProcessed, Tracks"
                    + " WHERE Tracks.TrackID = MirageProcessed.TrackID)");
            
            while(reader.Read() && !DisposeRequested) {
                scanQueue.Enqueue(Convert.ToInt32(reader["TrackID"]));
            }

            reader.Dispose();

            Dbg.WriteLine("Mirage: Done scanning library");

            if(!DisposeRequested) {
                ProcessQueue();
            }

            isScanning = false;
        }
        
        
        private void ProcessQueue()
        {
            if(processingQueue) {
                return;
            }
            Dbg.WriteLine("Mirage: Processing track queue for pending queries");
            
            processingQueue = true;
            int before = scanQueue.Count;
            
            while(scanQueue.Count > 0 && !DisposeRequested) {
                if (continuousPlaylist != null) {
                    before = Math.Max(before, scanQueue.Count);
                    int percent = (int)(100-((double)(scanQueue.Count)/(double)before) * 100.0);
                    continuousPlaylist.SetStatusLabelText("Mirage listens to your Music: " +
                        percent + "% done ("+(before-scanQueue.Count)+"/"+before+").");
                }
                object o = scanQueue.Dequeue();
                if (o is int) {
                    ProcessTrack(Globals.Library.GetTrack((int)o));
                }
            }
            
            if (continuousPlaylist != null)
                continuousPlaylist.SetStatusLabelText("Ready. Drag a song on the Playlist Generator to start!");
            
            Dbg.WriteLine("Mirage: Done processing track queue");
            
            processingQueue = false;
        }
        
        private void ProcessTrack(LibraryTrackInfo track)
        {
            if(track == null) {
                return;
            }
            if (track.Uri.IsLocalPath) {
            
                Dbg.WriteLine("Mirage: processing " + track.TrackId + "/" + track.Artist + "/" + track.Title);

                int status;
                Scms scms = Mir.Analyze(track.Uri.LocalPath);
                if (scms != null) {
                    status = 0;
                    db.AddTrack(track.TrackId, scms);
                } else {
                    status = -1;
                }
                
                Globals.Library.Db.Execute("INSERT INTO MirageProcessed"
                        + " (TrackID, Status) VALUES (" + track.TrackId + ", "
                        + status + ")");
            }
        }

        
        private void OnLibraryReloaded(object o, EventArgs args)
        {
            ScanLibrary();
        }
        
        private void OnLibraryTrackAdded(object o, LibraryTrackAddedArgs args)
        {
            if(DisposeRequested) {
                return;
            }
            
            scanQueue.Enqueue(args.Track.TrackId);
            if(!processingQueue) {
                Thread pThread = new Thread(new ThreadStart(ProcessQueue));
                pThread.IsBackground = true;
                pThread.Start();
            }
        }

        private void OnLibraryTrackRemoved(object o, LibraryTrackRemovedArgs args)
        {
            if(DisposeRequested) {
                return;
            }
            
            foreach(TrackInfo track in args.Tracks) {
                db.RemoveTrack(track.TrackId);
                Globals.Library.Db.Execute("DELETE FROM MirageProcessed"
                    + " WHERE TrackID = " + track.TrackId);
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
