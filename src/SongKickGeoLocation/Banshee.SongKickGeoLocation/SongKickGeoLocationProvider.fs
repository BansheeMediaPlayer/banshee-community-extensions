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

module Constants =
    let NAME = "SongKickGeoLocation"

type MozillaGeoLocation = JsonProvider<"./MozillaGeoLocationSample.json">
type FedoraGeoLocation  = JsonProvider<"./FedoraGeoLocationSample.json">
type OpenStreetMap      = XmlProvider<"./OpenStreetMapSample.xml">

type GeoResult =
    | MozillaResponse of MozillaGeoLocation.Root
    | FedoraResponse  of FedoraGeoLocation.Root
    | NoRespose

module Fun =
    let toFloat = JsonExtensions.AsFloat

type Provider() = 
    inherit BaseLocationProvider()

    let name             = Constants.NAME + ".Service"
    let mozillaApiKey    = "2363677d-0913-4a7d-8659-5df67963dc20"
    let fedoraUrl        = "https://geoip.fedoraproject.org/city"
    let mozillaUrl       = "https://location.services.mozilla.com/v1/geolocate?key="
    let openStreetMapUrl = "http://nominatim.openstreetmap.org/reverse?format=xml"

    override x.CityName
      with get() =
        match x.GetDataFromServer () with
        | MozillaResponse r -> x.DetermineCityByLatAndLong
                                 (Fun.toFloat r.Location.JsonValue?lat,
                                  Fun.toFloat r.Location.JsonValue?lng)
        | FedoraResponse  r -> r.City
        | NoRespose         -> ""

    override x.Latitude
      with get() =
        match x.GetDataFromServer () with
        | MozillaResponse x -> Fun.toFloat x.Location.JsonValue?lat
        | FedoraResponse  x -> Fun.toFloat x.JsonValue?latitude
        | NoRespose         -> 0.0

    override x.Longitude
      with get() =
        match x.GetDataFromServer () with
           | MozillaResponse x -> Fun.toFloat x.Location.JsonValue?lng
           | FedoraResponse  x -> Fun.toFloat x.JsonValue?latitude
           | NoRespose         -> 0.0

    member x.GetDataFromServer () =
        try MozillaResponse <| MozillaGeoLocation.Parse(x.SendPostRequestToMozilla ())
        with _ ->
            try FedoraResponse <| FedoraGeoLocation.Load(fedoraUrl)
            with _ -> NoRespose

    member x.DetermineCityByLatAndLong (lat, long) =
        let queryUrl = openStreetMapUrl + "&lat=" + lat.ToString()
                                        + "&lon=" + long.ToString()
                                        + "&zoom=11"
        let res = try Some <| OpenStreetMap.Load (queryUrl) with _ -> None
        match res with
        | Some x -> try x.Addressparts.City 
                    with _ -> (OpenStreetMap.GetSample().Addressparts).City 
                   // ^^^ if there were no "city" val in xml from server ^^^
        | None   -> (OpenStreetMap.GetSample().Addressparts).City

    member x.SendPostRequestToMozilla () =
        try Http.RequestString (mozillaUrl + mozillaApiKey,
                                headers = ["Content-Type", HttpContentTypes.Json],
                                body = TextRequest "{}")
        with _ -> ""
