//
// AlbumMetadataFixerSource.cs
//
// Authors:
//   Marcin Kolny <marcin.kolny@gmail.com>
//
// Copyright (C) 2014 Marcin Kolny
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
using System.Linq;
using System.Collections.Generic;
using Mono.Unix;
using Hyena.Data.Sqlite;
using Banshee.ServiceStack;
using Banshee.Fixup;

namespace Banshee.AlbumMetadataFixer
{
    public class AlbumMetadataFixerSource : Banshee.Fixup.Solver
    {
        private HyenaSqliteCommand find_cmd;
        private string new_album_name = "God's album";

        protected override void IdentifyCore ()
        {
            ServiceManager.DbConnection.Execute ("DELETE FROM CoreAlbums WHERE AlbumID NOT IN (SELECT DISTINCT(AlbumID) FROM CoreTracks)");
            ServiceManager.DbConnection.Execute (find_cmd, Generation);
        }

        public AlbumMetadataFixerSource ()
        {
            Id = "empty-albums";
            Name = Catalog.GetString ("Empty Album Name");
            Description = Catalog.GetString ("Displayed are tracks with empty album's name.");

            find_cmd = new HyenaSqliteCommand (String.Format (@"
            INSERT INTO MetadataProblems (ProblemType, TypeOrder, Generation, SolutionOptions, ObjectIds)
                SELECT
                    '{0}', 1, ?,
                    Title || ',' || (SELECT Name from CoreArtists where ArtistID = CoreTracks.ArtistID),
                    AlbumID || ',' || TrackID
                FROM CoreTracks
                WHERE IFNULL((SELECT Title from CoreAlbums where AlbumID = CoreTracks.AlbumID), '') = ''
                    GROUP BY TrackID 
                    ORDER BY Title", Id));

        }

        public override void Fix (IEnumerable<Problem> problems)
        {
            foreach (var problem in problems) {
                ServiceManager.DbConnection.Execute (
                    @"UPDATE CoreAlbums SET Title = ? WHERE AlbumID = ?;",
                    new_album_name, problem.ObjectIds [0]
                    );
            }
        }
    }
}