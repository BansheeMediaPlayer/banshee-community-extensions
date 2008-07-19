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

using System;
using System.Data;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

using Hyena;
using Banshee.Collection;
using Banshee.Collection.Database;
using Banshee.PlaybackController;
using Banshee.ServiceStack;
using Banshee.Sources;
using Mirage;
using Mono.Unix;
using Gtk;


namespace Banshee.Plugins.Mirage
{
    public class PlaylistGeneratorSource : Source, ITrackModelSource, IDisposable, IBasicPlaybackController
    {
        protected List<DatabaseTrackInfo> tracks = new List<DatabaseTrackInfo>();
        public static List<DatabaseTrackInfo> seeds = new List<DatabaseTrackInfo>();
        protected List<DatabaseTrackInfo> tracksOverride = new List<DatabaseTrackInfo>();
        
        protected Db db;

        protected MemoryTrackListModel track_model;
        
        public override int Count {
            get {
                return tracks.Count;
            }
        }
        
        private int current_track = 0;
        public TrackInfo CurrentTrack {
            get { return GetTrack (current_track); }
            set {
                int i = track_model.IndexOf (value);
                if (i != -1)
                    current_track = i;
            }
        }
        
        protected override string TypeUniqueId {
            get { return "Mirage"; }
        }
        
        public PlaylistGeneratorSource(string name, Db db)
                : base("mirage-playlist-generator", name, 100)
        {
            this.db = db;
            track_model = new MemoryTrackListModel ();
            
            SetStatus (Catalog.GetString("Ready. Drag a song on the Playlist Generator to start!"), false, false, null);
        }
        
        public void Dispose ()
        {
        }
        
        public virtual void Update ()
        {
            int[] trackId;
            
            Log.Debug("Mirage: PlaylistGeneratorSource.Update");
            // Add seed tracks 
            lock(tracks) {
                tracks.Clear();
                tracks.AddRange(tracksOverride);
                seeds.Clear();
                seeds.AddRange(tracksOverride);
                tracksOverride.Clear();

                // maintain the seed track order
                Gtk.Application.Invoke(delegate {
                    track_model.Clear();
                    for (int i = 0; i < tracks.Count; i++) {
                        track_model.Add(tracks[i]);
                    }
                    OnUpdated();
                });

                // initialize the similarity computation
                trackId = new int[tracks.Count];
                for (int i = 0; i < tracks.Count; i++) {
                    trackId[i] = tracks[i].TrackId;
                }
            }
            SimilarityCalculator sc =
                    new SimilarityCalculator(trackId, trackId, db,
                            UpdatePlaylist, 5);
            SetStatus(Catalog.GetString("Generating the playlist..."), false);
            Thread similarityCalculatorThread =
                    new Thread(new ThreadStart(sc.Compute));
            similarityCalculatorThread.Start();
        }
        
        protected void UpdatePlaylist(int[] playlist, int length)
        {
            Gtk.Application.Invoke(delegate {
                if (playlist == null) {
                    SetStatus(Catalog.GetString("Error building playlist."), true);
                    return;
                }
                
                lock(tracks) {
                    int sameArtistCount = 0;
                    int i = 0;
                    int pi = 0;
                    while ((i < Math.Min(playlist.Length, length)) && (pi < playlist.Length)) {
                        DatabaseTrackInfo track = DatabaseTrackInfo.Provider.FetchSingle(playlist[pi]);
                        bool sameArtist = track.Artist.Equals(seeds[seeds.Count-1].Artist);
                        pi++;
                        
                        if (sameArtist)
                            sameArtistCount++;
                        
                        if (sameArtist && (sameArtistCount > 0.5*length)) {
                            continue;
                        } else
                            i++;
                            
                        tracks.Add(track);
                        track_model.Add(track);
                    }
                    SetStatus(Catalog.GetString("Playlist ready."), false, false, null);
                }
                
                OnUpdated();
                HideStatus();
            });
            
        }
        
        public delegate void UpdatePlaylistDelegate(int[] playlist, int length);

        public void Reorder(DatabaseTrackInfo track, int position)
        {
            // Reorder Tracks
        }

        public void AddTrack(DatabaseTrackInfo track)
        {
            lock(tracks) {
                Log.DebugFormat("Adding track {0}", track.TrackTitle);
                tracksOverride.Add(track);
            }
            OnUpdated();
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
            Log.Debug("Mirage: MergeSourceInput");
            TrackListModel model = (source as ITrackModelSource).TrackModel;
            Hyena.Collections.Selection selection = model.Selection;
            if (selection.Count == 0)
                return;

            Log.Debug("Mirage: MergeSourceInput 2");
            foreach (DatabaseTrackInfo ti in model.SelectedItems) {
                AddTrack (ti);
            }

            Update();
            //DatabaseTrackInfo ti = (source as ITrackModelSource).TrackModel[0] as DatabaseTrackInfo;
            //AddTrack (ti);
        }

        
#region ITrackModelSource Implementation

        public TrackListModel TrackModel {
            get { return track_model; }
        }

        public AlbumListModel AlbumModel {
            get { return null; }
        }

        public ArtistListModel ArtistModel {
            get { return null; }
        }
        
        public System.Collections.Generic.IEnumerable<Banshee.Collection.Database.IFilterListModel> FilterModels {
            get { yield break; }
        }
        
        public bool HasDependencies {
            get { return false; }
        }

        public void Reload ()
        {
            track_model.Reload ();
        }

        public void RemoveSelectedTracks ()
        {
        }

        public void DeleteSelectedTracks ()
        {
            throw new Exception ("Should not call DeleteSelectedTracks on PlaylistGeneratorSource");
        }

        public bool CanAddTracks {
            get { return true; }
        }

        public bool CanRemoveTracks {
            get { return false; }
        }

        public bool CanDeleteTracks {
            get { return false; }
        }

        public bool ConfirmRemoveTracks {
            get { return false; }
        }
        
        public bool ShowBrowser {
            get { return false; }
        }

#endregion

#region IBasicPlaybackController

        void IBasicPlaybackController.First ()
        {
            ((IBasicPlaybackController)this).Next (false);
        }
        
        void IBasicPlaybackController.Next (bool restart)
        {
            TrackInfo next = NextTrack;
            if (next != null) {
                ServiceManager.PlayerEngine.OpenPlay (next);
            }
        }
        
        void IBasicPlaybackController.Previous (bool restart)
        {
        }
        
#endregion

        public TrackInfo NextTrack {
            get { return GetTrack (current_track + 1); }
        }

        private TrackInfo GetTrack (int track_num) {
            return (track_num > track_model.Count - 1) ? null : track_model[track_num];
        }

    }
}
