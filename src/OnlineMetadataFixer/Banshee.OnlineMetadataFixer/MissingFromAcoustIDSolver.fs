//
// MissingFromAcoustIDSolver.fs
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
open Mono.Unix

open Hyena
open Hyena.Data.Sqlite

open Banshee.ServiceStack

[<AbstractClass>]
type MissingFromAcoustIDSolver (problemId) as x = 
    inherit Banshee.Fixup.Solver ()
    let preferences = AcoustIDPreferences.Instance
    do
        base.Id <- problemId
        BinaryFunction.Add(base.Id, new Func<obj, obj, obj>(fun uri column -> x.GetSolutions (uri :?> string, column :?> string) :> obj))
                        
        let compute_fingerprints = new Banshee.Sources.PrimarySource.TrackEventHandler(
                                    fun s e ->
                                        try
                                            AcoustIDFingerprintJob.Instance.Start ()
                                        with :? System.ArgumentException -> () // if job already started
        )

        compute_fingerprints.Invoke (null, null)
        ServiceManager.SourceManager.MusicLibrary.add_TracksAdded (compute_fingerprints)
        ServiceManager.SourceManager.MusicLibrary.add_TracksChanged(
            fun s o ->
                try
                    AcoustIDSubmitJob.Instance.Start ()
                with :? System.ArgumentException -> () // if job already started
        )
        
        if not (ServiceManager.DbConnection.TableExists ("AcoustIDSubmissions")) then
            ServiceManager.DbConnection.Execute (@"
                CREATE TABLE AcoustIDSubmissions (
                    TrackID INTEGER UNIQUE,
                    Timestamp INTEGER NOT NULL
                )") |> ignore

    abstract member ProcessSolution: string * seq<Recording> -> string
    abstract member ProcessProblem: Banshee.Fixup.Problem -> unit

    member private x.GetSolutions (uri : string, column : string) : String = 
        try
            let id, list = AcoustIDReader.ReadFingerPrint (uri)
            x.ProcessSolution (column, list)
        with GstreamerError (ex) as e -> 
            Hyena.Log.Error (String.Format("Cannot read {0} fingerprint..", uri), e)
            ""

    override x.HasTrackDetails with get () = true
    
    override x.PreferencesSection with get () = preferences.Section

    member x.GetFindMethod (condition : string, ?columns : string) =
        new HyenaSqliteCommand (String.Format (@"
            INSERT INTO MetadataProblems (ProblemType, TypeOrder, Generation, SolutionOptions, ObjectIds, TrackDetails)
                SELECT
                    '{0}', 1, ?,
                     IFNULL(HYENA_BINARY_FUNCTION ('{0}', uri, {2}), '') as solutions,
                     TrackID,
                     Uri
                FROM CoreTracks
                WHERE
                    ({1}) AND IFNULL(solutions, '') <> ''
                    GROUP BY TrackID
                    ORDER BY Uri DESC", base.Id, condition, defaultArg columns "NULL"));

    override x.Fix (problems) =
        for problem in problems do
            if not (String.IsNullOrEmpty (problem.SolutionValue)) then
                x.ProcessProblem (problem)

    override x.SetStatus (status_message, preferences_page_id) =
        status_message.FreezeNotify ();
        if AcoustIDKeysHelper.ReadAcoustIDKey () = String.Empty then
            status_message.AddAction (new Banshee.Sources.MessageAction (Catalog.GetString ("AcoustID Settings"), new EventHandler (fun s e -> 
                try
                    let dialog = new Banshee.Preferences.Gui.PreferenceDialog ()
                    dialog.ShowSourcePageId (preferences_page_id)
                    dialog.Run () |> ignore
                    dialog.Destroy ()
                with :? ApplicationException -> ()
            )))
        status_message.ThawNotify ();

