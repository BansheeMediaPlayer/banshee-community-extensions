//
// LocalEventsSource.fs
//
// Author:
//   dimart.sp@gmail.com <Dmitrii Petukhov>
//
// Copyright (c) 2014 Dmitrii Petukhov
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

namespace Banshee.SongKickGeoLocation.UI

open System

open Gtk

open Mono.Addins
open Mono.Unix

open Banshee.Sources
open Banshee.Sources.Gui
open Banshee.ServiceStack

open Banshee.SongKick.Recommendations

open Banshee.Sources
open Banshee.Sources.Gui

open Banshee.SongKick.Recommendations
open Banshee.SongKick.Search
open Banshee.SongKick.UI

module Constants =
    let SORT_ORDER = 195

module Functions =
    let inline isNull< ^a when ^a : not struct> (x:^a) =
        obj.ReferenceEquals (x, Unchecked.defaultof<_>)

type public LocalEventsSource(events : Results<Event>) as this =
    inherit Source(AddinManager.CurrentLocalizer.GetString ("City Concerts"),
                   AddinManager.CurrentLocalizer.GetString ("City Concerts"),
                   Constants.SORT_ORDER,
                   "SongKick-city-concerts")

    [<DefaultValue>] val mutable Events : Results<Event>
    [<DefaultValue>] val mutable view : LocalEventsView

    do
        this.Events <- events
        base.Properties.SetStringList ("Icon.Name", "city_concerts_logo")
        base.Properties.SetString ("UnmapSourceActionLabel", Catalog.GetString ("Close Item"))
        base.Properties.SetString ("UnmapSourceActionIconName", "gtk-close")

        this.view <- new LocalEventsView(this,events)
        base.Properties.Set<ISourceContents> ("Nereid.SourceContents", this.view)
        Hyena.Log.Information ("SongKick CityConcerts source has been instantiated!")
    interface IUnmapableSource with
        member x.CanUnmap = true
        member x.ConfirmBeforeUnmap = false
        member x.Unmap() =
            if (this :> Source) = ServiceManager.SourceManager.ActiveSource
            then ServiceManager.SourceManager.SetActiveSource (this.Parent)
            this.Parent.RemoveChildSource (this)
            true

and public LocalEventsView(source : LocalEventsSource, events : Results<Event>) as this =
    inherit SearchBox<Event>(new EventsByLocationSearch())

    [<DefaultValue>] val mutable source : LocalEventsSource
    [<DefaultValue>] val mutable events : Results<Event>

    do
        this.source <- source
        this.events <- events
        this.UpdateEvents ()

    member x.UpdateEvents(events : Results<Event>) =
            this.events <- events
            this.UpdateEvents ()

    member x.UpdateEvents() =
            for e in events do
                this.event_model.Add(e)

            base.SetModel (this.event_model)
            this.event_search_view.OnUpdated ()

    interface ISourceContents with
        member x.SetSource (source : ISource) =
            this.source <- source :?> LocalEventsSource
            Functions.isNull this.source
        member x.ResetSource () = ()
        member x.Source with get() = this.source :> ISource
        member x.Widget with get() = this :> Widget

    override x.OnRowActivated (o : obj, args : Hyena.Data.Gui.RowActivatedArgs<Event>) =
            let musicEvent = args.RowValue
            System.Diagnostics.Process.Start (musicEvent.Uri) |> ignore
