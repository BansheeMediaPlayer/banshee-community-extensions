//
// GnomeApi.fs
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
module Banshee.Dap.Bluetooth.GnomeApi

open DBus

[<Literal>]
let IF_RFKILL = "org.gnome.SettingsDaemon.Rfkill"

let NAME_GNOME = "org.gnome.SettingsDaemon"

let PATH_GNOME_RFKILL = "/org/gnome/SettingsDaemon/Rfkill"

[<Interface (IF_RFKILL)>]
type IRfkill =
    abstract AirplaneMode : bool with get, set
    abstract HasAirplaneMode : bool
    abstract HardwareAirplaneMode : bool
    abstract BluetoothAirplaneMode : bool with get, set
    abstract BluetoothHasAirplaneMode : bool
    abstract BluetoothHardwareAirplaneMode : bool
