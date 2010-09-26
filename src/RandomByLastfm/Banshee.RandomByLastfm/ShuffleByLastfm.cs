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
using System.Linq;
using Banshee.Collection.Database;
using Banshee.Collection;
using Mono.Addins;
using Banshee.ServiceStack;
using Banshee.MediaEngine;
using Lastfm;
using Lastfm.Data;
using Hyena.Json;
using System.Collections.Generic;
using System.Threading;
using Hyena;
using System.Text;

namespace Banshee.RandomByLastfm
{

    public class RandomByLastfm : RandomBy
    {
        public static List<string> artistsAdded;
        public static List<string> similarArtists;

        public static readonly int MAX_ARTISTS = 50;
        public static readonly int MAX_ARTIST_ADD = 40;
        public static readonly int MIN_ARTIST_ADD = 5;
        public static readonly string ARTIST_QUERY = @"SELECT DISTINCT(CoreArtists.NameLowered) FROM CoreArtists WHERE CoreArtists.NameLowered in (?)";

        public static int similarityDepth;

        public static bool initiated = false;
        public static object initiatedLock = new object ();

        public RandomByLastfm () : base("lastfm_shuffle")
        {
            Label = AddinManager.CurrentLocalizer.GetString ("Shuffle by similar Artist (via Lastfm)");
            Adverb = AddinManager.CurrentLocalizer.GetString ("by similar Artist");
            Description = AddinManager.CurrentLocalizer.GetString ("Play songs similar to those already played (via Lastfm)");
            
            lock (initiatedLock) {
                if (!initiated) {
                    ServiceManager.PlayerEngine.ConnectEvent (RandomByLastfm.OnPlayerEvent, PlayerEvent.StateChange);
                    initiated = true;
                    artistsAdded = new List<string> ();
                    similarArtists = new List<string> ();
                }
            }
            
            Condition = "CoreArtists.NameLowered in (?)";
            OrderBy = "RANDOM()";
        }

        public override TrackInfo GetPlaybackTrack (DateTime after)
        {
            TrackInfo track = GetShufflerTrack (after);
            return track;
        }

        /// <summary>
        /// I'm not shure what this does...
        /// </summary>
        /// <param name="after">
        /// A <see cref="DateTime"/>
        /// </param>
        /// <returns>
        /// A <see cref="System.Boolean"/>
        /// </returns>
        public override bool Next (DateTime after)
        {
            return true;
        }

        public override DatabaseTrackInfo GetShufflerTrack (DateTime after)
        {
            DatabaseTrackInfo track = GetTrack (ShufflerQuery, similarArtists.ToArray (), after);
            if (track == null)
                return null;
            
            if (!similarArtists.Contains (track.AlbumArtist.ToLower ()))
                similarArtists.Add (track.AlbumArtist.ToLower ());
            
            return track;
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

            if (ServiceManager.PlayerEngine.ActiveEngine.CurrentState == PlayerState.Playing && !artistsAdded.Contains (currentTrack.ArtistName.ToLower ())) {

                if (!similarArtists.Contains (currentTrack.AlbumArtist.ToLower ())) {
                    // User changed Track to a not similar artist, clear list
                    Hyena.Log.Debug ("User changed track, clearing lists and resetting depth");
                    similarArtists.Clear ();
                    artistsAdded.Clear ();
                    similarityDepth = 0;
                }

                UnscheduleQueryjob ();
                ScheduleQueryjob();
            }
        }

        public static void ScheduleQueryjob()
        {
                Hyena.Log.Debug (string.Format ("Scheduling new LastfmQueryJob at Thread: {0}", Thread.CurrentThread.Name));
                Banshee.Kernel.Scheduler.Schedule (new LastfmQueryjob ());
        }

        public static void UnscheduleQueryjob ()
        {
            Hyena.Log.Debug ("Unscheduling old LastfmQueryjobs");
            Banshee.Kernel.Scheduler.Unschedule (typeof(LastfmQueryjob));
        }

        /// <summary>
        /// Query Lastfm for similar Artists
        /// </summary>
        /// <remarks>Executed via Kernel Scheduler</remarks>
        public static void QueryLastfm ()
        {
            // ProxyToMain not necessary when scheduling via Kernel
            //ThreadAssist.ProxyToMain(delegate {
            
            TrackInfo currentTrack = ServiceManager.PlayerEngine.ActiveEngine.CurrentTrack;
            LastfmArtistData artist = new LastfmArtistData (currentTrack.AlbumArtist);
            
            // Formular: numTake = Max(MIN_ARTIST_ADD, MAX_ARTIST_ADD/(2^similarityDepth))
            int numTake = Math.Max ((int)Math.Floor (MAX_ARTIST_ADD / (Math.Pow (2, similarityDepth))), MIN_ARTIST_ADD);
            
            // Artists from LastfmQuery
            // - Numbers are filtered
            // - SimilarArtists doesnt already contain artists
            // - Ordered by Matching Score
            var lastfmArtists = artist.SimilarArtists.Where (a => !IsNumber (a.Name) && !similarArtists.Contains (a.Name.ToLower ())).OrderByDescending (ar => ar.Match).Select (a => a.Name.ToLower ());
            
            // Artists that are present on local database
            // - Reduced by max number to get
            var newArtists = GetPresentArtists (lastfmArtists).Take (numTake);
            
            Hyena.Log.Debug (string.Format ("[RandomByLastfm] {0} present similar Artists, adding {1} at Depth {2}", similarArtists.Count, newArtists.Count (), similarityDepth));
            
            similarArtists.AddRange (newArtists);
            
            if (similarArtists.Count > MAX_ARTISTS) {
                Hyena.Log.Debug ("Maximum reached, clearing random artists");
                LimitList ();
            }
            
            artistsAdded.Add (currentTrack.AlbumArtist.ToLower ());
            similarityDepth++;
            //});
        }

        public static List<string> GetPresentArtists (IEnumerable<string> aLastfmArtists)
        {
            List<string> presentArtists = new List<string> ();

            using (var reader = ServiceManager.DbConnection.Query (ARTIST_QUERY, new object[] { aLastfmArtists.ToArray () })) {
                while (reader.Read ()) {
                    presentArtists.Add (reader[0] as string);
                }
            }
            return presentArtists;
        }


        public static void LimitList ()
        {
            Random rand = new Random (DateTime.Now.Millisecond);
            while (similarArtists.Count > MAX_ARTISTS) {
                similarArtists.RemoveAt (rand.Next (similarArtists.Count));
            }
        }

        /// <summary>
        /// Helper Method - Determines wether string is just a plain number
        /// </summary>
        /// <param name="aInput">
        /// A <see cref="System.String"/>
        /// </param>
        /// <returns>
        /// A <see cref="Boolean"/>
        /// </returns>
        public static Boolean IsNumber (string aInput)
        {
            int num;
            return int.TryParse (aInput, out num);
        }

        public static string Join (List<string> aInput, string aDelimiter)
        {
            if(aInput == null || aDelimiter == null) return string.Empty;
            StringBuilder sb = new StringBuilder ();
            for (int i = 0; i < aInput.Count; i++) {
                sb.Append (aInput[i]);
                if (i < aInput.Count - 1)
                    sb.Append (aDelimiter);
            }
            return sb.ToString ();
        }
    }
}
