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
open Banshee.Dap.Bluetooth.AdapterApi
open Banshee.Dap.Bluetooth.Wrappers
open Banshee.Dap.Bluetooth.Adapters
open Gtk
open Mono.Addins

module Functions =
    let AdapterString (adapters: seq<#IAdapter>) =
        match Seq.isEmpty adapters with
        | true -> AddinManager.CurrentLocalizer.GetString("No Bluetooth Adapters")
        | false -> let sb = StringBuilder("<b>")
                   adapters |> Seq.iteri (fun i x ->
                     match i with
                     | 0 -> sb.Append (x.Alias) |> ignore
                     | _ -> sb.AppendFormat (", {0}", x.Alias) |> ignore)
                   sb.Append("</b>").ToString()
    let BoxOf text image = let bx = new HBox(false, 5)
                           let local = AddinManager.CurrentLocalizer.GetString(text)
                           bx.PackStart(new Label(Text = local, UseMarkup = true), false, false, 0u)
                           bx.PackEnd(new Image(IconName = image), false, false, 0u)
                           bx.ShowAll ()
                           bx

type AdaptersWidget(am: AdapterManager) =
    inherit HBox(false, 4)
    let label = new Label(UseMarkup = true)
    let pow = new ToggleButton(Child = Functions.BoxOf "Power" "system-shutdown",
                               Active = false)
    let dis = new ToggleButton(Child = Functions.BoxOf "Discovery" "view-refresh",
                               Active = false)
    let ref () = label.Markup <- am.Adapters |> Seq.choose (fun k -> am.Adapter k) |> Functions.AdapterString
                 let ops = am.Adapters |> Seq.isEmpty |> not
                 pow.Active <- am.Powered
                 dis.Active <- am.Discovering
                 pow.Sensitive <- ops
                 dis.Sensitive <- ops
    do base.PackStart (label, false, false, 0u)
       base.PackEnd (pow, false, false, 0u)
       base.PackEnd (dis, false, false, 0u)
       base.ShowAll()
       pow.Toggled.Add (fun o -> if am.Powered <> pow.Active then am.Powered <- pow.Active)
       dis.Toggled.Add (fun o -> if am.Discovering <> dis.Active then am.Discovering <- dis.Active)
       am.Notify.Add(fun o -> ref ())
       ref ()
    member x.Power with get () = pow.Active
    member x.Discovery with get () = dis.Active
