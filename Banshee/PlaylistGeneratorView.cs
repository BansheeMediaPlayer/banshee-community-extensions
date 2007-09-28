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
using System.Collections;
using System.Collections.Generic;

using Gtk;

using Banshee.Base;
using Banshee.Dap;
using Banshee.Sources;
using Banshee.MediaEngine;
using Banshee.Plugins.Mirage.TrackView.Columns;

namespace Banshee.Plugins.Mirage
{
    public class PlaylistGeneratorView : TreeView, IDisposable
    {
        private List<TrackViewColumn> columns;
        private static PlaylistModel model = new PlaylistModel();
       
        private Gdk.Pixbuf ripping_pixbuf;
        private Gdk.Pixbuf now_playing_pixbuf;
        private Gdk.Pixbuf drm_pixbuf;
        private Gdk.Pixbuf resource_not_found_pixbuf;
        private Gdk.Pixbuf unknown_error_pixbuf;

        public TreeViewColumn RipColumn;

        PlaylistGeneratorSource pgs;

        public PlaylistGeneratorView(PlaylistGeneratorSource pgs)
        {        
            Model = model;
            
            // Load pixbufs
            drm_pixbuf = IconThemeUtils.LoadIcon(16, "emblem-readonly", "emblem-important", Stock.DialogError);
            resource_not_found_pixbuf = IconThemeUtils.LoadIcon(16, "emblem-unreadable", Stock.DialogError);
            unknown_error_pixbuf = IconThemeUtils.LoadIcon(16, "dialog-error", Stock.DialogError);
            ripping_pixbuf = Gdk.Pixbuf.LoadFromResource("cd-action-rip-16.png");
            now_playing_pixbuf = IconThemeUtils.LoadIcon(16, "media-playback-start", 
                Stock.MediaPlay, "now-playing-arrow");
                
            // Load configurable columns
            columns = new List<TrackViewColumn>();            
            columns.Add(new TrackNumberColumn());
            columns.Add(new ArtistColumn());
            columns.Add(new TitleColumn());
            columns.Add(new AlbumColumn());
            columns.Add(new GenreColumn());
            columns.Add(new YearColumn());
            columns.Add(new DurationColumn());
            columns.Add(new LastPlayedColumn());
            columns.Add(new PlayCountColumn());
            columns.Add(new RatingColumn());
            columns.Add(new UriColumn());
            columns.Add(new DateAddedColumn());
            columns.Sort();
            
            foreach(TrackViewColumn column in columns) {
                column.Model = model;
                AppendColumn(column);
                column.CreatePopupableHeader();
                column.SetMaxFixedWidth(this);
            }

            // Create static columns
            TreeViewColumn status_column = new TreeViewColumn();
            CellRendererPixbuf status_renderer = new CellRendererPixbuf();
            status_column.Expand = false;
            status_column.Resizable = false;
            status_column.Clickable = false;
            status_column.Reorderable = false;
            status_column.Widget = new Image(IconThemeUtils.LoadIcon(16, "audio-volume-high", "blue-speaker"));
            status_column.Widget.Show();
            status_column.PackStart(status_renderer, true);
            status_column.SetCellDataFunc(status_renderer, new TreeCellDataFunc(StatusColumnDataHandler));
            InsertColumn(status_column, 0);
            
            CellRendererToggle rip_renderer = new CellRendererToggle();
            rip_renderer.Activatable = true;
            rip_renderer.Toggled += OnRipToggled;
            
            RipColumn = new TreeViewColumn();
            RipColumn.Expand = false;
            RipColumn.Resizable = false;
            RipColumn.Clickable = false;
            RipColumn.Reorderable = false;
            RipColumn.Visible = false;
            RipColumn.Widget = new Gtk.Image(ripping_pixbuf);
            RipColumn.Widget.Show();
            RipColumn.PackStart(rip_renderer, true);
            RipColumn.SetCellDataFunc(rip_renderer, new TreeCellDataFunc(RipColumnDataHandler));
            InsertColumn(RipColumn, 1);
            
            TreeViewColumn void_hack_column = new TreeViewColumn();
            void_hack_column.Expand = false;
            void_hack_column.Resizable = false;
            void_hack_column.Clickable = false;
            void_hack_column.Reorderable = false;
            void_hack_column.FixedWidth = 1;
            AppendColumn(void_hack_column);

            // set up tree view
            RulesHint = true;
            HeadersClickable = false;
            HeadersVisible = true;
            Selection.Mode = SelectionMode.Browse;
            
            ColumnDragFunction = new TreeViewColumnDropFunc(CheckColumnDrop);

            model.DefaultSortFunc = new TreeIterCompareFunc(SimilarityTreeIterCompareFunc);

            SourceManager.ActiveSourceChanged += delegate(SourceEventArgs args) {
                if (SourceManager.ActiveSource == pgs) {
                    ButtonPressEvent += OnPlaylistViewButtonPressEvent;
                    Globals.ActionManager["NextAction"].Activated += OnNextAction;
                    Globals.ActionManager["PlayPauseAction"].Activated += OnPlayPauseAction;
                    Globals.ActionManager["PreviousAction"].Activated += OnPreviousAction;
                    PlayerEngineCore.EventChanged += OnPlayerEngineEventChanged;
                } else {
                    ButtonPressEvent -= OnPlaylistViewButtonPressEvent;
                    Globals.ActionManager["NextAction"].Activated -= OnNextAction;
                    Globals.ActionManager["PlayPauseAction"].Activated -= OnPlayPauseAction;
                    Globals.ActionManager["PreviousAction"].Activated -= OnPreviousAction;
                    PlayerEngineCore.EventChanged -= OnPlayerEngineEventChanged;
                }
            };

            this.pgs = pgs;
        }    

        private void OnRipToggled(object o, ToggledArgs args)
        {
            try {
                AudioCdTrackInfo ti = (AudioCdTrackInfo)model.PathTrackInfo(new TreePath(args.Path));
                CellRendererToggle renderer = (CellRendererToggle)o;
                ti.CanRip = !ti.CanRip;
                renderer.Active = ti.CanRip;
            } catch {
            }   
        }
        
        private bool CheckColumnDrop(TreeView tree, TreeViewColumn col, TreeViewColumn prev, TreeViewColumn next)
        {
            return prev != null && next != null;
        }
            
        public TrackInfo IterTrackInfo(TreeIter iter)
        {
            return Model.GetValue(iter, 0) as TrackInfo;
        }
       
        private void SetTrackPixbuf(CellRendererPixbuf renderer, TrackInfo track, bool nowPlaying)
        {
            if(nowPlaying) {
                renderer.Pixbuf = now_playing_pixbuf;
                return;
            } else if(track is AudioCdTrackInfo) {
                renderer.Pixbuf = null;
                return;
            }
            
            switch(track.PlaybackError) {
                case TrackPlaybackError.ResourceNotFound:
                    renderer.Pixbuf = resource_not_found_pixbuf;
                    break;
                case TrackPlaybackError.Drm:
                    renderer.Pixbuf = drm_pixbuf;
                    break;
                case TrackPlaybackError.Unknown:
                case TrackPlaybackError.CodecNotFound:
                    renderer.Pixbuf = unknown_error_pixbuf;
                    break;
                default:
                    renderer.Pixbuf = null;
                    break;
            }
        }
        
        protected void StatusColumnDataHandler(TreeViewColumn tree_column,
            CellRenderer cell, TreeModel tree_model, TreeIter iter)
        {
            TrackInfo ti = tree_model.GetValue(iter, 0) as TrackInfo;
            CellRendererPixbuf renderer = (CellRendererPixbuf)cell;

            renderer.CellBackground = null;
            foreach (TrackInfo t in PlaylistGeneratorSource.seeds) {
                if (ti == t) {
                    renderer.CellBackground = "#FFF065";
                }
            }
            
            if(PlayerEngineCore.CurrentTrack == null) {
                model.PlayingIter = TreeIter.Zero;
                SetTrackPixbuf(renderer, ti, false);
                return;
            } else if(model.PlayingIter.Equals(iter)) {
                SetTrackPixbuf(renderer, ti, true);
                return;
            } else if(!model.PlayingIter.Equals(TreeIter.Zero)) {
                SetTrackPixbuf(renderer, ti, false);
                return;
            }
            
            if(ti != null) {
                bool same_track = false;
                
                if(PlayerEngineCore.CurrentTrack != null && PlayerEngineCore.CurrentTrack.Uri != null) {
                    same_track = PlayerEngineCore.CurrentTrack.Uri.Equals(ti.Uri);
                    if(same_track) {
                        model.PlayingIter = iter;
                    }
                } 
                
                SetTrackPixbuf(renderer, ti, same_track);
            } else {
                renderer.Pixbuf = null;
            }
        }
        
        protected void RipColumnDataHandler(TreeViewColumn tree_column, CellRenderer cell, 
            TreeModel tree_model, TreeIter iter)
        {
            CellRendererToggle toggle = (CellRendererToggle)cell;
            AudioCdTrackInfo ti = model.IterTrackInfo(iter) as AudioCdTrackInfo;
 
            if(ti != null) {
                toggle.Sensitive = ti.CanPlay && !ti.IsRipped;
                toggle.Activatable = toggle.Sensitive;
                toggle.Active = ti.CanRip && !ti.IsRipped;
            } else {
                toggle.Active = false;
            }
        }

        public void PlayPath(TreePath path)
        {
            model.PlayPath(path);
            QueueDraw();
            ScrollToPlaying();
        }
        
        public void UpdateView()
        {
            QueueDraw();
            ScrollToPlaying();
        }

        public void ScrollToPlaying()
        {
            if(!IsRealized) {
                return;
            }
            
            Gdk.Rectangle cellRect = GetCellArea(model.PlayingPath, Columns[0]);

            Gdk.Point point = new Gdk.Point();
            WidgetToTreeCoords(cellRect.Left, cellRect.Top, out point.X, out point.Y);
            cellRect.Location = point;

            // we only care about vertical bounds
            if(cellRect.Location.Y < VisibleRect.Location.Y ||
                cellRect.Location.Y + cellRect.Size.Height > VisibleRect.Location.Y + VisibleRect.Size.Height) {
                ScrollToCell(model.PlayingPath, null, true, 0.5f, 0.0f);
            }
        }

        /* Mirage Modifications to Banshee/PlaylistView */

        public Dictionary<TrackInfo, float> similarity = new Dictionary<TrackInfo, float>();

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

                while (model.PathTrackInfo(tp) != ti) {
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

        public int SimilarityTreeIterCompareFunc(TreeModel _model, TreeIter a,
            TreeIter b)
        {
            float a1f = 0;
            float b1f = 0;
            if (SourceManager.ActiveSource != pgs)
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

        [GLib.ConnectBefore]
        private void OnPlaylistViewButtonPressEvent(object o,
            ButtonPressEventArgs args)
        {
            if (args.Event.Window != BinWindow)
                return;

            TreePath path;
            GetPathAtPos((int)args.Event.X,
                (int)args.Event.Y, out path);

            if(path == null)
                return;

            switch(args.Event.Type) {
                case Gdk.EventType.TwoButtonPress:
                    if(args.Event.Button != 1
                        || (args.Event.State &  (Gdk.ModifierType.ControlMask
                        | Gdk.ModifierType.ShiftMask)) != 0)
                        return;
                    Selection.UnselectAll();
                    Selection.SelectPath(path);
                    PlayPath(path);
                    return;
                case Gdk.EventType.ButtonPress:
                    if(Selection.PathIsSelected(path) &&
                   (args.Event.State & (Gdk.ModifierType.ControlMask |
                            Gdk.ModifierType.ShiftMask)) == 0)
                        args.RetVal = true;
                    return;
                default:
                    args.RetVal = false;
                    return;
            }
        }

        public void OnNextAction(object o, EventArgs e)
        {
            Application.Invoke(delegate {
                model.Advance();
                UpdateView();
            });
        }

        public void OnPlayPauseAction(object o, EventArgs e)
        {
            UpdateView();
        }

        public void OnPreviousAction(object o, EventArgs e)
        {
            Application.Invoke(delegate {
                model.Regress();
                UpdateView();
            });
        }

        private void OnPlayerEngineEventChanged(object o, PlayerEngineEventArgs args)
        {
            if (args.Event == PlayerEngineEvent.EndOfStream) {
                Application.Invoke(delegate {
                    model.Advance();
                    UpdateView();
                });
            }
        }
    }
}
