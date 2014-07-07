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

exception GstreamerError of string

type AcoustIDReader() = class
    static let acoustIDKey = "TP95csTg"
    static let timeout = uint64 Constants.SECOND * 10UL

    static member private BuildPipeline (filename) =
        if not Gst.Application.IsInitialized then 
            Gst.Application.Init ()

        let pipeline = new Pipeline ()
        let src = ElementFactory.Make ("filesrc", "source")
        let decoder = ElementFactory.Make ("decodebin", "decoder")
        let chromaPrint = ElementFactory.Make ("chromaprint", "processor")
        let sink = ElementFactory.Make ("fakesink", "sink")
        
        let elements = [src; decoder; chromaPrint; sink]
        
        (* todo why is it not working?
        if elements |> Seq.exists (fun x->x = null) then
            raise (GstreamerError ("Cannot create GStreamer elements"))
        Workaround :*)
        for element in elements do
            if element = null then
                raise (GstreamerError ("Cannot create GStreamer elements"))
            
        sink. ["sync"] <- 0
        src. ["location"] <- filename
        for e in elements do
            if not (e |> pipeline.Add) then
                raise (GstreamerError ("Cannot add element"))
        decoder |> src.Link |> AcoustIDReader.CheckLink
        sink |> chromaPrint.Link |> AcoustIDReader.CheckLink
        decoder.PadAdded.Add (fun args -> "sink" |> chromaPrint.GetStaticPad |> args.NewPad.Link = PadLinkReturn.Ok |> AcoustIDReader.CheckLink)
        
        pipeline, chromaPrint

    static member private CheckLink (is_ok) =
        if not is_ok then
            raise (GstreamerError ("Cannot link elements"))
    
    static member private LoadFingerprintFromGst (filename) =
        let pipeline, chromaPrint = AcoustIDReader.BuildPipeline (filename)
        
        if State.Playing |> pipeline.SetState = StateChangeReturn.Failure then
            raise (GstreamerError ("Cannot start pipeline"))
        
        let duration = ref -1L
        let state = ref State.Playing
        let pending = ref State.Playing
        let eos = pipeline.Bus.TimedPopFiltered(timeout, MessageType.Eos)
        if eos <> null && pipeline.QueryDuration (Format.Time, duration) then
            AcoustIDStorage.SaveFingerprint (chromaPrint. ["fingerprint"], filename, duration.Value)
            (duration.Value, Some (string chromaPrint. ["fingerprint"]))
        else
            (duration.Value, None)

    static member ReadFingerPrint (uri : string) =
        let su = new Hyena.SafeUri (uri)
        match su.IsFile with
        | true ->
            let filename = su.AbsolutePath
            Hyena.Log.DebugFormat ("Looking for {0} fingerprint", filename :> obj)
            let (dur, fileFP) = AcoustIDStorage.LoadFingerprint (filename)
            let (duration, fingerprint) = 
                if fileFP.IsNone then 
                    AcoustIDReader.LoadFingerprintFromGst (filename)
                else 
                    (dur, fileFP)
            
            if fingerprint.IsSome then
                let url = String.Format ("http://api.acoustid.org/v2/lookup?meta=recordings+releasegroups&format=json&client={0}&duration={1}&fingerprint={2}", acoustIDKey, duration / int64 Constants.SECOND, fingerprint.Value)
                let reader = new JSonAcoustIDReader (url)
                reader.GetInfo ()
            else
                (String.Empty, Seq.empty)
        | _ -> 
            Hyena.Log.WarningFormat ("Cannot read {0} fingerprint. Element is not a local file.", uri)
            (String.Empty, Seq.empty)
end
