//
// AdapterApi.fs
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
namespace Banshee.Dap.Bluetooth.AdapterApi

open System
open System.Collections.Generic

open Banshee.Dap.Bluetooth.DBusApi

open DBus

module Constants =
    [<Literal>]
    let IF_ADAPTER = "org.bluez.Adapter1"

[<Interface (Constants.IF_ADAPTER)>]
type IAdapter =
    abstract StartDiscovery : unit -> unit
    abstract StopDiscovery : unit -> unit
    abstract RemoveDevice : ObjectPath -> unit
    abstract Address : string with get
    abstract Name : string with get
    abstract Alias : string with get, set
    abstract Class : uint32 with get
    abstract Powered : bool with get, set
    abstract Discoverable : bool with get, set
    abstract Pairable : bool with get, set
    abstract Discovering : bool with get

type AdapterComparer() =
    interface IEqualityComparer<IAdapter> with
        member x.Equals (y,z) = y.Address = z.Address
        member x.GetHashCode y = y.Address.GetHashCode ()
