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

using System;
using System.Data;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Mono.Addins;
using Gtk;

using Hyena;
using Hyena.Data.Sqlite;
using Banshee.Collection;
using Banshee.Collection.Database;
using Banshee.Gui;
using Banshee.MediaEngine;
using Banshee.PlaybackController;
using Banshee.Playlist;
using Banshee.ServiceStack;
using Banshee.Sources;
using Mirage;

namespace Banshee.Mirage
{
    public class PlaylistGeneratorSource : PlaylistSource, IBasicPlaybackController, IDisposable
    {
        private static string playlist_name = "Playlist Generator";

        public static List<DatabaseTrackInfo> seeds = new List<DatabaseTrackInfo>();
        protected List<DatabaseTrackInfo> tracksOverride = new List<DatabaseTrackInfo>();
        protected List<DatabaseTrackInfo> skipped = new List<DatabaseTrackInfo>();
        protected DatabaseTrackInfo processed;
        protected Db db;
        InterfaceActionService action_service;
        uint ui_id;

        private int current_track = 0;
        public TrackInfo CurrentTrack {
            get { return GetTrack (current_track); }
            set {
                int i = DatabaseTrackModel.IndexOf (value);
                if (i != -1) {
                    current_track = i;
                }
            }
        }

        public PlaylistGeneratorSource (Db db)
                : base (AddinManager.CurrentLocalizer.GetString ("Playlist Generator"), null)
        {
            TypeUniqueId = "Mirage";
            BindToDatabase ();
            Initialize ();
            AfterInitialized ();
            
            this.db = db;
            processed = null;
            
            Order = 20;
            Properties.SetString ("Icon.Name", "source-mirage");
            
            ((DatabaseTrackListModel)TrackModel).ForcedSortQuery = "CorePlaylistEntries.ViewOrder ASC, CorePlaylistEntries.EntryID ASC";
            
            action_service = ServiceManager.Get<InterfaceActionService> ("InterfaceActionService");
            
            /*action_service.TrackActions.Add (new ActionEntry [] {
                new ActionEntry ("AddToPlayQueueAction", Stock.Add,
                    AddinManager.CurrentLocalizer.GetString ("Add to Play Queue"), "q",
                    AddinManager.CurrentLocalizer.GetString ("Append selected songs to the play queue"),
                    OnAddToPlayQueue)
            });*/
            
            action_service.GlobalActions.AddImportant (
                new ActionEntry ("ClearMiragePlaylistAction", Stock.Clear,
                    AddinManager.CurrentLocalizer.GetString ("Clear"), null,
                    AddinManager.CurrentLocalizer.GetString ("Remove all tracks from the play queue"),
                    OnClearPlaylist)
            );
            
            action_service.GlobalActions.Add (new ToggleActionEntry [] {
                new ToggleActionEntry ("ClearMiragePlaylistOnQuitAction", null, 
                                       AddinManager.CurrentLocalizer.GetString ("Clear on Quit"), null, 
                                       AddinManager.CurrentLocalizer.GetString ("Clear the play queue when quitting"), 
                                       OnClearPlaylistOnQuit, 
                                       MirageConfiguration.ClearOnQuitSchema.Get ())
            });
            
            ui_id = action_service.UIManager.AddUiFromResource ("GlobalUI.xml");
            Properties.SetString ("GtkActionPath", "/PlaylistContextMenu");
            
            action_service.PlaybackActions["NextAction"].Activated += OnNextPrevAction;
            action_service.PlaybackActions["PreviousAction"].Activated += OnNextPrevAction;
            
            ServiceManager.PlayerEngine.ConnectEvent (OnPlayerEvent, 
                                                      PlayerEvent.StartOfStream | 
                                                      PlayerEvent.Iterate);
            
            Reload ();
            
            if (Count == 0) {
                SetStatus (AddinManager.CurrentLocalizer.GetString ("Ready. Drag a song on the Playlist Generator to start!"), false, false, null);
            }
        }
        
        public void Dispose ()
        {
            action_service.UIManager.RemoveUi (ui_id);
            action_service.GlobalActions.Remove ("ClearMiragePlaylistAction");
            action_service.GlobalActions.Remove ("ClearMiragePlaylistOnQuitAction");
            
            if (MirageConfiguration.ClearOnQuitSchema.Get ()) {
                Clear ();
            }
        }
        
        private void BindToDatabase ()
        {
            int result = ServiceManager.DbConnection.Query<int> (
                "SELECT PlaylistID FROM CorePlaylists WHERE Special = 1 AND Name = ? LIMIT 1",
                playlist_name
            );
            
            if (result != 0) {
                DbId = result;
            } else {
                DbId = ServiceManager.DbConnection.Execute (new HyenaSqliteCommand (@"
                    INSERT INTO CorePlaylists (PlaylistID, Name, SortColumn, SortType, Special) 
                    VALUES (NULL, ?, -1, 0, 1)", playlist_name));
            }
        }
        
        public void FillPlaylist ()
        {
            // Add seed tracks 
            lock (TrackModel) {
                seeds.Clear();
                seeds.AddRange (tracksOverride);
                
                tracksOverride.Clear();
                
                OnUpdated();
            }
            SimilarTracks (seeds, seeds);
        }
        
        public delegate void UpdatePlaylistDelegate(int[] playlist);

        protected void UpdatePlaylist (int[] playlist)
        {
            Gtk.Application.Invoke(delegate {
                if (playlist == null) {
                    SetStatus (AddinManager.CurrentLocalizer.GetString ("Error building playlist. You might need to rescan your music collection."), true);
                    return;
                }
                
                int length_wanted = MirageConfiguration.PlaylistLength.Get ();
                lock (DatabaseTrackModel) {
                    RemoveTrackRange (DatabaseTrackModel, new Hyena.Collections.RangeCollection.Range (current_track + 1, Count - 1));
                    int sameArtistCount = 0;
                    int i = 0;
                    int pi = 0;
                    while ((i < Math.Min(playlist.Length, length_wanted)) && (pi < playlist.Length)) {
                        DatabaseTrackInfo track = DatabaseTrackInfo.Provider.FetchSingle(playlist[pi]);
                        bool sameArtist = track.Artist.Equals(seeds[seeds.Count-1].Artist);
                        pi++;
                        
                        if (sameArtist)
                            sameArtistCount++;
                        
                        // We don't want more than half of the tracks by the same artist
                        if (sameArtist && (sameArtistCount > 0.5*length_wanted)) {
                            continue;
                        } else
                            i++;
                        
                        AddTrack(track);
                    }
                    SetStatus (AddinManager.CurrentLocalizer.GetString ("Playlist ready."), false, false, null);
                }
                
                OnUpdated();
                HideStatus();
            });
        }
        
        /*public void Reorder(DatabaseTrackInfo track, int position)
        {
            // Reorder Tracks
        }*/

        protected override void AddTrack(DatabaseTrackInfo track)
        {
            lock (DatabaseTrackModel) {
                tracksOverride.Add(track);
            }
            base.AddTrack (track);
        }
        
        public override bool AcceptsInputFromSource (Source source)
        {
            return source == Parent || 
                (source.Parent == Parent || Parent == null || (source.Parent == null && !(source is PrimarySource)));
        }
        
        public override SourceMergeType SupportedMergeTypes {
            get { return SourceMergeType.ModelSelection; }
        }
        
        public override void MergeSourceInput (Source source, SourceMergeType mergeType)
        {
            bool was_empty = (Count == 0);
            base.MergeSourceInput (source, mergeType);

            if (was_empty) {
                for (int i = 0; i < TrackModel.Count; i++) {
                    tracksOverride.Add ((DatabaseTrackInfo)TrackModel[i]);
                }
                FillPlaylist ();
            }
        }
        
        /*private void OnAddToPlayQueue (object o, EventArgs args)
        {
            AddSelectedTracks (ServiceManager.SourceManager.ActiveSource);
        }*/
        
        private void Clear ()
        {
            current_track = 0;
            RemoveTrackRange ((DatabaseTrackListModel)TrackModel, new Hyena.Collections.RangeCollection.Range (0, Count));
            skipped.Clear ();
            tracksOverride.Clear ();
            Reload ();
        }

        private void OnClearPlaylist (object o, EventArgs args)
        {
            Clear ();
        }
        
        private void OnClearPlaylistOnQuit (object o, EventArgs args)
        {
            InterfaceActionService uia_service = ServiceManager.Get<InterfaceActionService> ();
            if (uia_service == null) {
                return;
            }
            
            ToggleAction action = (ToggleAction)uia_service.GlobalActions["ClearMiragePlaylistOnQuitAction"];
            MirageConfiguration.ClearOnQuitSchema.Set (action.Active);
        }

        public void OnNextPrevAction (object o, EventArgs e)
        {
            skipped.Add(ServiceManager.PlayerEngine.CurrentTrack as DatabaseTrackInfo);
        }

        protected override void OnTracksRemoved ()
        {
            if (ServiceManager.SourceManager.ActiveSource != this) {
                return;
            }

            // add removed tracks to exclude list
            foreach (int track_id in TrackModel.Selection) {
                DatabaseTrackInfo ti = (DatabaseTrackInfo)DatabaseTrackModel[track_id];

                if (!skipped.Exists( delegate (DatabaseTrackInfo t) { return t.TrackId == ti.TrackId; })) {
                    Log.DebugFormat ("Mirage - Adding {0}-{1} to exclude list",
                        ti.TrackId, ti.TrackTitle);
                    skipped.Add (ti);
                }
            }
            Reload ();
        }
       
        private void Refresh ()
        {
            processed = ServiceManager.PlayerEngine.CurrentTrack as DatabaseTrackInfo;
            
            foreach (DatabaseTrackInfo seed in seeds) {
                if (processed.TrackId == seed.TrackId) {
                    return;
                }
            }
                
            lock (DatabaseTrackModel) {
                if (DatabaseTrackModel.IndexOf(processed) < 0) {
                    // We're playing another source
                    return;
                }
                seeds.Add(processed);
                
                List<DatabaseTrackInfo> skip = new List<DatabaseTrackInfo>();
                
                skip.AddRange(seeds);
                if (skipped.Count > 0)
                    skip.AddRange(skipped);

                SimilarTracks(seeds, skip);
            }
        }

        private void SimilarTracks(List<DatabaseTrackInfo> tracks,
                List<DatabaseTrackInfo> exclude)
        {
            int[] trackIds = new int[tracks.Count];
            for (int i = 0; i < tracks.Count; i++) {
                Log.DebugFormat ("Mirage - Looking for similars to {0}-{1}", tracks[i].TrackId, tracks[i].TrackTitle);
                trackIds[i] = tracks[i].TrackId;
            }

            int[] excludeTrackIds = new int[exclude.Count];
            for (int i = 0; i < exclude.Count; i++) {
                Log.DebugFormat ("Mirage - Excluding {0}-{1}", exclude[i].TrackId, exclude[i].TrackTitle);
                excludeTrackIds[i] = exclude[i].TrackId;
            }

            SimilarityCalculator sc = new SimilarityCalculator(trackIds, excludeTrackIds, db, UpdatePlaylist);
            Thread similarityCalculatorThread = new Thread(new ThreadStart(sc.Compute));
            similarityCalculatorThread.Start();
        }

        private void OnPlayerEvent(PlayerEventArgs args)
        {
            if (ServiceManager.SourceManager.ActiveSource != this) {
                return;
            }
            
            switch (args.Event) {
                case PlayerEvent.StartOfStream:
                    if (CurrentTrack != ServiceManager.PlayerEngine.CurrentTrack) {
                        CurrentTrack = ServiceManager.PlayerEngine.CurrentTrack;
                    }
                    if (NextTrack == null) {
                        // We're at the last track in the playlist, we need new tracks
                        Refresh ();
                    }
                    break;
                case PlayerEvent.Iterate:
                    // if more than 60% of a track is played, use this track as
                    // a seed song for the next tracks.
                    if ((processed != ServiceManager.PlayerEngine.CurrentTrack) &&
                            (ServiceManager.PlayerEngine.Position > 
                             ServiceManager.PlayerEngine.Length * 0.6)) {
                        Refresh ();
                    }
                    break;
            }
        }

        bool IBasicPlaybackController.First ()
        {
            return ((IBasicPlaybackController)this).Next (false);
        }

        bool IBasicPlaybackController.Next (bool restart)
        {
            // We get the next track before removing the current one
            DatabaseTrackInfo next_track = NextTrack;
            if (processed != ServiceManager.PlayerEngine.CurrentTrack) {
                RemovePlayingTrack ();
            }
            current_track++;
            ServiceManager.PlayerEngine.OpenPlay (next_track);
            return true;
        }

        bool IBasicPlaybackController.Previous (bool restart)
        {
            TrackInfo previous = GetTrack (--current_track);
            if (previous != null) {
                ServiceManager.PlayerEngine.OpenPlay (previous);
            }
            return true;
        }
        
        private void RemovePlayingTrack ()
        {
            DatabaseTrackInfo playing_track = GetTrack (current_track);
            if (playing_track != null) {
                RemoveTrack (playing_track);
            }
        }
        
        public DatabaseTrackInfo NextTrack {
            get { return GetTrack (current_track + 1); }
        }

        private DatabaseTrackInfo GetTrack (int track_num) {
            return (track_num > DatabaseTrackModel.Count - 1) ? null : (DatabaseTrackInfo)DatabaseTrackModel[track_num];
        }

        public override bool ShowBrowser {
            get { return false; }
        }
    }
}
