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


using System.Data.Sql;

namespace Banshee.Plugins.Mirage
{
    public class PlaylistGeneratorSource : Banshee.Sources.ChildSource
    {
        protected List<TrackInfo> tracks = new List<TrackInfo>();
        public static List<TrackInfo> seeds = new List<TrackInfo>();
        protected List<TrackInfo> tracksOverride = new List<TrackInfo>();
        
        protected Db db;
        
        protected HBox uiContainer;
        protected PlaylistGeneratorView playlistView;
        private Label statusLabel = new Label();
        
        public override int Count {
            get {
                return tracks.Count;
            }
        }
        
        public override object TracksMutex {
            get {
                return ((IList)tracks).SyncRoot;
            }
        }

        public PlaylistGeneratorSource(string name, Db db)
                : base(name, 100)
        {
            this.db = db;

            Gtk.Application.Invoke(delegate {
                BuildInterface();
            });
        }
        
        public void OnLinkButtonClicked(object o, EventArgs e)
        {
            Gnome.Url.Show(((LinkButton)o).Uri);
        }
        
        public void SetStatusLabelText(string text)
        {
            Gtk.Application.Invoke(delegate {
                lock(statusLabel) {
                    statusLabel.Text = text;
                }
            });
        }
        
        private void BuildInterface()
        {
            
            ScrolledWindow view_scroll = new ScrolledWindow();
            view_scroll.HscrollbarPolicy = PolicyType.Automatic;
            view_scroll.VscrollbarPolicy = PolicyType.Automatic;
            view_scroll.ShadowType = ShadowType.In;

            playlistView = new PlaylistGeneratorView(this);
            view_scroll.Add(playlistView);
            VBox vb = new VBox(false, 2);
            vb.PackStart(view_scroll, true, true, 0);
            
            HBox hbex = new HBox(false, 4);
            Label l1 = new Label("<b>"+Catalog.GetString("Status:") +"</b> ");
            l1.UseMarkup = true;
            hbex.PackStart(l1, false, false, 0);
            statusLabel.Text = Catalog.GetString("Ready. Drag a song on the Playlist Generator to start!");
            hbex.PackStart(statusLabel, false, false, 0);
            hbex.PackStart(new HBox(), true, true, 0);
            
/*            LinkButton lb1 = new LinkButton("http://hop.at/mirage/", "");
            lb1.Clicked += OnLinkButtonClicked;
            lb1.Image = new Image(null, "mirage.png");
            hbex.PackStart(lb1, false, false, 0);*/
            
            LinkButton lb2 = new LinkButton("http://www.cp.jku.at/?miragelink", "");
            lb2.Image = new Image(null, "cp.png");
            lb2.Clicked += OnLinkButtonClicked;
            hbex.PackStart(lb2, false, false, 0);
            
            vb.PackStart(hbex, false, true, 0);
            
            uiContainer = new HBox();
            uiContainer.PackStart(vb, true, true, 0);
            uiContainer.ShowAll();
        }

        public override void Commit ()
        {
            int[] trackId;
            
            // Add seed tracks 
            lock(TracksMutex) {
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

                // initialize the similarity computation
                trackId = new int[tracks.Count];
                for (int i = 0; i < tracks.Count; i++) {
                    trackId[i] = tracks[i].TrackId;
                }
            }
            SimilarityCalculator sc =
                    new SimilarityCalculator(trackId, trackId, db,
                            UpdatePlaylist, 5);
            SetStatusLabelText(Catalog.GetString("Generating the playlist..."));
            Thread similarityCalculatorThread =
                    new Thread(new ThreadStart(sc.Compute));
            similarityCalculatorThread.Start();
        }
        
        protected void UpdatePlaylist(int[] playlist, int length)
        {
            Gtk.Application.Invoke(delegate {
                lock(TracksMutex) {
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
                    SetStatusLabelText(Catalog.GetString("Playlist ready."));
                }
                
                playlistView.QueueDraw();
                OnUpdated();
            });
            
        }
        
        public delegate void Playlist(int[] playlist, int length);
    

        public class SimilarityCalculator {
        
            int[] trackId;
            int[] excludeTrackId;
            Playlist play;
            int length;
            Db db;
            
            public SimilarityCalculator(int[] trackId, int[] excludeTrackId,
                    Db db, Playlist playlist, int length)
            {
                this.trackId = trackId;
                this.play = playlist;
                this.length = length;
                this.excludeTrackId = excludeTrackId;
                this.db = db;
            }
            
            public void Compute()
            {
                int[] playlist;
                playlist = Mir.SimilarTracks(trackId, excludeTrackId, db);
                play(playlist, length);
            }
        }
        

        public override void ShowPropertiesDialog()
        {
            // Show Properties
        }

        public override void Reorder(TrackInfo track, int position)
        {
            // Reorder Tracks
        }

        public override void AddTrack(TrackInfo track)
        {
            if(track is LibraryTrackInfo) {
                lock(TracksMutex) {
                    tracksOverride.Add(track);
                }
            }
            OnTrackAdded(track);
        }
        
        public override Gtk.Widget ViewWidget {
            get {
                return uiContainer;
            }
        }
        
        public override bool AcceptsInput {
            get {
                return true;
            }
        }

        public static Image icon = new Image(null, "source-localqueue.png");

        public override Gdk.Pixbuf Icon {
            get {
                return icon.Pixbuf;
            }
        }


    }
    
}
