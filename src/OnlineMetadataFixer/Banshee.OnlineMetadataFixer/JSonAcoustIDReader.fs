//
// JSonAcoustIDReader.fs
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

type AcoustIDJsonProvider = FSharp.Data.JsonProvider<"Resources/AcoustIDTrackInfo.json", EmbeddedResource="AcoustIDTrackInfo.json">

type JSonAcoustIDReader (url : string, completion_handler) = class
    // Workaround a FSharp.Data bug. See here: https://github.com/fsharp/FSharp.Data/issues/642
    let webClient = new System.Net.WebClient ()
    let jsonProvider = AcoustIDJsonProvider.Parse (webClient.DownloadString (url))
    let mutable currentID = String.Empty
    let mutable recordings = new List<Recording> ()
    let mutable completionHandler = completion_handler
    
    member x.ReadID () =
        currentID <- String.Empty
        
        if jsonProvider.Status |> x.CheckStatus then
            x.FindBestID ()
            completionHandler (currentID, recordings)
        else
            completionHandler (currentID, recordings)
        ()

    member x.CheckStatus = function
        | "ok" -> true
        | _ -> false

    member x.FindBestID () = 
        let mutable currentScore = 0.0

        for result in jsonProvider.Results do
            if Convert.ToDouble(result.Score) > currentScore then
                currentID <- result.Id
                currentScore <- result.Score |> Convert.ToDouble
                x.ReadRecordings (result)

    member x.ReadRecordings (result) = 
        recordings <- new List<Recording> ()
        for recording in result.Recordings do
            {
                ID = recording.Id; 
                Title = recording.Title; 
                Artists = x.ReadArtists (recording.Artists); 
                ReleaseGroups = x.ReadReleaseGroups (recording.Releasegroups)
            } |> recordings.Add 
    
    member x.ReadArtists art_list =
        let artists = new List<Artist> ()
        for artist in art_list do
            {ID = artist.Id; Name = artist.Name} |>  artists.Add
        artists
        
    member x.ReadReleaseGroups releasegroup_list =
        let releaseGroups = new List<ReleaseGroup> ()
        for releasegroup in releasegroup_list do
            {
                ID = releasegroup.Id;
                Title = releasegroup.Title;
                GroupType = releasegroup.Type;
                SecondaryTypes = x.ReadSecondaryTypes (releasegroup);
                Artists = x.ReadArtists (releasegroup.Artists)
            } |>  releaseGroups.Add // todo secondarytype, artists
        releaseGroups
        
    member x.ReadSecondaryTypes releasegroup = 
        let list = new List<string> ()
        for secType in releasegroup.Secondarytypes do
            secType |> list.Add
        list
end