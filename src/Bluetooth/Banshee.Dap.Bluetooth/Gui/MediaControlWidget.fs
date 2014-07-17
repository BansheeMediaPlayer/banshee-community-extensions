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

type MediaControlWidget(mc: INotifyMediaControl) as this =
    inherit HBox(false, 5)
    let play = new Button(Image = new Image(IconName = "media-playback-start"))
    let pause = new Button(Image = new Image(IconName = "media-playback-pause"))
    let stop = new Button(Image = new Image(IconName = "media-playback-stop"))
    let next = new Button(Image = new Image(IconName = "media-skip-forward"))
    let prev = new Button(Image = new Image(IconName = "media-skip-backward"))
    let fwd = new Button(Image = new Image(IconName = "media-seek-forward"))
    let rwd = new Button(Image = new Image(IconName = "media-seek-backward"))
    let vup = new Button(Image = new Image(IconName = "audio-volume-high"))
    let vdn = new Button(Image = new Image(IconName = "audio-volume-low"))
    do base.PackStart (prev, false, false, 0u)
       base.PackStart (rwd, false, false, 0u)
       base.PackStart (stop, false, false, 0u)
       base.PackStart (pause, false, false, 0u)
       base.PackStart (play, false, false, 0u)
       base.PackStart (fwd, false, false, 0u)
       base.PackStart (next, false, false, 0u)
       base.PackEnd (vup, false, false, 0u)
       base.PackEnd (vdn, false, false, 0u)
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
    member x.Refresh () = base.Visible <- mc.Connected
