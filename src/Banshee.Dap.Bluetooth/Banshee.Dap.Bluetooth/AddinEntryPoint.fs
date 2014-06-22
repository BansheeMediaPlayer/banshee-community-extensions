//
// BluetoothDap.fs
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
namespace Banshee.Dap.Bluetooth.Ui

open System
open System.Collections.Generic
open System.Linq

open Banshee.Collection
open Banshee.Dap
open Banshee.Dap.Bluetooth
open Banshee.Dap.Bluetooth.Mime
open Banshee.Dap.Bluetooth.Mime.Extensions
open Banshee.ServiceStack
open Banshee.Sources

open DBus

open Hyena
open Hyena.Data

open Mono.Addins

module Functions = 
    let IsPodcast (x: TrackInfo) = 
        "Podcast".Equals(x.Genre, StringComparison.InvariantCultureIgnoreCase)

type Id = uint32

type IPlaylistInfo =
    abstract Id : Id with get
    abstract Name : string with get
    abstract Tracks : IEnumerable<TrackInfo> with get

type MediaAction = | Added | Removed | Changed

[<AbstractClass>]
type MediaChangedArgs(a: MediaAction) =
    inherit EventArgs()
    member x.Action with get () = a

type PlaylistChangedArgs(a: MediaAction, p: IPlaylistInfo) =
    inherit MediaChangedArgs(a)
    member x.Playlist with get () = p

type TrackChangedArgs(a: MediaAction, t: TrackInfo) = 
    inherit MediaChangedArgs(a)
    member x.Track with get () = t

type TrackChangedHandler = delegate of obj * TrackChangedArgs -> unit
type PlaylistChangedHandler = delegate of obj * PlaylistChangedArgs -> unit

type IDapDevice =
    abstract Support : MediaType array with get

    abstract Music : IEnumerable<TrackInfo> with get
    abstract Videos : IEnumerable<TrackInfo> with get
    abstract Podcasts : IEnumerable<TrackInfo> with get
    abstract Playlists: IEnumerable<IPlaylistInfo> with get

    abstract Load : unit -> unit
    abstract AddTrack : TrackInfo -> unit
    abstract RemoveTrack : TrackInfo -> unit
    abstract AddPlaylist : IPlaylistInfo -> unit
    abstract RemovePlaylist : IPlaylistInfo -> unit

    [<CLIEvent>]
    abstract TrackChanged : IEvent<TrackChangedHandler,TrackChangedArgs>
    [<CLIEvent>]
    abstract PlaylistChanged : IEvent<PlaylistChangedHandler,PlaylistChangedArgs>

[<AbstractClass>]
type AbstractDevice() =
    let tc = Event<_,_>()
    let pc = Event<_,_>()
    let ti = SortedSet<TrackInfo>() :> ISet<_>
    let pi = SortedSet<IPlaylistInfo>() :> ISet<_>
    abstract cap : unit -> MediaType array
    abstract pull : unit -> unit
    abstract push : TrackInfo -> bool
    abstract pop : TrackInfo -> bool
    abstract push : IPlaylistInfo -> bool
    abstract pop : IPlaylistInfo -> bool
    interface IDapDevice with 
        member x.Support with get () = x.cap ()
        member x.Load () = x.pull()
        member x.Music with get () = ti.Where(fun (y: TrackInfo) -> IsAudio (ToMimeType y.MimeType))
        member x.Videos with get () = ti.Where(fun (y: TrackInfo) -> IsVideo (ToMimeType y.MimeType))
        member x.Podcasts with get () = ti.Where(fun y -> Functions.IsPodcast y)
        member x.Playlists with get () = pi :> IEnumerable<_>
        member x.AddTrack z = if not (ti.Contains z) && x.push z then 
                                ti.Add z |> ignore
                                let tca = new TrackChangedArgs(MediaAction.Added, z) in
                                  tc.Trigger(x, tca)
        member x.AddPlaylist y = if not (pi.Contains y) && x.push y then 
                                   pi.Add y |> ignore
                                   let pca = new PlaylistChangedArgs(MediaAction.Added, y) in
                                     pc.Trigger(x, pca)
        member x.RemoveTrack z = if x.pop z && ti.Remove z then
                                   let tca = new TrackChangedArgs(MediaAction.Removed, z) in
                                     tc.Trigger(x, tca)
        member x.RemovePlaylist y = if x.pop y && pi.Remove y then 
                                      pc.Trigger(x, new PlaylistChangedArgs(MediaAction.Removed, y))
        [<CLIEvent>]
        member x.TrackChanged = tc.Publish
        [<CLIEvent>]
        member x.PlaylistChanged = pc.Publish