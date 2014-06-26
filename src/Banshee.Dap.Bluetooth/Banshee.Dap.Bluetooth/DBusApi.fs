//
// DBusApi.fs
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
namespace Banshee.Dap.Bluetooth.DBusApi

open System
open System.Collections.Generic
open DBus

type Functions =
    static member InterfaceOf (x: Type) =
        try
          let attr = Attribute.GetCustomAttribute (x, typeof<InterfaceAttribute>) :?> InterfaceAttribute
          attr.Name
        with
        | _ -> x.FullName
    static member InterfaceOf<'t> () = Functions.InterfaceOf typeof<'t>


module Constants =
    [<Literal>]
    let IF_DBUS_PROPERTIES = "org.freedesktop.DBus.Properties"
    [<Literal>]
    let IF_DBUS_OBJ_MANAGER = "org.freedesktop.DBus.ObjectManager"

type StringVariantMap = Dictionary<string,obj>
type InterfacePropertyMap = Dictionary<string,StringVariantMap>
type ObjectInterfacePropertyMap = Dictionary<ObjectPath,InterfacePropertyMap>

type PropertiesChangedHandler = delegate of string * StringVariantMap * string[] -> unit

[<Interface (Constants.IF_DBUS_PROPERTIES)>]
type IProperties =
    abstract member Get : string -> string -> obj 
    abstract member Set : string -> string -> obj -> unit
    abstract member GetAll : string -> StringVariantMap
    [<CLIEvent>]
    abstract member PropertiesChanged : IDelegateEvent<PropertiesChangedHandler>

type InterfacesAddedHandler = delegate of ObjectPath * InterfacePropertyMap -> unit
type InterfacesRemovedHandler = delegate of ObjectPath * string[] -> unit

[<Interface (Constants.IF_DBUS_OBJ_MANAGER)>]
type IObjectManager =
    abstract member GetManagedObjects : unit -> ObjectInterfacePropertyMap
    [<CLIEvent>]
    abstract member InterfacesAdded : IDelegateEvent<InterfacesAddedHandler> 
    [<CLIEvent>]
    abstract member InterfacesRemoved : IDelegateEvent<InterfacesRemovedHandler>
