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

open System
open System.ComponentModel

open Banshee.Dap.Bluetooth.AdapterApi
open Banshee.Dap.Bluetooth.DBusApi
open Banshee.Dap.Bluetooth.DeviceApi
open Banshee.Dap.Bluetooth.MediaApi
open Banshee.Dap.Bluetooth.Mime
open Banshee.Dap.Bluetooth.Mime.Extensions
open Banshee.Dap.Bluetooth.ObexApi
open Banshee.Dap.Bluetooth.SupportApi
open Banshee.Hardware

open DBus

type SessionType = | Ftp | Map | Opp | Pbap | Sync

module Constants =
    let UUID_OBEXFTP      = "00001106-0000-1000-8000-00805f9b34fb"
    let UUID_HEADSET      = "00001108-0000-1000-8000-00805f9b34fb"
    let UUID_AUDIO_SOURCE = "0000110a-0000-1000-8000-00805f9b34fb"
    let UUID_AUDIO_SINK   = "0000110b-0000-1000-8000-00805f9b34fb"
    let UUID_AVRC_TARGET  = "0000110c-0000-1000-8000-00805f9b34fb"
    let UUID_AVRC         = "0000110e-0000-1000-8000-00805f9b34fb"

module Functions =
    let HasUuid (u: string) (x: IBluetoothDevice) =
        x.UUIDs |> Array.exists (fun uuid -> u = uuid.ToLower())
    let HasSync x = HasUuid Constants.UUID_OBEXFTP x
    let HasAudioIn x = HasUuid Constants.UUID_AUDIO_SINK x
    let HasAudioOut x = HasUuid Constants.UUID_AUDIO_SOURCE x
    let HasHeadset x = HasUuid Constants.UUID_HEADSET x
    let SessionOf x = match x with
                      | Ftp -> "ftp"
                      | Map -> "map"
                      | Opp -> "opp"
                      | Pbap -> "pbap"
                      | Sync -> "sync"

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

type IBansheeMediaControl =
    inherit INotifyPropertyChanged
    inherit IMediaControl

type IBansheeClient =
    inherit IClient

[<AbstractClass>]
type ObjectDecorator<'t> (obj: 't, ps: IPropertyManager) as this =
    let i = Functions.InterfaceOf<'t>()
    let e = Event<_,_>()
    do ps.PropertiesUpdated.Add (fun pua -> if i = pua.Interface then
                                              for p in pua.Properties do
                                                e.Trigger (this, new PropertyChangedEventArgs(p)))
    member x.GetProp p = ps.Get i p
    member x.SetProp p v = ps.Set i p v
    member x.Interface = i
    interface INotifyPropertyChanged with
        [<CLIEvent>]
        member x.PropertyChanged = e.Publish

type AdapterWrapper (obj: IAdapter, ps: IPropertyManager) =
    inherit ObjectDecorator<IAdapter>(obj, ps)
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
                                 obj.StartDiscovery ()
    member x.StopDiscovery () = obj.StopDiscovery ()
    member x.RemoveDevice y = obj.RemoveDevice y
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

type DeviceWrapper (obj: IBluetoothDevice, ps: IPropertyManager) =
    inherit ObjectDecorator<IBluetoothDevice>(obj, ps)
    member x.Sync = Functions.HasSync x
    member x.AudioIn = Functions.HasAudioIn x
    member x.AudioOut = Functions.HasAudioOut x
    member x.Headset = Functions.HasHeadset x
    member x.Disconnect () = obj.Disconnect ()
    member x.Connect () = obj.Connect ()
    member x.ConnectProfile y = obj.ConnectProfile y
    member x.DisconnectProfile y = obj.DisconnectProfile y
    member x.Pair () = obj.Pair ()
    member x.CancelPairing () = obj.CancelPairing ()
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

type MediaControlWrapper(obj: IMediaControl, ps: IPropertyManager) =
    inherit ObjectDecorator<IMediaControl>(obj, ps)
    member x.Play () = obj.Play ()
    member x.Pause () = obj.Pause ()
    member x.Stop () = obj.Stop ()
    member x.Next () = obj.Next ()
    member x.Previous () = obj.Previous ()
    member x.VolumeUp () = obj.VolumeUp ()
    member x.VolumeDown () = obj.VolumeDown ()
    member x.FastForward () = obj.FastForward ()
    member x.Rewind () = obj.Rewind ()
    member x.Connected = x.GetProp "Connected"
    interface IBansheeMediaControl with
        member x.Play () = x.Play ()
        member x.Pause () = x.Pause ()
        member x.Stop () = x.Stop ()
        member x.Next () = x.Next ()
        member x.Previous () = x.Previous ()
        member x.VolumeUp () = x.VolumeUp ()
        member x.VolumeDown () = x.VolumeDown ()
        member x.FastForward () = x.FastForward ()
        member x.Rewind () = x.Rewind ()
        member x.Connected = x.Connected

type ClientWrapper(obj: IClient, ps: IPropertyManager) =
    inherit ObjectDecorator<IClient>(obj, ps)
    member x.CreateSession (t, spm) = obj.CreateSession t spm
    member x.RemoveSession o = obj.RemoveSession o
    interface IBansheeClient with
        member x.CreateSession t spm = x.CreateSession (t, spm)
        member x.RemoveSession o = x.RemoveSession o

type SessionWrapper(obj: ISession, ps: IPropertyManager) =
    inherit ObjectDecorator<ISession>(obj, ps)
    member x.GetCapabilities () = obj.GetCapabilities ()
    member x.Source = x.GetProp "Source"
    member x.Destination = x.GetProp "Destination"
    member x.Target = x.GetProp "Target"
    member x.Root = x.GetProp "Root"
    interface ISession with
        member x.GetCapabilities () = x.GetCapabilities ()
        member x.Source = x.Source
        member x.Destination = x.Destination
        member x.Target = x.Target
        member x.Root = x.Root

type TransferWrapper(obj: ITransfer, ps: IPropertyManager) =
    inherit ObjectDecorator<ITransfer>(obj, ps)
    member x.Cancel () = obj.Cancel ()
    member x.Suspend () = obj.Suspend ()
    member x.Resume () = obj.Resume ()
    member x.Status = x.GetProp "Status"
    member x.Session = x.GetProp "Session"
    member x.Name = x.GetProp "Name"
    member x.Type = x.GetProp "Type"
    member x.Time = x.GetProp "Time"
    member x.Size = x.GetProp "Size"
    member x.Transferred = x.GetProp "Transferred"
    member x.Filename = x.GetProp "Filename"
    interface ITransfer with
        member x.Cancel () = x.Cancel ()
        member x.Suspend () = x.Suspend ()
        member x.Resume () = x.Resume ()
        member x.Status = x.Status
        member x.Session = x.Session
        member x.Name = x.Name
        member x.Type = x.Type
        member x.Time = x.Time
        member x.Size = x.Size
        member x.Transferred = x.Transferred
        member x.Filename = x.Filename

type FileTransferWrapper(obj: IFileTransfer, ps: IPropertyManager) =
    inherit SessionWrapper(obj, ps)
    member x.ChangeFolder y = obj.ChangeFolder y
    member x.CreateFolder y = obj.CreateFolder y
    member x.ListFolder () = obj.ListFolder ()
    member x.PutFile src dst = obj.PutFile src dst
    member x.Delete y = obj.Delete y
    interface IFileTransfer with
        member x.ChangeFolder y = x.ChangeFolder y
        member x.CreateFolder y = x.CreateFolder y
        member x.ListFolder () = x.ListFolder ()
        member x.PutFile src dst = x.PutFile src dst
        member x.Delete y = x.Delete y
