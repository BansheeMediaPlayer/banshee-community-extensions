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
using System.Collections.Generic;

using Banshee.Networking;
using Banshee.ServiceStack;
using Banshee.Collection;

using Banshee.Lyrics.Sources;
using Banshee.Lyrics.IO;
using Banshee.Streaming;

using Hyena;

namespace Banshee.Lyrics
{
    public delegate void LoadFinishedEventHandler (object o, LoadFinishedEventArgs e);
    public delegate void LoadStartedEventHandler (object o, EventArgs e);

    public class LoadFinishedEventArgs : EventArgs
    {
        public readonly string lyrics;
        public readonly string error;
        public readonly string suggestion;

        public LoadFinishedEventArgs (string lyrics, string suggestion, string error)
        {
            this.lyrics = lyrics;
            this.suggestion = suggestion;
            this.error = error;
        }
    }

    public class LyricsManager
    {

        public event LoadFinishedEventHandler LoadFinished;
        public event LoadStartedEventHandler LoadStarted;

        private List < ILyricsSource > sourceList;

        private LyricsCache cache = new LyricsCache ();

        private static LyricsManager instance = new LyricsManager ();

        private LyricsManager () : base()
        {
            sourceList = new List<ILyricsSource> ();
            sourceList.Add (new Lyrc ());
            sourceList.Add (new LeosLyrics ());
            sourceList.Add (new Lyriki ());
            sourceList.Add (new AutoLyrics ());
            sourceList.Add (new LyricWiki ());
            sourceList.Add (new LyricsPlugin ());
        }

        internal static LyricsManager Instance {
            get { return instance; }
        }

         /*
         * Refresh the lyrics for the current track
         */
        public void RefreshLyrics (TrackInfo track)
        {
            cache.DeleteLyrics (track);
            FetchLyrics (track);
        }

        /*
         * Get the lyricss for the current track
         */
        public void FetchLyrics (TrackInfo track)
        {
            string lyrics = null;
            string error = null;
            string suggestion = null;

            LoadStarted (null, null);

            ThreadAssist.SpawnFromMain (delegate {
                try {
                    if (cache.IsInCache (track)) {
                        lyrics = cache.ReadLyrics (track);
                    } else {
                        lyrics = DownloadLyrics (track);
                    }

                    if (IsLyricsOk (lyrics)) {
                        Save (track, lyrics);
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

                ThreadAssist.ProxyToMain (delegate {
                    LoadFinished (this, new LoadFinishedEventArgs (lyrics, suggestion, error));});
            });
        }

        public string DownloadLyrics (TrackInfo track)
        {
            if (track == null) {
                return null;
            }

            //check if the netowrk is up
            if (!ServiceManager.Get<Banshee.Networking.Network> ().Connected) {
                throw new Exception ("You don't seem to be connected to internet.<br>Check your network connection.");
            }
            
            //download the lyricss
            string lyrics = null;
            foreach (ILyricsSource source in sourceList) {
                try {
                    lyrics = source.GetLyrics (track.ArtistName, track.TrackTitle);
                } catch (Exception e) {
                    Log.Exception (e);
                    continue;
                }
                
                if (IsLyricsOk (lyrics)) {
                    lyrics = AttachFooter (Utils.ToNormalString(lyrics), source.Credits);
                    break;
                }
            }

            return lyrics;
        }

        public void UpdateDB (TrackInfo track, string lyrics) 
        {
            int track_id = ServiceManager.SourceManager.MusicLibrary.GetTrackIdForUri (track.Uri.AbsoluteUri);
            ServiceManager.DbConnection.Execute (
                        "INSERT OR REPLACE INTO LyricsDownloads (TrackID, Downloaded) VALUES (?, ?)",
                        track_id, IsLyricsOk(lyrics));
        }

        public void FetchLyricsFromLyrc (string url)
        {
            if (url == null) {
                return;
            }

            LoadStarted (this, null);

            /*obtain absolute url for the lyrics */
            Lyrc lyrc_server = (Lyrc) sourceList[0];
            if (!url.Contains (lyrc_server.Url)) {
                url = lyrc_server.Url + "/" + url;
            }

            /* get the lyrics from lyrc */
            string lyrics = lyrc_server.GetLyrics (url);

            LyricsManager.Instance.UpdateDB (ServiceManager.PlayerEngine.CurrentTrack, lyrics);
            if ( IsLyricsOk (lyrics)) {
                lyrics = AttachFooter (lyrics, lyrc_server.Credits);
                Save(ServiceManager.PlayerEngine.CurrentTrack, lyrics);
            }

            LoadFinished (this, new LoadFinishedEventArgs (lyrics, null, null));
        }

        private string GetSuggestions (TrackInfo track)
        {
            //Obtain suggestions from Lyrc
            ILyricsSource lyrc_server = sourceList[0];

            return lyrc_server.GetSuggestions (track.ArtistName, track.TrackTitle);
        }

        private bool IsLyricsOk (string l)
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

        private string AttachFooter (string lyrics, string credits)
        {
            if (lyrics == null) {
                return null;
            }
            return string.Format ("{0} \n\n {1}", lyrics, credits);
        }

        public void SaveLyrics (TrackInfo track, string lyrics, bool rewrite)
        {
            if (!IsLyricsOk (lyrics)) {
                /*update the db always */
                LyricsManager.Instance.UpdateDB (track, lyrics);
                return;
            }

            ThreadAssist.SpawnFromMain (delegate {
                if (rewrite) {
                    cache.DeleteLyrics (track);
                }
                Save (track, lyrics);
                LyricsManager.Instance.UpdateDB (track, lyrics);
            });
        }

        private void Save (TrackInfo track, string lyrics)
        {
            if (Utils.IsHtml (lyrics)) {
                lyrics = Utils.ToNormalString (lyrics);
            }

            if (!cache.IsInCache (track)) {
                cache.WriteLyrics (track, lyrics);
            }
            SaveToID3 (track, lyrics);
        }

        private void SaveToID3 (TrackInfo track, string lyrics)
        {
            TagLib.File file = StreamTagger.ProcessUri (track.Uri);
            if (file == null || lyrics.Equals(file.Tag.Lyrics)) {
                return;
            }

            file.Tag.Lyrics = lyrics;
            file.Save ();

            track.FileSize = Banshee.IO.File.GetSize (track.Uri);
            track.FileModifiedStamp = Banshee.IO.File.GetModifiedTime (track.Uri);
            track.LastSyncedStamp = DateTime.Now;
        }
    }
}
