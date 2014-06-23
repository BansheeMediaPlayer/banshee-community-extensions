open System
open System.Collections.Generic
open Gst
open FSharp.Data

type ReleaseGroup =  {
    ID : string;
    Title : string;
    GroupType : string;
    SecondaryTypes : List<string>;
}

type Artist = {
    ID : string;
    Name : string;
}

type Recording = {
    ID : string;
    Title : string;
    Artists : List<Artist>;
    ReleaseGroups : List<ReleaseGroup>;
}

type AcoustIDJsonProvider = FSharp.Data.JsonProvider<"AcoustIDTrackInfo.json">

type JSonAcoustIDReader (url : string, completion_handler) = class
    let jsonProvider = AcoustIDJsonProvider.Load (url)
    let mutable currentID = String.Empty
    let mutable recordings = new List<Recording> ()
    let mutable completionHandler = completion_handler
    
    member x.ReadID () =
        currentID <- String.Empty
        
        if jsonProvider.Status |> x.CheckStatus then
            completionHandler (currentID, recordings)
        else
            x.FindBestID ()
            completionHandler (currentID, recordings)

    member x.CheckStatus = function
        | "true" -> true
        | _ -> false

    member x.FindBestID () = 
        let mutable currentScore = 0.0m
        
        for result in jsonProvider.Results do
            if result.Score > currentScore then
                currentID <- result.Id
                currentScore <- result.Score
                x.ReadRecordings (result)

    member x.ReadRecordings (result) = 
        recordings <- new List<Recording> ()
        for recording in result.Recordings do
            {
                ID = recording.Id; 
                Title = recording.Title; 
                Artists = x.ReadArtists (recording.Artists); 
                ReleaseGroups = x.ReadReleaseGroups (recording.Releasegroups)
            } |> recordings.Add 
    
    member x.ReadArtists art_list =
        let artists = new List<Artist> ()
        for artist in art_list do
            {ID = artist.Id; Name = artist.Name} |>  artists.Add
        artists
        
    member x.ReadReleaseGroups releasegroup_list =
        let releaseGroups = new List<ReleaseGroup> ()
        for releasegroup in releasegroup_list do
            {ID = releasegroup.Id; Title = releasegroup.Title; GroupType = releasegroup.Type; SecondaryTypes = x.ReadSecondaryTypes (releasegroup)} |>  releaseGroups.Add // todo secondarytype, artists
        releaseGroups
        
    member x.ReadSecondaryTypes releasegroup = 
        let list = new List<string> ()
        for secType in releasegroup.Secondarytypes do
            secType |> list.Add
        list
end

type AcoustIDReader(key : string) = class
    let mutable filename = ""
    let mutable completionHandler = fun (id, list) -> ()
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
            failwith "Cannot create pipeline!"
            
        
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
            let url = String.Format (
                "http://api.acoustid.org/v2/lookup?meta=recordings+releasegroups&format=json&client={0}&duration={1}&fingerprint={2}",
                key, duration, fingerprint)
            let reader = new JSonAcoustIDReader (url, completionHandler)
            reader.ReadID ()
end

[<EntryPoint>]
let main(args) =
    Gst.Application.Init ()
    let loop = new GLib.MainLoop ()
    let reader = new AcoustIDReader ("TP95csTg")
    reader.GetID (args.[0], fun (id, list) ->
        Console.WriteLine ("Track ID: " + id)
        for record in list do
            Console.WriteLine ("=========================")
            Console.WriteLine ("Recording ID: " + record.ID)
            Console.WriteLine ("Title: " + record.Title);
            Console.WriteLine ("Artists: ")
            for artist in record.Artists do
                Console.WriteLine ("\t * {0} (ID: {1})", artist.Name, artist.ID)
            Console.WriteLine("Release Groups: ")
            for release_group in record.ReleaseGroups do
                let mutable sec_types = ""
                if release_group.SecondaryTypes.Count = 0 then
                    sec_types <- "no secondary types";
                else
                    for t in release_group.SecondaryTypes do
                        sec_types <- sec_types + t + ", "
                    sec_types <- sec_types.Remove (sec_types.Length - 2)
                Console.WriteLine ("\t * {0} (Type: {1} /Secondary types: {3}/, ID: {2})", release_group.Title, release_group.GroupType, release_group.ID, sec_types))

    loop.Run ()
    0
    