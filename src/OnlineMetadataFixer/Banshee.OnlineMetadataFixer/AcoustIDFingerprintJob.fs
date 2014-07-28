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

[<AllowNullLiteral>]
type AcoustIDFingerprintJob private () as this = class
    inherit DbIteratorJob (Catalog.GetString ("Computing Fingerprints"))
    static let mutable instance : AcoustIDFingerprintJob = null
    let bin_func_checker = "acoustid-fingerprint-checker"
    let bin_func_fingpr_exist = "acoustid-fingerprint-exists"
    do
        base.SetResources (Resource.Database)
        base.PriorityHints <- PriorityHints.LongRunning;

        base.IsBackground <- true;
        base.CanCancel <- true;
        base.DelayShow <- true;

        base.CountCommand <- new HyenaSqliteCommand (String.Format (@"
            SELECT count(DISTINCT TrackID) FROM CoreTracks 
            WHERE HYENA_BINARY_FUNCTION ('{0}', Uri, NULL) = 'ok'", bin_func_checker))
        base.SelectCommand <- new HyenaSqliteCommand (String.Format (@"
            SELECT 
                DISTINCT (Uri),
                (CASE
                    WHEN (IFNULL(CoreTracks.Title, '') = '' AND IFNULL(CoreArtists.Name, '') = '' AND IFNULL(CoreAlbums.Title, '') = '') THEN
                        0
                    WHEN (IFNULL(CoreTracks.Title, '') = '' OR IFNULL(CoreArtists.Name, '') = '') THEN
                        1
                    WHEN IFNULL(CoreAlbums.Title, '') = '' THEN
                        2
                    ELSE
                        3
                    END
                    ) as rank
            FROM CoreTracks
            JOIN CoreArtists ON CoreArtists.ArtistID = CoreTracks.ArtistID
            JOIN CoreAlbums ON  CoreAlbums.AlbumID = CoreTracks.AlbumID
            WHERE 
                HYENA_BINARY_FUNCTION ('{0}', Uri, NULL) = 'ok'
            ORDER BY rank ASC
            LIMIT 1
            ", bin_func_checker))

        try this.AddCheckerFunction () with :? ArgumentException -> () // if function was already added

    override this.IterateCore (reader : HyenaDataReader) =
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
        BinaryFunction.Add(bin_func_fingpr_exist, new Func<obj, obj, obj>(fun uri b ->
            match uri :?> string |> AcoustIDStorage.FingerprintExists with
            | true -> "ok"
            | _ -> "no"
            :> obj
            ))

    member this.Start () =
        base.Register ()
        instance.Finished.AddHandler (fun s e -> 
            instance <- null
            ServiceManager.DbConnection.Execute (String.Format (@"
                INSERT OR IGNORE INTO AcoustIDSubmissions (TrackID, Timestamp)
                    SELECT TrackID, 0
                    FROM CoreTracks
                    WHERE HYENA_BINARY_FUNCTION ('{0}', Uri, NULL) = 'ok'
                    ", bin_func_fingpr_exist)) |> ignore
            AcoustIDSubmitJob.Instance.Start ()
        )

    static member Instance with get() = 
                            if obj.ReferenceEquals (instance, Unchecked.defaultof<_>) then
                                instance <- new AcoustIDFingerprintJob ()
                            instance
end
