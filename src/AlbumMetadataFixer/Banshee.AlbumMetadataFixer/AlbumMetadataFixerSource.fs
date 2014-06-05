//
// AlbumMetadataFixerSource.fs 
//
// Authors:
//   Marcin Kolny <marcin.kolny@gmail.com>
//
// Copyright (C) 2014 Marcin Kolny
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

namespace Banshee.AlbumMetadataFixer

open System;
open System.Linq;
open System.Collections.Generic;
open Mono.Unix;
open Hyena.Data.Sqlite;
open Banshee.ServiceStack;

type AlbumMetadataFixerSource () as this = 
    inherit Banshee.Fixup.Solver()
        do
            base.Id <- "empty-albums";
            base.Name <- Catalog.GetString ("Empty Album Name");
            base.Description <- Catalog.GetString ("Displayed are tracks with empty album's name.");       
    let new_album_name = "God's album"
    let find_cmd = new HyenaSqliteCommand (String.Format (@"
            INSERT INTO MetadataProblems (ProblemType, TypeOrder, Generation, SolutionOptions, ObjectIds)
                SELECT
                    'empty-albums', 1, ?,
                    Title || ',' || (SELECT Name from CoreArtists where ArtistID = CoreTracks.ArtistID),
                    AlbumID || ',' || TrackID
                FROM CoreTracks
                WHERE IFNULL((SELECT Title from CoreAlbums where AlbumID = CoreTracks.AlbumID), '') = ''
                    GROUP BY TrackID 
                    ORDER BY Title"));
    override this.IdentifyCore () = 
          ServiceManager.DbConnection.Execute ("DELETE FROM CoreAlbums WHERE AlbumID NOT IN (SELECT DISTINCT(AlbumID) FROM CoreTracks)");
          ServiceManager.DbConnection.Execute (find_cmd, this.Generation);
          let q = ServiceManager.DbConnection.QueryEnumerable<string> (new HyenaSqliteCommand (String.Format(@"SELECT
                    Title || ',' || (SELECT Name from CoreArtists where ArtistID = CoreTracks.ArtistID)
                FROM CoreTracks
                WHERE IFNULL((SELECT Title from CoreAlbums where AlbumID = CoreTracks.AlbumID), '') = ''
                    GROUP BY TrackID 
                    ORDER BY Title")));
                    
          for itt in q do
            printfn "%s" itt;
          printfn "xx"
    override this.Fix (problems) =
        for problem in problems do
            ServiceManager.DbConnection.Execute (
            @"UPDATE CoreAlbums SET Title = ? WHERE AlbumID = ?;",
            new_album_name, problem.ObjectIds. [0]);