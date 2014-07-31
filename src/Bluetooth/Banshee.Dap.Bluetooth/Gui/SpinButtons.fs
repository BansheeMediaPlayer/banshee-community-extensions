//
// SpinButtons.fs
//
// Author:
//   Nicholas Little <arealityfarbetween@googlemail.com>
//
// Copyright (c) 2014 Nicholas Little
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
module Banshee.Dap.Bluetooth.Gui.SpinButtons

open System
open System.Text.RegularExpressions
open Banshee.Dap.Bluetooth.AuxillaryApi
open Gtk
open Gdk

type FormatSpinButton(adjust: Adjustment, climb, digits, get, set) as this =
    inherit SpinButton(adjust, climb, digits)
    let TRUE = 1
    let FALSE = 0
    let GTK_INPUT_ERROR = -1
    do this.Input.Add(fun o -> match this.Text |> get with
                               | Some v -> o.NewValue <- v
                                           o.RetVal <- TRUE
                               | None -> o.NewValue <- this.Value
                                         o.RetVal <- GTK_INPUT_ERROR)
       this.Output.Add(fun o -> this.Text <- this.Value |> set
                                o.RetVal <- TRUE)

let rex = @"^(\d{1,2}):(\d{2})$"

let inline to_value (hr,mn) = float hr * 60.0 + float mn

let inline from_text text =
    match text with
    | Match rex [full;hr;mn] -> to_value (hr,mn) |> Some
    | _ -> None

let inline from_value v =
    let hr = uint32 v / 60u
    let mn = uint32 v % 60u
    (hr,mn)

let inline to_text (hr,mn) = sprintf "%d:%02d" hr mn

type TimeSpinButton(adjust) =
    inherit FormatSpinButton(adjust, 1.0, 0u,
                             from_text,
                             from_value >> to_text,
                             Alignment = 0.5f,
                             WidthChars = 5)
    new () = new TimeSpinButton(new Adjustment(0.0, 0.0, 1439.0, 1.0, 30.0, 0.0))
