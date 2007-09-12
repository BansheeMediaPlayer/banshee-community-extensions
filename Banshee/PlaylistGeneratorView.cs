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
using System.Collections.Generic;
using System.Collections;
using Gtk;
using Gdk;
using Pango;
using Mono.Unix;

using Banshee;
using Banshee.Base;
using Banshee.Sources;
using Banshee.MediaEngine;
using Banshee.Plugins.Mirage;

namespace Banshee.Plugins.Mirage
{
    public class PlaylistGeneratorView : TreeView
    {
        private enum ColumnId : int {
            Track,
            Artist,
            Title,
            Album,
            Genre,
            Year,
            Time,
            PlayCount,
            LastPlayed,
            Similarity
        };
        
        ListStore model;
        
        ArrayList columns;
        TreeIter playingIter;
        PlaylistGeneratorSource ap;
        
        string seedSongColor = "#FFF065";
        Pixbuf nowPlayingPixbuf;
        
        Dictionary<TrackInfo, float> similarity = 
            new Dictionary<TrackInfo, float>();

        public event EventHandler Stopped;

        public PlaylistGeneratorView(PlaylistGeneratorSource ap)
        {
            ButtonPressEvent += OnPlaylistViewButtonPressEvent;
            Globals.ActionManager["NextAction"].Activated += OnNextAction;
            Globals.ActionManager["PlayPauseAction"].Activated += OnPlayPauseAction;
            Globals.ActionManager["PreviousAction"].Activated += OnPreviousAction;
            PlayerEngineCore.EventChanged += OnPlayerEngineEventChanged;
            
            this.ap = ap;
            columns = new ArrayList();
            
            columns.Add(new PlaylistColumn(null, Catalog.GetString("Track"), "track", 
                new TreeCellDataFunc(TrackCellTrack), new CellRendererText(),
                0, -1));
            columns.Add(new PlaylistColumn(null, Catalog.GetString("Artist"), "artist", 
                new TreeCellDataFunc(TrackCellArtist), new CellRendererText(),
                1, -1));
            columns.Add(new PlaylistColumn(null, Catalog.GetString("Title"), "title", 
                new TreeCellDataFunc(TrackCellTitle), new CellRendererText(),
                2, -1));
            columns.Add(new PlaylistColumn(null, Catalog.GetString("Album"), "album", 
                new TreeCellDataFunc(TrackCellAlbum), new CellRendererText(),
                3, -1));
            columns.Add(new PlaylistColumn(null, Catalog.GetString("Genre"), "genre", 
                new TreeCellDataFunc(TrackCellGenre), new CellRendererText(),
                4, -1));
            columns.Add(new PlaylistColumn(null, Catalog.GetString("Year"), "year", 
                new TreeCellDataFunc(TrackCellYear), new CellRendererText(),
                5, -1));
               columns.Add(new PlaylistColumn(null, Catalog.GetString("Time"), "time", 
                new TreeCellDataFunc(TrackCellTime), new CellRendererText(),
                6, -1));
            
            PlaylistColumn PlaysColumn = new PlaylistColumn(null, 
                Catalog.GetString("Plays"), "play_count",
                new TreeCellDataFunc(TrackCellPlayCount), 
                new CellRendererText(),
                8, -1);
            columns.Add(PlaysColumn);
            
            PlaylistColumn LastPlayedColumn = new PlaylistColumn(null, 
                Catalog.GetString("Last Played"), "last_played",
                new TreeCellDataFunc(TrackCellLastPlayed), 
                new CellRendererText(),
                9, -1);
            columns.Add(LastPlayedColumn);
            
            foreach(PlaylistColumn plcol in columns) {
                AppendColumn(plcol.Column);
            }

            TreeViewColumn playIndColumn = new TreeViewColumn();
            Gtk.Image playIndImg = new Gtk.Image(
                    IconThemeUtils.LoadIcon(16, "audio-volume-high",
                            "blue-speaker"));
            playIndImg.Show();
            playIndColumn.Expand = false;
            playIndColumn.Resizable = false;
            playIndColumn.Clickable = false;
            playIndColumn.Reorderable = false;
            playIndColumn.Widget = playIndImg;
            
            nowPlayingPixbuf =
                    IconThemeUtils.LoadIcon(16, "media-playback-start",
                            Stock.MediaPlay, "now-playing-arrow");
            
            CellRendererPixbuf indRenderer = new CellRendererPixbuf();
            playIndColumn.PackStart(indRenderer, true);
            playIndColumn.SetCellDataFunc(indRenderer, 
                new TreeCellDataFunc(TrackCellInd));
            InsertColumn(playIndColumn, 0);

            ColumnDragFunction = new TreeViewColumnDropFunc(CheckColumnDrop);

            model = new ListStore(typeof(TrackInfo));
            Model = model;
            
            RulesHint = true;
            HeadersClickable = false;
            HeadersVisible = true;
            Selection.Mode = SelectionMode.Browse;
            
            model.SetSortFunc((int)ColumnId.Similarity,
                new TreeIterCompareFunc(SimilarityTreeIterCompareFunc));
            model.SetSortColumnId((int)ColumnId.Similarity, SortType.Ascending);
        }
        
        public void OnNextAction(object o, EventArgs e)
        {
            if (SourceManager.ActiveSource != ap)
                return;

            GLib.Timeout.Add(500, delegate {
                Advance();
                UpdateView();
                return false;
            });
        }

        public void OnPlayPauseAction(object o, EventArgs e)
        {
            if (SourceManager.ActiveSource != ap)
                return;

            UpdateView();
        }

        public void OnPreviousAction(object o, EventArgs e)
        {
            if (SourceManager.ActiveSource != ap)
                return;

            GLib.Timeout.Add(500, delegate {
                Regress();
                UpdateView();
                return false;
            });
        }

        private void OnPlayerEngineEventChanged(object o, PlayerEngineEventArgs args)
        {
            if (SourceManager.ActiveSource != ap)
                return;

            switch(args.Event) {
                case PlayerEngineEvent.EndOfStream:
                    GLib.Timeout.Add(500, delegate {
                        Advance();
                        UpdateView();
                        return false;
                    });
                    break;                    
            }
        }
        
        public void Clear()
        {
            model.Clear();
               similarity.Clear();
        }

        public void RemoveTrack(TrackInfo ti)
        {
            if (ti == null)
                return;
        
            if (similarity.ContainsKey(ti)) {
                TreeIter tri;
                TreePath tp;
                model.GetIterFirst(out tri);
                tp = model.GetPath(tri);
                    
                while (PathTrackInfo(tp) != ti) {
                    tp.Next();
                }
                model.GetIter(out tri, tp);
                model.Remove(ref tri);
                similarity.Remove(ti);
            }
        }
        
        public void AddTrack(TrackInfo ti, float sim)
        {
            if (ti == null)
                return;
                
            if (!similarity.ContainsKey(ti)) {
                similarity.Add(ti, sim);
                ti.TreeIter = model.AppendValues(ti);
            } else {
                similarity[ti] = sim;
            }
        }

        public void UpdateView()
        {
            Gtk.Application.Invoke(delegate {
                QueueDraw();
                ScrollToPlaying();
            });
        }
        
        public TreePath PlayingPath
        {
            get {
                try {
                    return playingIter.Equals(TreeIter.Zero)
                            ? null : model.GetPath(playingIter);
                } catch (NullReferenceException) {
                    return null;
                }
            }
        }

        public void ScrollToPlaying()
        {
            Gdk.Rectangle cellRect = GetCellArea (PlayingPath,
                    Columns[0]);

            Point point = new Point ();
            WidgetToTreeCoords (cellRect.Left, cellRect.Top,
                    out point.X, out point.Y);
            cellRect.Location = point;

            // we only care about vertical bounds
            if (cellRect.Location.Y < VisibleRect.Location.Y ||
                cellRect.Location.Y + cellRect.Size.Height >
                        VisibleRect.Location.Y + VisibleRect.Size.Height) {
                ScrollToCell(PlayingPath, null, true, 0.5f, 0.0f);
            }
        }

        private bool CheckColumnDrop(TreeView tree, TreeViewColumn col,
                                     TreeViewColumn prev, TreeViewColumn next)
        {
            // Don't allow moving other columns before the first column
            return prev != null;
        }

        [GLib.ConnectBefore]
        private void OnPlaylistViewButtonPressEvent(object o, 
            ButtonPressEventArgs args)
        {
            if (args.Event.Window != BinWindow)
                return;

/* DISABLED for now            if(args.Event.Button == 3) {
                PlaylistMenuPopupTimeout(args.Event.Time);
            }*/
            
            TreePath path;
            GetPathAtPos((int)args.Event.X, 
                (int)args.Event.Y, out path);
        
            if(path == null)
                return;
            
            switch(args.Event.Type) {
                case EventType.TwoButtonPress:
                    if(args.Event.Button != 1
                        || (args.Event.State &  (ModifierType.ControlMask 
                        | ModifierType.ShiftMask)) != 0)
                        return;
                    Selection.UnselectAll();
                    Selection.SelectPath(path);
                    PlayPath(path);
                    return;
                case EventType.ButtonPress:
                    if(Selection.PathIsSelected(path) &&
                   (args.Event.State & (ModifierType.ControlMask |
                            ModifierType.ShiftMask)) == 0)
                        args.RetVal = true;
                    return;
                default:
                    args.RetVal = false;
                    return;
            }
        }

        public int SimilarityTreeIterCompareFunc(TreeModel _model, TreeIter a,
            TreeIter b)
        {
            float a1f = 0;
            float b1f = 0;
            if (SourceManager.ActiveSource != ap)
                return 0;
                
            lock(((ICollection)similarity).SyncRoot) {

                TrackInfo a1 = IterTrackInfo(a);
                TrackInfo b1 = IterTrackInfo(b);
                
                if (similarity.ContainsKey(a1)) {
                    a1f = similarity[a1];
                }
                if (similarity.ContainsKey(b1)) {
                    b1f = similarity[b1];
                }
            }
            
            return a1f < b1f ? -1 : (a1f == b1f ? 0 : 1);
        }

        protected void SetRendererAttributes(CellRendererText renderer, 
            string text, TreeIter iter)
        {
            renderer.Text = text;
            renderer.Weight = iter.Equals(playingIter) 
                ? (int)Pango.Weight.Bold 
                : (int)Pango.Weight.Normal;
            
            renderer.Foreground = null;
            renderer.CellBackground = null;
            renderer.Sensitive = true;
            
            TrackInfo ti = IterTrackInfo(iter);
            if (ti == null) {
                return;
            }
            
            foreach (TrackInfo t in ap.SeedSongs) {
                if (ti == t) {
                    renderer.CellBackground = seedSongColor;
                }
            }
        }

        protected void TrackCellInd(TreeViewColumn tree_column,
            CellRenderer cell, TreeModel tree_model, TreeIter iter)
        {
            TrackInfo ti = tree_model.GetValue(iter, 0) as TrackInfo;
            CellRendererPixbuf renderer = (CellRendererPixbuf)cell;
            renderer.CellBackground = null;
            
            foreach (TrackInfo t in ap.SeedSongs) {
                if (ti == t) {
                    renderer.CellBackground = seedSongColor;
                }
            }
           
            if(PlayerEngineCore.CurrentTrack == null) {
                playingIter = TreeIter.Zero;
                renderer.Pixbuf = null;
                return;
            }
        
            if(ti != null) {
                bool same_track = false;
                
                if(PlayerEngineCore.CurrentTrack != null) {
                    same_track = PlayerEngineCore.CurrentTrack == ti;
                }
                
                if(same_track) {
                    renderer.Pixbuf = nowPlayingPixbuf;
                    playingIter = iter;
                } else {
                    renderer.Pixbuf = null;
                }
            } else {
                renderer.Pixbuf = null;
            }
        }
        
        protected void TrackCellTrack(TreeViewColumn tree_column,
            CellRenderer cell, TreeModel tree_model, TreeIter iter)
        {
            TrackInfo ti = IterTrackInfo(iter);
            if (ti == null) {
                return;
            }
            
            SetRendererAttributes((CellRendererText)cell,
                ti.TrackNumber > 0 ?
                        Convert.ToString(ti.TrackNumber) : String.Empty, iter);
        }    
        
        protected void TrackCellArtist(TreeViewColumn tree_column,
            CellRenderer cell, TreeModel tree_model, TreeIter iter)
        {
            TrackInfo ti = IterTrackInfo(iter);
            if(ti == null) {
                return;
            }
            
            SetRendererAttributes((CellRendererText)cell, ti.Artist, iter);
        }
        
        protected void TrackCellTitle(TreeViewColumn tree_column,
            CellRenderer cell, TreeModel tree_model, TreeIter iter)
        {
            TrackInfo ti = IterTrackInfo(iter);
            if (ti == null) {
                return;
            }
            
            SetRendererAttributes((CellRendererText)cell, ti.Title, iter);
        }
        
        protected void TrackCellAlbum(TreeViewColumn tree_column,
            CellRenderer cell, TreeModel tree_model, TreeIter iter)
        {
            TrackInfo ti = IterTrackInfo(iter);
            if (ti == null) {
                return;
            }
            
            SetRendererAttributes((CellRendererText)cell, ti.Album, iter);
        }
        
        protected void TrackCellGenre(TreeViewColumn tree_column,
            CellRenderer cell, TreeModel tree_model, TreeIter iter)
        {
            TrackInfo ti = IterTrackInfo(iter);
            if (ti == null) {
                return;
            }
            
            SetRendererAttributes((CellRendererText)cell, ti.Genre, iter);
        }
        
        protected void TrackCellYear(TreeViewColumn tree_column,
            CellRenderer cell, TreeModel tree_model, TreeIter iter)
        {
            TrackInfo ti = IterTrackInfo(iter);
            if (ti == null) {
                return;
            }
            
            int year = ti.Year;
            SetRendererAttributes((CellRendererText)cell,
                    year > 0 ? Convert.ToString(year) : String.Empty, iter);
        }
        
        protected void TrackCellTime(TreeViewColumn tree_column,
            CellRenderer cell, TreeModel tree_model, TreeIter iter)
        {
            TrackInfo ti = IterTrackInfo(iter);
            if (ti == null) {
                return;
            }
            
            SetRendererAttributes((CellRendererText)cell, 
                ti.Duration.TotalSeconds < 0.0 ? Catalog.GetString("N/A") : 
                        DateTimeUtil.FormatDuration(
                                (long)ti.Duration.TotalSeconds), iter);
        }
        
        protected void TrackCellPlayCount(TreeViewColumn tree_column,
            CellRenderer cell, TreeModel tree_model, TreeIter iter)
        {
            TrackInfo ti = IterTrackInfo(iter);
            if (ti == null) {
                return;
            }
            
            uint plays = ti.PlayCount;
            SetRendererAttributes((CellRendererText)cell,
                    plays > 0 ? Convert.ToString(plays) : String.Empty, iter);
        }
        
        protected void TrackCellLastPlayed(TreeViewColumn tree_column,
            CellRenderer cell, TreeModel tree_model, TreeIter iter)
        {
            TrackInfo ti = IterTrackInfo(iter);
            if(ti == null) {
                return;
            }
            
            DateTime lastPlayed = ti.LastPlayed;
            
            string disp = String.Empty;
            
            if(lastPlayed > DateTime.MinValue) {
                disp = lastPlayed.ToString();
            }
            
            SetRendererAttributes((CellRendererText)cell,
                    String.Format("{0}", disp), iter);
        }
        
        public void PlayPath(TreePath path)
        {
            Play(path);
            UpdateView();
        }
        
        public TrackInfo IterTrackInfo(TreeIter iter)
        {
            object o = model.GetValue(iter, 0);
            if (o != null) {
                return o as TrackInfo;
            }

            return null;
        }

        public TrackInfo PathTrackInfo(TreePath path)
        {
            TreeIter iter;

            if(!model.GetIter(out iter, path))
                return null;

            return IterTrackInfo(iter);
        }


        public void Play(TreePath path)
        {
            TrackInfo ti = PathTrackInfo(path);
            if (ti == null)
                return;
            PlayerEngineCore.OpenPlay(ti);
            model.GetIter(out playingIter, path);
        }

        public void PlayIter(TreeIter iter)
        {
            TrackInfo ti = IterTrackInfo(iter);
            
            if(ti == null)
                return;
                
            if(ti.CanPlay) {
                PlayerEngineCore.Open(ti);
                PlayerEngineCore.Play();
                playingIter = iter;
            } else {
                playingIter = iter;
                Continue();
            }
        }

        public void PlayPause()
        {
        
        }
        
        public void Advance()
        {
            ChangeDirection(true);
        }

        public void Regress()
        {
            ChangeDirection(false);    
        }

        public void Continue()
        {
            Advance();
        }
        
        private void StopPlaying()
        {
            EventHandler handler = Stopped;
            if(handler != null) {
                handler(this, new EventArgs());
            }
            
            playingIter = TreeIter.Zero;
        }

        private void ChangeDirection(bool forward)
        {
            TreePath currentPath = null;
            TreeIter currentIter, nextIter = TreeIter.Zero;
            TrackInfo currentTrack = null, nextTrack;
            
            if(!playingIter.Equals(TreeIter.Zero)) {
                try {
                    currentPath = model.GetPath(playingIter);
                } catch(NullReferenceException) {
                }
            }
            
            if(currentPath == null) {
                if(!model.GetIterFirst(out nextIter)) {
                    StopPlaying();
                    return;
                }

                PlayIter(nextIter);
                return;
            }
        
            int count = Count();
            int index = FindIndex(currentPath);
            bool lastTrack = index == count - 1;
        
            if(count <= 0 || index >= count || index < 0) {
                StopPlaying();
                return;
            }
                
            currentTrack = PathTrackInfo(currentPath);
            currentIter = playingIter;

            if (forward) {
                if(lastTrack) {
                    if(!model.IterNthChild(out nextIter, 0)) {
                        StopPlaying();
                        return;
                    }
                } else {                
                    currentPath.Next();                
                    if(!model.GetIter(out nextIter, currentPath)) {
                        StopPlaying();
                        return;
                    }
                }
                
                nextTrack = IterTrackInfo(nextIter);
                nextTrack.PreviousTrack = currentIter;
            } else {
                if(currentTrack.PreviousTrack.Equals(TreeIter.Zero)) {
                    if(index > 0 && currentPath.Prev()) {
                        if(!model.GetIter(out nextIter, currentPath)) {
                            StopPlaying();
                            return;
                        }
                    } else {
                        StopPlaying();
                        return;
                    }
                } else {
                    nextIter = currentTrack.PreviousTrack;
                }
            }
            
            if(!nextIter.Equals(TreeIter.Zero)) {
                PlayIter(nextIter);
            } else {
                StopPlaying();
            }
        }

        public int Count()
        {
            return model.IterNChildren();
        }
        
        private int FindIndex(TreePath a)
        {
            TreeIter iter;
            TreePath b;
            int i, n;
    
            for(i = 0, n = Count(); i < n; i++) {
                model.IterNthChild(out iter, i);
                b = model.GetPath(iter);
                if(a.Compare(b) == 0) 
                    return i;
            }
    
            return -1;
        }



    }
}
