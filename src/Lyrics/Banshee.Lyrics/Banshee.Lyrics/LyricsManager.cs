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
using System.Linq;

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

        private readonly List <SourceData> sources = new List<SourceData> ();
        private readonly Lyrc lyrc;

        private LyricsCache cache = new LyricsCache ();

        private static LyricsManager instance = new LyricsManager ();

        private LyricsManager () : base()
        {
            sources.Add (new SourceData (lyrc = new Lyrc ()));
            sources.Add (new SourceData (new LyricWiki ()));
            sources.Add (new SourceData (new LeosLyrics ()));
            sources.Add (new SourceData (new Lyriki ()));
            sources.Add (new SourceData (new AutoLyrics ()));
            // Disabled as lyrics search doesn't work anymore
            // see https://bugzilla.gnome.org/show_bug.cgi?id=640570
            //sources.Add (new SourceData (new LyricsPlugin ()));
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
         * Get the lyrics for the current track
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
                    Log.DebugException (e);
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

            //check if the network is up
            if (!ServiceManager.Get<Banshee.Networking.Network> ().Connected) {
                throw new NetworkUnavailableException ("You don't seem to be connected to internet. Check your network connection.");
            }

            //download the lyrics
            string lyrics = null;
            foreach (var source in GetSources (SourceData.LyricsSelector)) {
                bool found = false;
                try {
                    lyrics = source.Source.GetLyrics (track.ArtistName, track.TrackTitle);
                    found = IsLyricsOk (lyrics);
                } catch (Exception e) {
                    Log.Exception (e);
                    continue;
                } finally {
                    source.IncrementLyrics (found);
                }

                if (found) {
                    lyrics = AttachFooter (Utils.ToNormalString(lyrics), source.Source.Credits);
                    Log.DebugFormat ("Fetched lyrics from {0} for {1} - {2}",
                        source.Source.Name, track.ArtistName, track.TrackTitle);
                    return lyrics;
                }
            }

            Log.DebugFormat ("Couldn't find lyrics for {0} - {1}", track.ArtistName, track.TrackTitle);
            return null;
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
            if (!url.Contains (lyrc.Url)) {
                url = lyrc.Url + "/" + url;
            }

            /* get the lyrics from lyrc */
            string lyrics = lyrc.GetLyrics (url);

            LyricsManager.Instance.UpdateDB (ServiceManager.PlayerEngine.CurrentTrack, lyrics);
            if ( IsLyricsOk (lyrics)) {
                lyrics = AttachFooter (lyrics, lyrc.Credits);
                Save(ServiceManager.PlayerEngine.CurrentTrack, lyrics);
            }

            LoadFinished (this, new LoadFinishedEventArgs (lyrics, null, null));
        }

        private string GetSuggestions (TrackInfo track)
        {
            string suggestions = null;
            foreach (var source in GetSources (SourceData.SuggestionsSelector)) {
                bool found = false;
                try {
                    suggestions = source.Source.GetSuggestions (track.ArtistName, track.TrackTitle);
                    found = !String.IsNullOrEmpty (suggestions);
                } catch (Exception e) {
                    Log.Exception (e);
                    continue;
                } finally {
                    source.IncrementSuggestions (found);
                }

                if (found) {
                    Log.DebugFormat ("Fetched suggestions from {0} for {1} - {2}",
                        source.Source.Name, track.ArtistName, track.TrackTitle);
                    return suggestions;
                }
            }
            Log.DebugFormat ("Couldn't find any suggestions for {0} - {1}", track.ArtistName, track.TrackTitle);
            return null;
        }

        private bool IsLyricsOk (string l)
        {
            return !String.IsNullOrEmpty (l);
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

            if (Banshee.Configuration.Schema.LibrarySchema.WriteMetadata.Get ()) {
                SaveToID3 (track, lyrics);
            }
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

        private IEnumerable<SourceData> GetSources (Func<SourceData, double> selector)
        {
            return sources.OrderBy<SourceData, double> (selector);
        }

        private class SourceData
        {
            public ILyricsSource Source { get; private set; }
            public int LyricsTotal { get; private set; }
            public int LyricsFound { get; private set; }
            public int SuggestionsTotal { get; private set; }
            public int SuggestionsFound { get; private set; }

            public SourceData (ILyricsSource source)
            {
                Source = source;
                LyricsTotal = 0;
                LyricsFound = 0;
                SuggestionsTotal = 0;
                SuggestionsFound = 0;
            }

            public static double LyricsSelector (SourceData data)
            {
                return data.LyricsTotal > 0 ? 1d - (double)data.LyricsFound / data.LyricsTotal : 0d;
            }

            public static double SuggestionsSelector (SourceData data)
            {
                return data.SuggestionsTotal > 0 ? 1d - (double)data.SuggestionsFound / data.SuggestionsTotal : 0d;
            }

            public void IncrementLyrics (bool found)
            {
                LyricsTotal++;
                if (found) {
                    LyricsFound++;
                }
            }

            public void IncrementSuggestions (bool found)
            {
                SuggestionsTotal++;
                if (found) {
                    SuggestionsFound++;
                }
            }
        }
    }
}
