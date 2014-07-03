//
// InversionApi.fs
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
namespace Banshee.Dap.Bluetooth.InversionApi

open System
open System.Collections.Generic
open Banshee.Dap.Bluetooth.DBusApi
open Banshee.Dap.Bluetooth.SupportApi
open DBus

type ObjectAction = | Added | Changed | Removed

type ObjectChangedArgs(a: ObjectAction, obj: obj, path: ObjectPath) =
    inherit EventArgs()
    member x.Action = a
    member x.Object = obj
    member x.Path = path

type ObjectChangedHandler = delegate of obj * ObjectChangedArgs -> unit

type DBusInverter(bus: Bus, name: string, path: ObjectPath) =
    let oman = bus.GetObject<IObjectManager>(name, path)
    let obm = Dictionary<ObjectPath, IDBusWrapper>()
    let itm = Dictionary<string, Type>()
    let tfm = Dictionary<Type, Factory>()
    let eve = Event<ObjectChangedHandler,ObjectChangedArgs>()
    let add (o: ObjectPath) (ip: InterfacePropertyMap) =
        if not (obm.ContainsKey o) then
          obm.[o] <- DBusWrapper(bus, name, o, ip)
        let dw = obm.[o]
        ip |> Seq.iter (fun i -> if itm.ContainsKey i.Key then
                                   let t = itm.[i.Key]
                                   let wp = dw.Put t tfm.[t]
                                   match wp with
                                   | Some x -> eve.Trigger (dw, ObjectChangedArgs(Added, x, o))
                                   | _ -> printfn "Creation of %s as %s Failed" (t.ToString()) i.Key)
    let rem (o:ObjectPath) (is:string array) =
        if obm.ContainsKey o then
          let dw = obm.[o]
          is |> Seq.iter (fun i -> if itm.ContainsKey i then
                                     let wp = dw.Remove itm.[i]
                                     match wp with
                                     | Some x -> eve.Trigger (dw, ObjectChangedArgs(Removed, x, o))
                                     | None -> ())
    do oman.add_InterfacesAdded(fun o ip -> add o ip)
       oman.add_InterfacesRemoved(fun o is -> rem o is)
    member x.Register<'t> fac = let t = typeof<'t>
                                let i = Functions.InterfaceOf t
                                itm.[i] <- t
                                tfm.[t] <- fac
    member x.Refresh () = oman.GetManagedObjects() |> Seq.iter (fun oip -> add oip.Key oip.Value)
    member x.ResolveAll<'t> () = obm.Values |> Seq.choose (fun o -> o.Get<'t>())
    member x.ObjectChanged = eve.Publish
