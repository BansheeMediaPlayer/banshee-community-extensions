//
// MissingMostOfMetadataSource.fs
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
open System.Linq
open System.Text.RegularExpressions
open System.Collections.Generic

open Hyena
open Hyena.Data.Sqlite

open Banshee.ServiceStack

open Mono.Unix;

type MissingMostOfMetadataSource() = 
    inherit MissingFromAcoustIDSource("missing-most-of-metadata-online-fix")
    let job = AcoustIDFingerprintJob.Instance
    do
        base.Name <- Catalog.GetString ("Missing Most of Metadata Fix");
        base.Description <- Catalog.GetString ("Displayed are tracks loaded in Banshee without most of metadata");

    override this.IdentifyCore () =
        "DELETE FROM CoreArtists WHERE ArtistID NOT IN (SELECT DISTINCT(ArtistID) FROM CoreTracks)"
        |> ServiceManager.DbConnection.Execute 
        |> ignore
        "DELETE FROM CoreAlbums WHERE AlbumID NOT IN (SELECT DISTINCT(AlbumID) FROM CoreTracks)"
        |> ServiceManager.DbConnection.Execute 
        |> ignore
        
        (@"IFNULL((SELECT Name from CoreArtists where ArtistID = CoreTracks.ArtistID), '') = '' AND
        IFNULL(Title, '') = '' AND
        IFNULL((SELECT Title from CoreAlbums where AlbumID = CoreTracks.AlbumID), '') = ''"
        |> this.GetFindMethod, this.Generation)
        |> ServiceManager.DbConnection.Execute |> ignore;
    
    override x.ProcessSolution (id, recordings) =
        let solutions = new HashSet<String> ()
        for recording in recordings do
            for release in recording.ReleaseGroups do
                solutions.Add (String.Join(", ", recording.Artists.Select(fun z -> z.Name)) + " - " + recording.Title + " - " + release.Title) |> ignore
        String.Join(";;", solutions)
        
    override x.ProcessProblem (problem) =
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
