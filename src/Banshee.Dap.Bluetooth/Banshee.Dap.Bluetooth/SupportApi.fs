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

namespace Banshee.Dap.Bluetooth.SupportApi

open System
open System.ComponentModel
open System.Collections.Generic
open System.Linq

open Banshee.Dap.Bluetooth.DBusApi

open DBus

module Functions =
    let Merge (y: seq<KeyValuePair<'a,'b>>) (x: IDictionary<'a,'b>) = 
        for z in y do x.[z.Key] <- z.Value
        x

type PropertiesUpdatedArgs(ps: string[]) = 
        inherit EventArgs()
        member x.Properties with get() = ps

type PropertiesUpdatedHandler = delegate of obj * PropertiesUpdatedArgs -> unit

type IPropertyManager =
    abstract member Get : string -> string -> 'a
    abstract member Set : string -> string -> obj -> unit
    abstract member All : string -> StringVariantMap
    abstract member Use : string -> unit
    abstract member Has : string -> bool
    [<CLIEvent>]
    abstract member PropertiesUpdated : IEvent<PropertiesUpdatedHandler,PropertiesUpdatedArgs>

type PropertyManager(bus: Bus, name: string, path: ObjectPath, ipv: InterfacePropertyMap) as this =
    let op = bus.GetObject<IProperties>(name, path)
    let ce = Event<_,_>()
    do op.add_PropertiesChanged(fun i pv ip -> if not (this.Has i) then ipv.[i] <- pv
                                               else Functions.Merge pv ipv.[i] |> ignore
                                               for p in ip do ipv.[i].[p] <- None
                                               let pu = Array.append (pv.Keys.ToArray()) ip
                                               let arg = new PropertiesUpdatedArgs(pu)
                                               ce.Trigger(op, arg)
                                               printfn "Properties Changed: %s" i)
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
    member x.PropertiesUpdated = ce.Publish
    interface IPropertyManager with
        member x.Get i p = x.Get i p
        member x.Set i p v = x.Set i p v
        member x.All i = x.All i
        member x.Use i = x.Use i
        member x.Has i = x.Has i
        [<CLIEvent>]
        member x.PropertiesUpdated = x.PropertiesUpdated
    new(bus, name, path) = PropertyManager(bus, name, path, InterfacePropertyMap())

[<AbstractClass>]
type DBusWrapper<'T>(bus: Bus, name: string, path: ObjectPath, ps: IPropertyManager) as this = 
    do printfn "Wrapping %s at %s for %s" typeof<'T>.Name (path.ToString()) name
    let ob = bus.GetObject<'T>(name, path)
    let pc = Event<_,_>()
    do printfn "Created Wrapper"
       ps.PropertiesUpdated.Add(fun pua -> for y in pua.Properties do 
                                             pc.Trigger(this, new PropertyChangedEventArgs(y)))
    member x.Name with get () = name
    member x.Path with get () = path
    member x.Properties with get () = ps
    member x.Object with get () = ob
    override x.Equals y = match y with
                          | :? DBusWrapper<'T> as yaw -> name = yaw.Name && path = yaw.Path
                          | _ -> false
    override x.GetHashCode () = name.GetHashCode() ^^^ path.GetHashCode()
    interface INotifyPropertyChanged with
        [<CLIEvent>]
        member x.PropertyChanged = pc.Publish
    new(bus, name, path, ipv: InterfacePropertyMap) = 
        new DBusWrapper<_>(bus, name, path, PropertyManager(bus, name, path, ipv))
    new(bus, name, path) = new DBusWrapper<_>(bus, name, path, InterfacePropertyMap())