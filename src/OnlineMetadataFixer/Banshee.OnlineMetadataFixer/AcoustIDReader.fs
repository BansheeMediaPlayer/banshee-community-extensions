//
// AcoustIDReader.fs
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

open Gst

type AcoustIDReader() = class
    static let acoustIDKey = "TP95csTg"
    static let timeout = uint64 Constants.SECOND * 10UL
    
    static member private BuildPipeline (filename) =
        let pipeline = new Pipeline ()
        let src = ElementFactory.Make ("filesrc", "source")
        let decoder = ElementFactory.Make ("decodebin", "decoder")
        let chromaPrint = ElementFactory.Make ("chromaprint", "processor")
        let sink = ElementFactory.Make ("fakesink", "sink")
        
        let elements = [src; decoder; chromaPrint; sink];
        
        (* todo 
        if elements |> List.tryFind (fun x -> x = null) <> None then
            pipeline <- null
            failwith "Cannot create pipeline!" *)

        sink. ["sync"] <- 0
        src. ["location"] <- filename
        for e in elements do
            if not (e |> pipeline.Add) then
                failwith "Cannot add element!"
            
        decoder |> src.Link |> AcoustIDReader.CheckLink
        sink |> chromaPrint.Link |> AcoustIDReader.CheckLink
        decoder.PadAdded.Add (fun args -> "sink" |> chromaPrint.GetStaticPad |> args.NewPad.Link = PadLinkReturn.Ok |> AcoustIDReader.CheckLink)
        
        pipeline, chromaPrint

    static member private CheckLink (is_ok) =
        if not is_ok then
            failwith "Cannot link elements!"
    
    static member ReadFingerPrint (filename) =
        let pipeline, chromaPrint = AcoustIDReader.BuildPipeline (filename)
        
        if State.Playing |> pipeline.SetState = StateChangeReturn.Failure then
            failwith "Cannot start pipeline"
            
        let duration = ref -1L
        let state = ref State.Playing
        let pending = ref State.Playing
        let eos = pipeline.Bus.TimedPopFiltered(timeout, MessageType.Eos)
        if eos <> null && pipeline.QueryDuration (Format.Time, duration) then
            let url = String.Format ("http://api.acoustid.org/v2/lookup?meta=recordings+releasegroups&format=json&client={0}&duration={1}&fingerprint={2}", acoustIDKey, duration.Value / int64 Constants.SECOND, chromaPrint. ["fingerprint"])
            let reader = new JSonAcoustIDReader (url)
            reader.GetInfo ()
        else
            (String.Empty, new List<Recording> ())
end
