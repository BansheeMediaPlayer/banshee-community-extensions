//
// RandomByLastfm.cs
//
// Author:
//   Raimo Radczewski <raimoradczewski@googlemail.com>
//
// Copyright (C) 2010 Raimo Radczewski
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using Mono.Addins;
using Lastfm.Data;
using Hyena;
using Banshee.Collection.Database;
using Banshee.Collection;
using Banshee.ServiceStack;
using Banshee.MediaEngine;
using System.Net;

namespace Banshee.RandomByLastfm
{
    public class RandomByLastfm : RandomBy
    {
        private static List<string> artists_added;
        private static List<string> similar_artists;

        // These values have proved to assure a good mix
        // possible future enhancement would be to let the user change these via gui
        private const int MAX_ARTISTS = 20;
        private const int MAX_ARTIST_ADD = 10;
        private const int MIN_ARTIST_ADD = 5;
        private const string ARTIST_QUERY = @"SELECT DISTINCT(CoreArtists.NameLowered) FROM CoreArtists WHERE CoreArtists.NameLowered in (?)";

        private static string track_condition = String.Format ("AND CoreArtists.NameLowered in (?) {0} ORDER BY RANDOM()", RANDOM_CONDITION);

        private static int similarity_depth;

        private static bool initiated = false;
        private static object initiated_lock = new object ();

        private static bool searchActive = false;
        private static object searchActive_lock = new object ();

        private bool disposed = false;

        public RandomByLastfm () : base("lastfm_shuffle")
        {
            Label = AddinManager.CurrentLocalizer.GetString ("Shuffle by similar Artist (via Lastfm)");
            Adverb = AddinManager.CurrentLocalizer.GetString ("by similar artist");
            Description = AddinManager.CurrentLocalizer.GetString ("Play songs similar to those already played (via Lastfm)");
            
            lock (initiated_lock) {
                if (!initiated) {
                    ServiceManager.PlayerEngine.ConnectEvent (RandomByLastfm.OnPlayerEvent, PlayerEvent.StateChange);
                    initiated = true;
                    artists_added = new List<string> ();
                    similar_artists = new List<string> ();
                }
            }
            
            Condition = "CoreArtists.NameLowered in (?)";
            OrderBy = "RANDOM()";
        }

        public void Dispose ()
        {
            if (disposed)
                return;
            
            ThreadAssist.ProxyToMain (delegate {
                ServiceManager.PlayerEngine.DisconnectEvent (OnPlayerEvent);
                similar_artists = null;
                artists_added = null;
                lock (initiated_lock)
                    initiated = false;
                
                disposed = true;
            });
        }

        public override TrackInfo GetPlaybackTrack (DateTime after)
        {
            return Cache.GetSingleWhere (track_condition, similar_artists.ToArray (), after, after);
        }

        public override bool Next (DateTime after)
        {
            return true;
        }

        public override DatabaseTrackInfo GetShufflerTrack (DateTime after)
        {
            return GetTrack (ShufflerQuery, similar_artists.ToArray (), after);
        }

        /// <summary>
        /// Catch PlayerEvent and schedule new lastfm query, as long as current Artist is not the last artist queried
        /// </summary>
        /// <param name="args">
        /// A <see cref="PlayerEventArgs"/>
        /// </param>
        private static void OnPlayerEvent (PlayerEventArgs args)
        {
            TrackInfo currentTrack = ServiceManager.PlayerEngine.CurrentTrack;
            if (currentTrack == null)
                return;
            
            string currentArtist = Hyena.StringUtil.SearchKey (currentTrack.AlbumArtist);
            
            if (ServiceManager.PlayerEngine.ActiveEngine.CurrentState == PlayerState.Playing && !artists_added.Contains (currentArtist)) {
                
                if (!similar_artists.Contains (currentArtist)) {
                    // User changed Track to a not similar artist, clear list
                    Log.Debug ("RandomByLastfm: User changed track, clearing lists and resetting depth");
                    similar_artists.Clear ();
                    artists_added.Clear ();
                    similarity_depth = 0;
                    similar_artists.Add (currentArtist);
                }
                
                lock (searchActive_lock) {
                    if (searchActive) {
                        Log.Debug ("Another Query is already running, aborting");
                        return;
                    }
                    searchActive = true;
                }
                
                ThreadAssist.SpawnFromMain (delegate {
                    QueryLastfm ();
                    lock (searchActive_lock) {
                        searchActive = false;
                    }
                });
            }
        }

        /// <summary>
        /// Query Lastfm for similar Artists
        /// </summary>
        /// <remarks>Executed via Kernel Scheduler</remarks>
        public static void QueryLastfm ()
        {
            TrackInfo currentTrack = ServiceManager.PlayerEngine.ActiveEngine.CurrentTrack;
            LastfmArtistData artist = new LastfmArtistData (currentTrack.AlbumArtist);
            
            // Formular: numTake = Max(MIN_ARTIST_ADD, MAX_ARTIST_ADD/(2^similarityDepth))
            // Simple formular, so "derived" artists don't change the list too much
            int numTake = Math.Max ((int)Math.Floor (MAX_ARTIST_ADD / (Math.Pow (2, similarity_depth))), MIN_ARTIST_ADD);
            
            LastfmData<SimilarArtist> lastfmSimilarArtists;
            
            try {
                lastfmSimilarArtists = artist.SimilarArtists;
            } catch (WebException e) {
                Log.Warning (e.ToString ());
                return;
            }
            
            // Artists from LastfmQuery
            // - Numbers are filtered
            // - SimilarArtists doesnt already contain artists
            // - Ordered by Matching Score
            var lastfmArtists = lastfmSimilarArtists.Where (a => !IsNumber (a.Name) && !similar_artists.Contains (Hyena.StringUtil.SearchKey (a.Name))).OrderByDescending (ar => ar.Match).Select (a => Hyena.StringUtil.SearchKey (a.Name));
            
            // Artists that are present on local database
            // - Reduced by max number to get
            var newArtists = GetPresentArtists (lastfmArtists).Take (numTake);
            
            Log.DebugFormat ("RandomByLastfm: {0} present similar Artists, adding {1} at Depth {2}", similar_artists.Count, newArtists.Count (), similarity_depth);
            
            similar_artists.AddRange (newArtists);
            
            if (similar_artists.Count > MAX_ARTISTS) {
                Log.Debug ("RandomByLastfm: Maximum reached, clearing random artists");
                LimitList ();
            }
            
            artists_added.Add (currentTrack.AlbumArtist.ToLower ());
            similarity_depth++;
        }

        public static IEnumerable<string> GetPresentArtists (IEnumerable<string> aLastfmArtists)
        {
            using (var reader = ServiceManager.DbConnection.Query (ARTIST_QUERY, new object[] { aLastfmArtists.ToArray () })) {
                while (reader.Read ()) {
                    yield return reader[0] as string;
                }
            }
        }


        public static void LimitList ()
        {
            Random rand = new Random (DateTime.Now.Millisecond);
            while (similar_artists.Count > MAX_ARTISTS) {
                similar_artists.RemoveAt (rand.Next (similar_artists.Count));
            }
        }

        /// <summary>
        /// Helper Method - Determines whether string is just a plain number
        /// </summary>
        /// <param name="aInput">
        /// A <see cref="System.String"/>
        /// </param>
        /// <returns>
        /// A <see cref="Boolean"/>
        /// </returns>
        /// <remarks>Artists like 213 are filtered too! Check for leading zero?</remarks>
        public static Boolean IsNumber (string aInput)
        {
            int num;
            return int.TryParse (aInput, out num);
        }
    }
}
