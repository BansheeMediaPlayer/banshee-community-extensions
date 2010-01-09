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

        private List < ILyricSource > sourceList;       /*the sources for download lyrics */

        private LyricsCache cache = new LyricsCache (); /*object to manage saved lyrics */

        private static LyricsManager instance = new LyricsManager ();

        private LyricsManager () : base()
        {
            sourceList = new List<ILyricSource> ();
            sourceList.Add (new Lyrc ());
            sourceList.Add (new LeosLyrics ());
            sourceList.Add (new Lyriki ());
            sourceList.Add (new AutoLyrics ());
            //sourceList.Add (new LyricWiki ());
        }

        internal static LyricsManager Instance {
            get { return instance; }
        }


         /*
         * Refresh the lyrics for the current track
         */
        public void RefreshLyrics (string artist, string track_title)
        {
            cache.DeleteLyric (ServiceManager.PlayerEngine.CurrentTrack.ArtistName,
                ServiceManager.PlayerEngine.CurrentTrack.TrackTitle);

            FetchLyrics (artist,track_title);
        }

        /*
         * Get the lyrics for the current track
         */
        public void FetchLyrics (string artist, string track_title)
        {
            string lyric = null;
            string error = null;
            string suggestion = null;

            LoadStarted (null, null);

            Banshee.Base.ThreadAssist.SpawnFromMain (delegate {
                try {
                    lyric = DownloadLyrics (artist, track_title, false);

                    if (lyric == null) {
                        suggestion = GetSuggestions (artist, track_title);
                    }

                    if (LyricOutOfDate (artist, track_title)) {
                        return;
                    }
                } catch (Exception e) {
                    Hyena.Log.DebugException (e);
                    error = e.Message;
                }

                Banshee.Base.ThreadAssist.ProxyToMain (delegate { 
                    LoadFinished (this, new LoadFinishedEventArgs (lyric, suggestion, error)); });
            });
        }

        public void WriteLyric (string artist, string title, string lyric)
        {
            cache.WriteLyric (artist, title, lyric.Replace ("\n", "<br>"));
        }
        
        public string DownloadLyrics (string artist, string title, bool force)
        {
            if (artist == null || title == null) {
                return null;
            }

            //check if the lyric is in cache (if force = true force a new fetch of the lyrics)
            if (!force && cache.IsInCache (artist, title)) {
                return cache.ReadLyric (artist, title);
            }
            
            //check if the netowrk is up
            if (!ServiceManager.Get<Banshee.Networking.Network> ().Connected) {
                throw new Exception ("You don't seem to be connected to internet.<br>Check your network connection.");
            }
            
            //download the lyrics
            string lyric = null;
            foreach (ILyricSource source in sourceList) {
                try {
                    lyric = source.GetLyrics (artist, title);
                } catch (Exception e) {
                    Log.Exception (e);
                    continue;
                }

                //write Lyrics in cache if ok
                if (IsLyricOk (lyric)) {
                    lyric = AttachFooter (lyric, source.Credits);
                    cache.WriteLyric (artist, title, lyric);
                    break;
                }
            }

            return lyric;
        }

        public void GetLyricsFromLyrc (string url)
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
                string artist = ServiceManager.PlayerEngine.CurrentTrack.ArtistName;
                string track_title = ServiceManager.PlayerEngine.CurrentTrack.TrackTitle;
                cache.WriteLyric (artist, track_title, lyric);
            }

            LoadFinished (this, new LoadFinishedEventArgs (lyric, null, null));
        }

        private string GetSuggestions (string artist, string title)
        {
            //Obtain suggestions from Lyrc
            ILyricSource lyrc_server = sourceList[0];

            string suggestions = lyrc_server.GetSuggestions (artist, title);
            return AttachFooter (suggestions, sourceList[0].Credits);
        }

        private bool IsLyricOk (string l)
        {
            return l != null && !l.Equals ("");
        }

        private bool LyricOutOfDate (string artist, string title)
        {
            if ( ServiceManager.PlayerEngine == null || ServiceManager.PlayerEngine.CurrentTrack == null ) {
                return true;
            }
            string current_artist = ServiceManager.PlayerEngine.CurrentTrack.ArtistName;
            string current_title = ServiceManager.PlayerEngine.CurrentTrack.TrackTitle;
            return artist != current_artist || title != current_title;
        }

        private string AttachFooter (string lyric, string credits)
        {
            if (lyric == null) {
                return null;
            }
            return string.Format ("{0} <br><br> <i>{1}</i>", lyric, credits);
        }
    }
}
