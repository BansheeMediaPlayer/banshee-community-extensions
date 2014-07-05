//
// AdaptersWidget.fs
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

open System.Text
open Banshee.Dap.Bluetooth
open Banshee.Dap.Bluetooth.Wrappers
open Gtk
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

type AdaptersWidget(dm: DeviceManager) =
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
