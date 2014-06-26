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
open System.Text
open System.Collections.Generic
open Banshee.Sources
open Banshee.Sources.Gui
open Banshee.ServiceStack
open Banshee.Dap.Bluetooth
open Banshee.Dap.Bluetooth.AdapterApi
open Banshee.Dap.Bluetooth.DeviceApi
open Banshee.Dap.Bluetooth.SupportApi
open Banshee.Dap.Bluetooth.InversionApi
open Banshee.Dap.Bluetooth.Wrappers
open Banshee.Gui
open DBus
open Gtk
open Hyena
open Mono.Addins

module Functions =
    let inline IconOf< ^a when ^a : (member Icon : string)> (x: ^a) : string = 
        let icon = (^a : (member Icon : string) (x))
        if Functions.IsNull icon then "bluetooth"
        else icon
    let AdapterString adapters = 
        let sb = StringBuilder("<b>")
        adapters |> Seq.iteri (fun i (x: IBansheeAdapter) -> match i with
                                                             | 0 -> sb.Append (x.Alias) |> ignore
                                                             | _ -> sb.AppendFormat (", {0}", x.Alias) |> ignore)
        sb.Append("</b>").ToString()
    let BoxOf text image = let bx = new HBox(false, 5)
                           let local = AddinManager.CurrentLocalizer.GetString(text)
                           bx.PackStart(new Label(Text = local, UseMarkup = true), false, false, 0u)
                           bx.PackEnd(new Image(IconName = image), false, false, 0u)
                           bx.ShowAll ()
                           bx

module Constants =
    let SORT = 190 // Template Default: Puts us in "Online Media"
    let NAME = "Banshee.Dap.Bluetooth.Gui"

type AdapterControls(dm: DeviceManager) =
    inherit Bin()
    let box = new HBox(false, 4)
    let label = new Label(UseMarkup = true)
    let pow = new ToggleButton(Child = Functions.BoxOf "Power" "system-shutdown",
                               Active = false)
    let dis = new ToggleButton(Child = Functions.BoxOf "Discovery" "view-refresh",
                               Active = false)
    //let pow = new Switch(Active = false)
    //let dis = new Switch(Active = false)
    let pev = Event<_>()
    let dev = Event<_>()
    do box.PackStart (label, false, false, 0u)
       box.PackEnd (pow, false, false, 0u)
       //box.PackEnd (new Label("<b>Power</b>", UseMarkup = true), false, false, 0u)
       box.PackEnd (dis, false, false, 0u)
       //box.PackEnd (new Label("<b>Discovery</b>", UseMarkup = true), false, false, 0u)
       base.Add box
       base.ShowAll()
       label.Markup <- dm.Adapters |> Functions.AdapterString
       pow.Toggled.Add (fun o -> pev.Trigger())
       dis.Toggled.Add (fun o -> dev.Trigger())
       //pow.AddNotification ("active", fun o x -> pev.Trigger())
       //dis.AddNotification ("active", fun o x -> dev.Trigger())
    member x.Power with get () = pow.Active
                   and set v = pow.Active <- v
    member x.Discovery with get () = dis.Active
                       and set v = dis.Active <- v
    member x.Refresh () = label.Markup <- dm.Adapters |> Functions.AdapterString
    [<CLIEvent>]
    member x.PowerEvent = pev.Publish
    [<CLIEvent>]
    member x.DiscoverEvent = dev.Publish

type DeviceWidget(dev: IBansheeDevice) as this =
    inherit HBox(false, 4)
    static let PIXBUF_PREFS = "gnome-settings"
    static let PIXBUF_PAIR = Gdk.Pixbuf.LoadFromResource("paired-black.png")
    static let PIXBUF_AI = "audio-input-microphone"
    static let PIXBUF_AO = "audio-speakers"
    static let PIXBUF_HS = "audio-headphones"
    let icon = new Image()
    let label = new Label()
    let ai = new ToggleButton(Image = new Image(IconName = PIXBUF_AI))
    let ao = new ToggleButton(Image = new Image(IconName = PIXBUF_AO))
    let hs = new ToggleButton(Image = new Image(IconName = PIXBUF_HS))
    let pair = new ToggleButton(Image = new Image(PIXBUF_PAIR))
    let conf = new Button(new Image(IconName = PIXBUF_PREFS))
    do  base.PackStart (icon, false, false, 0u)
        base.PackStart (label, false, false, 0u)
        base.PackEnd (conf, false, false, 0u)
        base.PackEnd (pair, false, false, 0u)
        base.PackEnd (ai, false, false, 0u)
        base.PackEnd (ao, false, false, 0u)
        base.PackEnd (hs, false, false, 0u)
        base.ShowAll ()
        pair.Clicked.Add(fun o -> match (pair.Active, dev.Paired) with
                                  | (true, false) -> dev.Pair ()
                                  | _ -> ())
        ai.Clicked.Add(fun o -> match (ai.Active, dev.Connected) with
                                | (true, false) -> dev.ConnectProfile Constants.UUID_AUDIO_SOURCE
                                | (false, true) -> dev.DisconnectProfile Constants.UUID_AUDIO_SOURCE
                                | _ -> ())
        ao.Clicked.Add(fun o -> match (ao.Active, dev.Connected) with
                                | (true, false) -> dev.ConnectProfile Constants.UUID_AUDIO_SINK
                                | (false, true) -> dev.DisconnectProfile Constants.UUID_AUDIO_SINK
                                | _ -> ())
        this.Refresh ()
    member x.Refresh () : unit = icon.IconName <- Functions.IconOf dev
                                 label.Text <- sprintf "%s - RSSI: %d" dev.Alias dev.RSSI
                                 ai.Visible <- dev.AudioOut
                                 ao.Visible <- dev.AudioIn
                                 hs.Visible <- dev.Headset
                                 pair.Sensitive <- not dev.Paired
                                 pair.Active <- dev.Paired
                                 conf.Sensitive <- dev.Sync

type ManagerContents(s, dm: DeviceManager) as this =
    inherit VBox(false, 10)
    let act = new AdapterControls(dm)
    let box = new VBox()
    let awm = Dictionary<IBansheeAdapter,_>()
    let dwm = Dictionary<IBansheeDevice,DeviceWidget>()
    do 
       base.PackStart (act, false, false, 10u)
       base.PackStart (box, true, true, 10u)
       base.ShowAll ()

       act.PowerEvent.Add (fun o -> if dm.Powered <> act.Power then dm.Powered <- act.Power)
       act.DiscoverEvent.Add (fun o -> if dm.Discovering <> act.Discovery then dm.Discovering <- act.Discovery)

       dm.AdapterChanged.Add (fun o -> match o.Action with
                                       | Added -> this.Add o.Adapter
                                                  act.Refresh ()
                                       | Changed -> act.Power <- dm.Powered
                                                    act.Discovery <- dm.Discovering
                                                    act.Refresh ()
                                       | Removed -> this.Remove o.Adapter |> ignore
                                                    act.Refresh ())
       dm.DeviceChanged.Add (fun o -> match o.Action with
                                      | Added -> this.Add o.Device
                                      | Changed -> dwm.[o.Device].Refresh()
                                      | Removed -> this.Remove o.Device |> ignore)

       dm.Adapters |> Seq.iter (fun o -> this.Add o)
       dm.Devices |> Seq.iter (fun o -> this.Add o)
    member x.Add (o: obj) = match o with
                            | :? IBansheeAdapter as a -> awm.[a] <- null
                                                         act.Power <- act.Power || a.Powered
                                                         act.Discovery <- act.Discovery || a.Discovering
                            | :? IBansheeDevice as d -> dwm.[d] <- new DeviceWidget(d)
                                                        box.PackStart (dwm.[d], false, false, 10u)
                            | _ -> ()
    member x.Remove (o: obj) = match o with
                               | :? IBansheeAdapter as a -> awm.Remove a
                               | :? IBansheeDevice as d -> box.Remove dwm.[d]
                                                           dwm.Remove d
                               | _ -> false
    interface ISourceContents with
        member x.SetSource y = false
        member x.ResetSource () = ()
        member x.Widget with get () = x :> Widget
        member x.Source with get () = s

type ManagerSource(dm: DeviceManager) as this = 
    inherit Source (AddinManager.CurrentLocalizer.GetString ("Bluetooth Manager"),
                    AddinManager.CurrentLocalizer.GetString ("Bluetooth Manager"),
                    190,
                    "extension-unique-id")
    let name = Constants.NAME + ".ManagerSource"
    //let act = new ManagerActions(dm) // FIXME: ToggleButton/Switch ToolItem
    do 
        Log.DebugFormat ("Instantiating {0}", name)
        base.Properties.SetStringList ("Icon.Name", "bluetooth")  
        base.Properties.Set<ISourceContents> ("Nereid.SourceContents", new ManagerContents(this, dm))
    
type ManagerService(name: string) =
    do Log.DebugFormat ("Instantiating {0}", name)
    let dm = DeviceManager (Bus.System)
    let ms = ManagerSource (dm)
    interface IExtensionService with
        member x.Dispose () = Log.DebugFormat ("Disposing {0}", name)
                              ServiceManager.SourceManager.RemoveSource ms
        member x.ServiceName = name
        member x.Initialize () = Log.DebugFormat ("Initializing {0}", name)
                                 ServiceManager.SourceManager.AddSource ms
    new() = new ManagerService (Constants.NAME + ".ManagerService")
