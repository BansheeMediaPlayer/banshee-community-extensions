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
using System.Linq;
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
        protected List<DatabaseTrackInfo> suggested = new List<DatabaseTrackInfo>();
        protected List<DatabaseTrackInfo> skipped = new List<DatabaseTrackInfo>();
        protected List<DatabaseTrackInfo> played = new List<DatabaseTrackInfo>();
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
            /* TODO: Add Ban Button or "Exclude from Playlists"
            action_service.GlobalActions.AddImportant (
                new ActionEntry ("BanFromMiragePlaylistAction", Stock.No,
                    AddinManager.CurrentLocalizer.GetString ("Ban Song"), null,
                    AddinManager.CurrentLocalizer.GetString ("Permanently ban the selected song from generated playlists"),
                    OnBanFromPlaylist)
            );
            */
            action_service.GlobalActions.AddImportant (
                new ActionEntry ("SaveMiragePlaylistAction", Stock.Add,
                    AddinManager.CurrentLocalizer.GetString ("Save as Playlist"), null,
                    AddinManager.CurrentLocalizer.GetString ("Save the Playlist to your Music Library"),
                    OnSavePlaylist)
            );
            
            action_service.GlobalActions.Add (new ToggleActionEntry [] {
                new ToggleActionEntry ("ClearMiragePlaylistOnQuitAction", null, 
                    AddinManager.CurrentLocalizer.GetString ("Clear on Quit"), null, 
                    AddinManager.CurrentLocalizer.GetString ("Clear the play queue when quitting"), 
                    OnClearPlaylistOnQuit, 
                    MirageConfiguration.ClearOnQuitSchema.Get ())
            });

            
            ui_id = action_service.UIManager.AddUiFromResource ("GlobalUI.xml");
            Properties.SetString ("ActiveSourceUIResource", "ActiveSourceUI.xml");
            Properties.SetString ("GtkActionPath", "/PlaylistContextMenu");
            
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

        public delegate void UpdatePlaylistDelegate(int[] playlist, bool append);
        
        protected void UpdatePlaylist (int[] playlist, bool append)
        {
            Gtk.Application.Invoke(delegate {
                if (playlist == null) {
                    SetStatus (AddinManager.CurrentLocalizer.GetString ("Error building playlist. You might need to rescan your music library."), true);
                    return;
                }
                
                int length_wanted = MirageConfiguration.PlaylistLength.Get ();
                lock (DatabaseTrackModel) {

                    // check if we have to delete the suggested songs, or just
                    // append
                    if (!append) {
                        RemoveTrackRange (DatabaseTrackModel, new Hyena.Collections.RangeCollection.Range (current_track+1, Count-1));
                    }

                    int sameArtistCount = 0;
                    int i = 0;
                    int pi = 0;
                    suggested.Clear();
                    while ((i < Math.Min(playlist.Length, length_wanted)) && (pi < playlist.Length)) {
                        DatabaseTrackInfo track = DatabaseTrackInfo.Provider.FetchSingle(playlist[pi]);
                        if (track == null) {
                            pi++;
                            continue;
                        }
                        bool sameArtist = track.Artist.Equals(seeds[seeds.Count-1].Artist);
                        pi++;
                        
                        if (sameArtist)
                            sameArtistCount++;
                        
                        // We don't want more than half of the tracks by the same artist
                        if (sameArtist && (sameArtistCount > 0.5*length_wanted)) {
                            continue;
                        } else
                            i++;
                       
                        suggested.Add(track); 
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

        public override bool AcceptsInputFromSource (Source source)
        {
            return source == Parent || 
                (source.Parent == Parent || Parent == null || (source.Parent == null && !(source is PrimarySource)));
        }
        
        public override SourceMergeType SupportedMergeTypes {
            get { return SourceMergeType.ModelSelection; }
        }
        
        public override void MergeSourceInput (Source from, SourceMergeType mergeType)
        {
            DatabaseSource source = from as DatabaseSource;
            if (source == null || !(source.TrackModel is DatabaseTrackListModel)) {
                return;
            }

            bool was_empty = (Count == 0);

            // if playlist is empty start a new one
            if (was_empty) {
                base.MergeSourceInput (source, mergeType);
                seeds.Clear();
                for (int i = 0; i < TrackModel.Count; i++) {
                    seeds.Add ((DatabaseTrackInfo)TrackModel[i]);
                }

                played.Clear();
                played.AddRange(seeds);

                SimilarTracks (seeds, played, false);

            // if playlist is not empty append the songs
            } else {

                seeds.Clear();
                DatabaseTrackListModel tm = (DatabaseTrackListModel)(source.TrackModel);
                CachedList<DatabaseTrackInfo> cached_list = CachedList<DatabaseTrackInfo>.CreateFromModelSelection (tm);
                foreach (DatabaseTrackInfo ti in cached_list) {
                    seeds.Add(ti);
                }

                base.MergeSourceInput (source, mergeType);

                // Add the currently suggested songs to the played list, so
                // they (and all added songs) do not get removed when the
                // playlist is updated
                played.AddRange(suggested);
                played.AddRange(seeds);

                List<DatabaseTrackInfo> exclude = new List<DatabaseTrackInfo>();
                exclude.AddRange(played);
                exclude.AddRange(skipped);

                SimilarTracks (seeds, exclude, true);
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
            played.Clear ();
            suggested.Clear ();

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

        private class MiragePlaylistSource : PlaylistSource {

            public MiragePlaylistSource() : 
                base(AddinManager.CurrentLocalizer.GetString ("Mirage Playlist"), ServiceManager.SourceManager.MusicLibrary)
            {
            }

            public void AddTracks (DatabaseTrackListModel from)
            {
                AddTrackRange(from, new Hyena.Collections.RangeCollection.Range(0, from.Count-1));
                OnTracksAdded();
            }
        }

        private void OnSavePlaylist (object o, EventArgs args)
        {
            MiragePlaylistSource playlist = new MiragePlaylistSource ();
            playlist.Save ();
            playlist.PrimarySource.AddChildSource (playlist);
            // FIXME : There's probably a better way to keep the same track order in the playlist
            playlist.DatabaseTrackModel.ForcedSortQuery = "CorePlaylistEntries.ViewOrder ASC, CorePlaylistEntries.EntryID ASC";
            playlist.AddTracks (DatabaseTrackModel);
            playlist.DatabaseTrackModel.Reload ();
            playlist.DatabaseTrackModel.ForcedSortQuery = null;
            playlist.NotifyUser ();

            //SourceView.BeginRenameSource (playlist);
        }

        /*
        private void OnBanFromPlaylist (object o, EventArgs args)
        {
        }
        */

        protected override void OnTracksRemoved ()
        {
            if (ServiceManager.SourceManager.ActiveSource != this) {
                return;
            }

            // add removed tracks to exclude list
            foreach (int track_id in TrackModel.Selection) {
                DatabaseTrackInfo ti = (DatabaseTrackInfo)DatabaseTrackModel[track_id];

                if (!played.Exists( delegate (DatabaseTrackInfo t) { return t.TrackId == ti.TrackId; })) {
                    Log.DebugFormat ("Mirage - Adding {0}-{1} to exclude list",
                        ti.TrackId, ti.TrackTitle);
                    skipped.Add (ti);
                }
            }
            Reload ();
        }

        private bool ContainsTrack(List<DatabaseTrackInfo> tlist, DatabaseTrackInfo ti)
        {
            return tlist.Exists(
                delegate (DatabaseTrackInfo t)
                {
                    return t.TrackId == ti.TrackId;
                });
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
                played.Add(processed);

                seeds.Clear();
                seeds.Add(processed);
                
                List<DatabaseTrackInfo> exclude = new List<DatabaseTrackInfo>();
                exclude.AddRange(played);
                exclude.AddRange(skipped);

                SimilarTracks(seeds, exclude, false);
            }
        }

        private void SimilarTracks(List<DatabaseTrackInfo> seed_tracks, List<DatabaseTrackInfo> exclude_tracks, bool append)
        {
            var seed_track_ids    = seed_tracks.Select    (t => t.TrackId).ToArray ();
            var exclude_track_ids = exclude_tracks.Select (t => t.TrackId).ToArray ();

            var sc = new SimilarityCalculator (seed_track_ids, exclude_track_ids, db, UpdatePlaylist, append);

            var similarity_calc_thread = new Thread (new ThreadStart(sc.Compute));
            similarity_calc_thread.Start();
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

                        // only refresh if the track was not played yet
                        if (!ContainsTrack(played, ServiceManager.PlayerEngine.CurrentTrack as DatabaseTrackInfo)) {
                            Refresh ();
                        } else {
                            processed = ServiceManager.PlayerEngine.CurrentTrack as DatabaseTrackInfo;
                        }

                    }
                    break;
            }
        }

        bool IBasicPlaybackController.First ()
        {
            ServiceManager.PlayerEngine.OpenPlay(GetTrack(current_track));
            return true;
        }

        bool IBasicPlaybackController.Next (bool restart)
        {
            if (ServiceManager.PlayerEngine.CurrentTrack == null) {
                ServiceManager.PlayerEngine.OpenPlay(GetTrack(current_track));
                return true;
            }

            // We get the next track before removing the current one
            DatabaseTrackInfo next_track = NextTrack;
            if ((processed != ServiceManager.PlayerEngine.CurrentTrack) &&
                    (!ContainsTrack(played, ServiceManager.PlayerEngine.CurrentTrack as DatabaseTrackInfo))) {
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
                skipped.Add(playing_track);
                Log.DebugFormat ("Mirage - Adding {0}-{1} to exclude list (skipped)",
                    playing_track.TrackId, playing_track.TrackTitle);
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
        
        public override bool CanUnmap {
            get { return false; }
        }
    }
}
