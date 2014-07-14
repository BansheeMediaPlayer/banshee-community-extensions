//
// OnlineMetadataFixerSource.fs 
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

namespace Banshee.OnlineMetadataFixer

open System;
open System.Linq;
open System.Xml;
open System.Collections.Generic;
open Mono.Unix;
open Hyena.Data.Sqlite;
open Banshee.ServiceStack;

type MusicBrainzTracksXmlProvider = FSharp.Data.XmlProvider<"Resources/MusicBrainzTracks.xml", EmbeddedResource="MusicBrainzTracks.xml">

type OnlineMetadataFixerSource () = 
    inherit Banshee.Fixup.Solver()
    do
        base.Id <- "empty-albums"
        base.Name <- Catalog.GetString ("Empty Album Name")
        base.Description <- Catalog.GetString ("Displayed are tracks with empty album's name.")
    
        BinaryFunction.Add(base.Id, new Func<obj, obj, obj>(fun a b -> OnlineMetadataFixerSource.GetAlbumTitle (a :?> string, b :?> string) :> obj))

    static member GetAlbumTitle (title : String, artist : String) : String =
        match (title, artist) with
        | (title, artist) when String.IsNullOrEmpty(title) || String.IsNullOrEmpty(artist) -> ""
        | (_, _) -> 
            let url = String.Format("http://musicbrainz.org/ws/1/track/?type=xml&artist={0}&title={1}", artist, title);
            Hyena.Log.DebugFormat ("Looking for {0} - {1} metadata", artist, title)
            try
                // Workaround a FSharp.Data bug. See here: https://github.com/fsharp/FSharp.Data/issues/642
                let webClient = new System.Net.WebClient ()
                let tracks = MusicBrainzTracksXmlProvider.Parse (webClient.DownloadString (url))
                (";;", seq {
                        for t in tracks.TrackList.Tracks do
                            for r in t.ReleaseList.Releases do yield r.Title
                    }
                    |> Set.ofSeq)
                |> String.Join
            with
            | :? System.Net.WebException as ex -> 
                Hyena.Log.Exception(ex)
                ""

    override x.HasTrackDetails with get () = true
    
    override this.IdentifyCore () =
        let artistOrNothing = "IFNULL((SELECT Name from CoreArtists where ArtistID = CoreTracks.ArtistID), '')"
        let findCmd = new HyenaSqliteCommand (String.Format (@"
            INSERT INTO MetadataProblems (ProblemType, TypeOrder, Generation, SolutionValue, SolutionOptions, ObjectIds, TrackDetails)
                SELECT
                    '{1}', 1, ?,
                     COALESCE (
                        NULLIF (
                            MIN(CASE (upper(Title) = Title AND NOT lower(Title) = Title)
                                WHEN 1 THEN '~~~'
                                ELSE Title END),
                            '~~~'),
                        Title) as val,
                     IFNULL(HYENA_BINARY_FUNCTION ('{1}', Title, {0}), '') as albums,                        
                     TrackID,
                     Title || ',' || {0}
                FROM CoreTracks
                WHERE IFNULL((SELECT Title from CoreAlbums where AlbumID = CoreTracks.AlbumID), '') = '' AND albums <> ''
                    GROUP BY TrackID 
                    ORDER BY Title DESC", artistOrNothing, base.Id))
        ServiceManager.DbConnection.Execute ("DELETE FROM CoreAlbums WHERE AlbumID NOT IN (SELECT DISTINCT(AlbumID) FROM CoreTracks)") |> ignore;
        ServiceManager.DbConnection.Execute (findCmd, this.Generation) |> ignore;

    override this.Fix (problems) =
        for problem in problems do
            let albumId = ServiceManager.DbConnection.Query<int>(@"SELECT AlbumID from CoreAlbums where Title = ?", problem.SolutionValue)
            let newId = 
                if albumId = 0 then
                    ServiceManager.DbConnection.Execute (@"INSERT INTO CoreAlbums (Title) VALUES (?)", problem.SolutionValue) |> ignore
                    ServiceManager.DbConnection.Query<int>(@"SELECT AlbumID from CoreAlbums where Title = ?", problem.SolutionValue)
                else
                    albumId
                
            ServiceManager.DbConnection.Execute (@"UPDATE CoreTracks SET AlbumID = ? WHERE TrackID = ?;",
               newId, problem.ObjectIds. [0]) |> ignore;
            
