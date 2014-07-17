//
// SupportApi.fs
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
module Banshee.Dap.Bluetooth.SupportApi

open System
open System.ComponentModel
open System.Collections.Generic
open System.Linq

open Banshee.Dap.Bluetooth.DBusApi

open DBus

let Merge (y: seq<KeyValuePair<'a,'b>>) (x: IDictionary<'a,'b>) =
    for z in y do x.[z.Key] <- z.Value
    x
let inline IsNull< ^a when ^a : not struct> (x: ^a) =
    obj.ReferenceEquals (x, Unchecked.defaultof<_>)

type PropertiesUpdatedArgs(i: string, ps: string[]) =
    inherit EventArgs()
    member x.Interface with get () = i
    member x.Properties with get() = ps

type PropertiesUpdatedHandler = delegate of obj * PropertiesUpdatedArgs -> unit

type IPropertyManager =
    abstract member Get : string -> string -> 'a
    abstract member Set : string -> string -> obj -> unit
    abstract member All : string -> StringVariantMap
    abstract member Use : string -> unit
    abstract member Has : string -> bool
    abstract member Not : string -> unit
    [<CLIEvent>]
    abstract member PropertiesUpdated : IEvent<PropertiesUpdatedHandler,PropertiesUpdatedArgs>

type PropertyManager(bus: Bus, name: string, path: ObjectPath, ipv: InterfacePropertyMap) as this =
    let op = bus.GetObject<IProperties>(name, path)
    let ce = Event<_,_>()
    do op.add_PropertiesChanged(fun i pv ip -> if not (this.Has i) then ipv.[i] <- pv
                                               else Merge pv ipv.[i] |> ignore
                                               for p in ip do ipv.[i].[p] <- null
                                               let pu = Array.append (pv.Keys.ToArray()) ip
                                               let arg = new PropertiesUpdatedArgs(i, pu)
                                               ce.Trigger(op, arg))
    member x.Get i p = try
                        ipv.[i].[p] :?> 'a
                       with
                       | _ -> Unchecked.defaultof<'a>
    member x.Set i p v = try
                           op.Set i p v
                           ipv.[i].[p] <- v
                         with
                         | _ -> ()
    member x.All i = ipv.[i]
    member x.Use i = ipv.[i] <- op.GetAll i
    member x.Has i = ipv.ContainsKey i
    member x.Not i = ipv.Remove i |> ignore
    member x.PropertiesUpdated = ce.Publish
    interface IPropertyManager with
        member x.Get i p = x.Get i p
        member x.Set i p v = x.Set i p v
        member x.All i = x.All i
        member x.Use i = x.Use i
        member x.Has i = x.Has i
        member x.Not i = x.Not i
        [<CLIEvent>]
        member x.PropertiesUpdated = x.PropertiesUpdated
    new(bus, name, path) = PropertyManager(bus, name, path, InterfacePropertyMap())

type Factory = obj -> IPropertyManager -> obj

type IDBusContainer =
    inherit IEquatable<IDBusContainer>
    inherit IComparable
    inherit IComparable<IDBusContainer>
    abstract Name : string with get
    abstract Path : ObjectPath with get
    abstract Get : unit -> 't option

type IDBusWrapper =
    inherit IDBusContainer
    abstract Pop : Type -> obj option
    abstract Put : Type -> Factory -> obj option
    abstract IsEmpty : bool with get
    abstract Properties : IPropertyManager with get

type DBusWrapper(bus: Bus, name: string, path: ObjectPath, ps: IPropertyManager) =
    let tim = Dictionary<Type, string>()
    let iom = Dictionary<string, obj>()
    member x.Name with get () = name
    member x.Path with get () = path
    member x.Properties with get () = ps
    member x.Pop t =
        if tim.ContainsKey t then
           let i = tim.[t]
           let o = iom.[i]
           iom.Remove i |> ignore
           tim.Remove t |> ignore
           Some o
         else None
    member x.Put t f =
        try
          let i = Functions.InterfaceOf t
          let dbo = bus.GetObject(t, name, path)
          let o = f dbo ps
          if not (ps.Has i) then ps.Use i
          tim.[t] <- i
          iom.[i] <- o
          Some o
        with
        | _ -> None
    member x.Get<'t> () =
        try
          let i = tim.[typeof<'t>]
          let o = iom.[i] :?> 't
          Some o
        with
        | _ -> None
    member x.IsEmpty = 0 = iom.Count
    member x.CompareTo (y: obj) =
        match y with
        | :? IDBusWrapper as y -> let xt = (x.Name, x.Path)
                                  let yt = (y.Name, y.Path)
                                  compare xt yt
        | _ -> 0
    override x.Equals y =
        match y with
        | :? IDBusWrapper as ydw -> x.Name = ydw.Name && x.Path = ydw.Path
        | _ -> false
    override x.GetHashCode () = x.Name.GetHashCode() ^^^ x.Path.GetHashCode()
    interface IDBusContainer with
        member x.Name = x.Name
        member x.Path = x.Path
        member x.Equals y = x.Equals y
        member x.CompareTo (y: IDBusContainer) = x.CompareTo y
        member x.CompareTo (y: obj) = x.CompareTo y
        member x.Get () = x.Get ()
    interface IDBusWrapper with
        member x.IsEmpty = x.IsEmpty
        member x.Pop t = x.Pop t
        member x.Put t f = x.Put t f
        member x.Properties = x.Properties
    new(bus, name, path, ipv: InterfacePropertyMap) =
        new DBusWrapper(bus, name, path, PropertyManager(bus, name, path, ipv))
    new(bus, name, path) = new DBusWrapper(bus, name, path, InterfacePropertyMap())
