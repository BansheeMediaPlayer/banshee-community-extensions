//
// ObexApi.fs
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
namespace Banshee.Dap.Bluetooth.ObexApi

open System.Collections.Generic

open Banshee.Dap.Bluetooth.DBusApi
open Banshee.Dap.Bluetooth.DeviceApi

open DBus

module Constants =
    [<Literal>]
    let IF_CLIENT = "org.bluez.obex.Client1"
    [<Literal>]
    let IF_SESSION = "org.bluez.obex.Session1"
    [<Literal>]
    let IF_PUSH = "org.bluez.obex.ObjectPush1"
    [<Literal>]
    let IF_TRANSFER = "org.bluez.obex.Transfer1"
    [<Literal>]
    let IF_FILETRANSFER = "org.bluez.obex.FileTransfer1"

[<Interface (Constants.IF_CLIENT)>]
type IClient =
    abstract CreateSession : string -> StringVariantMap -> ObjectPath
    abstract RemoveSession : ObjectPath -> unit

[<Interface (Constants.IF_SESSION)>]
type ISession =
    abstract GetCapabilities : unit -> string
    abstract Source : string with get
    abstract Destination : string with get
    abstract Target : string with get
    abstract Root : string with get

[<Interface (Constants.IF_TRANSFER)>]
type ITransfer =
    abstract Cancel : unit -> unit
    abstract Suspend : unit -> unit
    abstract Resume : unit -> unit
    abstract Status : string with get
    abstract Session : ObjectPath with get
    abstract Name : string with get
    abstract Type : string with get
    abstract Time : uint64 with get
    abstract Size : uint64 with get
    abstract Transferred : uint64 with get
    abstract Filename : string with get

[<Interface (Constants.IF_FILETRANSFER)>]
type IFileTransfer =
    inherit ISession
    abstract ChangeFolder : string -> unit
    abstract CreateFolder : string -> unit
    abstract ListFolder : unit -> KeyValuePair<string,obj>[][]
    abstract PutFile : string -> string -> ObjectPath
    abstract Delete : string -> unit
