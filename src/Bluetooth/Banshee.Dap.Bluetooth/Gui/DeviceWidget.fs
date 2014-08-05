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

open System
open Banshee.ServiceStack
open Banshee.Dap.Bluetooth
open Banshee.Dap.Bluetooth.Wrappers
open Banshee.Dap.Bluetooth.Devices
open Banshee.Dap.Bluetooth.Client
open Banshee.Dap.Bluetooth.DapGlue
open Banshee.Dap.Bluetooth.SupportApi
open Banshee.Dap.Bluetooth.Gui.SpinButtons
open Gtk
open Hyena.ThreadAssist

type DeviceWidget(dev: IBansheeDevice) =
    inherit VBox(false, 5, MarginLeft = 10, MarginRight = 10)
    static let ICON_SIZE = int IconSize.LargeToolbar
    static let PIXBUF_PREFS = "gnome-settings"
    static let PIXBUF_PAIR = Gdk.Pixbuf.LoadFromResource("paired-black.png")
    static let PIXBUF_SYNC = Gdk.Pixbuf.LoadFromResource("21cc-sync.png")
    static let PIXBUF_AI = "audio-input-microphone"
    static let PIXBUF_AO = "audio-speakers"
    static let PIXBUF_HS = "audio-headphones"
    static let PIXBUF_TIME = "preferences-system-time"
    static let toggle_icon x =
        new ToggleButton(Image = new Image(IconName = x, IconSize = ICON_SIZE))
    static let toggle_buffer (x: Gdk.Pixbuf) =
        new ToggleButton(Image = new Image(x))
    let dev_bt = dev.Device
    let mutable mcw = Unchecked.defaultof<_>
    let line1 = new HBox(false, 5)
    let line2 = new HBox(false, 0)
    let sbox = new HBox(false, 5)
    let icon = new Image(IconName = IconOf dev_bt, IconSize = ICON_SIZE)
    let label = new Label(UseMarkup = true)
    let ai = toggle_icon PIXBUF_AI
    let ao = toggle_icon PIXBUF_AO
    let hs = toggle_icon PIXBUF_HS
    let pair = toggle_buffer PIXBUF_PAIR
    let conn = toggle_buffer PIXBUF_SYNC
    let conf = toggle_icon PIXBUF_TIME
    let time = new TimeSpinButton()
    let has_ai () = dev.HasSupport Feature.AudioIn
    let has_ao () = dev.HasSupport Feature.AudioOut
    let has_hs () = dev.HasSupport Feature.Headset
    let has_sy () = dev.HasSupport Feature.Sync
    let act_ai () = dev.IsConnected Feature.AudioIn
    let act_ao () = dev.IsConnected Feature.AudioOut
    let act_hs () = dev.IsConnected Feature.Headset
    let act_sy () = dev.IsConnected Feature.Sync
    let act x y f = match (x, y()) with
                    | (true, false) -> dev.Connect f
                    | (false, true) -> dev.Disconnect f
                    | _ -> ()
    let markup () = if dev_bt.Connected then
                      sprintf "<b>%s</b>" dev_bt.Alias
                    else
                      dev_bt.Alias
    let fresh () =
        Block (fun () ->
            pair.Visible <- not dev_bt.Paired
            label.Markup <- markup()
            ai.Active <- act_ai()
            ao.Active <- act_ao()
            hs.Active <- act_hs()
            conn.Active <- act_sy()
            conf.Active <- dev.Config.Auto
            ai.Visible <- has_ai()
            ao.Visible <- has_ao()
            hs.Visible <- has_hs()
            sbox.Visible <- has_sy()
            time.Visible <- dev.Config.Auto
            time.Value <- float dev.Config.Time
            match (dev.MediaControl, IsNull mcw) with
            | (Some mc, true) -> mcw <- new MediaControlWidget(mc)
                                 line2.PackStart (mcw, true, true, 0u)
            | (None, false) -> line2.Remove mcw
                               mcw.Dispose ()
                               mcw <- Unchecked.defaultof<_>
            | _ -> ()
        )
    do  sbox.PackStart (time, false, false, 0u)
        sbox.PackStart (conf, false, false, 0u)
        sbox.PackStart (conn, false, false, 0u)
        line1.PackStart (pair, false, false, 0u)
        line1.PackStart (icon, false, false, 0u)
        line1.PackStart (label, false, false, 0u)
        line1.PackEnd (ai, false, false, 0u)
        line1.PackEnd (ao, false, false, 0u)
        line1.PackEnd (hs, false, false, 0u)
        line1.PackEnd (sbox, false, false, 0u)
        base.PackStart (line1, false, false, 0u)
        base.PackStart (line2, false, false, 0u)
        base.ShowAll ()
        fresh ()
        conf.Toggled.Add(fun o ->
            match (conf.Active, dev.Config.Auto) with
            | (x, y) when x <> y -> dev.Config.Auto <- x
            | _ -> ())
        time.ValueChanged.Add(fun o -> dev.Config.Time <- time.ValueAsInt)
        pair.Toggled.Add(fun o -> match (pair.Active, dev_bt.Paired) with
                                  | (true, false) -> dev_bt.Pair ()
                                  | _ -> ())
        ai.Toggled.Add(fun o -> act ai.Active act_ai Feature.AudioIn)
        ao.Toggled.Add(fun o -> act ao.Active act_ao Feature.AudioOut)
        hs.Toggled.Add(fun o -> act hs.Active act_hs Feature.Headset)
        conn.Toggled.Add(fun o -> act conn.Active act_sy Feature.Sync)
        dev.Notify.Add(fun o -> fresh ())
    member x.Refresh () = fresh ()
