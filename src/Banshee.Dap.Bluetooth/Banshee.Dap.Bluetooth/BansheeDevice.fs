//
// BansheeDevice.fs
//
// Author:
//   Nicholas Little <arealityfarbetween@googlemail.com>
//
// Copyright (c) 2014 Nicholas Little
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
namespace Banshee.Dap.Bluetooth

open Banshee.Dap.Bluetooth.Adapters
open Banshee.Dap.Bluetooth.Client
open Banshee.Dap.Bluetooth.DapGlueApi
open Banshee.Dap.Bluetooth.Devices
open Banshee.Dap.Bluetooth.Wrappers
open Banshee.Dap.Bluetooth.SupportApi
open Banshee.ServiceStack
open DBus
open Hyena

type IBansheeDevice =
    abstract Device : INotifyDevice with get
    abstract MediaControl : INotifyMediaControl option with get
    abstract Support : Set<Feature> with get
    abstract HasSupport : Feature -> bool
    abstract IsConnected : Feature -> bool
    abstract Connect : Feature -> unit
    abstract Disconnect : Feature -> unit
    abstract Notify : IEvent<unit>

type BansheeDevice(path: ObjectPath,
                   am: AdapterManager,
                   dm: DeviceManager,
                   cm: ClientManager) =
    let notify = Event<_>()
    let dev = dm.Device path
    let dap = BluetoothDevice(dev)
    let mutable src : BluetoothSource = Unchecked.defaultof<_>
    let rec src_clean a =
        ThreadAssist.ProxyToMain(fun () ->
            match (IsNull src, a) with
            | (true, true) ->
                src <- new BluetoothSource(dap, cm)
                src.Ejected.Add(fun o -> src_clean false)
                src.DeviceInitialize (dap)
                src.LoadDeviceContents ()
                ServiceManager.SourceManager.AddSource(src)
                notify.Trigger()
            | (false, false) ->
                ServiceManager.SourceManager.RemoveSource(src)
                src.Unmap () |> ignore
                src <- Unchecked.defaultof<_>
                notify.Trigger()
            | _ -> ())
    let conn (a,f) =
                 if a then am.PowerOn dev.Adapter
                 let uuid = FeatureToUuid f
                 printfn "Connecting: %A => %s" (a,f) uuid
                 match (a,f) with
                 | (true, Feature.Sync) -> src_clean true
                 | (false, Feature.Sync) -> src_clean false
                 | (true, _) -> dev.ConnectProfile uuid
                 | (false, _) -> dev.DisconnectProfile uuid
    do dev.PropertyChanged.Add(fun o -> notify.Trigger())
    member x.Device = dev
    member x.Support = dev.UUIDs |> FromUuids
    member x.Connect y = conn (true, y)
    member x.Disconnect y = conn (false, y)
    member x.Connected = dm.Transports |> Seq.map (fun o -> dm.Transport o)
                                       |> Seq.filter (fun o -> path = o.Device)
                                       |> Seq.map (fun o -> o.UUID)
                                       |> FromUuids
    interface IBansheeDevice with
        member x.Device = x.Device
        member x.MediaControl =
            dm.MediaControls
            |> Seq.tryPick (fun o -> if path = o then
                                       dm.MediaControl o |> Some
                                     else
                                       None)
        member x.Support = x.Support
        member x.HasSupport y = x.Support |> Set.contains y
        member x.Connect y = x.Connect y
        member x.Disconnect y = x.Disconnect y
        member x.IsConnected y = match y with
                                 | AudioIn -> x.Connected |> Set.contains AudioOut
                                 | AudioOut -> x.Connected |> Set.contains AudioIn
                                 | Feature.Sync -> IsNull src |> not
                                 | _ -> x.Connected |> Set.contains y
        member x.Notify = notify.Publish
