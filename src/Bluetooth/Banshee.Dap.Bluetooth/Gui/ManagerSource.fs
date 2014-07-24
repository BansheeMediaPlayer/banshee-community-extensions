//
// ManagerSource.fs
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
namespace Banshee.Dap.Bluetooth.Gui

open System
open System.Collections.Generic
open Banshee.Sources
open Banshee.Sources.Gui
open Banshee.ServiceStack
open Banshee.Dap.Bluetooth
open Banshee.Dap.Bluetooth.Adapters
open Banshee.Dap.Bluetooth.Devices
open Banshee.Dap.Bluetooth.Client
open Banshee.Dap.Bluetooth.DapGlueApi
open Banshee.Dap.Bluetooth.InversionApi
open Banshee.Dap.Bluetooth.Wrappers
open Banshee.Gui
open DBus
open Gtk
open Hyena
open Hyena.Log

module Constants =
    let SORT = 410 // Puts us in "Devices"
    let NAME = "Banshee.Dap.Bluetooth.Gui"

type ManagerContents(s, am: AdapterManager, dm: DeviceManager, cm: ClientManager) =
    inherit VBox(false, 10)
    let act = new AdaptersWidget(am)
    let box = new VBox()
    let dwm = Dictionary<ObjectPath,DeviceWidget>()
    let add p = let dw = new DeviceWidget(BansheeDevice(p, am, dm, cm))
                dwm.[p] <- dw
                box.PackStart (dw, false, false, 10u)
    let rem p = use dw = dwm.[p]
                box.Remove dw
                dwm.Remove p |> ignore
    do
       base.PackStart (act, false, false, 10u)
       base.PackStart (box, true, true, 10u)
       base.ShowAll ()
       dm.DeviceEvent.Add (fun o -> match o.Action with
                                    | Added -> add o.Path
                                    | Changed -> if dwm.ContainsKey o.Path then
                                                   dwm.[o.Path].Refresh()
                                    | Removed -> rem o.Path)
       dm.TransportEvent.Add (fun o -> if dwm.ContainsKey o.Object.Device then
                                         dwm.[o.Object.Device].Refresh())
       dm.Devices |> Seq.iter (fun o -> add o)
    interface ISourceContents with
        member x.SetSource y = false
        member x.ResetSource () = ()
        member x.Widget with get () = x :> Widget
        member x.Source with get () = s

type ManagerSource(am: AdapterManager, dm: DeviceManager, cm: ClientManager) as this =
    inherit Source (Singular "Bluetooth Manager",
                    "Bluetooth Manager",
                    Constants.SORT,
                    "extension-unique-id")
    do Infof "Dap.Bluetooth: Initializing ManagerSource"
       base.Properties.SetStringList ("Icon.Name", "bluetooth")
       base.Properties.Set<ISourceContents> ("Nereid.SourceContents", new ManagerContents(this, am, dm, cm))
       base.Initialize ();

type ManagerService(name: string) =
    let mutable am : AdapterManager = Unchecked.defaultof<_>
    let mutable dm : DeviceManager = Unchecked.defaultof<_>
    let mutable cm : ClientManager = Unchecked.defaultof<_>
    let mutable ms : ManagerSource = Unchecked.defaultof<_>
    member x.Dispose () = ServiceManager.SourceManager.RemoveSource (ms)
    member x.ServiceName = name
    member x.Initialize () = am <- AdapterManager(Bus.System)
                             dm <- DeviceManager(Bus.System)
                             cm <- ClientManager(Bus.Session)
    member x.DelayedInitialize () = ms <- new ManagerSource(am, dm, cm)
                                    ServiceManager.SourceManager.AddSource (ms)
    interface IService with
        member x.ServiceName = x.ServiceName
    interface IDisposable with
        member x.Dispose () = x.Dispose ()
    interface IExtensionService with
        member x.Initialize () = x.Initialize ()
    interface IDelayedInitializeService with
        member x.DelayedInitialize () = x.DelayedInitialize ()
    new() = new ManagerService (Constants.NAME + ".ManagerService")
