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
module Banshee.Dap.Bluetooth.Devices

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
open Banshee.Dap.Bluetooth.Adapters

open Hyena.Log
open DBus

type Feature =
    | Sync
    | AudioOut
    | AudioIn
    | Headset

let UuidToFeature =
    function | UUID_AUDIO_SINK   -> Some Feature.AudioOut
             | UUID_AUDIO_SOURCE -> Some Feature.AudioIn
             | UUID_HEADSET      -> Some Feature.Headset
             | UUID_OBEXFTP      -> Some Feature.Sync
             | _ -> None
let FeatureToUuid =
    function | Feature.AudioOut -> UUID_AUDIO_SINK
             | Feature.AudioIn  -> UUID_AUDIO_SOURCE
             | Feature.Headset  -> UUID_HEADSET
             | Feature.Sync     -> UUID_OBEXFTP
let FromUuids uuids =
    uuids |> Seq.choose UuidToFeature
          |> Set.ofSeq

type DBusEventArgs<'t>(a: ObjectAction, path: ObjectPath, obj: 't) =
    inherit EventArgs()
    member x.Action = a
    member x.Path = path
    member x.Object = obj

type DeviceManager(system: Bus) =
    let oman = DBusInverter(system, NAME_BLUEZ, ObjectPath.Root)
    let devices = ConcurrentDictionary<_,INotifyDevice>() :> IDictionary<_,_>
    let media = ConcurrentDictionary<_,INotifyMediaControl>() :> IDictionary<_,_>
    let transports = ConcurrentDictionary<_,INotifyMediaTransport>() :> IDictionary<_,_>
    let notify_device = Event<_>()
    let notify_media = Event<_>()
    let notify_transport = Event<_>()
    let add (p: ObjectPath) (o: obj) =
        match box o with
        | :? INotifyDevice as dw -> devices.Add(p, dw)
                                    notify_device.Trigger(DBusEventArgs(Added, p, dw))
        | :? INotifyMediaControl as mcw -> media.Add(p, mcw)
                                           notify_media.Trigger(DBusEventArgs(Added, p, mcw))
        | :? INotifyMediaTransport as mtw -> transports.[p] <- mtw
                                             notify_transport.Trigger(DBusEventArgs(Added, p, mtw))
        | _ -> o.ToString() |> Warnf "Dap.Bluetooth: Ignoring Added %s"
    let rem (p: ObjectPath) (o: obj) =
        match o with
        | :? INotifyDevice as dw -> devices.Remove p |> ignore
                                    notify_device.Trigger(DBusEventArgs(Removed, p, dw))
                                    true
        | :? INotifyMediaControl as mcw -> media.Remove p |> ignore
                                           notify_media.Trigger(DBusEventArgs(Removed, p, mcw))
                                           true
        | :? INotifyMediaTransport as mtw -> transports.Remove p |> ignore
                                             notify_transport.Trigger(DBusEventArgs(Removed, p, mtw))
                                             true
        | _ -> o.ToString() |> Warnf "Dap.Bluetooth: Ignoring Removed %s"
               false
    do
        oman.Register<IDevice> (fun o p -> let d = o :?> IDevice
                                           DeviceWrapper(d, p) :> obj)
        oman.Register<IMediaControl> (fun o p -> let m = o :?> IMediaControl
                                                 MediaControlWrapper(m, p) :> obj)
        oman.Register<IMediaTransport> (fun o p -> let m = o :?> IMediaTransport
                                                   MediaTransportWrapper(m, p) :> obj)
        oman.ObjectChanged.Add(fun o -> match o.Action with
                                        | Added -> add o.Path o.Object
                                        | Removed -> rem o.Path o.Object |> ignore
                                        | _ -> ())
        oman.Refresh ()
    member x.DeviceEvent = notify_device.Publish
    member x.MediaEvent = notify_media.Publish
    member x.TransportEvent = notify_transport.Publish
    member x.Devices = devices.Keys
    member x.Device y = devices.[y]
    member x.MediaControls = media.Keys
    member x.MediaControl y = media.[y]
    member x.Transports = transports.Keys
    member x.Transport y = transports.[y]
