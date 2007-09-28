/***************************************************************************
 *  PlaylistModel.cs
 *
 *  Copyright (C) 2005-2006 Novell, Inc.
 *  Written by Aaron Bockover <aaron@abock.org>
 ****************************************************************************/

/*  THIS FILE IS LICENSED UNDER THE MIT LICENSE AS OUTLINED IMMEDIATELY BELOW: 
 *
 *  Permission is hereby granted, free of charge, to any person obtaining a
 *  copy of this software and associated documentation files (the "Software"),  
 *  to deal in the Software without restriction, including without limitation  
 *  the rights to use, copy, modify, merge, publish, distribute, sublicense,  
 *  and/or sell copies of the Software, and to permit persons to whom the  
 *  Software is furnished to do so, subject to the following conditions:
 *
 *  The above copyright notice and this permission notice shall be included in 
 *  all copies or substantial portions of the Software.
 *
 *  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
 *  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
 *  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
 *  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
 *  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING 
 *  FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
 *  DEALINGS IN THE SOFTWARE.
 */

using System;
using System.Collections;
using System.Threading;
using Gtk;

using Banshee.Base;
using Banshee.MediaEngine;
using Banshee.Sources;
using Banshee.Database;

namespace Banshee.Plugins.Mirage
{
    public enum RepeatMode {
        None,
        All,
        Single,
        ErrorHalt
    }

    public class PlaylistModel : ListStore
    {
        private static int uid;
        private TimeSpan totalDuration = new TimeSpan(0);

        private TreeIter playingIter;
        
        private RepeatMode repeat = RepeatMode.None;
        private bool shuffle = false;
        
        public event EventHandler Updated;
        public event EventHandler Stopped;

        public static int NextUid
        {
            get {
                return uid++;
            }
        }
        
        public PlaylistModel() : base(typeof(TrackInfo))
        {
            PlayerEngineCore.EventChanged += delegate(object o, PlayerEngineEventArgs args) {
                switch(args.Event) {
                    case PlayerEngineEvent.StartOfStream:
                        playingIter = TreeIter.Zero;
                        return;
                }
            };
        }

        public void AddTrack(TrackInfo ti)
        {
            AddTrack(ti, true);
        }
        
        public void AddTrack(TrackInfo ti, bool raiseUpdate)
        {
            if(ti == null)
                return;

            totalDuration += ti.Duration;
            ti.TreeIter = AppendValues(ti);
            
            if(raiseUpdate) {
                RaiseUpdated(this, new EventArgs());
            }
        }

        // --- Helper Methods ---
        
        public TrackInfo IterTrackInfo(TreeIter iter)
        {
            object o = GetValue(iter, 0);
            if(o != null) {
              return o as TrackInfo;
            }
            
            return null;
        }
        
        public TrackInfo PathTrackInfo(TreePath path)
        {
            TreeIter iter;
            
            if(!GetIter(out iter, path))
                return null;
                
            return IterTrackInfo(iter);
        }

        private bool can_save_sort_id = true;

        public void ClearSortOrder()
        {
            can_save_sort_id = false;
            SetSortColumnId(-1, SortType.Ascending);
            can_save_sort_id = true;
        }

        public void RestoreSortOrder()
        {
            SetSortColumnId(SourceManager.ActiveSource.SortColumn, SourceManager.ActiveSource.SortType);
        }
        
        protected override void OnSortColumnChanged()
        {
            if(!can_save_sort_id) {
                return;
            }
            
            int sort_column;
            SortType sort_type;
            
            GetSortColumnId(out sort_column, out sort_type);
            
            SourceManager.ActiveSource.SortColumn = sort_column;
            SourceManager.ActiveSource.SortType = sort_type;
        }
        
        // --- Playback Methods ---
        
        public void PlayPath(TreePath path)
        {
            TrackInfo ti = PathTrackInfo(path);
            if(ti == null)
                return;
                
            PlayerEngineCore.OpenPlay(ti);
            GetIter(out playingIter, path);
        }
        
        public void PlayIter(TreeIter iter)
        {
            TrackInfo ti = IterTrackInfo(iter);
            if(ti == null)
                return;
                
            if(ti.CanPlay) {
                PlayerEngineCore.OpenPlay(ti);
                playingIter = iter;
            } else {
                playingIter = iter;
                Continue();
            }
        }
        
        // --- IPlaybackModel 
        
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
            // TODO: Implement random playback without repeating a track 
            // until all Tracks have been played first (see Legacy Sonance)
            
            TreePath currentPath = null;
            TreeIter currentIter, nextIter = TreeIter.Zero;
            TrackInfo currentTrack = null, nextTrack;
            
            if(!playingIter.Equals(TreeIter.Zero)) {
                try {
                    currentPath = GetPath(playingIter);
                } catch(NullReferenceException) {
                }
            }
            
            if(currentPath == null) {
                if(shuffle) {
                    if(!GetRandomTrackIter(out nextIter)) {
                        StopPlaying();
                        return;
                    }
                } else if(!GetIterFirst(out nextIter)) {
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
        
            if(repeat == RepeatMode.Single) {
                if(repeat == RepeatMode.ErrorHalt) {
                    StopPlaying();
                    return;
                }
                
                nextIter = currentIter;
            } else if(forward) {
                if(lastTrack && repeat == RepeatMode.ErrorHalt) {
                    StopPlaying();
                    return;
                }
                
                if(lastTrack && repeat == RepeatMode.All) {
                    if(!IterNthChild(out nextIter, 0)) {
                        StopPlaying();
                        return;
                    }
                } else if(shuffle) {
                    if(!GetRandomTrackIter(out nextIter)) {
                        StopPlaying();
                        return;
                    }
                } else {                
                    currentPath.Next();                
                    if(!GetIter(out nextIter, currentPath)) {
                        StopPlaying();
                        return;
                    }
                }
                
                nextTrack = IterTrackInfo(nextIter);
                nextTrack.PreviousTrack = currentIter;
            } else {
                if(currentTrack.PreviousTrack.Equals(TreeIter.Zero)) {
                    if(index > 0 && currentPath.Prev()) {
                        if(!GetIter(out nextIter, currentPath)) {
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
            return IterNChildren();
        }
        
        private int FindIndex(TreePath a)
        {
            TreeIter iter;
            TreePath b;
            int i, n;
    
            for(i = 0, n = Count(); i < n; i++) {
                IterNthChild(out iter, i);
                b = GetPath(iter);
                if(a.Compare(b) == 0) 
                    return i;
            }
    
            return -1;
        }

        private bool GetRandomTrackIter(out TreeIter iter)
        {
            const double HOVER_FREQUENCY = 0.60;

            if(SourceManager.ActiveSource is LibrarySource
               && !playingIter.Equals(TreeIter.Zero)
               && Count () == Globals.Library.Tracks.Count) {   // XXX: Gross way to check that there isn't a search active
                TrackInfo last_track = IterTrackInfo(playingIter);
                
                if(Globals.Random.NextDouble() < HOVER_FREQUENCY) {
                    DbCommand command = new DbCommand(
                        @"SELECT TrackID 
                            FROM Tracks 
                            WHERE Genre = :genre
                            ORDER BY RANDOM() LIMIT 1",
                            "genre", last_track.Genre
                    );
                    
                    int id = 0;
                    try {
                        id = Convert.ToInt32((string)Globals.Library.Db.QuerySingle(command));
                    } catch { } 
    
                    if(id > 0) {
                        LibraryTrackInfo track = Globals.Library.GetTrack(id);
                        if (track != null) {
                            iter = track.TreeIter;
                            return true;
                        }
                    }
                }
            }

            int randIndex = Globals.Random.Next(0, Count());
            return IterNthChild(out iter, randIndex);
        }
        
        public void ClearModel()
        {
            totalDuration = new TimeSpan(0);
            playingIter = TreeIter.Zero;
            Clear();
                
            if(Updated != null && ThreadAssist.InMainThread) {
                Updated(this, new EventArgs());
            }
        }
        
        public void RemoveTrack(ref TreeIter iter, TrackInfo track)
        {
            TrackInfo ti = track;
            if(ti == null) {
                ti = IterTrackInfo(iter);
            }
            
            totalDuration -= ti.Duration;
            ti.TreeIter = TreeIter.Zero;
            
            if(iter.Equals(playingIter)) {
                playingIter = TreeIter.Zero;
            }
            
            Remove(ref iter);
            RaiseUpdated(this, new EventArgs());
        }
        
        public void RemoveTrack(TrackInfo track)
        {
            TreeIter iter = track.TreeIter;

            if (!iter.Equals(TreeIter.Zero)) {
                RemoveTrack(ref iter, track);
            } else {
                Console.WriteLine ("failed to remove track {0}", track);
            }
        }
        
        public int GetIterIndex(TreeIter iter)
        {
            TreePath path = GetPath(iter);
            return path == null ? - 1 : path.Indices[0];
        }
        
        // --- Event Raise Handlers ---

        private void RaiseUpdated(object o, EventArgs args)
        {
            EventHandler handler = Updated;
            if(handler != null)
                handler(o, args);
        }
        
        public TimeSpan TotalDuration 
        {
            get {
                return totalDuration;
            }
        }
        
        public TreePath PlayingPath
        {
            get {
                try {
                    return playingIter.Equals(TreeIter.Zero) 
                        ? null : GetPath(playingIter);
                } catch(NullReferenceException) {
                    return null;
                }
            }
        }
        
        public TreeIter PlayingIter {
            set {
                playingIter = value;
            }

            get {
                return playingIter;
            }
        }
        
        public RepeatMode Repeat {
            set {
                repeat = value;
            }
            
            get {
                return repeat;
            }
        }
        
        public bool Shuffle {
            set {
                shuffle = value;
            }
            
            get {
                return shuffle;
            }
        }
        
        public TrackInfo FirstTrack {
          get {
              TreeIter iter = TreeIter.Zero;
              if(GetIterFirst(out iter) && !iter.Equals(TreeIter.Zero))
                  return IterTrackInfo(iter);
              return null;
          }
       }
    }
}
