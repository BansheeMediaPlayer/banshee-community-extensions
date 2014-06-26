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
open Banshee.Dap.Bluetooth.ObexApi
open Banshee.Dap.Bluetooth.SupportApi
open Banshee.Dap.Bluetooth.InversionApi
open Banshee.Dap.Bluetooth.Wrappers

open DBus

module Functions =
    let PathAdapter x = sprintf "/org/bluez/hci%d" x
    let DBusMac (x: string) = x.Replace (":", "_")
    let PathDevice x y = sprintf "/org/bluez/hci%d/dev_%s" x (DBusMac y)
    let DictConv x = x |> Seq.map (|KeyValue|) |> Map.ofSeq
    let rec PrintMap x = 
        let PrintVal (v: obj) = match v with
                                | :? IDictionary as vd -> "{ " + PrintMap vd + " }"
                                | _ -> sprintf "%s" (v.ToString())
        let sb = StringBuilder()
        for key in x.Keys do 
            sb.AppendLine(sprintf "%s => %s" (key.ToString()) (PrintVal x.[key])) |> ignore
        sb.ToString()
    let PrintOipMap (oipv: ObjectInterfacePropertyMap) = 
        DictConv oipv
        |> Map.iter (fun o ipv -> printfn "%s -> {" (o.ToString())
                                  DictConv ipv 
                                  |> Map.iter (fun i pv -> printf "\t%s => {" (i.ToString())
                                                           let map = DictConv pv
                                                           match map with
                                                           | map when Map.isEmpty map -> printfn " }"
                                                           | _  -> Map.iter (fun p v -> printf "\n\t\t%s = %s" p (v.ToString())) map
                                                                   printfn "\n\t}")
                                  printfn "}")

module Constants =
    let NAME_BLUEZ = "org.bluez"
    let NAME_BLUEZ_OBEX = "org.bluez.obex"

type AdapterChangedArgs(a:ObjectAction, d: IBansheeAdapter, p: ObjectPath) =
    inherit ObjectChangedArgs(a, d, p)
    member x.Adapter = d
type DeviceChangedArgs(a:ObjectAction, d: IBansheeDevice, p: ObjectPath) =
    inherit ObjectChangedArgs(a, d, p)
    member x.Device = d

type AdapterChangedHandler = delegate of obj * AdapterChangedArgs -> unit
type DeviceChangedHandler = delegate of obj * DeviceChangedArgs -> unit

type DeviceManager(system: Bus) as this =
    let oman = DBusInverter(system, Constants.NAME_BLUEZ, ObjectPath.Root)
    let adapters = ConcurrentDictionary<_,IBansheeAdapter>() :> IDictionary<_,_>
    let devices = ConcurrentDictionary<_,IBansheeDevice>() :> IDictionary<_,_>
    let ac = Event<AdapterChangedHandler,AdapterChangedArgs>()
    let dc = Event<DeviceChangedHandler,DeviceChangedArgs>()
    let add (p: ObjectPath) (o: obj) =
        printfn "Added"
        match box o with
        | :? IBansheeAdapter as aw -> adapters.Add(p, aw)
                                      printfn "Added Adapter"
                                      aw.PropertyChanged.Add(fun o -> ac.Trigger(this, AdapterChangedArgs(Changed, aw, p)))
                                      ac.Trigger (this, AdapterChangedArgs(Added, aw, p))
        | :? IBansheeDevice as dw -> devices.Add(p, dw)
                                     printfn "Added Device"
                                     dw.PropertyChanged.Add(fun o -> dc.Trigger(this, DeviceChangedArgs(Changed, dw, p)))
                                     dc.Trigger(this, DeviceChangedArgs(Added, dw, p))
        | _ -> printfn "Added: %s" (o.GetType().FullName)
    let rem (p: ObjectPath) =
        printfn "Removed"
        match (adapters.ContainsKey p, devices.ContainsKey p) with
        | (true, false) -> let a = adapters.[p]
                           adapters.Remove p |> ignore
                           printfn "Removed Adapter"
                           ac.Trigger(this, AdapterChangedArgs(Removed, a, p))
                           true
        | (false, true) -> let d = devices.[p]
                           devices.Remove p |> ignore
                           printfn "Removed Device"
                           dc.Trigger(this, DeviceChangedArgs(Removed, d, p))
                           true
        | _ -> false
    do
        oman.Register<IAdapter> (fun o p -> let a = o :?> IAdapter
                                            AdapterWrapper(a, p) :> obj)
        oman.Register<IBluetoothDevice> (fun o p -> let d = o :?> IBluetoothDevice
                                                    DeviceWrapper(d, p) :> obj)
        oman.ObjectChanged.Add(fun o -> match o.Action with
                                        | Added -> add o.Path o.Object
                                        | Removed -> rem o.Path |> ignore
                                        | _ -> ())
        oman.Refresh ()
    member x.AdapterChanged = ac.Publish
    member x.DeviceChanged = dc.Publish
    member x.Adapters = adapters.Values
    member x.Devices = devices.Values
    member x.Powered with get () = x.Adapters |> Seq.exists (fun (o: IBansheeAdapter) -> o.Powered)
                     and set y = x.Adapters |> Seq.iter (fun (o: IBansheeAdapter) -> if y <> o.Powered then o.Powered <- y)
    member x.Discovering with get () = Seq.exists (fun (o: IBansheeAdapter) -> o.Discovering) x.Adapters
                         and set y = Seq.iter (fun (o: IBansheeAdapter) -> if o.Discovering <> y then
                                                                             if y then o.StartDiscovery ()
                                                                             else o.StopDiscovery ()) x.Adapters
