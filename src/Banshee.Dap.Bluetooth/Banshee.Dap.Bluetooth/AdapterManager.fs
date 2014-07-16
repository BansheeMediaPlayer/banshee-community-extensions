//
// AdapterManager.fs
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
module Banshee.Dap.Bluetooth.Adapters

open System.Collections.Generic
open System.ComponentModel
open Banshee.Dap.Bluetooth.AdapterApi
open Banshee.Dap.Bluetooth.InversionApi
open Banshee.Dap.Bluetooth.Wrappers
open DBus
open global.Bluetooth

let tryFind y (dict: IDictionary<_,_>) =
    if dict.ContainsKey y then Some dict.[y]
    else None

let SetKillswitch x =
    use ks = new Killswitch()
    let xs = match x with
             | true -> KillswitchState.SoftBlocked
             | false -> KillswitchState.Unblocked
    match (xs, ks.State) with
    | (_, KillswitchState.HardBlocked)
    | (_, KillswitchState.NoAdapter) -> false
    | (xs, y) when xs = y -> true
    | (xs, _) -> ks.State <- xs
                 true

type AdapterManager(bus: Bus) =
    let inv = DBusInverter(bus, NAME_BLUEZ, ObjectPath.Root)
    let ads = Dictionary<ObjectPath, AdapterWrapper>()
    let notify = Event<_>()
    let get_pow () = ads.Values |> Seq.exists (fun a -> a.Powered)
    let get_dis () = ads.Values |> Seq.exists (fun a -> a.Discovering)
    let set_ksw x = SetKillswitch x |> ignore
                    notify.Trigger()
    let set_pow x = not x |> set_ksw
                    ads.Values |> Seq.iter (fun a -> if x <> a.Powered then a.Powered <- x)
    let set_dis x = if x then set_pow x
                    ads.Values |> Seq.iter (fun a -> match (x, a.Discovering) with
                                                     | (y, z) when y = z -> ()
                                                     | (true, _) -> a.StartDiscovery()
                                                     | (false, _) -> a.StopDiscovery())
    let handle = PropertyChangedEventHandler(fun o x -> notify.Trigger())
    do
        inv.Register<IAdapter> (fun o p -> let a = o :?> IAdapter
                                           AdapterWrapper(a, p) :> obj)
        inv.ObjectChanged.Add(fun o ->
          let adapter = o.Object :?> AdapterWrapper
          match o.Action with
          | Added -> ads.[o.Path] <- adapter
                     adapter.PropertyChanged.AddHandler(handle)
                     notify.Trigger()
          | Removed -> adapter.PropertyChanged.RemoveHandler(handle)
                       ads.Remove o.Path |> ignore
                       notify.Trigger()
          | _ -> ())
        inv.Refresh ()
    member x.Adapters = ads.Keys
    member x.Adapter y = ads |> tryFind y
    member x.PowerOn y = set_ksw false
                         ads.[y].Powered <- true
    member x.Powered with get () = get_pow ()
                     and set v = set_pow v
    member x.Discovering with get () = get_dis ()
                         and set v = set_dis v
    member x.Notify = notify.Publish
