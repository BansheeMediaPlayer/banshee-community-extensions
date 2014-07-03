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

type AcoustIDFingerprintJob () = class
    inherit DbIteratorJob (Catalog.GetString ("Computing Fingerprints"))
    let binary_function = "acoustid-fingerprint-checker"
    do
        base.SetResources (Resource.Database)
        base.PriorityHints <- PriorityHints.LongRunning;

        base.IsBackground <- true;
        base.CanCancel <- true;
        base.DelayShow <- true;

        base.CountCommand <- new HyenaSqliteCommand (String.Format (@"
            SELECT count(DISTINCT TrackID) FROM CoreTracks 
            WHERE HYENA_BINARY_FUNCTION ('{0}', Uri, NULL) = 'ok'", binary_function))
        base.SelectCommand <- new HyenaSqliteCommand (String.Format (@"
            SELECT DISTINCT Uri FROM CoreTracks 
            WHERE HYENA_BINARY_FUNCTION ('{0}', uri, NULL) = 'ok'", binary_function))
            
        try
            BinaryFunction.Add(binary_function, new Func<obj, obj, obj>(fun uri b -> 
                let ok = not (uri 
                    :?> string
                    |> AcoustIDStorage.FingerprintExists) && (uri
                    :?> string
                    |> AcoustIDStorage.FingerprintMayExists)

                match ok with
                | true -> "ok"
                | _ -> "no"
                :> obj
                ))
        with :? ArgumentException -> () // if function was already added

    override this.IterateCore (reader : HyenaDataReader) =
        base.Status <- reader.Get<string> (0)
        AcoustIDReader.ReadFingerPrint (reader.Get<string> (0)) |> ignore

    override this.OnCancelled () =
        base.AbortThread ()
        
    member this.Start () =
        base.Register ()
end
