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
using System.IO;
using Lastfm;

namespace Banshee.RandomByLastfm
{
    public class RandomByLastfmUserTopArtists : RandomBy
    {
        private static WeightedRandom<int> weightedRandom;

        private const string ARTIST_QUERY = @"SELECT ArtistID, NameLowered FROM CoreArtists GROUP BY NameLowered HAVING NameLowered in (?) OR MusicBrainzID in (?)";

        private static string track_condition = String.Format ("AND CoreArtists.ArtistID = ? {0} ORDER BY RANDOM()", RANDOM_CONDITION);

        private static bool initiated = false;
        private static object initiated_lock = new object ();
        private static short instanceCount = 0;

        private bool disposed = false;

        public RandomByLastfmUserTopArtists () : base("lastfm_shuffle_topartists")
        {
            Label = AddinManager.CurrentLocalizer.GetString ("Shuffle by your Top Artists (via Lastfm)");
            Adverb = AddinManager.CurrentLocalizer.GetString ("by your top artists");
            Description = AddinManager.CurrentLocalizer.GetString ("Play songs from your Top Artists (via Lastfm)");
            
            Condition = "CoreArtists.ArtistID = ?";
            OrderBy = "RANDOM()";

            lock (initiated_lock) {
                instanceCount++;
                if (!initiated) {
                    ServiceManager.PlayerEngine.ConnectEvent (RandomByLastfmUserTopArtists.OnPlayerEvent, PlayerEvent.StateChange);
                    initiated = true;
                    Log.Debug("RandomByLastfm: Initialising List");
                    weightedRandom = new WeightedRandom<int>();
                }
            }
            //QueryLastfm();
        }

        public void Dispose ()
        {
            if (disposed)
                return;
            
            ThreadAssist.ProxyToMain (delegate {
                //ServiceManager.PlayerEngine.DisconnectEvent (RandomByLastfmUserTopArtists.OnPlayerEvent);
                lock (initiated_lock) {
                    initiated = false;
                    instanceCount--;
                    if(instanceCount < 1) {
                        weightedRandom = null;
                    }
                }
                disposed = true;
            });
        }

        private static void OnPlayerEvent (PlayerEventArgs args) {
            if(ServiceManager.PlayerEngine.CurrentState != PlayerState.Ready) return;
            ThreadAssist.ProxyToMain (delegate {
                Log.Debug("RandomByLastfmUserTopArtists: Starting Query");
                QueryLastfm();
                ServiceManager.PlayerEngine.DisconnectEvent(RandomByLastfmUserTopArtists.OnPlayerEvent);
            });
        }

        public override TrackInfo GetPlaybackTrack (DateTime after)
        {
            return Cache.GetSingleWhere (track_condition, weightedRandom.GetInvertedRandom(), after, after);
        }

        public override bool Next (DateTime after)
        {
            return true;
        }

        public override DatabaseTrackInfo GetShufflerTrack (DateTime after)
        {
            return GetTrack (ShufflerQuery, weightedRandom.GetInvertedRandom(), after);
        }


        /// <summary>
        /// Query Lastfm for UserTopArtists
        /// </summary>
        /// <remarks>Executed via Kernel Scheduler</remarks>
        public static void QueryLastfm ()
        {
            Account account = LastfmCore.Account;

            if(account.UserName == null | account.UserName == string.Empty) return;
            LastfmUserData userData = new LastfmUserData(account.UserName);
            LastfmData<UserTopArtist> topArtists = userData.GetTopArtists(TopType.Overall);

            Log.Debug("RandomByLastfmUserTopArtists: Searching for present artists");
            Dictionary<int, int> artists = GetPresentArtists(topArtists);
            Log.Debug(String.Format("RandomByLastfmUserTopArtists: Found {0} of {1} Artists", artists.Count, topArtists.Count));
            foreach(int cArtistId in artists.Keys) {
                weightedRandom.Add(cArtistId, artists[cArtistId]);
            }
            Log.Debug(weightedRandom.ToString());
        }

        public static Dictionary<int, int> GetPresentArtists (LastfmData<UserTopArtist> aData)
        {
            Dictionary<string, int> artistMatch = new Dictionary<string, int>();
            Dictionary<int, int> artistIdsAndRank = new Dictionary<int, int>();
            List<string> mbids = new List<string>();
            foreach(UserTopArtist cArtist in aData) {
                string tmp = Hyena.StringUtil.SearchKey (cArtist.Name);
                if(!artistMatch.ContainsKey(tmp)) {
                    artistMatch.Add(tmp, cArtist.Rank);
                    mbids.Add(cArtist.MbId);
                }
            }

            using (var reader = ServiceManager.DbConnection.Query (ARTIST_QUERY, artistMatch.Keys.ToArray(), mbids.ToArray ())) {
                while (reader.Read ()) {
                    int cId = (int)(long)reader[0];
                    string cName = reader[1] as string;
                    if(artistMatch.ContainsKey(cName)) {
                        artistIdsAndRank.Add(cId, artistMatch[cName]);
                    }
                }
            }
            return artistIdsAndRank;
        }
    }
}
