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
open System.Linq

open Banshee.Sources
open Banshee.Kernel
open Banshee.ServiceStack
open Banshee.Configuration

open Banshee.SongKick.Recommendations
open Banshee.SongKick.Search
open Banshee.SongKick

open Banshee.SongKick.LocationProvider
open Banshee.SongKickGeoLocation.UI

open Hyena

open Mono.Addins

type Service () as this =
    let default_refresh_time = TimeSpan.FromHours (12.0)
    let delay_refresh_time   = TimeSpan.FromMinutes (15.0)
    let config_variable_name = "last_recommended_gigs_scan"
    let banshee_window = Service.GetBansheeWindow ()

    let mutable refresh_timeout_id = uint32 0

    static let gigs_source = new RecommendedGigsSource ()

    let extension_event_handler =
        Mono.Addins.ExtensionEventHandler (fun s a ->
            let addinEngine = s :?> AddinEngine
            if (addinEngine <> null && addinEngine.CurrentAddin.Id = this.ExtensionId) then
                let addins = AddinManager.Registry.GetAddins ()
                let isEnabled = addins.Any (fun (a) -> a.LocalId = this.ExtensionId && a.Enabled)

                if (not isEnabled) then
                    Log.Debug ("SongKick extension is being disabled, performing cleanup")
                    this.OnDisabled ())

    member x.Initialize () =
        LocationProviderManager.Register this
        ServiceManager.Get<Banshee.Networking.Network>().StateChanged.AddHandler x.OnNetworkStateChanged
        AddinManager.ExtensionChanged.AddHandler extension_event_handler

    member private x.GetRecommededGigs () =
        let gigs = new Results<Event> ()
        let search = new EventsByArtistSearch ()
        let recommendation_provider = new Banshee.SongKick.Search.RecommendationProvider ()
        let recommendations = recommendation_provider.GetRecommendations ()
        for artist in recommendations do
            if (not (String.IsNullOrEmpty (artist.Name))) then
                search.GetResultsPage (new Query (System.Nullable (), artist.Name))
                for res in search.ResultsPage.results do
                    if x.IsItInUserCity (res.Location.Latitude, res.Location.Longitude) then
                       res.ArtistName <- artist.Name
                       gigs.Add (res)
        gigs

    member private x.RefreshRecommededGigs = new TimeoutHandler (fun () ->
        match
           (LocationProviderManager.HasProvider,
            (ServiceManager.Get<Banshee.Networking.Network> ()).Connected,
            x.IsItTimeToRefresh ()) with
        | (true, true, true) ->
            Log.Debug ("Refreshing list of local gigs")
            Scheduler.Schedule (new DelegateJob (fun () ->
                let gigs = x.GetRecommededGigs ()
                x.NotifyUser(gigs)
                DatabaseConfigurationClient.Client.Set<DateTime> (config_variable_name, DateTime.Now)
                x.UpdateRunTimeout ()))
            true
         | (_, false, true) -> // if it's time to refresh gigs but there is no internet connection
                               // then we delay refresh for "delay_refresh_time"
                               // but it will be ok if internet connection appears earlier! :)
                               x.UpdateRunTimeout (delay_refresh_time)
                               true
         | _ -> true)

    member private x.GetLastUpdateTime () =
        try
            DatabaseConfigurationClient.Client.Get<DateTime> (config_variable_name, DateTime.MinValue)
        with
            | :? FormatException as e ->
                    Log.Warning (String.Format ("{0} is malformed, resetting to default value", config_variable_name))
                    DatabaseConfigurationClient.Client.Set<DateTime> (config_variable_name, DateTime.MinValue)
                    DateTime.MinValue

    member private x.IsItTimeToRefresh () =
        let last_update = x.GetLastUpdateTime ()
        let past = DateTime.UtcNow.Subtract (last_update.ToUniversalTime())
        past > default_refresh_time

    member private x.GetNextRetryTime () =
        let last_update = x.GetLastUpdateTime ()
        if last_update = DateTime.MinValue then
            // if this is the first time when user turn on GeoLocation
            // or saved time was in incorrect format
            // then we should try to update gigs right now
            // we also give back delay to refresh gigs
            x.RefreshRecommededGigs.Invoke () |> ignore
            delay_refresh_time
        else
            let past = DateTime.UtcNow.Subtract (last_update.ToUniversalTime())
            if past > default_refresh_time then
                // if too much time has passed since last Banshee's start
                // then we should try to update gigs right now
                // we also give back delay to refresh gigs
                x.RefreshRecommededGigs.Invoke () |> ignore
                delay_refresh_time
            else
                default_refresh_time.Subtract (past)

    member private x.UpdateRunTimeout (retryTime : TimeSpan) =
        let t = (uint32) retryTime.TotalMilliseconds
        Application.IdleTimeoutRemove (refresh_timeout_id) |> ignore
        refresh_timeout_id <- Application.RunTimeout (t, x.RefreshRecommededGigs)
        Log.Information ("Recommended gigs list will be updated in " + retryTime.Duration().ToString())

    member private x.UpdateRunTimeout () =
        let retryTime = x.GetNextRetryTime ()
        x.UpdateRunTimeout (retryTime)

    member private x.IsItInUserCity (lat : float, long : float) =
        abs (lat  - LocationProviderManager.GetLatitude)  < 1.0 &&
        abs (long - LocationProviderManager.GetLongitude) < 1.0

    member private x.NotifyUser (gigs : Results<Event>) =
        if gigs.Count = 0 then
            ThreadAssist.ProxyToMain (fun () ->
                let notification = new Notifications.Notification ("SongKick can't find a gig near you!",
                                     "Maybe it's time to take a trip? Check out where to go in Banshee!")
                notification.Icon <- Gdk.Pixbuf.LoadFromResource ("songkick_logo_300x300.png")
                notification.Show ())
        else
            for src in ServiceManager.SourceManager.Sources do
                if (src :? SongKickSource && not (src.ContainsChildSource gigs_source)) then
                    src.AddChildSource (gigs_source)

            gigs_source.View.UpdateGigs (gigs)

            if (banshee_window : Gtk.Window).Focus.HasFocus then
                gigs_source.NotifyUser ()
            else
                banshee_window.Focus.FocusInEvent.AddHandler x.OnFocusInEvent

            if gigs.Count > 3 then
                ThreadAssist.ProxyToMain (fun () ->
                    let notification = new Notifications.Notification ("SongKick found so many gigs near you!",
                                         "Check out all of them in Banshee!")
                    notification.Icon <- Gdk.Pixbuf.LoadFromResource ("songkick_logo_300x300.png")
                    notification.Show ())
            else
                for g in gigs do
                    ThreadAssist.ProxyToMain (fun () ->
                        let notification = new Notifications.Notification ()
                        notification.Body <- String.Format (
                                "{0} in {1} on {2} at {3}",
                                g.ArtistName,
                                LocationProviderManager.GetCityName,
                                g.StartDateTime.ToShortDateString(),
                                g.StartDateTime.ToShortTimeString())
                        notification.Summary <- "SongKick found a gig near you!"
                        notification.Icon <- Gdk.Pixbuf.LoadFromResource ("songkick_logo_300x300.png")
                        notification.Show ())

    static member private GetBansheeWindow () =
         Gtk.Window.ListToplevels () |> Array.find (fun w -> w :? Banshee.Gui.BaseClientWindow)

    member private x.OnFocusInEvent =
        Gtk.FocusInEventHandler (fun o a ->
            gigs_source.NotifyUser ()
            banshee_window.Focus.FocusInEvent.RemoveHandler x.OnFocusInEvent)

    member private x.OnNetworkStateChanged =
        new Banshee.Networking.NetworkStateChangedHandler (fun o a ->
            ThreadAssist.SpawnFromMain (fun () ->
                Threading.Thread.Sleep 500
                LocationProviderManager.RefreshGeoPosition ()
                x.RefreshRecommededGigs.Invoke () |> ignore))

    member private x.ExtensionId with get () = AddinManager.CurrentAddin.Id

    member private x.OnDisabled () =
        AddinManager.ExtensionChanged.RemoveHandler extension_event_handler
        DatabaseConfigurationClient.Client.Set (config_variable_name, Nullable<DateTime>())
        x.Dispose ()

    member x.Dispose () =
        Application.IdleTimeoutRemove (refresh_timeout_id) |> ignore
        ServiceManager.Get<Banshee.Networking.Network>().StateChanged.RemoveHandler x.OnNetworkStateChanged
        LocationProviderManager.Detach (this)

    member x.ServiceName = Constants.NAME + ".Service"

    interface ICityNameObserver with
        //someone should set RunTimeout for the first time, so we do it here manually
        member x.OnCityNameUpdated (cn) = x.UpdateRunTimeout ()
    interface IService with
        member x.ServiceName = x.ServiceName
    interface IDisposable with
        member x.Dispose () = x.Dispose ()
    interface IExtensionService with
        member x.Initialize () = x.Initialize ()
