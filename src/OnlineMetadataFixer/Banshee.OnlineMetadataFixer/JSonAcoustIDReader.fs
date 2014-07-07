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

type JSonAcoustIDReader (url : string) = class
    // Workaround a FSharp.Data bug. See here: https://github.com/fsharp/FSharp.Data/issues/642
    let webClient = new System.Net.WebClient ()
    let jsonProvider = AcoustIDJsonProvider.Parse (webClient.DownloadString (url))

    member x.GetInfo () =
        match jsonProvider.Status with
        | "ok" -> x.GetBestInfo ()
        | _ -> (String.Empty, Seq.empty)

    member private x.GetBestInfo () = 
        jsonProvider.Results 
        |> Seq.fold (fun a o -> 
        (
            o.Score |> Convert.ToDouble,
            JSonAcoustIDReader.ReadRecordings (o),
            o.Id
        ) :: a) []
        |> Seq.maxBy (fun (score, recordings, id) -> score)
        |> fun (s, r, i) -> (i, r)

    static member private ReadRecordings (result) = 
        seq {
            for recording in result.Recordings do
                yield {
                    ID = recording.Id; 
                    Title = recording.Title; 
                    Artists = recording.Artists |> JSonAcoustIDReader.ReadArtists
                    ReleaseGroups = recording.Releasegroups |>Seq.fold (
                                        fun a o -> 
                                        { 
                                            ID = o.Id; 
                                            Title = o.Title; 
                                            GroupType = if o.Type.IsNone then "" else o.Type.Value; 
                                            SecondaryTypes = o.Secondarytypes; 
                                            Artists = JSonAcoustIDReader.ReadArtists (o.Artists)
                                        } :: a) []
                }
            }

    static member private ReadArtists art_list =
        art_list |> Seq.fold (fun a o -> {ID = o.Id; Name = o.Name} :: a) []
end