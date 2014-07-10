//
// SongKickGeoLocationProvider.fs 
//
// Authors:
//   Dmitrii Petukhov <dimart.sp@gmail.com>
//
// Copyright (C) 2014 Dmitrii Petukhov
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

open FSharp.Data
open FSharp.Data.JsonExtensions

open Banshee.SongKick.LocationProvider
open Banshee.ServiceStack
open Banshee.Sources

open Hyena
open Hyena.Data

open Mono.Addins

open CacheService

module Constants =
    let NAME = "SongKickGeoLocation"

type MozillaGeoLocation = JsonProvider<"./MozillaGeoLocationSample.json">
type FedoraGeoLocation  = JsonProvider<"./FedoraGeoLocationSample.json">
type UnifiedGeoLocation = JsonProvider<"./GeoResults.json">

type OpenStreetMap      = XmlProvider<"./OpenStreetMapSample.xml">

type Response =
    | MozillaResponse of MozillaGeoLocation.Root
    | FedoraResponse  of FedoraGeoLocation.Root
    | NoResponse

type GeoPosition =
    | Determined of UnifiedGeoLocation.Root
    | NotDetermined

module Fun =
    let toFloat = JsonExtensions.AsFloat

type Provider() as this =
    inherit BaseLocationProvider()
    let name             = Constants.NAME
    let cache            = CacheManager.GetInstance.Initialize(name)
    let mozillaApiKey    = "2363677d-0913-4a7d-8659-5df67963dc20"
    let fedoraUrl        = "https://geoip.fedoraproject.org/city"
    let mozillaUrl       = "https://location.services.mozilla.com/v1/geolocate?key="
    let openStreetMapUrl = "http://nominatim.openstreetmap.org/reverse?format=xml"

    do
       cache.CacheStateChanged.AddHandler this.OnCacheChanged

    override x.CityName
      with get() =
        match x.DetermineGeoPosition () with
        | Determined r  -> r.City
        | NotDetermined -> "your city"

    override x.Latitude
      with get() =
        match x.DetermineGeoPosition () with
        | Determined r  -> (float) r.Lat
        | NotDetermined -> 0.0

    override x.Longitude
      with get() =
        match x.DetermineGeoPosition () with
        | Determined r  -> (float) r.Lng
        | NotDetermined -> 0.0

    member private x.DetermineGeoPosition () =
        match cache.Get "geoposition" with
        | Some r -> Determined (UnifiedGeoLocation.Parse (r.ToString ()))
        | None   -> x.GetGeoPositionUsingResponse (x.GetResponseFromServer ())

    member private x.GetResponseFromServer () =
        try MozillaResponse <| MozillaGeoLocation.Parse(x.SendPostRequestToMozilla ())
        with _ ->
            try FedoraResponse <| FedoraGeoLocation.Load(fedoraUrl)
            with _ -> NoResponse

    member private x.ReverseGeoCode (lat, long) =
        let queryUrl = openStreetMapUrl + "&lat=" + lat.ToString()
                                        + "&lon=" + long.ToString()
                                        + "&zoom=11"
        let res = try Some <| OpenStreetMap.Load (queryUrl) with _ -> None
        match res with
        | Some x -> try x.Addressparts.City 
                    with _ -> (OpenStreetMap.GetSample().Addressparts).City
        | None   -> (OpenStreetMap.GetSample().Addressparts).City

    member private x.SendPostRequestToMozilla () =
        try Http.RequestString (mozillaUrl + mozillaApiKey,
                                headers = ["Content-Type", HttpContentTypes.Json],
                                body = TextRequest "{}")
        with _ -> ""
    
    member private x.GetUnifiedJson lat lng city =
        "{\n" + 
        "\"lat\": " + lat.ToString() + ",\n" +
        "\"lng\": " + lng.ToString() + ",\n" +
        "\"city\": \"" + city.ToString() + "\"\n" +
        "}"

    member private x.GetGeoPositionUsingResponse res =
        match res with
        | MozillaResponse r -> let lat = r.Location.JsonValue?lat
                               let lng = r.Location.JsonValue?lng
                               let cn  = x.ReverseGeoCode (Fun.toFloat r.Location.JsonValue?lat, 
                                                           Fun.toFloat r.Location.JsonValue?lng)
                               let unifiedjson = x.GetUnifiedJson lat lng cn
                               cache.Add "geoposition" unifiedjson
                               Determined <| UnifiedGeoLocation.Parse (unifiedjson)
        | FedoraResponse  r -> let lat = r.JsonValue?latitude
                               let lng = r.JsonValue?longitude
                               let cn  = r.City
                               let unifiedjson = x.GetUnifiedJson lat lng cn
                               cache.Add "geoposition" unifiedjson
                               Determined <| UnifiedGeoLocation.Parse (unifiedjson)
        | NoResponse        -> NotDetermined

    member private x.UpdateExpiredCache (key : string) (oldValue : UnifiedGeoLocation.Root) =
        match key with
        | "geoposition" -> match x.GetGeoPositionUsingResponse (x.GetResponseFromServer ()) with
                           | Determined r  -> cache.Add "geoposition" r
                           | NotDetermined -> cache.Add "geoposition" oldValue
        | _ -> ()
        Hyena.Log.Information <| name + ": cached GeoPosition updated"

    member private x.OnCacheChanged =
        CacheService.CacheChangedHandler (fun o a ->
            match a.State with
            | Expired -> x.UpdateExpiredCache a.Key (UnifiedGeoLocation.Parse (a.Value.ToString ()))
            | _ -> ())
