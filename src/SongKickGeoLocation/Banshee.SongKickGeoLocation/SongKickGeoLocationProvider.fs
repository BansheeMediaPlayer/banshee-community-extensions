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

open System

open FSharp.Data
open FSharp.Data.JsonExtensions

open Banshee.SongKick.LocationProvider
open Banshee.ServiceStack
open Banshee.Sources

open Hyena
open Hyena.Data

open CacheService

module Constants =
    let NAME = "SongKickGeoLocation"

module Fun =
    let ToFloat (x, y) = (JsonExtensions.AsFloat x, JsonExtensions.AsFloat y)

type MozillaResponse  = JsonProvider<"./Resources/MozillaResponseSample.json",
                                     EmbeddedResource="MozillaResponseSample.json">
type FedoraResponse   = JsonProvider<"./Resources/FedoraResponseSample.json",
                                     EmbeddedResource="FedoraResponseSample.json">
type SongKickResponse = JsonProvider<"./Resources/SongKickResponseSample.json",
                                     EmbeddedResource="SongKickResponseSample.json">
type GeoResult        = JsonProvider<"./Resources/GeoResultSample.json",
                                     EmbeddedResource="GeoResultSample.json">

type Response =
    | SongKickResponse of SongKickResponse.Root
    | MozillaResponse  of MozillaResponse.Root
    | FedoraResponse   of FedoraResponse.Root
    | NoResponse

type GeoLocation =
    | Determined of GeoResult.Root
    | NotDetermined

type Provider () =
    inherit BaseLocationProvider ()
    let name                  = Constants.NAME
    let cache                 = CacheManager.GetInstance.Initialize (name.ToLower ())
    let songkick_api_key      = Banshee.SongKick.SongKickCore.APIKey
    let mozilla_api_key       = "2363677d-0913-4a7d-8659-5df67963dc20"
    let reverse_geocode_uri   = "http://api.songkick.com/api/3.0/search/locations.json?location=geo:{0},{1}"
                                + "&apikey=" + songkick_api_key
    let mozilla_request_uri   = "https://location.services.mozilla.com/v1/geolocate?key=" + mozilla_api_key
    let fedora_request_uri    = "https://geoip.fedoraproject.org/city"
    let songkick_request_uri  = "http://api.songkick.com/api/3.0/search/locations.json?location=clientip"
                                + "&apikey=" + songkick_api_key

    member private x.DetermineGeoLocation () =
        match cache.Get "geoposition" with
        | Some v -> Determined <| GeoResult.Parse (v.ToString ())
        | None   -> let response = x.GetResponseFromServer ()
                    x.GetGeoLocation (response)

    member private x.GetResponseFromServer () =
        try SongKickResponse <| SongKickResponse.Load (songkick_request_uri) with _ ->
        try FedoraResponse   <| FedoraResponse.Load (fedora_request_uri)     with _ ->
        try MozillaResponse  <| MozillaResponse.Parse (x.SendPostRequestToMozilla ())
        with _ -> NoResponse

    member private x.ReverseGeoCode (lat, long) =
        let req = String.Format (reverse_geocode_uri, lat.ToString (), long.ToString ())
        let res = SongKickResponse.Load (req)
        match res.ResultsPage.Status with
        | "ok" when res.ResultsPage.TotalEntries > 0
               -> res.ResultsPage.Results.Location.[0].City.DisplayName
        | _    -> "undefined"

    member private x.SendPostRequestToMozilla () =
        try Http.RequestString (mozilla_request_uri,
                                headers = ["Content-Type", HttpContentTypes.Json],
                                body = TextRequest "{}")
        with _ -> ""
    
    member private x.GetUnifiedJson lat lng city =
        "{\n" + 
        "\"lat\": " + lat.ToString() + ",\n" +
        "\"lng\": " + lng.ToString() + ",\n" +
        "\"city\": \"" + city.ToString() + "\"\n" +
        "}"

    member private x.GetGeoLocation res =
        let unifiedJson =
            match res with
            | SongKickResponse r when r.ResultsPage.TotalEntries > 0 ->
                let lat = r.ResultsPage.Results.Location.[0].City.Lat
                let lng = r.ResultsPage.Results.Location.[0].City.Lng
                let cn  = r.ResultsPage.Results.Location.[0].City.DisplayName
                Some <| x.GetUnifiedJson lat lng cn

            | FedoraResponse r ->
                let lat = r.JsonValue?latitude
                let lng = r.JsonValue?longitude
                let cn  = r.City
                Some <| x.GetUnifiedJson lat lng cn

            | MozillaResponse r ->
                let lat = r.Location.Lat
                let lng = r.Location.Lng
                let jvLatAndLong = r.Location.JsonValue?lat, r.Location.JsonValue?lng
                let cn  = x.ReverseGeoCode <| Fun.ToFloat jvLatAndLong
                Some <| x.GetUnifiedJson lat lng cn

            | _ -> None

        match unifiedJson with
        | Some json -> cache.Add "geoposition" json
                       Determined (GeoResult.Parse json)
        | None      -> NotDetermined

    override x.CityName
      with get() =
        match x.DetermineGeoLocation () with
        | Determined p  -> p.City
        | NotDetermined -> "undefined"

    override x.Latitude
      with get() =
        match x.DetermineGeoLocation () with
        | Determined r  -> (float) r.Lat
        | NotDetermined -> 0.0

    override x.Longitude
      with get() =
        match x.DetermineGeoLocation () with
        | Determined r  -> (float) r.Lng
        | NotDetermined -> 0.0
