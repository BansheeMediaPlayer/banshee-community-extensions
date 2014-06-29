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
open Banshee.Dap
open Banshee.Dap.Bluetooth
open Banshee.Dap.Bluetooth.AdapterApi
open Banshee.Dap.Bluetooth.Client
open Banshee.Dap.Bluetooth.DBusApi
open Banshee.Dap.Bluetooth.DeviceApi
open Banshee.Dap.Bluetooth.MediaApi
open Banshee.Dap.Bluetooth.SupportApi
open Banshee.Dap.Bluetooth.InversionApi
open Banshee.Dap.Bluetooth.Wrappers
open Banshee.Dap.Bluetooth.DapGlueApi
open Banshee.Gui
open DBus
open Gtk
open Hyena
open Mono.Addins

module Functions =
    let AdapterString adapters =
        match Seq.isEmpty adapters with
        | true -> AddinManager.CurrentLocalizer.GetString("No Bluetooth Adapters")
        | false -> let sb = StringBuilder("<b>")
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
    let SORT = 410 // Puts us in "Devices"
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
                          let ops = dm.Adapters |> Seq.isEmpty |> not
                          pow.Sensitive <- ops
                          dis.Sensitive <- ops
    [<CLIEvent>]
    member x.PowerEvent = pev.Publish
    [<CLIEvent>]
    member x.DiscoverEvent = dev.Publish

type DeviceWidget(dev: IBansheeDevice, cm: ClientManager) as this =
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
    let conf = new ToggleButton("\u21cc")
    let src = ref None
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
        conf.Clicked.Add(fun o -> match (conf.Active, !src) with
                                  | (true, None) -> let ftp = cm.CreateSession dev.Address Ftp
                                                    let d = new BluetoothDevice(dev)
                                                    let s = new BluetoothSource(d, ftp)
                                                    s.LoadDeviceContents ()
                                                    src := Some s
                                                    ServiceManager.SourceManager.AddSource (s)
                                  | (false, Some x) -> ServiceManager.SourceManager.RemoveSource (x)
                                                       src := None
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

type MediaControlWidget(mc: IBansheeMediaControl) as this =
    inherit VBox(false, 2)
    let lbl = new Label(UseMarkup = true)
    let ctrls = new HBox(false, 2)
    let play = new Button(Image = new Image(IconName = "media-playback-start"))
    let pause = new Button(Image = new Image(IconName = "media-playback-pause"))
    let stop = new Button(Image = new Image(IconName = "media-playback-stop"))
    let next = new Button(Image = new Image(IconName = "media-skip-forward"))
    let prev = new Button(Image = new Image(IconName = "media-skip-backward"))
    let fwd = new Button(Image = new Image(IconName = "media-seek-forward"))
    let rwd = new Button(Image = new Image(IconName = "media-seek-backward"))
    let vup = new Button(Image = new Image(IconName = "audio-volume-high"))
    let vdn = new Button(Image = new Image(IconName = "audio-volume-low"))
    do ctrls.PackStart (prev, false, false, 0u)
       ctrls.PackStart (rwd, false, false, 0u)
       ctrls.PackStart (stop, false, false, 0u)
       ctrls.PackStart (pause, false, false, 0u)
       ctrls.PackStart (play, false, false, 0u)
       ctrls.PackStart (fwd, false, false, 0u)
       ctrls.PackStart (next, false, false, 0u)
       ctrls.PackEnd (vup, false, false, 0u)
       ctrls.PackEnd (vdn, false, false, 0u)
       base.PackStart (lbl, false, false, 10u)
       base.PackEnd (ctrls, false, false, 5u)
       base.ShowAll ()
       this.Refresh ()

       play.Clicked.Add (fun o -> mc.Play ())
       pause.Clicked.Add (fun o -> mc.Pause ())
       stop.Clicked.Add (fun o -> mc.Stop ())
       next.Clicked.Add (fun o -> mc.Next ())
       prev.Clicked.Add (fun o -> mc.Previous ())
       rwd.Clicked.Add (fun o -> mc.Rewind ())
       fwd.Clicked.Add (fun o -> mc.FastForward ())
       vup.Clicked.Add (fun o -> mc.VolumeUp ())
       vdn.Clicked.Add (fun o -> mc.VolumeDown ())
       mc.PropertyChanged.Add(fun o -> this.Refresh ())
    member x.Refresh () = match (mc.Connected, lbl.Text) with
                          | (true, "<b>Ready</b>") -> ()
                          | (false, "<b>Not Connected</b>") -> ()
                          | (y, _) -> if y then lbl.Markup <- "<b>Ready</b>"
                                      else lbl.Markup <- "<b>Not Connected</b>"

type ManagerContents(s, dm: DeviceManager, cm: ClientManager) as this =
    inherit VBox(false, 10)
    let act = new AdapterControls(dm)
    let box = new VBox()
    let awm = Dictionary<IBansheeAdapter,_>()
    let dwm = Dictionary<IBansheeDevice,DeviceWidget>()
    let mwm = Dictionary<IMediaControl,MediaControlWidget>()
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
       dm.MediaControlChanged.Add (fun o -> match o.Action with
                                            | Added -> this.Add o.MediaControl
                                            | Changed -> mwm.[o.MediaControl].Refresh()
                                            | Removed -> this.Remove o.MediaControl |> ignore)

       dm.Adapters |> Seq.iter (fun o -> this.Add o)
       dm.Devices |> Seq.iter (fun o -> this.Add o)
       dm.MediaControls |> Seq.iter (fun o -> this.Add o)
    member x.Add (o: obj) = match o with
                            | :? IBansheeAdapter as a -> awm.[a] <- null
                                                         act.Power <- act.Power || a.Powered
                                                         act.Discovery <- act.Discovery || a.Discovering
                            | :? IBansheeDevice as d -> dwm.[d] <- new DeviceWidget(d, cm)
                                                        box.PackStart (dwm.[d], false, false, 10u)
                            //| :? IBansheeMediaControl as m -> mwm.[m] <- new MediaControlWidget(m)
                            //                                  box.PackStart (mwm.[m], false, false, 10u)
                            | _ -> ()
    member x.Remove (o: obj) = match o with
                               | :? IBansheeAdapter as a -> awm.Remove a
                               | :? IBansheeDevice as d -> box.Remove dwm.[d]
                                                           dwm.Remove d
                               | :? IBansheeMediaControl as m -> box.Remove mwm.[m]
                                                                 mwm.Remove m
                               | _ -> false
    interface ISourceContents with
        member x.SetSource y = false
        member x.ResetSource () = ()
        member x.Widget with get () = x :> Widget
        member x.Source with get () = s

type ManagerSource(dm: DeviceManager, cm: ClientManager) as this =
    inherit Source (AddinManager.CurrentLocalizer.GetString ("Bluetooth Manager"),
                    "Bluetooth Manager",
                    Constants.SORT,
                    "extension-unique-id")
    //let act = new ManagerActions(dm) // FIXME: ToggleButton/Switch ToolItem
    do printfn "Initializing ManagerSource"
       base.Properties.SetStringList ("Icon.Name", "bluetooth")
       base.Properties.Set<ISourceContents> ("Nereid.SourceContents", new ManagerContents(this, dm, cm))
       base.Initialize ();

type ManagerService(name: string) =
    do Log.DebugFormat ("Instantiating {0}", name)
    let mutable dm : DeviceManager = Unchecked.defaultof<_>
    let mutable cm : ClientManager = Unchecked.defaultof<_>
    member x.DeviceManager = dm
    member x.Dispose () = ()
    member x.ServiceName = name
    member x.Initialize () = dm <- DeviceManager(Bus.System)
                             cm <- ClientManager(Bus.Session)
    member x.DelayedInitialize () = ServiceManager.SourceManager.AddSource (new ManagerSource(dm, cm))
    interface IService with
        member x.ServiceName = x.ServiceName
    interface IDisposable with
        member x.Dispose () = x.Dispose ()
    interface IExtensionService with
        member x.Initialize () = x.Initialize ()
    interface IDelayedInitializeService with
        member x.DelayedInitialize () = x.DelayedInitialize ()
    new() = new ManagerService (Constants.NAME + ".ManagerService")
