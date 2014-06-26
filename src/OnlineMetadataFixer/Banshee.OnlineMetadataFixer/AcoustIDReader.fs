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

type AcoustIDReader(key : string) = class
    let mutable filename = ""
    let mutable completionHandler = fun (id : string, list : List<Recording>) -> ()
    let mutable pipeline = null
    let mutable duration = -1L
    let mutable fingerprint = ""
    
    member x.Key = key
    
    member x.GetID (file, completion_handler) = 
        filename <- file
        completionHandler <- completion_handler
        x.StartPipeline ()
    
    member x.StartPipeline () =
        pipeline <- new Pipeline ()
        let src = ElementFactory.Make ("filesrc", "source")
        let decoder = ElementFactory.Make ("decodebin", "decoder")
        let chromaPrint = ElementFactory.Make ("chromaprint", "processor")
        let sink = ElementFactory.Make ("fakesink", "sink")
        
        pipeline.Bus.AddSignalWatch ()
        x.MsgHandler |> pipeline.Bus.Message.Add
        
        let elements = [src; decoder; chromaPrint; sink];
        
        (* todo 
        if elements |> List.tryFind (fun x -> x = null) <> None then
            pipeline <- null
            failwith "Cannot create pipeline!" *)
            
        
        sink. ["sync"] <- 0
        src. ["location"] <- filename
        for e in elements do
            e |> x.TryAdd
            
        decoder |> src.Link |> x.CheckLink
        sink |> chromaPrint.Link |> x.CheckLink
        
        decoder.PadAdded.Add (fun args -> "sink" |> chromaPrint.GetStaticPad |> args.NewPad.Link = PadLinkReturn.Ok |> x.CheckLink)
        
        if State.Playing |> pipeline.SetState = StateChangeReturn.Failure then
            failwith "Cannot start pipeline"

    member x.TryAdd (element) =
        if not (element |> pipeline.Add) then
            failwith "Cannot add element!"
    
    member x.CheckLink (is_ok) =
        if not is_ok then
            failwith "Cannot link elements!"
            
    member x.MsgHandler args =
        match args.Message.Type with
        | MessageType.DurationChanged -> 
            let ok, dur =  pipeline.QueryDuration (Format.Time)
            if ok then
                duration <- dur / int64 Gst.Constants.SECOND
                x.ReadID()
        | MessageType.Eos -> () // todo finish
        | MessageType.Tag -> 
            let tags = args.Message.ParseTag ()
            let ok, fingpr = "chromaprint-fingerprint" |> tags.GetString
            
            if (ok && fingpr <> null) then
                fingerprint <- fingpr
                x.ReadID()
        | _ -> ()
                
    member x.ReadID () =
        if (duration = -1L || String.IsNullOrEmpty (fingerprint)) then
            ()
            // todo: timeout or sth?
        else
            let url = String.Format ("http://api.acoustid.org/v2/lookup?meta=recordings+releasegroups&format=json&client={0}&duration={1}&fingerprint={2}", key, duration, fingerprint)
            let reader = new JSonAcoustIDReader (url, completionHandler)
            reader.ReadID ()
end
