//  
// Author:
//   Christian Martellini <christian.martellini@gmail.com>
//
// Copyright (C) 2009 Christian Martellini
// 
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// 

using System;
using System.Threading;
using System.Collections.Generic;

using Mono.Unix;

using Banshee.Networking;
using Banshee.ServiceStack;
using Banshee.Collection;

using Banshee.Lyrics.Sources;
using Banshee.Lyrics.IO;
using Banshee.Lyrics.Gui;
using Banshee.Streaming;

using Hyena;

namespace Banshee.Lyrics
{
    public delegate void LoadFinishedEventHandler (object o, LoadFinishedEventArgs e);
    public delegate void LoadStartedEventHandler (object o, EventArgs e);

    public class LoadFinishedEventArgs : EventArgs
    {
        public readonly string lyric;
        public readonly string error;
        public readonly string suggestion;

        public LoadFinishedEventArgs (string lyric, string suggestion, string error)
        {
            this.lyric = lyric;
            this.suggestion = suggestion;
            this.error = error;
        }
    }

    public class LyricsManager
    {

        public event LoadFinishedEventHandler LoadFinished;
        public event LoadStartedEventHandler LoadStarted;

        private List < ILyricSource > sourceList;

        private LyricsCache cache = new LyricsCache ();

        private static LyricsManager instance = new LyricsManager ();

        private LyricsManager () : base()
        {
            sourceList = new List<ILyricSource> ();
            sourceList.Add (new Lyrc ());
            sourceList.Add (new LeosLyrics ());
            sourceList.Add (new Lyriki ());
            sourceList.Add (new AutoLyrics ());
            sourceList.Add (new LyricWiki ());
        }

        internal static LyricsManager Instance {
            get { return instance; }
        }

         /*
         * Refresh the lyric for the current track
         */
        public void RefreshLyric (TrackInfo track)
        {
            cache.DeleteLyric (track);
            FetchLyric (track);
        }

        /*
         * Get the lyrics for the current track
         */
        public void FetchLyric (TrackInfo track)
        {
            string lyric = null;
            string error = null;
            string suggestion = null;

            LoadStarted (null, null);

            Banshee.Base.ThreadAssist.SpawnFromMain (delegate {
                try {
                    if (cache.IsInCache (track)) {
                        lyric = cache.ReadLyric (track);
                    } else {
                        lyric = DownloadLyric (track);
                    }

                    if (IsLyricOk (lyric)) {
                        Save (track, lyric);
                    } else {
                        suggestion = GetSuggestions (track);
                    }

                    if (LyricOutOfDate (track)) {
                        return;
                    }
                } catch (Exception e) {
                    Hyena.Log.DebugException (e);
                    error = e.Message;
                }

                Banshee.Base.ThreadAssist.ProxyToMain (delegate {
                    LoadFinished (this, new LoadFinishedEventArgs (lyric, suggestion, error));});
            });
        }

        public string DownloadLyric (TrackInfo track)
        {
            if (track == null) {
                return null;
            }

            //check if the netowrk is up
            if (!ServiceManager.Get<Banshee.Networking.Network> ().Connected) {
                throw new Exception ("You don't seem to be connected to internet.<br>Check your network connection.");
            }
            
            //download the lyrics
            string lyric = null;
            foreach (ILyricSource source in sourceList) {
                try {
                    lyric = source.GetLyrics (track.ArtistName, track.TrackTitle);
                } catch (Exception e) {
                    Log.Exception (e);
                    continue;
                }
                
                if (IsLyricOk (lyric)) {
                    lyric = AttachFooter (Utils.ToNormalString(lyric), source.Credits);
                    break;
                }
            }

            return lyric;
        }

        public void UpdateDB (TrackInfo track, string lyric) 
        {
            int track_id = ServiceManager.SourceManager.MusicLibrary.GetTrackIdForUri (track.Uri.AbsoluteUri);
            ServiceManager.DbConnection.Execute (
                        "INSERT OR REPLACE INTO LyricsDownloads (TrackID, Downloaded) VALUES (?, ?)",
                        track_id, IsLyricOk(lyric));
        }

        public void FetchLyricFromLyrc (string url)
        {
            if (url == null) {
                return;
            }

            LoadStarted (this, null);

            /*obtain absolute url for the lyric */
            Lyrc lyrc_server = (Lyrc) sourceList[0];
            if (!url.Contains (lyrc_server.Url)) {
                url = lyrc_server.Url + "/" + url;
            }

            /* get the lyric from lyrc */
            string lyric = lyrc_server.GetLyrics (url);
            if ( IsLyricOk (lyric)) {
                lyric = AttachFooter (lyric, lyrc_server.Credits);
                Save(ServiceManager.PlayerEngine.CurrentTrack, lyric);
            }

            LoadFinished (this, new LoadFinishedEventArgs (lyric, null, null));
        }

        private string GetSuggestions (TrackInfo track)
        {
            //Obtain suggestions from Lyrc
            ILyricSource lyrc_server = sourceList[0];

            string suggestions = lyrc_server.GetSuggestions (track.ArtistName, track.TrackTitle);
            return AttachFooter (suggestions, sourceList[0].Credits);
        }

        private bool IsLyricOk (string l)
        {
            return l != null && !l.Equals ("");
        }

        private bool LyricOutOfDate (TrackInfo track)
        {
            if (ServiceManager.PlayerEngine == null || ServiceManager.PlayerEngine.CurrentTrack == null) {
                return true;
            }
            string current_artist = ServiceManager.PlayerEngine.CurrentTrack.ArtistName;
            string current_title = ServiceManager.PlayerEngine.CurrentTrack.TrackTitle;
            return track.ArtistName != current_artist || track.TrackTitle != current_title;
        }

        private string AttachFooter (string lyric, string credits)
        {
            if (lyric == null) {
                return null;
            }
            return string.Format ("{0} \n\n {1}", lyric, credits);
        }

        public void SaveLyric (TrackInfo track, string lyric, bool rewrite)
        {
            Banshee.Base.ThreadAssist.SpawnFromMain (delegate {
                if (rewrite) {
                    cache.DeleteLyric (track);
                }
                Save (track, lyric);
            });
        }

        private void Save (TrackInfo track, string lyric)
        {
            if (Utils.IsHtml(lyric)) {
                lyric = Utils.ToNormalString(lyric);
            }

            if (!cache.IsInCache (track)) {
                cache.WriteLyric (track, lyric);
            }
            SaveToID3 (track, lyric);

            /*update the db */
            LyricsManager.Instance.UpdateDB (track, lyric);
        }

        private void SaveToID3 (TrackInfo track, string lyric)
        {
            TagLib.File file = StreamTagger.ProcessUri (track.Uri);
            if (file == null || lyric.Equals(file.Tag.Lyrics)) {
                return;
            }

            file.Tag.Lyrics = lyric;
            file.Save ();

            track.FileSize = Banshee.IO.File.GetSize (track.Uri);
            track.FileModifiedStamp = Banshee.IO.File.GetModifiedTime (track.Uri);
            track.LastSyncedStamp = DateTime.Now;
        }
    }
}
