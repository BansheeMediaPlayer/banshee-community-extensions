//
// InvalidMetadataSource.fs
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

open System
open System.Collections.Generic
open System.Linq

open Hyena
open Hyena.Data.Sqlite

open Banshee.ServiceStack

open Mono.Unix

type InvalidMetadataSource() = 
    inherit MissingFromAcoustIDSource("invalid-metadata-online-fix")
    let job = AcoustIDFingerprintJob.Instance
    do
        base.Name <- Catalog.GetString ("Invalid Metadata Fix");
        base.Description <- Catalog.GetString ("Displayed are tracks with invalid(different than AcoustID) metadata");
        
        BinaryFunction.Add("can-be-fixed", new Func<obj, obj, obj>(fun a b -> InvalidMetadataSource.CanBeFixed (a :?> string, b) :> obj))

    static member private CanBeFixed (url : String, b : obj) : String =
        match url |> AcoustIDStorage.FingerprintMayExists with
        | true -> "true"
        | _ -> "false"

    override this.IdentifyCore () =
        (
            this.GetFindMethod (
                ("IFNULL(HYENA_BINARY_FUNCTION ('{0}', uri, NULL), 'false') = 'true'", "can-be-fixed")
                |> String.Format,
                @"Title || ' - ' 
                || (SELECT Title FROM CoreAlbums  WHERE AlbumID = CoreTracks.AlbumID) || ' - ' 
                || (SELECT Name FROM CoreArtists  WHERE ArtistID = CoreTracks.ArtistID)"
            ),
            this.Generation
        )
        |> ServiceManager.DbConnection.Execute |> ignore;
    
    override x.ProcessSolution (column, recordings) =
        let solutions = new HashSet<String> ()
        if not (column |> String.IsNullOrEmpty) then
            solutions.Add (column) |> ignore
        for recording in recordings do
            for release in recording.ReleaseGroups do
                solutions.Add (String.Join(", ", recording.Artists.Select(fun z -> z.Name)) + " - " + recording.Title + " - " + release.Title) |> ignore
        String.Join(";;", solutions)
        
    override x.ProcessProblem (problem) =
        ()
