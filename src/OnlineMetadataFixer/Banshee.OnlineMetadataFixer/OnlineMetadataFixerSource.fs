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
        do
            base.Id <- "empty-albums";
            base.Name <- Catalog.GetString ("Empty Album Name");
            base.Description <- Catalog.GetString ("Displayed are tracks with empty album's name.");       

        let find_cmd = new HyenaSqliteCommand (String.Format (@"
            INSERT INTO MetadataProblems (ProblemType, TypeOrder, Generation, SolutionOptions, ObjectIds)
                SELECT
                    'empty-albums', 1, ?,
                     Title || ',' || IFNULL((SELECT Name from CoreArtists where ArtistID = CoreTracks.ArtistID), ''),
                    AlbumID || ',' || TrackID
                FROM CoreTracks
                WHERE IFNULL((SELECT Title from CoreAlbums where AlbumID = CoreTracks.AlbumID), '') = ''
                    GROUP BY TrackID 
                    ORDER BY Title"));

    static member GetAlbumTitle (artist : String, title : String) : String =
        let url = String.Format("http://musicbrainz.org/ws/1/track/?type=xml&artist={0}&title={1}", artist, title);
        let mutable album_name = Unchecked.defaultof<String>;
        let reader = new XmlTextReader (url);
        while reader.Read() && album_name = Unchecked.defaultof<String> do 
            if reader.Name = "release" then
                reader.Read () |> ignore;
                while reader.Name <> "release" && album_name = Unchecked.defaultof<String> do
                    if reader.Name = "title" then
                        album_name <- reader.ReadString ();
            reader.Read();
        album_name;                
    
    override this.IdentifyCore () =              
        ServiceManager.DbConnection.Execute ("DELETE FROM CoreAlbums WHERE AlbumID NOT IN (SELECT DISTINCT(AlbumID) FROM CoreTracks)") |> ignore;
        ServiceManager.DbConnection.Execute (find_cmd, this.Generation) |> ignore;

    override this.Fix (problems) =
        for problem in problems do
            let track_info = problem.SolutionOptions. [0].Split(',');
            ServiceManager.DbConnection.Execute (@"UPDATE CoreAlbums SET Title = ? WHERE AlbumID = ?;",
               OnlineMetadataFixerSource.GetAlbumTitle(track_info. [1], track_info. [0]), problem.ObjectIds. [0]) |> ignore;
            
