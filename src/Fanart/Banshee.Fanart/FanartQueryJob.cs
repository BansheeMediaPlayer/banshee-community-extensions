//
// FanartQueryJob.cs
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
using System.Collections;
using System.IO;
using System.Net;
//using System.Xml;
using System.Text;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Web;
using System.Text.RegularExpressions;

using Hyena;
using Banshee.Base;
using Banshee.Metadata;
using Banshee.Kernel;
using Banshee.Collection;
using Banshee.Streaming;
using Banshee.Networking;
using Banshee.Collection.Database;
using MusicBrainz;
using Fanart;
using Banshee.ServiceStack;

namespace Banshee.Fanart
{
    public class FanartQueryJob : MetadataServiceJob
    {
        public FanartQueryJob (IBasicTrackInfo track)
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
                var artistMusicbrainzID = dbtrack.Artist.MusicBrainzId ?? dbtrack.ArtistMusicBrainzId;

                if (String.IsNullOrEmpty (artistMusicbrainzID)) { 
                    Hyena.Log.Debug (String.Format ("FanartQueryJob : Trying to get MusicBrainzId of an artist {0}",
                        dbtrack.ArtistName ?? ""));

                    var artistQuery = MusicBrainz.Artist.Query (Track.ArtistName);
                    var artist = artistQuery.PerfectMatch ();
                    artistMusicbrainzID = (artist != null) ? artist.Id : null;

                    SaveDbMusicBrainz (Track.ArtistName, artistMusicbrainzID);
                }


                if (!String.IsNullOrEmpty (artistMusicbrainzID)) {
                    try {
                        Hyena.Log.Debug (String.Format("FanartQueryJob : Retrieving artist image for MBId={0}", artistMusicbrainzID));

                        var fanartDownloader = new FanartDownloader (FanartCore.ApiKey);
                        var answer = fanartDownloader.GetFanartArtistPage (artistMusicbrainzID);
                        var results = Results.FromString (answer);

                        var correctResuts = results as CorrectResults;
                        if (correctResuts != null) {
                            var bestImageInfo = correctResuts.BestArtistImageInfo;
                            if (bestImageInfo != null) {
                                Hyena.Log.Debug ("FanartQueryJob: Artist image should be downloaded");
                                SaveArtistImage (bestImageInfo.Url, artistMusicbrainzID);
                                var downloaded = true;
                                SaveDbImageData (artistMusicbrainzID, downloaded);
                                /*
                                var dbTrack = Track as DatabaseTrackInfo;
                                if (dbTrack != null) {
                                    dbTrack.ArtistMusicBrainzId = artistMusicbrainzID;
                                }
                                */

                                return true;
                            } else {
                                Hyena.Log.Debug ("FanartQueryJob: No artist image was found");
                            }
                        } else {
                            Hyena.Log.Debug ("FanartQueryJob: Results were incrrect");
                            return false;
                        }
                    } catch (Exception e) {
                        Hyena.Log.Debug (String.Format ("Could not download image for {0}, bacause of exception {1}", 
                            artistMusicbrainzID, e));
                    }
                }
                return false;

            } else {
                Hyena.Log.Debug ("Fanart: dbtrack info is null in FanartQueryJob");
            }
            return false;
        }

        bool SaveArtistImage (string uri, string artistMusicbrainzID) {
            var filename = FanartArtistImageSpec.CreateArtistImageFileName (artistMusicbrainzID);

            if (SaveHttpStream (new Uri (uri), FanartArtistImageSpec.GetPath (filename), null)) {
                // TODO: add code here
                return true;
            }
            return false;
            /*
            string artwork_id = Track.ArtworkId;

            if (SaveHttpStreamCover (new Uri (uri), artwork_id, null)) {
                Log.Debug ("Downloaded cover art", artwork_id);
                StreamTag tag = new StreamTag ();
                tag.Name = CommonTags.AlbumCoverId;
                tag.Value = artwork_id;

                AddTag (tag);
                return true;
            }
            */

        }

        private void SaveDbImageData (string artistMusicbrainzID, bool downloaded)
        {
            ServiceManager.DbConnection.Execute (
                "INSERT OR REPLACE INTO ArtistImageDownloads (MusicBrainzID, Downloaded, LastAttempt) VALUES (?, ?, ?)",
                artistMusicbrainzID, downloaded, DateTime.Now);
        }

        private void SaveDbMusicBrainz (string artistName, string artistMBDI) {
            ServiceManager.DbConnection.Execute (
                "INSERT OR REPLACE INTO ArtistMusicBrainz (ArtistName, MusicBrainzId, LastAttempt) VALUES (?, ?, ?)",
                artistName, artistMBDI, DateTime.Now);
        }
    }
}

