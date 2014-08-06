//
// MediaControlButton.fs
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
open Gtk.Extensions
open Hyena.Log
open Hyena.ThreadAssist

type MediaControlButton (img: Image, mc: INotifyMediaControl) as this =
    inherit ToggleButton (Image = img)
    let menu = new VBox (true, 1)
    let pop = new Popover (this)
    let item x = new Button (Image = x)
    let button_icon x = new Image (IconName = x) |> item
    let hboxof xs =
        let box = new HBox (true, 1)
        xs |> Seq.iter (fun x -> box.PackStart (x, true, true, 0u))
        box
    let pack x = menu.PackStart (x, false, false, 0u)
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
       hboxof [prev;next] |> pack
       hboxof [rwd;fwd] |> pack
       hboxof [pause;stop;play] |> pack
       hboxof [vup;vdn] |> pack
       menu.ShowAll ()
       pop.Add menu
       pop.Hidden.Add hidden

       play.Clicked.Add (fun o -> mc.Play ())
       pause.Clicked.Add (fun o -> mc.Pause ())
       stop.Clicked.Add (fun o -> mc.Stop ())
       next.Clicked.Add (fun o -> mc.Next ())
       prev.Clicked.Add (fun o -> mc.Previous ())
       rwd.Clicked.Add (fun o -> mc.Rewind ())
       fwd.Clicked.Add (fun o -> mc.FastForward ())
       vup.Clicked.Add (fun o -> mc.VolumeUp ())
       vdn.Clicked.Add (fun o -> mc.VolumeDown ())
    override x.OnToggled () = if x.Active then pop.Show ()
    new (mc) =
        let icon = "applications-multimedia"
        let size = int IconSize.LargeToolbar
        let img = new Image (IconName = icon, IconSize = size)
        new MediaControlButton (img, mc)
