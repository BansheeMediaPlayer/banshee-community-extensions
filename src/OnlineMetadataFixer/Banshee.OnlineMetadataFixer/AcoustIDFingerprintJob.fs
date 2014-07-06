//
// AcoustIDFingerprintJob.fs
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

open Banshee.ServiceStack

open Hyena.Data.Sqlite
open Hyena.Jobs

open Mono.Unix

type AcoustIDFingerprintJob private () as this = class
    inherit DbIteratorJob (Catalog.GetString ("Computing Fingerprints"))
    static let instance = AcoustIDFingerprintJob () // todo try with lazy()
    let bin_func_checker = "acoustid-fingerprint-checker"
    let bin_func_rank = "acoustid-fingerprint-rank"
    do
        base.SetResources (Resource.Database)
        base.PriorityHints <- PriorityHints.LongRunning;

        base.IsBackground <- true;
        base.CanCancel <- true;
        base.DelayShow <- true;
        printfn "utworzono obiekt"
        base.CountCommand <- new HyenaSqliteCommand (String.Format (@"
            SELECT count(DISTINCT TrackID) FROM CoreTracks 
            WHERE HYENA_BINARY_FUNCTION ('{0}', Uri, NULL) = 'ok'", bin_func_checker))
        base.SelectCommand <- new HyenaSqliteCommand (String.Format (@"
            SELECT 
                DISTINCT (Uri)
            FROM CoreTracks 
            WHERE HYENA_BINARY_FUNCTION ('{0}', uri, NULL) = 'ok'
            ", bin_func_checker, bin_func_rank))
            
        try this.AddCheckerFunction () with :? ArgumentException -> () // if function was already added
        try this.AddRankFunction () with :? ArgumentException -> () // if function was already added

    override this.IterateCore (reader : HyenaDataReader) =
        printfn "iteruje core"
        base.Status <- reader.Get<string> (0)
        AcoustIDReader.ReadFingerPrint (reader.Get<string> (0)) |> ignore

    override this.OnCancelled () =
        base.AbortThread ()

    member private this.AddCheckerFunction () =
        BinaryFunction.Add(bin_func_checker, new Func<obj, obj, obj>(fun uri b ->
            match not (uri 
                :?> string
                |> AcoustIDStorage.FingerprintExists) && (uri
                :?> string
                |> AcoustIDStorage.FingerprintMayExists) with
            | true -> "ok"
            | _ -> "no"
            :> obj
            ))

    member private this.AddRankFunction () =
        BinaryFunction.Add(bin_func_rank, new Func<obj, obj, obj>(fun id b -> 
            let track_id = id |> Convert.ToInt32
            //SELECT tracks.*, artists.name, albums.name FROM tracks  join artists on artists.id = tracks.artist join albums on  albums.id=tracks.album where tracks.id = 2
            let reader = ServiceManager.DbConnection.Query (String.Format (@"
                SELECT 
                    CoreTracks.Title, CoreArtists.Name, CoreAlbums.Name, CoreTracks.Url
                FROM CoreTracks
                JOIN CoreArtists on CoreArtists.ArtistID = CoreTracks.ArtistID
                JOIN CoreAlbums on CoreAlbums.AlbumID = CoreTracks.AlbumID
                WHERE CoreTracks.TrackID = {0}", id |> Convert.ToInt32));
            if reader.Read () then
                let empty_title = String.IsNullOrEmpty(reader.Get<string> (0))
                let empty_artist = String.IsNullOrEmpty(reader.Get<string> (1))
                let empty_album = String.IsNullOrEmpty(reader.Get<string> (2))
                let res = 
                    if empty_title && empty_artist && empty_album then
                        "0"
                    else if empty_title || empty_artist then
                        "1"
                    else if empty_album then
                        "2"
                    else
                        "3"
                Console.WriteLine ("Url: {0}, rank: {1}", reader.Get<string>(3), res)
                res :> obj
            else
                "3" :> obj
            ))

    member this.Start () =
        printfn "startuje"
        base.Register ()
        
    static member Instance with get() = instance
end
