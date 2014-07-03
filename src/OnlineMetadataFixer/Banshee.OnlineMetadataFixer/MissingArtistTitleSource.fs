//
// MissingArtistTitleSource.fs
//
// Author:
//   Marcin Kolny <marcin.kolny@gmail.com>
//
// Copyright (c) 2014 Marcin Kolny
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

namespace Banshee.OnlineMetadataFixer

open System;
open System.Linq;
open System.Text
open System.Text.RegularExpressions
open System.Collections.Generic;

open Hyena
open Hyena.Data.Sqlite

open Banshee.ServiceStack

open Mono.Unix;

type MissingArtistTitleSource () = 
    inherit Banshee.Fixup.Solver()
    let mutable job = new AcoustIDFingerprintJob ()
    let fixId = "missing-artist-online-fix"
    let findCmd = new HyenaSqliteCommand (String.Format (@"
        INSERT INTO MetadataProblems (ProblemType, TypeOrder, Generation, SolutionOptions, ObjectIds, TrackInfo)
            SELECT
                '{0}', 1, ?,
                 IFNULL(HYENA_BINARY_FUNCTION ('{0}', uri, NULL), '') as solutions,
                 TrackID,
                 Uri
            FROM CoreTracks
            WHERE
                (IFNULL((SELECT Name from CoreArtists where ArtistID = CoreTracks.ArtistID), '') = '' OR
                IFNULL(Title, '') = '') AND IFNULL(solutions, '') <> ''
                GROUP BY TrackID
                ORDER BY Uri DESC", fixId));
    do
        base.Id <- fixId
        base.Name <- Catalog.GetString ("Missing Artist and Titles Fix");
        base.Description <- Catalog.GetString ("Displayed are tracks loaded in Banshee");

        BinaryFunction.Add(base.Id, new Func<obj, obj, obj>(fun uri b -> MissingArtistTitleSource.GetSolutions (uri :?> string, b) :> obj))
        ServiceManager.SourceManager.add_SourceAdded (
            fun e -> 
                job <- new AcoustIDFingerprintJob ()
(*                job.add_Finished (
                    fun e -> 
                        job <- null
                        ())*)
                job.Start ()
                ()
            )

    static member private GetSolutions (uri : string, b : obj) : String = 
        try
            let id, list = AcoustIDReader.ReadFingerPrint (uri)
            let solutions = new StringBuilder ()
            for recording in list do
                solutions.Append (String.Join(", ", recording.Artists.Select(fun z -> z.Name))) |> ignore
                solutions.Append (" - ") |> ignore
                solutions.Append (recording.Title) |> ignore
                solutions.Append (";;") |> ignore
            match solutions.Length > 2 with
            | true -> solutions.ToString (0, solutions.Length - 2)
            | _ -> solutions.ToString ()
        with GstreamerError (ex) -> 
            Hyena.Log.WarningFormat ("Cannot read {0} fingerprint. Internal error: {1}.", uri, ex)
            ""

    override this.HasTrackInfo () = 
        true

    override this.IdentifyCore () =
        ServiceManager.DbConnection.Execute ("DELETE FROM CoreAlbums WHERE AlbumID NOT IN (SELECT DISTINCT(AlbumID) FROM CoreTracks)") |> ignore;
        ServiceManager.DbConnection.Execute (findCmd, this.Generation) |> ignore;
        ()

    override this.Fix (problems) =
        for problem in problems do
            if not (String.IsNullOrEmpty (problem.SolutionValue)) then
                let trackMetadata = Regex.Split (problem.SolutionValue, " - ")
                ServiceManager.DbConnection.Execute (@"UPDATE CoreTracks SET Title = ? WHERE TrackID = ?;", trackMetadata. [1], problem.ObjectIds. [0]) |> ignore
                
                let artistId = ServiceManager.DbConnection.Query<int>(@"SELECT ArtistID from CoreArtists where Name = ?", trackMetadata. [0])
                let newId = 
                    if artistId = 0 then
                        ServiceManager.DbConnection.Execute (@"INSERT INTO CoreArtists (Name) VALUES (?)", trackMetadata. [0]) |> ignore
                        ServiceManager.DbConnection.Query<int>(@"SELECT ArtistID from CoreArtists where Name = ?", trackMetadata. [0])
                    else
                        artistId
                ServiceManager.DbConnection.Execute (@"UPDATE CoreTracks SET ArtistID = ? WHERE TrackID = ?;",
                   newId, problem.ObjectIds. [0]) |> ignore;
