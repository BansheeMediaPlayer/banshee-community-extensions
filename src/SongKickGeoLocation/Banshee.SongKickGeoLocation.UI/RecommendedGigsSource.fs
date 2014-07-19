//
// RecommendedGigsSource.fs
//
// Author:
//   Dmitrii Petukhov <dimart.sp@gmail.com>
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

type public RecommendedGigsSource () as this =
    inherit Source(AddinManager.CurrentLocalizer.GetString ("Gigs, Recommended for You"),
                   AddinManager.CurrentLocalizer.GetString ("Gigs, Recommended for You"),
                   Constants.SORT_ORDER,
                   "SongKick-recommended-gigs")

    let view = new RecommendedGigsView (this)

    do
        base.Properties.SetStringList ("Icon.Name", "recommended_gigs_logo")
        base.Properties.SetString ("UnmapSourceActionLabel", Catalog.GetString ("Close Item"))
        base.Properties.SetString ("UnmapSourceActionIconName", "gtk-close")

        base.Properties.Set<ISourceContents> ("Nereid.SourceContents", view)
        Hyena.Log.Information ("SongKick CityConcerts source has been instantiated!")

    member x.View with get () = view

    interface IUnmapableSource with
        member x.CanUnmap = true
        member x.ConfirmBeforeUnmap = false
        member x.Unmap() =
            if (x :> Source) = ServiceManager.SourceManager.ActiveSource then
                ServiceManager.SourceManager.SetActiveSource (x.Parent)
            x.Parent.RemoveChildSource (x)
            true

and public RecommendedGigsView (s : RecommendedGigsSource) as this =
    inherit SearchBox<Event> (new EventsByLocationSearch ())

    do
        this.UpdateGigs ()

    member x.UpdateGigs(gigs : Results<Event>) =
            x.event_model.Clear ()
            for g in gigs do
                x.event_model.Add(g)
            x.UpdateGigs ()

    member private x.UpdateGigs() =
            x.SetModel (x.event_model)
            x.event_search_view.OnUpdated ()

    interface ISourceContents with
        member x.SetSource s = false 
        member x.ResetSource () = ()
        member x.Source with get() = s :> ISource
        member x.Widget with get() = x :> Widget

    override x.OnRowActivated (o : obj, args : Hyena.Data.Gui.RowActivatedArgs<Event>) =
            let musicEvent = args.RowValue
            System.Diagnostics.Process.Start (musicEvent.Uri) |> ignore
