//
// ClientManager.fs
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
module Banshee.Dap.Bluetooth.Client

open System.Collections.Generic
open System.Linq

open Banshee.Dap.Bluetooth.DBusApi
open Banshee.Dap.Bluetooth.InversionApi
open Banshee.Dap.Bluetooth.ObexApi
open Banshee.Dap.Bluetooth.Wrappers
open Banshee.Dap.Bluetooth.SupportApi

open Hyena.Log

open DBus

let NAME_BLUEZ_OBEX = "org.bluez.obex"

type ClientManager(session: Bus) =
    let inv = DBusInverter(session, NAME_BLUEZ_OBEX, ObjectPath.Root)
    let clients = Dictionary<ObjectPath,IClient>() :> IDictionary<_,_>
    let sessions = Dictionary<ObjectPath,IFileTransfer>() :> IDictionary<_,_>
    let transfers = Dictionary<ObjectPath,INotifyTransfer>() :> IDictionary<_,_>
    let add (o: obj) p =
        match o with
        | :? IClient as c -> clients.[p] <- c
        | :? IFileTransfer as s -> sessions.[p] <- s
        | :? INotifyTransfer as t -> transfers.[p] <- t
        | _ -> o.ToString() |> Warnf "Ignoring Added: %s"
    let rem (o: obj) (p: ObjectPath) =
        match o with
        | :? IClient -> clients.Remove p
        | :? IFileTransfer -> sessions.Remove p
        | :? INotifyTransfer -> transfers.Remove p
        | _ -> o.ToString() |> Warnf "Ignoring Removed: %s"
               false
    do inv.Register<IClient>(fun o p -> o)
       inv.Register<ISession>(fun o p -> let s = o :?> ISession
                                         SessionWrapper(s, p) :> obj)
       inv.Register<ITransfer>(fun o p -> let t = o :?> ITransfer
                                          TransferWrapper(t, p) :> obj)
       inv.Register<IFileTransfer>(fun o p -> o)
       inv.ObjectChanged.Add(fun o -> match o.Action with
                                      | Added -> add o.Object o.Path
                                      | Changed -> ()
                                      | Removed -> rem o.Object o.Path |> ignore)
       inv.Refresh ()
    member x.Client = clients.Values.FirstOrDefault()
    member x.Clients = clients.Values
    member x.Session p =
        let valid = not (IsNull p) && sessions.ContainsKey p
        if valid then
          Some sessions.[p]
        else
          None
    member x.CreateSession addr t =
        let sot = SessionOf t
        let cp = StringVariantMap()
        cp.["Target"] <- sot
        x.Client.CreateSession addr cp
    member x.RemoveSession p = x.Client.RemoveSession p
    member x.Transfers y = transfers.Values |> Seq.filter (fun t -> y = t.Session)
