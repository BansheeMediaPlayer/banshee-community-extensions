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
namespace Banshee.Dap.Bluetooth.DapGlueApi

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
open Banshee.Hardware
open Banshee.Sources
open Banshee.ServiceStack
open DBus
open Hyena
open Hyena.Data.Sqlite
open Mono.Addins

type NodeType = | Folder | File

type RemoteNode = {
    path  : string list
    name  : string
    ntype : NodeType
    size  : uint64
    ctime : uint64
    atime : uint64
    mtime : uint64
    }

module Functions =
    let Singular x = AddinManager.CurrentLocalizer.GetString x
    let Plural x xs n = AddinManager.CurrentLocalizer.GetPluralString (x, xs, n)
    let inline IconOf< ^a when ^a : (member Icon : string)> (x: ^a) : string =
        let icon = (^a : (member Icon : string) (x))
        if Functions.IsNull icon then "bluetooth"
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
    let NodeTypeOf x = match x.ToString() with
                       | "file" -> File
                       | "folder" -> Folder
                       | _ -> failwith "Invalid Node Type: %s" x
    let RemoteNodeOf path (x: Map<string,_>) =
        let UInt64Of x = let v = ref 0UL
                         UInt64.TryParse(x.ToString(), v) |> ignore
                         !v
        let LookupOrDefaultOf tv key def =
            let v = Map.tryFind key x
            match v with
            | Some v -> tv v
            | None -> def
        let nm = x.["Name"].ToString()
        let nt = x.["Type"].ToString() |> NodeTypeOf
        let size = LookupOrDefaultOf UInt64Of "Size" 0UL
        let ctime = LookupOrDefaultOf UInt64Of "Created" 0UL
        let atime = LookupOrDefaultOf UInt64Of "Accessed" 0UL
        let mtime = LookupOrDefaultOf UInt64Of "Modified" 0UL
        { path = path;
          name = nm;
          ntype = nt;
          size = size;
          ctime = ctime;
          atime = atime;
          mtime = mtime }
    let Iterate (ftp: IFileTransfer) root =
        let ToDict (x: KeyValuePair<_,_>[]) = x :> seq<_> |> Functions.DictConv
        let ListFolder path =
            ftp.ListFolder ()
            |> Seq.map (fun afi -> ToDict afi |> RemoteNodeOf path)
        let rec InnerIterate root (path: string list) (acc: RemoteNode list) =
            ftp.ChangeFolder root
            let path = root::path
            let children =
              ListFolder path
              |> Seq.fold (fun state i ->
                let state = i::state
                match i.ntype with
                | File -> state
                | Folder -> InnerIterate i.name path state) []
            ftp.ChangeFolder ".."
            acc@children
        InnerIterate root [] []
    let FuzzyLookup (x: DatabaseTrackInfo) =
        let ArtistIdOf (x: string) =
            let faq = HyenaSqliteCommand("select ArtistID from CoreArtists where Name like ? limit 1")
            ServiceManager.DbConnection.Query<int>(faq, x)
        let AlbumIdOf (x: int) (y: string) =
            let faq = HyenaSqliteCommand("select AlbumID from CoreAlbums where ArtistID = ? and Title like ? limit 1")
            ServiceManager.DbConnection.Query<int>(faq, x, y)
        match (x.ArtistName, x.AlbumTitle) with
        | (null, null) | ("", "") -> x
        | _ ->
          try
            let art = ArtistIdOf x.ArtistName
            let alb = AlbumIdOf art x.AlbumTitle
            let faq = "PrimarySourceID = 1 and CoreTracks.ArtistID = ? " +
                      "and CoreTracks.AlbumID = ? and CoreTracks.TrackNumber = ? " +
                      "and CoreTracks.Title like ?"
            let dti = DatabaseTrackInfo.Provider.FetchFirstMatching(faq, art, alb, x.TrackNumber, x.TrackTitle)
            if Functions.IsNull dti then x
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

type BluetoothDevice(dev: IBansheeDevice) =
    let vpd = Functions.VendorProductDeviceOf dev
    let icon = Functions.IconOf dev
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
    interface IDevice with
        member x.Uuid = x.Uuid
        member x.Serial = x.Serial
        member x.Product = x.Product
        member x.Vendor = x.Vendor
        member x.Name = x.Name
        member x.MediaCapabilities = x.MediaCapabilities
        member x.ResolveRootUsbDevice () = x.ResolveRootUsbDevice ()
        member x.ResolveUsbPortInfo () = x.ResolveUsbPortInfo ()

type BluetoothSource(dev: BluetoothDevice, ftp: IFileTransfer) =
    inherit DapSource() with
    //override x.PurgeOnLoad = false
    override x.IsReadOnly = false
    override x.BytesUsed = 0L
    override x.BytesCapacity = Int64.MaxValue
    override x.Import () = ()
    override x.CanImport = false
    override x.GetIconNames () = [| dev.Icon |]
    override x.LoadFromDevice () =
        base.SetStatus (Functions.Singular "Loading Track Information...", false)
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
            x.Uri <- Functions.SafeUriOf fp
        let files = Functions.Iterate ftp dev.MediaCapabilities.AudioFolders.[0]
        files |> List.iteri (fun i f ->
            let txt = Functions.Singular "Processing Track {0} of {1}\u2026"
            x.SetStatus (String.Format (txt, i, files.Length), false)
            match (f.ntype, ToMimeType f.name) with
            | (File,m) when IsAudio m ->
                let dti = DatabaseTrackInfo(PrimarySource = x)
                fx f dti
                let look = Functions.FuzzyLookup dti
                DatabaseTrackInfo(look, Uri = dti.Uri, PrimarySource = x).Save(false)
            | (Folder,f) -> ()
            | _ -> ())
        base.OnTracksAdded ()
    override x.AddTrackToDevice (y, z) =
          let root = dev.MediaCapabilities.AudioFolders.[0]
          let fsart = FileNamePattern.Escape y.DisplayArtistName
          let fsalb = FileNamePattern.Escape y.DisplayAlbumTitle
          let fsttl = FileNamePattern.Escape y.DisplayTrackTitle
          let mt = ToMimeType y.MimeType
          let fn = sprintf "%02d. %s.%s" y.TrackNumber fsttl (ExtensionOf mt)
          let uri = Functions.SafeUriOf (fn::fsalb::fsart::root::[])

          printfn "cd: %s" root
          ftp.ChangeFolder root
          try
            printfn "cd: %s" fsart
            ftp.ChangeFolder fsart
          with
          | _ -> printfn "md: %s" fsart
                 ftp.CreateFolder fsart
          try
            printfn "cd: %s" fsalb
            ftp.ChangeFolder fsalb
          with
          | _ -> printfn "md: %s" fsalb
                 ftp.CreateFolder fsalb
          ftp.PutFile z.AbsolutePath fn |> ignore
          let dti = DatabaseTrackInfo(y, PrimarySource = x, Uri = uri)
          dti.Save(false)
          printfn "cd: ../../../"
          ftp.ChangeFolder ".."
          ftp.ChangeFolder ".."
          ftp.ChangeFolder ".."
    override x.DeleteTrack y =
        let rec del path =
            match path with
            | fn::[] -> printfn "rm: %s" fn
                        ftp.Delete fn
                        true
            | dir::rest -> printfn "cd: %s" dir
                           ftp.ChangeFolder dir
                           let rv = del rest
                           printfn "cd: .."
                           ftp.ChangeFolder ".."
                           rv
            | [] -> false
        let uepath = Uri(y.Uri.AbsoluteUri).AbsolutePath |> Uri.UnescapeDataString
        printfn "DeleteTrack: %s" uepath
        uepath.Split("/".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)
        |> List.ofArray |> del
    //member private x.OnTrackExists y = base.OnTrackExists y
    do base.DeviceInitialize dev
       base.Initialize ()
       base.TrackEqualHandler <- fun dti ti -> dti.Uri = ti.Uri
