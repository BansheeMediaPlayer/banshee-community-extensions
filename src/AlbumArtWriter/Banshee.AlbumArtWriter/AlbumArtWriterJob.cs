//
// AlbumArtWriterJob.cs
//
// Authors:
//   Kevin Anthony <Kevin@NosideRacing.com>
//
// Copyright (C) 2011 Kevin Anthony
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
using System.IO;

using Mono.Addins;

using Hyena;
using Hyena.Jobs;
using Hyena.Data.Sqlite;

using Banshee.Base;
using Banshee.Collection.Database;
using Banshee.ServiceStack;

namespace Banshee.AlbumArtWriter
{
    public class AlbumArtWriterJob : DbIteratorJob
    {
        private DateTime CurrentTime;
	private DateTime TimeOffset;
        public AlbumArtWriterJob () : base (AddinManager.CurrentLocalizer.GetString ("Saving Cover Art To Album folders"))
        {
            CurrentTime = DateTime.Now;
            TimeOffset = CurrentTime-TimeSpan.FromDays(7);
            CountCommand = new HyenaSqliteCommand (@"
                                    SELECT count(DISTINCT CoreTracks.AlbumID)
                                        FROM CoreTracks, CoreAlbums
                                    WHERE
                                        CoreTracks.PrimarySourceID = ? AND
                                        CoreTracks.AlbumID = CoreAlbums.AlbumID AND
                                        CoreTracks.AlbumID NOT IN (
                                            SELECT AlbumID from AlbumArtWriter WHERE
                                            SavedOrTried > 0 AND LastUpdated >= ?)
					",ServiceManager.SourceManager.MusicLibrary.DbId,TimeOffset
            );

            SelectCommand = new HyenaSqliteCommand (@"
                SELECT DISTINCT CoreAlbums.AlbumID, CoreAlbums.Title, CoreArtists.Name, CoreTracks.Uri, CoreTracks.TrackID
                    FROM CoreTracks, CoreArtists, CoreAlbums
                    WHERE
                        CoreTracks.PrimarySourceID = ? AND
                        CoreTracks.AlbumID = CoreAlbums.AlbumID AND
                        CoreAlbums.ArtistID = CoreArtists.ArtistID AND
                        CoreTracks.AlbumID NOT IN (
                            SELECT AlbumID from AlbumArtWriter WHERE
                            SavedOrTried > 0 AND LastUpdated >= ?)
                    GROUP BY CoreTracks.AlbumID ORDER BY CoreTracks.DateUpdatedStamp DESC LIMIT ?",
                ServiceManager.SourceManager.MusicLibrary.DbId, TimeOffset,1
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

        {
            public int DbId {
                private class CoverartTrackInfo : DatabaseTrackInfo
                set { TrackId = value; }
            }
        }

        protected override void IterateCore (HyenaDataReader reader)
        {
            var track = new CoverartTrackInfo () {
                AlbumTitle = reader.Get<string> (1),
                ArtistName = reader.Get<string> (2),
                PrimarySource = ServiceManager.SourceManager.MusicLibrary,
                Uri = new SafeUri (reader.Get<string> (3)),
                DbId = reader.Get<int> (4),
                AlbumId = reader.Get<int> (0)
            };

            Status = String.Format (AddinManager.CurrentLocalizer.GetString ("{0} - {1}"), track.ArtistName, track.AlbumTitle);
            WriteArt (track);
        }

        private void WriteArt (DatabaseTrackInfo track)
        {
            string ArtWorkPath = CoverArtSpec.GetPath (track.ArtworkId);
            string WritePath = Path.Combine (Path.GetDirectoryName (track.LocalPath), "album.jpg");

            if (File.Exists (ArtWorkPath) ) {
                if (!File.Exists (WritePath)) {
                    try {
                        File.Copy (ArtWorkPath, WritePath);
                        Log.DebugFormat ("Copying: {0} \t\t to: {1}", ArtWorkPath, WritePath);
                        ServiceManager.DbConnection.Execute (
                            "INSERT OR REPLACE INTO AlbumArtWriter (AlbumID, SavedOrTried,LastUpdated) VALUES (?, ?, ?)",
                            track.AlbumId, 3, CurrentTime);
                    } catch (IOException error) {
                        ServiceManager.DbConnection.Execute (
                            "INSERT OR REPLACE INTO AlbumArtWriter (AlbumID, SavedOrTried,LastUpdated) VALUES (?, ?, ?)",
                            track.AlbumId, 1, CurrentTime);
                        Log.Warning (error.Message);
                    }
                } else {
                    Log.DebugFormat ("Album already has artwork in folder {0}", WritePath);
                    ServiceManager.DbConnection.Execute (
                        "INSERT OR REPLACE INTO AlbumArtWriter (AlbumID, SavedOrTried,LastUpdated) VALUES (?, ?, ?)",
                        track.AlbumId, 2, CurrentTime);
                }
            } else {
                Log.DebugFormat ("Artwork does not exist for album {0} - {1} - {2}", track.AlbumArtist, track.AlbumTitle, ArtWorkPath);
                ServiceManager.DbConnection.Execute (
                    "INSERT OR REPLACE INTO AlbumArtWriter (AlbumID, SavedOrTried,LastUpdated) VALUES (?, ?, ?)",
                    track.AlbumId, 1, CurrentTime);
            }
        }

        protected override void OnCancelled ()
        {
            AbortThread ();
        }
    }
}
