//
// MediaApi.fs
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
module Banshee.Dap.Bluetooth.MediaApi

open DBus

[<Literal>]
let IF_MEDIA_CONTROL = "org.bluez.MediaControl1"
[<Literal>]
let IF_MEDIA_TRANSPORT = "org.bluez.MediaTransport1"

type TransportState = | Idle | Pending | Active

let StringToState = function
    | "idle" -> Idle
    | "pending" -> Pending
    | "active" -> Active
    | x -> failwith "Transport State Not Recognised: %s" x

[<Interface (IF_MEDIA_CONTROL)>]
type IMediaControl =
    abstract Play : unit -> unit
    abstract Pause : unit -> unit
    abstract Stop : unit -> unit
    abstract Next : unit -> unit
    abstract Previous : unit -> unit
    abstract VolumeUp : unit -> unit
    abstract VolumeDown : unit -> unit
    abstract FastForward : unit -> unit
    abstract Rewind : unit -> unit
    abstract Connected : bool with get

[<Interface (IF_MEDIA_TRANSPORT)>]
type IMediaTransport =
    //abstract Aquire : unit -> (ObjectPath * uint16 * uint16)
    //abstract TryAquire : unit -> (ObjectPath * uint16 * uint16)
    //abstract Release : unit -> unit
    abstract Device : ObjectPath with get
    abstract UUID : string with get
    abstract Codec : byte with get
    abstract State : string with get
