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
using Banshee;
using Banshee.Base;
using Banshee.Sources;
using Banshee.Plugins;
using Mirage;
using Mono.Unix;
using Gtk;
using Banshee.MediaEngine;
using System.Data.Sql;

namespace Banshee.Plugins.Mirage
{
    public class ContinuousGeneratorSource : PlaylistGeneratorSource
    {
        TrackInfo processed;
        protected List<TrackInfo> skipped = new List<TrackInfo>();
    
        public ContinuousGeneratorSource(string name, Db db)
                : base(name, db)
        {
            PlayerEngineCore.EventChanged += OnPlayerEngineEventChanged;
            processed = null;
            Globals.ActionManager["NextAction"].Activated += OnNextPrevAction;
            Globals.ActionManager["PreviousAction"].Activated += OnNextPrevAction;
        }

        public void OnNextPrevAction(object o, EventArgs e)
        {
            skipped.Add(PlayerEngineCore.CurrentTrack);
        }
        

        public override void Commit()
        {
            int[] trackId;
            
            Dbg.WriteLine("Commit");
            
            lock(TracksMutex) {

                // add the seed tracks
                tracks.Clear();
                tracks.AddRange(tracksOverride);
                seeds.Clear();
                seeds.AddRange(tracksOverride);
                tracksOverride.Clear();

                // maintain the seed track order
                Gtk.Application.Invoke(delegate {
                    playlistView.Clear();
                    for (int i = 0; i < tracks.Count; i++) {
                        playlistView.AddTrack(tracks[i], i);
                    }
                    playlistView.QueueDraw();
                    OnUpdated();
                });
                
                // start looking for similar songs
                trackId = new int[tracks.Count];
                for (int i = 0; i < tracks.Count; i++) {
                    trackId[i] = tracks[i].TrackId;
                }
                
                // set processed track, to omit duble computation
                processed = seeds[0];
            }
                
            // to start, compute the 5 most similar songs to the seed
            SimilarityCalculator sc =
                    new SimilarityCalculator(trackId, trackId,
                            db, UpdatePlaylist, 5);
            Thread similarityCalculatorThread =
                    new Thread(new ThreadStart(sc.Compute));
            similarityCalculatorThread.Start();
        }
        
        
        protected void NewPlaylist(int[] playlist, int length)
        {
            Gtk.Application.Invoke(delegate {
                List<TrackInfo> rm = new List<TrackInfo>();

                lock(TracksMutex) {
                    // remove tracks
                    foreach (TrackInfo t in tracks) {
                        
                        bool noRemove = false;
                        for (int j = 0; j < seeds.Count; j++) {
                            if (seeds[j] == t) {
                                noRemove = true;
                                playlistView.AddTrack(t, 0);
                                break;
                            }
                        }
                        
                        if (!noRemove) {
                            rm.Add(t);
                        }
                    }
                    
                    foreach (TrackInfo t in rm) {
                        tracks.Remove(t);
                        playlistView.RemoveTrack(t);
                    }

                    int sameArtistCount = 0;
                    int i = 0;
                    int pi = 0;
                    while ((i < Math.Min(playlist.Length, length)) && (pi < playlist.Length)) {
                        TrackInfo track = Globals.Library.GetTrack(playlist[pi]);
                        bool sameArtist = track.Artist.Equals(seeds[seeds.Count-1].Artist,
                            StringComparison.CurrentCultureIgnoreCase);
                        pi++;
                        
                        if (sameArtist)
                            sameArtistCount++;
                        
                        if (sameArtist && (sameArtistCount > 0.5*length)) {
                            continue;
                        } else
                            i++;
                            
                        tracks.Add(track);
                        playlistView.AddTrack(track, tracks.Count);
                    }
                    SetStatusLabelText("Playlist ready.");
                }
                
                playlistView.QueueDraw();
                OnUpdated();
            });
        }
        
        private void SimilarTracks(List<TrackInfo> tracks,
                List<TrackInfo> exclude)
        {
            int[] trackId = new int[tracks.Count];
            for (int i = 0; i < tracks.Count; i++) {
                trackId[i] = tracks[i].TrackId;
            }

            int[] excludeTrackId = new int[exclude.Count];
            for (int i = 0; i < exclude.Count; i++) {
                excludeTrackId[i] = exclude[i].TrackId;
            }

            SimilarityCalculator sc =
                    new SimilarityCalculator(trackId, excludeTrackId,
                            db, NewPlaylist, 5);
            Thread similarityCalculatorThread =
                    new Thread(new ThreadStart(sc.Compute));
            similarityCalculatorThread.Start();
        }
        
        private void OnPlayerEngineEventChanged(object o,
                PlayerEngineEventArgs args)
        {
            if (SourceManager.ActiveSource != this) {
                return;
            }
            
            switch (args.Event) {
                case PlayerEngineEvent.Iterate:
                
                    // if more than 60% of a track is played, use this track as
                    // a seed song for the next track.
                    if ((processed != PlayerEngineCore.CurrentTrack) &&
                            (PlayerEngineCore.Position
                                    > PlayerEngineCore.Length * 0.6)) {

                        processed = PlayerEngineCore.CurrentTrack;
                        lock(TracksMutex) {   
                            if (!seeds.Contains(processed)) {
                                // If we're playing another source
                                if (!tracks.Contains(processed)) {
                                    return;
                                }
                                List<TrackInfo> t = new List<TrackInfo>();
                                t.Add(processed);
                                seeds.Add(processed);
                                
                                List<TrackInfo> skip = new List<TrackInfo>();
                                
                                skip.AddRange(seeds);
                                if (skipped.Count > 0)
                                    skip.AddRange(skipped);
                                SimilarTracks(t, skip);
                            }
                        }
                    }
                    break;
            }
        }

    }
}
