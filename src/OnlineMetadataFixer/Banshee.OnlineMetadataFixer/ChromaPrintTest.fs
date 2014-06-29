module  Banshee.OnlineMetadataFixerTest

open System


[<EntryPoint>]
let main(args) =
    Gst.Application.Init ()
    let loop = new GLib.MainLoop ()
    let id, list = Banshee.OnlineMetadataFixer.AcoustIDReader.ReadFingerPrint (args.[0])
    Console.WriteLine ("Track ID: {0}", id)
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
                sec_types <- String.Join (", ", release_group.SecondaryTypes.ToArray())
            let mutable arts = ""
            if release_group.Artists.Count = 0 then
                arts <- "no artists types";
            else
                let preetyList = seq {for art in release_group.Artists do yield String.Format("\n\t\t\t * Name: {0} (ID: {1})", art.Name, art.ID)}
                arts <- String.Join (", ", preetyList)
            Console.WriteLine ("\t * {0} (\n\t\t * Type: {1} /Secondary types: {3}/\n\t\t * Artists: {4}\n\t\t * ID: {2})", release_group.Title, release_group.GroupType, release_group.ID, sec_types, arts)

    loop.Run ()
    0
