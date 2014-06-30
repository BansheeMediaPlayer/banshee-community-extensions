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

type OnlineMetadataFixerSource () = 
    inherit Banshee.Fixup.Solver()
    let fixId = "empty-albums"
    let artistOrNothing = "IFNULL((SELECT Name from CoreArtists where ArtistID = CoreTracks.ArtistID), '')"
    let findCmd = new HyenaSqliteCommand (String.Format (@"
        INSERT INTO MetadataProblems (ProblemType, TypeOrder, Generation, SolutionValue, SolutionOptions, ObjectIds, TrackInfo)
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
                 AlbumID || ',' || TrackID,
                 Title || ',' || {0}
            FROM CoreTracks
            WHERE IFNULL((SELECT Title from CoreAlbums where AlbumID = CoreTracks.AlbumID), '') = '' AND albums <> ''
                GROUP BY TrackID 
                ORDER BY Title DESC", artistOrNothing, fixId));
    do
        base.Id <- fixId
        base.Name <- Catalog.GetString ("Empty Album Name")
        base.Description <- Catalog.GetString ("Displayed are tracks with empty album's name.")
    
        BinaryFunction.Add(base.Id, new Func<obj, obj, obj>(fun a b -> OnlineMetadataFixerSource.GetAlbumTitle (a :?> string, b :?> string) :> obj))

    static member GetAlbumTitle (title : String, artist : String) : String =
        match (title, artist) with
        | (title, artist) when String.IsNullOrEmpty(title) || String.IsNullOrEmpty(artist) -> ""
        | (_, _) -> 
            let url = String.Format("http://musicbrainz.org/ws/1/track/?type=xml&artist={0}&title={1}", artist, title);
            Hyena.Log.DebugFormat ("Looking for {0} - {1} metadata", artist, title)
            let s = new HashSet<string> ()
            try
                let reader = new XmlTextReader (url);
                while reader.Read() do
                    if reader.Name = "release" then
                        reader.Read() |> ignore
                        while reader.Name <> "release" do
                            if reader.Name = "title" then
                                s.Add (reader.ReadInnerXml()) |> ignore
                            reader.Read() |> ignore
                String.Join (";;", s)
            with
                | _ -> ""

    override x.HasTrackInfo () : bool = 
        true
    
    override this.IdentifyCore () =              
        ServiceManager.DbConnection.Execute ("DELETE FROM CoreAlbums WHERE AlbumID NOT IN (SELECT DISTINCT(AlbumID) FROM CoreTracks)") |> ignore;
        ServiceManager.DbConnection.Execute (findCmd, this.Generation) |> ignore;

    override this.Fix (problems) =
        for problem in problems do
            let track_info = problem.SolutionOptions. [0].Split(',');
            ServiceManager.DbConnection.Execute (@"UPDATE CoreAlbums SET Title = ? WHERE AlbumID = ?;",
               problem.SolutionValue, problem.ObjectIds. [0]) |> ignore;
            
