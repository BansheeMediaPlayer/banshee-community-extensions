//
// MissingAlbumSource.fs
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

open Hyena
open Hyena.Data.Sqlite

open Banshee.ServiceStack

open Mono.Unix;

type MissingAlbumSource () = 
    inherit MissingFromAcoustIDSource("missing-album-online-fix")
    do
        base.Name <- Catalog.GetString ("Missing Albums Fix");
        base.Description <- Catalog.GetString ("Displayed are tracks loaded in Banshee without album metadata");

    override x.IdentifyCore () =
        "DELETE FROM CoreAlbums WHERE AlbumID NOT IN (SELECT DISTINCT(AlbumID) FROM CoreTracks)"
        |> ServiceManager.DbConnection.Execute 
        |> ignore
        
        ("IFNULL((SELECT Title from CoreAlbums where AlbumID = CoreTracks.AlbumID), '') = ''"
        |> x.GetFindMethod, x.Generation)
        |> ServiceManager.DbConnection.Execute |> ignore;
    
    override x.ProcessSolution (id, recordings) =
        let groups = new HashSet<String> ()
        for recording in recordings do
            for releaseGroup in recording.ReleaseGroups do
                releaseGroup.Title |> groups.Add |> ignore
        String.Join(";;", groups)
        
    override x.ProcessProblem (problem) =
        let albumId = ServiceManager.DbConnection.Query<int>(@"SELECT AlbumID from CoreAlbums where Title = ?", problem.SolutionValue)
        let newId = 
            if albumId = 0 then
                ServiceManager.DbConnection.Execute (@"INSERT INTO CoreAlbums (Title) VALUES (?)", problem.SolutionValue) |> ignore
                ServiceManager.DbConnection.Query<int>(@"SELECT AlbumID from CoreAlbums where Title = ?", problem.SolutionValue)
            else
                albumId
        ServiceManager.DbConnection.Execute (@"UPDATE CoreTracks SET AlbumID = ? WHERE TrackID = ?;",
                   newId, problem.ObjectIds. [0]) |> ignore;
