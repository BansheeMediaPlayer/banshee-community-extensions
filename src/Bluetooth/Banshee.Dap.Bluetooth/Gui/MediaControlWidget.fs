//
// MediaControlWidget.fs
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

open Banshee.Dap.Bluetooth.Wrappers
open Gtk
open Hyena.Log
open Hyena.ThreadAssist

type MediaControlButton(img: Image, mc: INotifyMediaControl) as this =
    inherit ToggleButton(Image = img)
    let menu = new Menu()
    let button_icon x =
        let img = new Image(IconName = x)
        let item = new MenuItem (Child = img)
        item
    let play = button_icon "media-playback-start"
    let pause = button_icon "media-playback-pause"
    let stop = button_icon "media-playback-stop"
    let next = button_icon "media-skip-forward"
    let prev = button_icon "media-skip-backward"
    let fwd = button_icon "media-seek-forward"
    let rwd = button_icon "media-seek-backward"
    let vup = button_icon "audio-volume-high"
    let vdn = button_icon "audio-volume-low"
    let hidden _ = this.Active <- false
    do
       menu.Append prev
       menu.Append rwd
       menu.Append stop
       menu.Append pause
       menu.Append play
       menu.Append fwd
       menu.Append next
       menu.Append vup
       menu.Append vdn
       menu.ShowAll ()
       menu.Hidden.Add hidden

       play.Activated.Add (fun o -> mc.Play ())
       pause.Activated.Add (fun o -> mc.Pause ())
       stop.Activated.Add (fun o -> mc.Stop ())
       next.Activated.Add (fun o -> mc.Next ())
       prev.Activated.Add (fun o -> mc.Previous ())
       rwd.Activated.Add (fun o -> mc.Rewind ())
       fwd.Activated.Add (fun o -> mc.FastForward ())
       vup.Activated.Add (fun o -> mc.VolumeUp ())
       vdn.Activated.Add (fun o -> mc.VolumeDown ())
    override x.OnToggled () =
        if x.Active then
          menu.Popup ()
        else
          menu.Popdown ()
    new (mc) =
        let img = new Image(IconName = "media-playback-start", IconSize = int IconSize.LargeToolbar)
        new MediaControlButton (img, mc)
