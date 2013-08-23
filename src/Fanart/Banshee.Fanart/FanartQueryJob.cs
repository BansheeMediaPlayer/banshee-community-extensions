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
            } else if (!InternetConnected) {
                return false;
            }
            */

            DatabaseTrackInfo dbtrack;
            dbtrack = Track as DatabaseTrackInfo;
            /*
            Release release;

            // If we have the MBID of the album, we can do a direct MusicBrainz lookup
            if (dbtrack != null && dbtrack.AlbumMusicBrainzId != null) {

                release = Release.Get (dbtrack.AlbumMusicBrainzId);
                if (!String.IsNullOrEmpty (release.GetAsin ()) && SaveCover (String.Format (AmazonUriFormat, release.GetAsin ()))) {
                    return true;
                }

                // Otherwise we do a MusicBrainz search
            } else {
                ReleaseQueryParameters parameters = new ReleaseQueryParameters ();
                parameters.Title = Track.AlbumTitle;
                parameters.Artist = Track.AlbumArtist;
                if (dbtrack != null) {
                    parameters.TrackCount = dbtrack.TrackCount;
                }

                Query<Release> query = Release.Query (parameters);
                release = query.PerfectMatch ();

                foreach (Release r in query.Best ()) {
                    if (!String.IsNullOrEmpty (r.GetAsin ()) && SaveCover (String.Format (AmazonUriFormat, r.GetAsin ())) ) {
                        return true;
                    }
                }
            }

            if (release == null) {
                return false;
            }
            */
            // No success with ASIN, let's try with other linked URLs
            // skipped

            return false;
        }

        private bool SaveCover (string uri) {
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
            return false;
        }

    }
}

