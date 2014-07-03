//
// SongKickGeoLocationService.fs
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

namespace Banshee.SongKickGeoLocation

open System
open Banshee.Sources
open Banshee.Kernel
open Banshee.ServiceStack
open Banshee.NotificationArea
open Banshee.SongKick.Recommendations
open Banshee.SongKick.Search
open Banshee.SongKick
open Banshee.SongKick.LocationProvider
open Banshee.SongKickGeoLocation.UI
open Hyena

type Service() as this =
    let refresh_timeout = uint32 (1000 * 60 * 60 * 12) // 12 hours
    let banshee_window = Service.GetBansheeWindow ()
    let mutable local_events = new Results<Event> ()
    let mutable refresh_timeout_id = uint32 0
    let events_source = new LocalEventsSource (local_events)

    member x.Initialize () =
        LocationProviderManager.Register this
        ThreadAssist.SpawnFromMain (fun() ->
            refresh_timeout_id <- Application.RunTimeout (refresh_timeout, x.RefreshLocalConcertsList))

    member x.RefreshLocalConcertsList = new TimeoutHandler (fun () ->
        if not LocationProviderManager.HasProvider ||
           not (ServiceManager.Get<Banshee.Networking.Network> ()).Connected
        then true
        else
        Hyena.Log.Debug ("Refreshing list of local concerts")
        Scheduler.Schedule (new DelegateJob (fun () ->
            let search = new EventsByArtistSearch ()
            let recommendation_provider = new Banshee.SongKick.Search.RecommendationProvider ()
            let recommendations = recommendation_provider.getRecommendations ()
            for artist in recommendations do
                search.GetResultsPage (new Query(System.Nullable(), artist.Name))
                for res in search.ResultsPage.results do
                    if x.IsItInUserCity (res.Location.Latitude, res.Location.Longitude) && not (local_events.Contains res)
                    then res.ArtistName <- artist.Name
                         local_events.Add (res)
            x.NotifyUser()))
        true)

    member x.IsItInUserCity (lat : float, long : float) =
        abs (lat  - float LocationProviderManager.GetLatitude)  < 1.0 &&
        abs (long - float LocationProviderManager.GetLongitude) < 1.0

    member x.NotifyUser () =
        for src in ServiceManager.SourceManager.Sources do
            if (src :? SongKickSource && not (src.ContainsChildSource events_source))
            then src.AddChildSource (events_source)

        events_source.view.UpdateEvents local_events

        if (banshee_window : Gtk.Window).Focus.HasFocus
        then events_source.NotifyUser ()
        else banshee_window.Focus.FocusInEvent.AddHandler x.OnFocusInEvent

        for e in local_events do
            let notification = new Notifications.Notification ()
            notification.Body <- String.Format (
                    "{0} in {1} on {2} at {3}",
                    e.ArtistName,
                    LocationProviderManager.GetCityName,
                    e.StartDateTime.ToShortDateString(),
                    e.StartDateTime.ToShortTimeString())
            notification.Summary <- "SongKick found a gig near you!"
            notification.Icon <- Gdk.Pixbuf.LoadFromResource ("songkick_logo_300x300.png")
            notification.Show ()

    static member GetBansheeWindow () =
         Gtk.Window.ListToplevels () |> Array.find (fun w -> w :? Banshee.Gui.BaseClientWindow)

    member x.OnFocusInEvent =
        new Gtk.FocusInEventHandler (fun obj args ->
            events_source.NotifyUser ()
            banshee_window.Focus.FocusInEvent.RemoveHandler x.OnFocusInEvent)

    member x.Dispose ()  = Application.IdleTimeoutRemove (refresh_timeout_id)   |> ignore
    member x.ServiceName = Constants.NAME + ".Service"

    interface ICityNameObserver with
        member x.OnCityNameUpdated (cn : string) = x.RefreshLocalConcertsList.Invoke() |> ignore
    interface IService with
        member x.ServiceName = x.ServiceName
    interface IDisposable with
        member x.Dispose () = x.Dispose ()
    interface IExtensionService with
        member x.Initialize () = x.Initialize ()
