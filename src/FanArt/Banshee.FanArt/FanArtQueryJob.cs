//
// FanArtQueryJob.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//   Aurélien Mino <aurelien.mino@gmail.com>
//   Tomasz Maczyński <tmtimon@gmail.com>
//
// Copyright (C) 2006-2008 Novell, Inc.
// Copyright (C) 2010 Aurélien Mino
// Copyright 2013 Tomasz Maczyński
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;

using Hyena;

using Banshee.Metadata;
using Banshee.Collection;
using Banshee.Collection.Database;
using Banshee.ServiceStack;

using MusicBrainz;
using FanArt;
using CacheService;

namespace Banshee.FanArt
{
    public class FanArtQueryJob : MetadataServiceJob
    {
        private Cache cache = CacheManager.GetInstance.Initialize ("fanart");

        public FanArtQueryJob (IBasicTrackInfo track)
        {
            Track = track;
        }

        public override void Run ()
        {
            Lookup ();
        }

        public bool Lookup ()
        {
            if (Track == null || (Track.MediaAttributes & TrackMediaAttributes.Podcast) != 0) {
                return false;
            }
            /*
            string artwork_id = Track.ArtworkId;

            if (artwork_id == null) {
                return false;
            } else if (CoverArtSpec.CoverExists (artwork_id)) {
                return false;
            } else  */
            if (!InternetConnected) {
                return false;
            }

            DatabaseTrackInfo dbtrack = Track as DatabaseTrackInfo;

            if (dbtrack != null) {
                Log.Debug (String.Format ("FanArtQueryJob : Processing DatabaseTrackInfo {0}", dbtrack));
                var artistMusicbrainzID = dbtrack.Artist.MusicBrainzId ?? dbtrack.ArtistMusicBrainzId;

                if (String.IsNullOrEmpty (artistMusicbrainzID)) { 
                    Hyena.Log.Debug (String.Format ("FanArtQueryJob : Trying to get MusicBrainzId of an artist {0}",
                        dbtrack.ArtistName ?? ""));

                    var artistQuery = MusicBrainz.Artist.Query (Track.ArtistName);
                    var artist = artistQuery.PerfectMatch ();
                    artistMusicbrainzID = (artist != null) ? artist.Id : null;

                    SaveDbMusicBrainz (Track.ArtistName, artistMusicbrainzID);
                }


                if (!String.IsNullOrEmpty (artistMusicbrainzID)) {
                    try {
                        Log.Debug (String.Format ("FanArtQueryJob : Retrieving artist image for MBId={0}", artistMusicbrainzID));

                        return FanArtDownload (artistMusicbrainzID);

                    } catch (Exception e) {
                        Hyena.Log.Debug (String.Format ("Could not download image for {0}, because of exception {1}", 
                            artistMusicbrainzID, e));
                    }
                }
                return false;
            }

            Log.Debug ("FanArt: dbtrack info is null in FanArtQueryJob");

            return false;
        }

        private bool FanArtDownload (string artistMusicbrainzID)
        {
            var fanartDownloader = new FanArtDownloader (FanArtCore.ApiKey);
            string answer;
            if (cache.Get (artistMusicbrainzID) != null) {
                answer = cache.Get (artistMusicbrainzID).Value.ToString();
            } else {
                answer = fanartDownloader.GetFanArtArtistPage (artistMusicbrainzID);
                cache.Add (artistMusicbrainzID, answer);
            }
            var results = Results.FromString (answer);
            return Save (artistMusicbrainzID, results);
        }

        private bool Save (string artistMusicbrainzID, Results results)
        {
            var downloaded = false;
            var correctResuts = results as CorrectResults;
            if (correctResuts != null) {
                var bestImageInfo = correctResuts.BestArtistImageInfo;
                if (bestImageInfo != null) {
                    Hyena.Log.Debug ("FanArtQueryJob: Artist image should be downloaded");
                    SaveArtistImage (bestImageInfo.Url, artistMusicbrainzID);
                    downloaded = true;
                    /*
                    var dbTrack = Track as DatabaseTrackInfo;
                    if (dbTrack != null) {
                    dbTrack.ArtistMusicBrainzId = artistMusicbrainzID;
                    }
                    */
                } else {
                    Log.Debug ("FanArtQueryJob: No artist image was found");
                }
            } else {
                Log.Debug ("FanArtQueryJob: Results were incorrect");
            }

            SaveDbImageData (artistMusicbrainzID, downloaded);
            return downloaded;
        }

        private bool SaveArtistImage (string uri, string artistMusicbrainzID)
        {
            var filename = FanArtArtistImageSpec.CreateArtistImageFileName (artistMusicbrainzID);

            return SaveHttpStream (new Uri (uri), FanArtArtistImageSpec.GetPath (filename), null);
        }

        private void SaveDbImageData (string artistMusicbrainzID, bool downloaded)
        {
            ServiceManager.DbConnection.Execute (
                "INSERT OR REPLACE INTO ArtistImageDownloads (MusicBrainzID, Downloaded, LastAttempt) VALUES (?, ?, ?)",
                artistMusicbrainzID, downloaded, DateTime.Now);
        }

        private void SaveDbMusicBrainz (string artistName, string artistMbId) {
            ServiceManager.DbConnection.Execute (
                "INSERT OR REPLACE INTO ArtistMusicBrainz (ArtistName, MusicBrainzId, LastAttempt) VALUES (?, ?, ?)",
                artistName, artistMbId, DateTime.Now);
        }
    }
}

