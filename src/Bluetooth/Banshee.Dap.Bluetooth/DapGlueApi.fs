//
// DapGlueApi.fs
//
// Author:
//   Nicholas J. Little <arealityfarbetween@googlemail.com>
//
// Copyright (c) 2014 Nicholas J. Little
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
module Banshee.Dap.Bluetooth.DapGlueApi

open System
open System.IO
open System.Globalization
open System.Collections
open System.Collections.Generic
open System.Text.RegularExpressions

open Banshee.Base
open Banshee.Collection.Database
open Banshee.Dap
open Banshee.Dap.Bluetooth
open Banshee.Dap.Bluetooth.AuxillaryApi
open Banshee.Dap.Bluetooth.Mime
open Banshee.Dap.Bluetooth.Mime.Extensions
open Banshee.Dap.Bluetooth.ObexApi
open Banshee.Dap.Bluetooth.SupportApi
open Banshee.Dap.Bluetooth.Wrappers
open Banshee.Dap.Bluetooth.Ftp
open Banshee.Dap.Bluetooth.Client
open Banshee.Hardware
open Banshee.Sources
open Banshee.ServiceStack
open DBus
open Hyena
open Hyena.Log
open Hyena.Data.Sqlite
open Mono.Addins

let Singular x = AddinManager.CurrentLocalizer.GetString x
let Plural x xs n = AddinManager.CurrentLocalizer.GetPluralString (x, xs, n)
let inline IconOf< ^a when ^a : (member Icon : string)> (x: ^a) : string =
    let icon = (^a : (member Icon : string) (x))
    if IsNull icon then "bluetooth"
    else icon
let inline VendorProductDeviceOf< ^t when ^t : (member Modalias : string)> (x: ^t) =
    let parse x = Int16.Parse(x, NumberStyles.HexNumber)
    let def = (-1s, -1s, -1s)
    try
      let alias = (^t : (member Modalias : string) (x))
      let vpd = alias.Remove(0, alias.LastIndexOf(":") + 1)
      let parts = vpd.Split("vpd".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)
      match parts.Length with
      | 3 -> let v = parse parts.[0]
             let p = parse parts.[1]
             let d = parse parts.[2]
             (v, p, d)
      | _ -> def
    with
    | _ -> def
let SafeUriOf x = SafeUri(Uri("bt:///" + String.Join("/", List.rev x)))
let Iterate (ftp: ICrawler) root =
    let ListFolder path = ftp.List()
    let rec InnerIterate root (path: string list) (acc: RemoteNode list) =
        if ftp.Down root then
          let path = root::path
          let children =
            ListFolder path
            |> Seq.fold (fun state i ->
              let state = i::state
              match i.ntype with
              | File -> state
              | Folder -> InnerIterate i.name path state) []
          ftp.Up()
          acc@children
        else acc
    InnerIterate root [] []
let dbifill (node: RemoteNode) (dbi: DatabaseTrackInfo) =
    let uri = node.name::node.path |> SafeUriOf
    let path = uri.AbsoluteUri |> Uri.UnescapeDataString
    Debugf "Evaluating: %A" path
    let dn = Path.GetDirectoryName path
    let fn = Path.GetFileNameWithoutExtension path
    let ex = Path.GetExtension path
    dbi.FileSize <- Convert.ToInt64 node.size
    dbi.Uri <- uri
    dbi.MimeType <- ToMimeType ex |> ToString
    match dn with
    | Match "^.*/[^/]+/([^/]+) - ([^/]+)$" [full;artist;album] ->
        Debugf "Match 1.1: '%s' by '%s'" album artist
        dbi.ArtistName <- artist
        dbi.AlbumTitle <- album
    | Match "^.*/[^/]+/([^/]+)/([^/]+)$" [full;artist;album] ->
        Debugf "Match 1.2: '%s' by '%s'" album artist
        dbi.ArtistName <- artist
        dbi.AlbumTitle <- album
    | _ -> ()
    match fn with
    | Match "^(\d+)\.\s*([^-]+)$" [full;no;title] ->
        Debugf "Match 2.1: %A" [full;no;title]
        dbi.TrackNumber <- Int32.Parse no
        dbi.TrackTitle <- title
    | Match "^(\d+) - ([^-]+)$" [full;no;title] ->
        Debugf "Match 2.2: %A" [full;no;title]
        dbi.TrackNumber <- Int32.Parse no
        dbi.TrackTitle <- title
    | Match "^([^-]+) - ([^-]+) - (\d+) - ([^-]+)$" [full;artist;album;no;title] ->
        Debugf "Match 2.3: %A" [full;artist;album;no;title]
        dbi.ArtistName <- artist
        dbi.AlbumTitle <- album
        dbi.TrackNumber <- Int32.Parse no
        dbi.TrackTitle <- title
    | Match "^([^-]+) - (\d+) - ([^-]+)$" [full;artist;no;title] ->
        Debugf "Match 2.4: %A" [full;artist;no;title]
        dbi.ArtistName <- artist
        dbi.TrackNumber <- Int32.Parse no
        dbi.TrackTitle <- title
    | _ ->
        Debugf "Default: %A" uri
        dbi.TrackTitle <- fn
let FuzzyLookup (x: DatabaseTrackInfo) =
    let TrackIdOf (i: int) (x: string) (sz: int64) =
        let faq = HyenaSqliteCommand("select TrackID from CoreTracks where " +
                                     "FileSize = ? and " +
                                     "TrackNumber = ? and Title like ? limit 1")
        ServiceManager.DbConnection.Query<int64>(faq, sz, i, "%" + x + "%")
    let ArtistIdOf (x: string) =
        let faq = HyenaSqliteCommand("select ArtistID from CoreArtists where " +
                                     "Name like ? limit 1")
        ServiceManager.DbConnection.Query<int>(faq, "%" + x + "%")
    let AlbumIdOf (x: int) (y: string) =
        let faq = HyenaSqliteCommand("select AlbumID from CoreAlbums where " +
                                     "ArtistID = ? and Title like ? limit 1")
        ServiceManager.DbConnection.Query<int>(faq, x, "%" + y + "%")
    try
      let tid = TrackIdOf x.TrackNumber x.TrackTitle x.FileSize
      if 0L <> tid then
        Debugf "Got Lucky: %d => %d. %s at %d bytes"
          tid x.TrackNumber x.TrackTitle x.FileSize
        DatabaseTrackInfo.Provider.FetchSingle tid
      else
        match (x.ArtistName, x.AlbumTitle) with
        | (null, null) | ("", "") ->
          Warnf "No Help 1: %A" x
          x
        | _ ->
          let art = ArtistIdOf x.ArtistName
          let alb = AlbumIdOf art x.AlbumTitle
          let num = x.TrackNumber
          let ttl = "%" + x.TrackTitle + "%"
          let faq = "PrimarySourceID = 1 and CoreTracks.ArtistID = ? " +
                    "and CoreTracks.AlbumID = ? and CoreTracks.TrackNumber = ? " +
                    "and CoreTracks.Title like ?"
          let dti = DatabaseTrackInfo.Provider.FetchFirstMatching(faq, art, alb, num, ttl)
          if IsNull dti then
            Warnf "No Help 2: %A" x
            x
          else if dti.FileSize <> x.FileSize then
            Warnf "Excepting FileSize, Looks Like %A (%d vs %d)" dti dti.FileSize x.FileSize
            x
          else dti
    with
    | _ -> x

type BluetoothCapabilities() =
    interface IDeviceMediaCapabilities with
        member x.AudioFolders = [| Singular "Music" |]
        member x.VideoFolders = [||]
        member x.CoverArtFileName = ""
        member x.CoverArtFileType = ""
        member x.CoverArtSize = 0
        member x.FolderDepth = 2
        member x.FolderSeparator = '/'
        member x.IsType y = "bluetooth" = y
        member x.PlaybackMimeTypes = [| for m in [ Mp3; Ogg ] do yield ToString m |]
        member x.PlaylistFormats = [||]
        member x.PlaylistPaths = [||]

type BluetoothDevice(dev: DeviceApi.IDevice) =
    let vpd = VendorProductDeviceOf dev
    let icon = IconOf dev
    member x.Icon = icon
    member x.Uuid = dev.Modalias
    member x.Serial = dev.Address
    member x.Product = let _,p,_ = vpd
                       sprintf "0x%04X" p
    member x.Vendor = let v,_,_ = vpd
                      sprintf "0x%04X" v
    member x.Name = dev.Alias
    member x.MediaCapabilities = BluetoothCapabilities() :> IDeviceMediaCapabilities
    member x.ResolveRootUsbDevice () = null
    member x.ResolveUsbPortInfo () = null
    member x.Device = dev
    interface IDevice with
        member x.Uuid = x.Uuid
        member x.Serial = x.Serial
        member x.Product = x.Product
        member x.Vendor = x.Vendor
        member x.Name = x.Name
        member x.MediaCapabilities = x.MediaCapabilities
        member x.ResolveRootUsbDevice () = x.ResolveRootUsbDevice ()
        member x.ResolveUsbPortInfo () = x.ResolveUsbPortInfo ()

type LoadCache() =
    let mutable cache = Unchecked.defaultof<_>
    let confirmed = HashSet<int64>()
    member x.Start y =
        cache <- CachedList<DatabaseTrackInfo>.CreateFromSourceModel y
        Debugf "LoadCache: cached %d tracks" cache.Count
    member x.Confirm y = confirmed.Add y
    member x.Done () =
        cache.Count - confirmed.Count
        |> Debugf "LoadCache: confirmed %d, removing %d tracks" confirmed.Count
        confirmed
        |> Seq.map (fun id -> DatabaseTrackInfo(CacheEntryId = id))
        |> cache.Remove
        cache
    member x.Dispose () =
        if IsNull cache |> not then
            cache.Dispose ()
            confirmed.Clear ()
            cache <- Unchecked.defaultof<_>
    interface IDisposable with
        override x.Dispose () = x.Dispose ()

type BluetoothSource(dev: BluetoothDevice, cm: ClientManager) =
    inherit DapSource()
    let load = new LoadCache()
    let ftp = Crawler(dev.Device.Address, cm)
    let ue = Event<_>()
    do base.SupportsVideo <- false
       base.SupportsPlaylists <- false
    member x.Ejected = ue.Publish
    override x.IsReadOnly = false
    override x.BytesUsed = 0L
    override x.BytesCapacity = Int64.MaxValue
    override x.Import () = ()
    override x.CanImport = false
    override x.GetIconNames () = [| dev.Icon |]
    override x.Eject () =
        base.Eject ()
        ftp.Drop ()
        ue.Trigger()
    member x.RemoveTrackList (y: CachedList<_>) =
        ServiceManager.DbConnection.Execute (
            x.remove_list_command,
            DateTime.Now,
            y.CacheId,
            y.CacheId
        ) |> ignore
    override x.PreLoad () =
        load.Start x.DatabaseTrackModel
    override x.PostLoad () =
        load.Done () |> x.RemoveTrackList
        load.Dispose ()
        base.PostLoad ()
    override x.DeviceInitialize dev =
        base.DeviceInitialize dev
        if ftp.Init () |> not then
          failwith "Crawler Initialisation Failed."
        x.Initialize ()
    member x.SequentialLoad () =
        x.PreLoad ()
        x.LoadFromDevice ()
        x.PostLoad ()
    override x.LoadFromDevice () =
        base.SetStatus (Singular "Loading Track Information...", false)
        let files = Iterate ftp dev.MediaCapabilities.AudioFolders.[0]
        files |> List.iteri (fun i f ->
            let txt = Singular "Processing Track {0} of {1}\u2026"
            x.SetStatus (String.Format (txt, i, files.Length), false)
            let tid = f.name::f.path |> SafeUriOf |> DatabaseTrackInfo.GetTrackIdForUri
            if 0L <> tid then
              load.Confirm tid |> ignore
            else
              match (f.ntype, ToMimeType f.name) with
              | (File,m) when IsAudio m ->
                  let dti = DatabaseTrackInfo(PrimarySource = x)
                  dbifill f dti
                  let look = FuzzyLookup dti
                  Debugf "BluetoothSource: Looks like '%A'" look
                  DatabaseTrackInfo(look, Uri = dti.Uri, PrimarySource = x).Save(false)
              | (Folder,f) -> ()
              | _ -> ())
        base.OnTracksAdded ()
    override x.AddTrackToDevice (y, z) =
        let root = dev.MediaCapabilities.AudioFolders.[0]
        let fsart = FileNamePattern.Escape y.DisplayAlbumArtistName
        let fsalb = FileNamePattern.Escape y.DisplayAlbumTitle
        let fsttl = FileNamePattern.Escape y.DisplayTrackTitle
        let mt = ToMimeType y.MimeType
        let fn = sprintf "%02d. %s.%s" y.TrackNumber fsttl (ExtensionOf mt)
        let uri = SafeUriOf (fn::fsalb::fsart::root::[])

        ftp.Path <- fsalb::fsart::root::[]
        ftp.PutFile z.AbsolutePath fn |> ignore
        ftp.Up() // FIXME: It's not intended to call this here, but it's
               // handy since it blocks until the transfer is complete
        let dti = DatabaseTrackInfo(y, PrimarySource = x, Uri = uri)
        dti.Save(false)
    override x.DeleteTrack y =
        let rec del path =
            match path with
            | fn::[] -> ftp.Delete fn
                        true
            | dir::rest -> if ftp.Down dir then
                             let rv = del rest
                             if ftp.List() |> Seq.isEmpty then
                               ftp.Up()
                               ftp.Delete dir
                             else ftp.Up()
                             rv
                           else false
            | [] -> false
        ftp.Root()
        let uepath = Uri(y.Uri.AbsoluteUri).AbsolutePath |> Uri.UnescapeDataString
        Infof "Dap.Bluetooth: DeleteTrack: %s" uepath
        uepath.Split("/".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)
        |> List.ofArray |> del
