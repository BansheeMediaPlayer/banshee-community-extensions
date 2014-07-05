//
// DeviceWidget.fs
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

open Banshee.ServiceStack
open Banshee.Dap.Bluetooth.Wrappers
open Banshee.Dap.Bluetooth.Client
open Banshee.Dap.Bluetooth.DapGlueApi
open Gtk

type DeviceWidget(dev: IBansheeDevice, cm: ClientManager) as this =
    inherit HBox(false, 4)
    static let PIXBUF_PREFS = "gnome-settings"
    static let PIXBUF_PAIR = Gdk.Pixbuf.LoadFromResource("paired-black.png")
    static let PIXBUF_SYNC = Gdk.Pixbuf.LoadFromResource("21cc-sync.png")
    static let PIXBUF_AI = "audio-input-microphone"
    static let PIXBUF_AO = "audio-speakers"
    static let PIXBUF_HS = "audio-headphones"
    let icon = new Image()
    let label = new Label(UseMarkup = true)
    let bbox = new HBox(true, 10)
    let ai = new ToggleButton(Image = new Image(IconName = PIXBUF_AI))
    let ao = new ToggleButton(Image = new Image(IconName = PIXBUF_AO))
    let hs = new ToggleButton(Image = new Image(IconName = PIXBUF_HS))
    let pair = new ToggleButton(Image = new Image(PIXBUF_PAIR))
    let conf = new ToggleButton(Image = new Image(PIXBUF_SYNC))
    let src = ref None
    let DeviceString () = if dev.Connected then
                            sprintf "<b>%s</b>" dev.Alias
                          else
                            sprintf "%s" dev.Alias
    do  base.PackStart (icon, false, false, 0u)
        base.PackStart (label, false, false, 0u)
        bbox.PackEnd (pair, false, false, 0u)
        bbox.PackEnd (conf, false, false, 0u)
        bbox.PackEnd (ai, false, false, 0u)
        bbox.PackEnd (ao, false, false, 0u)
        bbox.PackEnd (hs, false, false, 0u)
        base.PackEnd (bbox, false, false, 10u)
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
                                 label.Markup <- sprintf "%s - RSSI: %d" (DeviceString()) dev.RSSI
                                 ai.Visible <- dev.AudioOut
                                 ao.Visible <- dev.AudioIn
                                 hs.Visible <- dev.Headset
                                 conf.Visible <- dev.Sync
                                 pair.Visible <- not dev.Paired
