//
// ArtistImageJob.cs
//
// Author:
//   James Willcox <snorp@novell.com>
//   Gabriel Burt <gburt@novell.com>
//   Tomasz Maczyński <tmtimon@gmail.com>
//
// Copyright (C) 2005-2008 Novell, Inc.
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
using Banshee.ServiceStack;
using Hyena.Data.Sqlite;
using Hyena;
using Hyena.Jobs;

namespace Banshee.Fanart
{
    public class ArtistImageJob : DbIteratorJob
    {
        private DateTime last_scan = DateTime.MinValue;
        private TimeSpan retry_every = TimeSpan.FromDays (7);

        public ArtistImageJob (DateTime lastScan)  : base ("Downloading Artists' Images")
        {
            last_scan = lastScan;

            // Since we do last_scan - retry_every, avoid out-of-range error by ensuring
            // the last_scan date isn't already MinValue
            if (last_scan == DateTime.MinValue) {
                last_scan = DateTime.Now - TimeSpan.FromDays (365*50);
            }

            CountCommand = new HyenaSqliteCommand (
                @"SELECT count(DISTINCT CoreArtists.ArtistID)
                    FROM CoreTracks, CoreArtists
                    WHERE
                        CoreTracks.PrimarySourceID = ? AND
                        CoreTracks.DateUpdatedStamp > ? AND
                        CoreTracks.ArtistID = CoreArtists.ArtistID AND
                        CoreTracks.ArtistID NOT IN (
                            SELECT ArtistImageDownloads.ArtistID FROM ArtistImageDownloads WHERE
                                LastAttempt > ? OR Downloaded = 1)",
               ServiceManager.SourceManager.MusicLibrary.DbId, last_scan, last_scan - retry_every
            );


            SelectCommand = new HyenaSqliteCommand (String.Format (
                @"SELECT DISTINCT CoreArtists.ArtistID, CoreArtists.Name, {0}, CoreTracks.TrackID
                    FROM CoreTracks, CoreArtists
                    WHERE
                        CoreTracks.PrimarySourceID = ? AND
                        CoreTracks.DateUpdatedStamp > ? AND
                        CoreTracks.ArtistID = CoreArtists.ArtistID AND
                        CoreTracks.ArtistID NOT IN (
                            SELECT ArtistImageDownloads.ArtistID FROM ArtistImageDownloads WHERE
                                LastAttempt > ? OR Downloaded = 1)
                    GROUP BY CoreTracks.ArtistID ORDER BY CoreTracks.DateUpdatedStamp DESC LIMIT ?",
                    Banshee.Query.BansheeQuery.UriField.Column),
                    ServiceManager.SourceManager.MusicLibrary.DbId, last_scan, last_scan - retry_every, 1
            );



            SetResources (Resource.Database);
            PriorityHints = PriorityHints.LongRunning;

            IsBackground = true;
            CanCancel = true;
            DelayShow = true;
        }

        public void Start ()
        {
            Register ();
        }

        #region implemented abstract members of DbIteratorJob

        protected override void IterateCore (HyenaDataReader reader)
        {
            Log.Debug ("ArtistImageJob.IterateCore method is called ");

            // throw new NotImplementedException ();
        }

        #endregion
    }
}

