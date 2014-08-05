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

open System
open System.Timers
open System.Reflection
open Banshee.Dap.Bluetooth.Adapters
open Banshee.Dap.Bluetooth.Client
open Banshee.Dap.Bluetooth.Configuration
open Banshee.Dap.Bluetooth.DapGlue
open Banshee.Dap.Bluetooth.Devices
open Banshee.Dap.Bluetooth.Wrappers
open Banshee.Dap.Bluetooth.SupportApi
open Banshee.ServiceStack
open Banshee.Sources
open DBus
open Hyena
open Hyena.Log

type IBansheeDevice =
    abstract Config : DeviceSchema with get
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
    let timer = new System.Timers.Timer()
    let cnf = DeviceSchema(dev.Address)
    let dap = BluetoothDevice(dev)
    let mutable src : BluetoothSource = Unchecked.defaultof<_>
    let src_add = InvokeHandler(fun () -> ServiceManager.SourceManager.AddSource src)
    let src_rem = InvokeHandler(fun () -> ServiceManager.SourceManager.RemoveSource src)
    let src_destroy () =
        if IsNull src |> not then
          ThreadAssist.BlockingProxyToMain src_rem
          src.Unmap () |> ignore
          src <- Unchecked.defaultof<_>
    let src_construct schedule =
        try
          src <- new BluetoothSource(dap, cm)
          src.DeviceInitialize (dap, false)
          ThreadAssist.BlockingProxyToMain src_add
          src.SequentialLoad ()
          src.Ejected.Add src_destroy
          if schedule && not src.Sync.AutoSync then
            src.Sync.DapLoaded ()
            let m = typeof<Banshee.Dap.DapSync>.GetMethod ("RateLimitedSync", BindingFlags.NonPublic ||| BindingFlags.Instance)
            if IsNull m |> not then
              Debugf "Dap.Bluetooth: Executing Synchronously %A" m
              m.Invoke (src.Sync, [||]) |> ignore
              if schedule then
                src_destroy ()
            else
              Warnf "Dap.Bluetooth: Executing Asynchronously"
              src.Sync.Sync ()
        with
        | e -> Log.Warning e
               src <- Unchecked.defaultof<_>
    let src_clean a schedule =
        ThreadAssist.SpawnFromMain (fun () ->
          match (IsNull src, a) with
          | (true, true) ->
              src_construct schedule
              notify.Trigger ()
          | (false, false) ->
              src_destroy ()
              notify.Trigger ()
          | _ -> ()
        ) |> ignore
    let conn (a,f) =
        if a then am.PowerOn dev.Adapter
        let uuid = FeatureToUuid f
        let deacon = if a then "Connecting" else "Disconnecting"
        let sf = sprintf "%s %A (%s)" deacon f uuid
        Infof "Dap.Bluetooth: %s" sf
        match (a,f) with
        | (x, Feature.Sync) -> src_clean x false
        | (true, _) -> dev.ConnectProfile uuid
        | (false, _) -> dev.DisconnectProfile uuid
    let set_sync () =
        if cnf.Auto then
          let next = cnf.Next
          let span = next - DateTime.Now
          timer.Interval <- span.TotalMilliseconds
          timer.Enabled <- true
          Infof "BansheeDevice: next sync for %s at %A" dev.Alias next
        else
          timer.Enabled <- false
        notify.Trigger ()
    do set_sync ()
       dev.PropertyChanged.Add(fun o -> notify.Trigger())
       timer.Elapsed.Add (fun o -> src_clean true true
                                   set_sync ())
       cnf.Notify.Add set_sync
    member x.Config = cnf
    member x.Device = dev
    member x.Support = dev.UUIDs |> FromUuids
    member x.Connect y = conn (true, y)
    member x.Disconnect y = conn (false, y)
    member x.Connected = dm.Transports |> Seq.map (fun o -> dm.Transport o)
                                       |> Seq.filter (fun o -> path = o.Device)
                                       |> Seq.map (fun o -> o.UUID)
                                       |> FromUuids
    interface IBansheeDevice with
        member x.Config = x.Config
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
