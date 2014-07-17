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
        printfn "Got Lucky: %d => %d. %s at %d bytes"
          tid x.TrackNumber x.TrackTitle x.FileSize
        DatabaseTrackInfo.Provider.FetchSingle tid
      else
        match (x.ArtistName, x.AlbumTitle) with
        | (null, null) | ("", "") ->
          printfn "No Help: %d. %s at %d bytes"
            x.TrackNumber x.TrackTitle x.FileSize
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
            printfn "No Help: by %s from %s - %d. %s at %d bytes"
              x.ArtistName x.AlbumTitle x.TrackNumber x.TrackTitle x.FileSize
            x
          else dti
    with
    | _ -> x

type BluetoothCapabilities() =
    interface IDeviceMediaCapabilities with
        member x.AudioFolders = [| "Music" |]
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

type BluetoothSource(dev: BluetoothDevice, cm: ClientManager) =
    inherit DapSource()
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
    override x.DeviceInitialize dev =
        base.DeviceInitialize dev
        if ftp.Init () |> not then
          failwith "Crawler Initialisation Failed."
        x.Initialize ()
    override x.LoadFromDevice () =
        base.SetStatus (Singular "Loading Track Information...", false)
        let nne (f: string) (x: DatabaseTrackInfo) =
            let fn = Path.GetFileNameWithoutExtension f
            let ex = Path.GetExtension f
            let m = Regex.Match(fn, "^(\d+)\.\s(.*)$")
            if m.Success then
              x.TrackNumber <- Int32.Parse(m.Groups.[1].Value)
              x.TrackTitle <- m.Groups.[2].Value
            else
              x.TrackTitle <- fn
            x.MimeType <- ToMimeType ex |> ToString
        let fx (rn: RemoteNode) (x: DatabaseTrackInfo) =
            let fp = rn.name::rn.path
            match fp with
            | fn::al::ar::root::[] -> x.ArtistName <- ar
                                      x.AlbumTitle <- al
                                      nne fn x
            | fn::root::[] -> nne fn x
            | _ -> failwith "Case Not Possible: %A" rn
            x.FileSize <- Convert.ToInt64 rn.size
            x.Uri <- SafeUriOf fp
        let files = Iterate ftp dev.MediaCapabilities.AudioFolders.[0]
        files |> List.iteri (fun i f ->
            let txt = Singular "Processing Track {0} of {1}\u2026"
            x.SetStatus (String.Format (txt, i, files.Length), false)
            match (f.ntype, ToMimeType f.name) with
            | (File,m) when IsAudio m ->
                let dti = DatabaseTrackInfo(PrimarySource = x)
                fx f dti
                let look = FuzzyLookup dti
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
        printfn "DeleteTrack: %s" uepath
        uepath.Split("/".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)
        |> List.ofArray |> del
