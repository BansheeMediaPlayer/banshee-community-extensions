open System
open System.Collections.Generic;
open Gst;

type ReleaseGroup =  {
    ID : string;
    Title : string;
    Type : string;
    SecondaryType : string;
}

type Artist = {
    ID : string;
    Name : string;
}

type Recording = {
    ID : string;
    Title : string;
    Artist : List<Artist>;
    ReleaseGroups : List<ReleaseGroup>;
}

type AcoustIDReader(key : string) = class
    let mutable filename = ""
    let mutable completionHandler = fun () -> ()
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
        
        if elements |> List.tryFind (fun x -> x = null) <> None then
            pipeline <- null
            ()
            
        
        sink. ["sync"] <- 0
        src. ["location"] <- filename
        for e in elements do
            e |> x.TryAdd
            
        decoder |> src.Link |> x.CheckLink
        sink |> chromaPrint.Link |> x.CheckLink
        
        decoder.PadAdded.Add (fun args -> "sink" |> chromaPrint.GetStaticPad |> args.NewPad.Link = PadLinkReturn.Ok |> x.CheckLink)
        
        if State.Playing |> pipeline.SetState = StateChangeReturn.Failure then
            failwith "Cannot start pipeline"
        ()

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
                printfn "%s" fingerprint
                x.ReadID()
        | _ -> ()
                
    member x.ReadID () =
        if (fingerprint = null || duration = -1L) then
            // todo: timeout or sth?
            ()
        let url = String.Format (
            "http://api.acoustid.org/v2/lookup?meta=recordings+releasegroups&format=xml&client={0}&duration={1}&fingerprint={2}",
            key, duration, fingerprint)
        ()
        // let xmlReader = new XmlAcoustIDReader (url, completionHandler)
        // xmlReader.ReadID ();
end

[<EntryPoint>]
let main(args) =
    Gst.Application.Init ()
    let loop = new GLib.MainLoop ()
    let reader = new AcoustIDReader ("TP95csTg")
    reader.GetID (args.[0], fun () -> printfn "Hej");
    
    loop.Run ()
    0
    