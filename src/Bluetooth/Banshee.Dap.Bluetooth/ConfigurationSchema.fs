//
// DeviceConfigurationSchema.fs
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
module Banshee.Dap.Bluetooth.Configuration

open System
open Banshee.Configuration
open Banshee.Dap.Bluetooth.DapGlue

let AUTO_DSHORT = Singular "Whether scheduled synchronisation is enabled for this device"
let TIME_DSHORT = Singular "What time scheduled synchronisation should occur"

type DeviceSchema(addr: string) =
    let ns = "plugins.bluetooth.devices." + addr
    let auto = SchemaEntry<bool> (ns, "sync_enabled", false, AUTO_DSHORT, "")
    let time = SchemaEntry<int> (ns, "sync_time", 0, TIME_DSHORT, "")
    let notify = Event<_>()
    member x.Auto with get () = auto.Get ()
                  and  set v  = auto.Set v |> ignore
                                notify.Trigger()
    member x.Time with get () = time.Get ()
                  and  set v  = time.Set v |> ignore
                                notify.Trigger()
    member x.Next =
        let next = TimeSpan (x.Time / 60, x.Time % 60, 0) |> DateTime.Today.Add
        if DateTime.Now >= next then next.AddDays 1.0
        else next
    member x.Notify = notify.Publish
