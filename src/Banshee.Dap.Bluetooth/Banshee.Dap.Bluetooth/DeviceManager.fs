//
// BluetoothManager.fs
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
namespace Banshee.Dap.Bluetooth

open System
open System.ComponentModel
open System.Text
open System.Collections
open System.Collections.Concurrent
open System.Collections.Generic

open Banshee.Dap.Bluetooth.DBusApi
open Banshee.Dap.Bluetooth.AdapterApi
open Banshee.Dap.Bluetooth.DeviceApi
open Banshee.Dap.Bluetooth.MediaApi
open Banshee.Dap.Bluetooth.ObexApi
open Banshee.Dap.Bluetooth.SupportApi
open Banshee.Dap.Bluetooth.InversionApi
open Banshee.Dap.Bluetooth.Wrappers

open Bluetooth

open DBus

module Functions =
    let SetKillswitch x =
        use ks = new Killswitch()
        let xs = match x with
                 | true -> KillswitchState.SoftBlocked
                 | false -> KillswitchState.Unblocked
        match (xs, ks.State) with
        | (xs, y) when xs = y -> true
        | (_, KillswitchState.HardBlocked)
        | (_, KillswitchState.NoAdapter) -> false
        | (xs, _) -> ks.State <- xs
                     true

module Constants =
    let NAME_BLUEZ = "org.bluez"

type AdapterChangedArgs(a:ObjectAction, d: IBansheeAdapter, p: ObjectPath) =
    inherit ObjectChangedArgs(a, d, p)
    member x.Adapter = d
type DeviceChangedArgs(a:ObjectAction, d: IBansheeDevice, p: ObjectPath) =
    inherit ObjectChangedArgs(a, d, p)
    member x.Device = d
type MediaControlArgs(a: ObjectAction, m: IBansheeMediaControl, p: ObjectPath) =
    inherit ObjectChangedArgs(a, m, p)
    member x.MediaControl = m

type AdapterChangedHandler = delegate of obj * AdapterChangedArgs -> unit
type DeviceChangedHandler = delegate of obj * DeviceChangedArgs -> unit
type MediaControlHandler = delegate of obj * MediaControlArgs -> unit

type DeviceManager(system: Bus) as this =
    let oman = DBusInverter(system, Constants.NAME_BLUEZ, ObjectPath.Root)
    let adapters = ConcurrentDictionary<_,IBansheeAdapter>() :> IDictionary<_,_>
    let devices = ConcurrentDictionary<_,IBansheeDevice>() :> IDictionary<_,_>
    let media = ConcurrentDictionary<_,IBansheeMediaControl>() :> IDictionary<_,_>
    let ac = Event<AdapterChangedHandler,AdapterChangedArgs>()
    let dc = Event<DeviceChangedHandler,DeviceChangedArgs>()
    let mc = Event<MediaControlHandler,MediaControlArgs>()
    let add (p: ObjectPath) (o: obj) =
        match box o with
        | :? IBansheeAdapter as aw -> adapters.Add(p, aw)
                                      aw.PropertyChanged.Add(fun o -> ac.Trigger(this, AdapterChangedArgs(Changed, aw, p)))
                                      ac.Trigger (this, AdapterChangedArgs(Added, aw, p))
        | :? IBansheeDevice as dw -> devices.Add(p, dw)
                                     dw.PropertyChanged.Add(fun o -> dc.Trigger(this, DeviceChangedArgs(Changed, dw, p)))
                                     dc.Trigger(this, DeviceChangedArgs(Added, dw, p))
        | :? IBansheeMediaControl as mw -> media.Add(p, mw)
                                           mc.Trigger(this, MediaControlArgs(Added, mw, p))
        | _ -> o.ToString() |> printfn "Ignoring Added: %s"
    let rem (p: ObjectPath) (o: obj) =
        match o with
        | :? IBansheeAdapter -> let a = adapters.[p]
                                adapters.Remove p |> ignore
                                ac.Trigger(this, AdapterChangedArgs(Removed, a, p))
                                true
        | :? IBansheeDevice -> let d = devices.[p]
                               devices.Remove p |> ignore
                               dc.Trigger(this, DeviceChangedArgs(Removed, d, p))
                               true
        | :? IBansheeMediaControl -> let m = media.[p]
                                     media.Remove p |> ignore
                                     mc.Trigger (this, MediaControlArgs(Removed, m, p))
                                     true
        | _ -> o.ToString() |> printfn "Ignoring Removed: %s"
               false
    do
        oman.Register<IAdapter> (fun o p -> let a = o :?> IAdapter
                                            AdapterWrapper(a, p) :> obj)
        oman.Register<IBluetoothDevice> (fun o p -> let d = o :?> IBluetoothDevice
                                                    DeviceWrapper(d, p) :> obj)
        oman.Register<IMediaControl> (fun o p -> let m = o :?> IMediaControl
                                                 MediaControlWrapper(m, p) :> obj)
        oman.ObjectChanged.Add(fun o -> match o.Action with
                                        | Added -> add o.Path o.Object
                                        | Removed -> rem o.Path o.Object |> ignore
                                        | _ -> ())
        oman.Refresh ()
    member x.AdapterChanged = ac.Publish
    member x.DeviceChanged = dc.Publish
    member x.MediaControlChanged = mc.Publish
    member x.Adapters = adapters.Values
    member x.Devices = devices.Values
    member x.MediaControls = media.Values
    member x.Powered with get () = x.Adapters |> Seq.exists (fun (o: IBansheeAdapter) -> o.Powered)
                     and set y =
                        not y |> Functions.SetKillswitch |> ignore
                        x.Adapters
                        |> Seq.iter (fun (o: IBansheeAdapter) -> if y <> o.Powered then o.Powered <- y)
    member x.Discovering with get () = Seq.exists (fun (o: IBansheeAdapter) -> o.Discovering) x.Adapters
                         and set y = Seq.iter (fun (o: IBansheeAdapter) -> if o.Discovering <> y then
                                                                             if y then Functions.SetKillswitch false |> ignore
                                                                                       o.StartDiscovery ()
                                                                             else o.StopDiscovery ()) x.Adapters
