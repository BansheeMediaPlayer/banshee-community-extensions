//
// RandomByLastfmSimilarArtists.cs
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
using System.IO;
using Banshee.Networking;

namespace Banshee.RandomByLastfm
{
    public class RandomByLastfmSimilarArtists : RandomBy
    {
        // These values have proved to assure a good mix
        // possible future enhancement would be to let the user change these via gui
        private const int MAX_ARTISTS = 200;
        private const string ARTIST_QUERY = @"SELECT ArtistID, NameLowered FROM CoreArtists GROUP BY NameLowered HAVING NameLowered in (?)";

        private static bool initiated = false;
        private static object initiated_lock = new object ();
        private static short instanceCount = 0;

        private static bool searchActive = false;
        private static object searchActive_lock = new object ();

        private static WeightedRandom<int> weightedRandom;
        private static List<int> processedArtists;

        private bool disposed = false;

        public RandomByLastfmSimilarArtists () : base("lastfm_shuffle_similar_artists")
        {
            Label = AddinManager.CurrentLocalizer.GetString ("Shuffle by similar Artists (via Lastfm)");
            Adverb = AddinManager.CurrentLocalizer.GetString ("by similar artists");
            Description = AddinManager.CurrentLocalizer.GetString ("Play songs similar to those already played (via Lastfm)");
            
            lock (initiated_lock) {
                instanceCount++;
                if (!initiated) {
                    ServiceManager.PlayerEngine.ConnectEvent (RandomByLastfmSimilarArtists.OnPlayerEvent, PlayerEvent.StateChange);
                    initiated = true;
                    weightedRandom = new WeightedRandom<int> ();
                    processedArtists = new List<int> ();
                }
            }
            
            Condition = "CoreArtists.ArtistID = ?";
            OrderBy = "RANDOM()";
        }

        public void Dispose ()
        {
            if (disposed)
                return;
            
            ThreadAssist.ProxyToMain (delegate {
                ServiceManager.PlayerEngine.DisconnectEvent (RandomByLastfmSimilarArtists.OnPlayerEvent);
                lock (initiated_lock) {
                    initiated = false;
                    instanceCount--;
                    if (instanceCount < 1) {
                        weightedRandom = null;
                        processedArtists = null;
                    }
                }
                disposed = true;
            });
        }

        protected override IEnumerable<object> GetConditionParameters (DateTime after)
        {
			yield return weightedRandom.GetRandom ();
        }

        public override bool Next (DateTime after)
        {
            return true;
        }

        /// <summary>
        /// Catch PlayerEvent and schedule new lastfm query, as long as current Artist is not the last artist queried
        /// </summary>
        /// <param name="args">
        /// A <see cref="PlayerEventArgs"/>
        /// </param>
        private static void OnPlayerEvent (PlayerEventArgs args)
        {
            DatabaseTrackInfo currentTrack = ServiceManager.PlayerEngine.CurrentTrack as DatabaseTrackInfo;
            if (currentTrack == null)
                return;
            
            int currentArtistId = currentTrack.ArtistId;
            if (ServiceManager.PlayerEngine.ActiveEngine.CurrentState == PlayerState.Playing && !processedArtists.Contains (currentArtistId)) {
                if (!weightedRandom.Contains (currentArtistId)) {
                    // User changed Track to a not similar artist, clear list
                    Log.Debug ("RandomByLastfmSimilarArtists: User changed track, clearing lists and resetting depth");
                    weightedRandom.Clear ();
                    weightedRandom.Add (currentArtistId, 1d);
                }
                
                lock (searchActive_lock) {
                    if (searchActive) {
                        Log.Debug ("RandomByLastfmSimilarArtists: Another Query is already running, aborting");
                        return;
                    } else {
                        searchActive = true;
                    }
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
            if(!ServiceManager.Get<Network>().Connected)
                return;

            DatabaseTrackInfo currentTrack = ServiceManager.PlayerEngine.ActiveEngine.CurrentTrack as DatabaseTrackInfo;
            if (currentTrack == null)
                return;
            
            LastfmArtistData artist = new LastfmArtistData (currentTrack.AlbumArtist);
            
            LastfmData<SimilarArtist> lastfmSimilarArtists;
            
            // Gotta catch 'em all Pattern
            try {
                lastfmSimilarArtists = artist.SimilarArtists;
            } catch (Exception e) {
                Log.Warning (e.ToString ());
                return;
            }
            
            // Artists from LastfmQuery
            // - Numbers are filtered
            // - Ordered by Matching Score
            var lastfmArtists = lastfmSimilarArtists.Where (a => !IsNumber (a.Name)).OrderByDescending (a => a.Match).Select (a => a);
            
            // Artists that are present on local database
            // - Reduced by max number to get
            var newArtists = GetPresentArtists (lastfmArtists);
            
            Log.DebugFormat ("RandomByLastfmSimilarArtists: {0} present similar Artists, adding {1} with factor {2}", weightedRandom.Count, newArtists.Count (), weightedRandom.Value (currentTrack.ArtistId));
            
            foreach (int currentId in newArtists.Keys) {
                weightedRandom.Add (currentId, newArtists[currentId], currentTrack.ArtistId);
            }
            
            if (weightedRandom.Count > MAX_ARTISTS) {
                Log.Debug ("RandomByLastfmSimilarArtists: Maximum reached, clearing random artists");
                LimitList ();
            }
            
            processedArtists.Add (currentTrack.ArtistId);
        }

        public static Dictionary<int, double> GetPresentArtists (IEnumerable<SimilarArtist> aLastfmArtists)
        {
            Dictionary<string, double> nameToMatch = new Dictionary<string, double> ();
            Dictionary<int, double> artistIdAndMatch = new Dictionary<int, double> ();
            foreach (SimilarArtist cArtist in aLastfmArtists) {
                string tmpName = Hyena.StringUtil.SearchKey (cArtist.Name);
                if (!nameToMatch.ContainsKey (tmpName))
                    nameToMatch.Add (tmpName, cArtist.Match);
            }
            
            using (var reader = ServiceManager.DbConnection.Query (ARTIST_QUERY, new object[] { nameToMatch.Keys.ToArray () })) {
                while (reader.Read ()) {
                    int cArtistId = (int)(long)reader[0];
                    string cArtistName = reader[1] as string;
                    if (!artistIdAndMatch.ContainsKey (cArtistId) && nameToMatch.ContainsKey (cArtistName)) {
                        artistIdAndMatch.Add (cArtistId, nameToMatch[cArtistName]);
                    }
                }
            }
            return artistIdAndMatch;
        }


        public static void LimitList ()
        {
            while (weightedRandom.Count > MAX_ARTISTS) {
                weightedRandom.Remove (weightedRandom.GetInvertedRandom ());
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
