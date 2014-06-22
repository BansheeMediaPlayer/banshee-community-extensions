//
// Wrappers.fs
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

namespace Banshee.Dap.Bluetooth.Wrappers

open System.ComponentModel
open Banshee.Dap.Bluetooth.DBusApi
open Banshee.Dap.Bluetooth.AdapterApi
open Banshee.Dap.Bluetooth.DeviceApi
open Banshee.Dap.Bluetooth.SupportApi
open DBus

module Constants =
    let UUID_OBEXFTP = "00001106-0000-1000-8000-00805f9b34fb"
    let UUID_HEADSET = "00001108-0000-1000-8000-00805f9b34fb"
    let UUID_AUDIO_SOURCE = "0000110a-0000-1000-8000-00805f9b34fb"
    let UUID_AUDIO_SINK = "0000110b-0000-1000-8000-00805f9b34fb"
    let UUID_AVRC = "0000110c-0000-1000-8000-00805f9b34fb"

module Functions =
    let HasUuid (u: string) (x: IBluetoothDevice) = 
        x.UUIDs |> Array.exists (fun uuid -> u = uuid.ToLower())
    let HasSync x = HasUuid Constants.UUID_OBEXFTP x
    let HasAudioIn x = HasUuid Constants.UUID_AUDIO_SINK x
    let HasAudioOut x = HasUuid Constants.UUID_AVRC x
    let HasHeadset x = HasUuid Constants.UUID_HEADSET x

type IBansheeAdapter =
    inherit IAdapter
    inherit INotifyPropertyChanged

type IBansheeDevice =
    inherit IBluetoothDevice 
    inherit INotifyPropertyChanged
    abstract Sync : bool with get
    abstract AudioIn : bool with get
    abstract AudioOut : bool with get
    abstract Headset : bool with get

type AdapterWrapper (bus: Bus, name: string, path: ObjectPath, ps: InterfacePropertyMap) =
    inherit DBusWrapper<IAdapter>(bus, name, path, ps)
    do if 0 = ps.Count then base.Properties.Use Constants.IF_ADAPTER
    member private x.GetProp y = x.Properties.Get Constants.IF_ADAPTER y
    member private x.SetProp y z = x.Properties.Set Constants.IF_ADAPTER y z
    member x.Address = x.GetProp "Address"
    member x.Name = x.GetProp "Name"
    member x.Alias with get () = x.GetProp "Alias"
                   and set y = x.SetProp "Alias" y
    member x.Class with get () = x.GetProp "Class"
    member x.Powered with get () = x.GetProp "Powered"
                     and set y = x.SetProp "Powered" y
    member x.Discoverable with get () = x.GetProp "Discoverable"
                          and set y = x.SetProp "Discoverable" y
    member x.Pairable with get () = x.GetProp "Pairable"
                      and set y = x.SetProp "Pairable" y
    member x.Discovering with get () = x.GetProp "Discovering"
    member x.StartDiscovery () = if not x.Powered then x.Powered <- true
                                 x.Object.StartDiscovery ()
    member x.StopDiscovery () = x.Object.StopDiscovery ()
    member x.RemoveDevice y = x.Object.RemoveDevice y
    interface IBansheeAdapter with
        member x.Address = x.Address
        member x.Name = x.Name
        member x.Alias with get () = x.Alias
                       and set y = x.Alias <- y
        member x.Class = x.Class
        member x.Powered with get () = x.Powered
                         and set y = x.Powered <- y
        member x.Discoverable with get () = x.Discoverable
                              and set y = x.Discoverable <- y
        member x.Pairable with get () = x.Pairable
                          and set y = x.Pairable <- y
        member x.Discovering with get () = x.Discovering
        member x.StartDiscovery () = x.StartDiscovery ()
        member x.StopDiscovery () = x.StopDiscovery ()
        member x.RemoveDevice y = x.RemoveDevice y

type DeviceWrapper (bus: Bus, name: string, path: ObjectPath, ps: InterfacePropertyMap) =
    inherit DBusWrapper<IBluetoothDevice>(bus, name, path, ps)
    do if 0 = ps.Count then base.Properties.Use Constants.IF_DEVICE
    member private x.GetProp y = x.Properties.Get Constants.IF_DEVICE y
    member private x.SetProp y z = x.Properties.Set Constants.IF_DEVICE y z
    member x.Sync = Functions.HasSync x
    member x.AudioIn = Functions.HasAudioIn x
    member x.AudioOut = Functions.HasAudioOut x
    member x.Headset = Functions.HasHeadset x
    member x.Disconnect () = x.Object.Disconnect ()
    member x.Connect () = x.Object.Connect ()
    member x.ConnectProfile y = x.Object.ConnectProfile y
    member x.DisconnectProfile y = x.Object.DisconnectProfile y
    member x.Pair () = x.Object.Pair ()
    member x.CancelPairing () = x.Object.CancelPairing ()
    member x.Address = x.GetProp "Address"
    member x.Name = x.GetProp "Name"
    member x.Alias with get () = x.GetProp "Alias"
                   and set y = x.SetProp "Alias" y
    member x.Class = x.GetProp "Class"
    member x.Appearance = x.GetProp "Appearance"
    member x.Icon = x.GetProp "Icon"
    member x.Paired = x.GetProp "Paired"
    member x.Trusted with get () = x.GetProp "Trusted"
                     and set y = x.SetProp "Trusted" y
    member x.Blocked with get () = x.GetProp "Blocked"
                     and set y = x.SetProp "Blocked" y
    member x.LegacyPairing = x.GetProp "LegacyPairing"
    member x.RSSI = x.GetProp "RSSI"
    member x.Connected = x.GetProp "Connected"
    member x.UUIDs = x.GetProp "UUIDs"
    member x.Modalias = x.GetProp "Modalias"
    member x.Adapter = x.GetProp "Adapter"
    interface IBansheeDevice with
        member x.Sync = x.Sync
        member x.AudioIn = x.AudioIn
        member x.AudioOut = x.AudioOut
        member x.Headset = x.Headset
        member x.Disconnect () = x.Disconnect ()
        member x.Connect () = x.Connect ()
        member x.ConnectProfile y = x.ConnectProfile y
        member x.DisconnectProfile y = x.DisconnectProfile y
        member x.Pair () = x.Pair ()
        member x.CancelPairing () = x.CancelPairing ()
        member x.Address = x.Address
        member x.Name = x.Name
        member x.Alias with get () = x.Alias
                       and set y = x.Alias <- y
        member x.Class = x.Class
        member x.Appearance = x.Appearance
        member x.Icon = x.Icon
        member x.Paired = x.Paired
        member x.Trusted with get () = x.Trusted
                         and set y = x.Trusted <- y
        member x.Blocked with get () = x.Blocked
                         and set y = x.Blocked <- y
        member x.LegacyPairing = x.LegacyPairing
        member x.RSSI = x.RSSI
        member x.Connected = x.Connected
        member x.UUIDs = x.UUIDs
        member x.Modalias = x.Modalias
        member x.Adapter = x.Adapter
