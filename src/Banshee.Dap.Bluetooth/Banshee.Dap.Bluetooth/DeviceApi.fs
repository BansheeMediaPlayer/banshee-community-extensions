//
// DeviceApi.fs
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
namespace Banshee.Dap.Bluetooth.DeviceApi

open System
open System.Collections.Generic

open Banshee.Dap.Bluetooth.DBusApi

open DBus

module Constants =
    [<Literal>]
    let IF_DEVICE = "org.bluez.Device1"

[<Interface (Constants.IF_DEVICE)>]
type IBluetoothDevice =
    abstract Disconnect : unit -> unit
    abstract Connect : unit -> unit
    abstract ConnectProfile : string -> unit
    abstract DisconnectProfile : string -> unit
    abstract Pair : unit -> unit
    abstract CancelPairing : unit -> unit
    abstract Address : string with get
    abstract Name : string with get
    abstract Alias : string with get, set
    abstract Class : uint32 with get
    abstract Appearance : uint16 with get 
    abstract Icon : string with get
    abstract Paired : bool with get
    abstract Trusted : bool with get, set
    abstract Blocked : bool with get, set
    abstract LegacyPairing : bool with get
    abstract RSSI : int16 with get
    abstract Connected : bool with get
    abstract UUIDs : string[] with get
    abstract Modalias : string with get
    abstract Adapter : ObjectPath with get

type DeviceComparer() =
    interface IEqualityComparer<IBluetoothDevice> with
        member x.Equals (y,z) = y.Address = z.Address
        member x.GetHashCode y = y.Address.GetHashCode ()
