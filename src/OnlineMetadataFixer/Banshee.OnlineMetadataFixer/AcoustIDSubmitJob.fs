//
// AcoustIDSubmitJob.fs
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
type AcoustIDSubmitJob private () as this = class
    inherit DbIteratorJob (Catalog.GetString ("Submitting metadata to AcoustID service"))
    static let mutable instance : AcoustIDSubmitJob = null
    let bin_func_checker = "acoustid-submission-checker"
    do
        base.SetResources (Resource.Database)
        base.PriorityHints <- PriorityHints.LongRunning;

        base.IsBackground <- true;
        base.CanCancel <- true;
        base.DelayShow <- true;

        base.CountCommand <- new HyenaSqliteCommand (@"
            SELECT count(DISTINCT CoreTracks.TrackID) FROM CoreTracks
            JOIN AcoustIDSubmissions ON AcoustIDSubmissions.TrackID = CoreTracks.TrackID
            WHERE 
                AcoustIDSubmissions.Timestamp < CoreTracks.DateUpdatedStamp")
        base.SelectCommand <- new HyenaSqliteCommand (String.Format (@"
            {0}
            WHERE
                AcoustIDSubmissions.Timestamp < CoreTracks.DateUpdatedStamp
            LIMIT 1", AcoustIDSender.SelectCommand))
            
        try this.AddCheckerFunction () with :? ArgumentException -> () // if function was already added

    member private this.AddCheckerFunction () =
        BinaryFunction.Add(bin_func_checker, new Func<obj, obj, obj>(fun uri b ->
            match uri :?> string |> AcoustIDStorage.FingerprintExists with
            | true -> "ok"
            | _ -> "no"
            :> obj
            ))

    override this.IterateCore (reader : HyenaDataReader) =
        base.Status <- reader.Get<string> (0)
        AcoustIDReader.ReadFingerPrint (reader.Get<string> (0)) |> ignore // to be sure that fingerprint was computed
        try
            Hyena.Log.Debug (String.Format ("Trying to send metadata of {0} file to an AcoustID service", reader.Get<string> (0)))
            AcoustIDSender.Send (
                reader.Get<string> (0),
                reader.Get<int> (1),
                reader.Get<int> (2),
                reader.Get<string> (3),
                reader.Get<string> (4),
                reader.Get<string> (5),
                reader.Get<string> (6),
                reader.Get<int> (7),
                reader.Get<int> (8),
                reader.Get<int> (9)
            ) |> ignore
        with :? System.ArgumentException as ex -> // in case of invalid obligatory fields
            Hyena.Log.DebugException (ex)
        ServiceManager.DbConnection.Execute (
            new HyenaSqliteCommand ("UPDATE AcoustIDSubmissions SET Timestamp = ? WHERE TrackID = ?"),
            DateTime.Now, reader.Get<int> (10)) |> ignore

    override this.OnCancelled () =
        base.AbortThread ()

    member this.Start () =
        base.Register ()
        instance.Finished.AddHandler (fun s e -> instance <- null)

    static member Instance with get() = 
                            if obj.ReferenceEquals (instance, Unchecked.defaultof<_>) then
                                instance <- new AcoustIDSubmitJob ()
                            instance
end
