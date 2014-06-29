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
open DBus
open Hyena

module Functions =
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
    let Iterate (ftp: IFileTransfer) root filter =
        let ToDict (x: KeyValuePair<_,_>[]) = x :> seq<_> |> Functions.DictConv
        let rec InnerIterate root (acc: string list list) (file: string list) =
            ftp.ChangeFolder root
            ftp.ListFolder()
            |> Seq.fold (fun state afi -> let fi = ToDict afi
                                          let tp = fi.["Type"].ToString()
                                          let nm = fi.["Name"].ToString()
                                          if "folder" = tp then
                                            let f = InnerIterate nm state (nm::file)
                                            ftp.ChangeFolder ".."
                                            f
                                          else if filter nm then (nm::file |> List.rev)::state
                                          else state) acc
        printfn "Iterate: From Root = '%s'" root
        let files = InnerIterate root [] []
        files |> List.rev

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
                       Convert.ToString(p)
    member x.Vendor = let v,_,_ = vpd
                      Convert.ToString(v)
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
    override x.IsReadOnly = true
    override x.BytesUsed = 0L
    override x.BytesCapacity = 0L
    override x.Import () = ()
    override x.CanImport = false
    override x.GetIconNames () = [| dev.Icon |]
    override x.LoadFromDevice () =
        base.SetStatus ("Loading Track Information...", false)
        let nne (f: string) (x: DatabaseTrackInfo) =
            let fn = Path.GetFileNameWithoutExtension f
            let ex = Path.GetExtension f
            let m = Regex.Match(fn, "^(\d+)\.\s(.*)$")
            if m.Success then
              x.TrackNumber <- Int32.Parse(m.Groups.[1].Value)
              x.TrackTitle <- m.Groups.[2].Value
            x.MimeType <- ToMimeType ex |> ToString
        let fx f (x: DatabaseTrackInfo) =
            match f with
            | ar::al::fn::[] -> x.AlbumArtist <- ar
                                x.AlbumTitle <- al
                                nne fn x
            | fn::[] -> nne fn x
            | _ -> ()
            x.Uri <- new SafeUri("file:///" + String.Join("/", f))
        Functions.Iterate ftp dev.MediaCapabilities.AudioFolders.[0] (fun x -> ToMimeType x |> IsAudio)
        |> Seq.iter (fun f -> printfn "%A" f
                              let dti = DatabaseTrackInfo(PrimarySource = x)
                              fx f dti
                              dti.Save())
        base.OnTracksAdded ()
    override x.AddTrackToDevice (y, z) = ()
    override x.DeleteTrack y = false
    do base.DeviceInitialize dev
       base.Initialize ()
       base.TrackEqualHandler <- fun dti ti -> ti.Uri.AbsolutePath.EndsWith(dti.Uri.AbsolutePath)
