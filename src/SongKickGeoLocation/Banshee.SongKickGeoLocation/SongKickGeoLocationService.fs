﻿//
// SongKickGeoLocationSource.fs 
//
// Authors:
//   Cool Extension Author <cool.extension@author.com>
//
// Copyright (C) 2011 Cool Extension Author
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

open Banshee.ServiceStack
open Banshee.Sources

open Hyena
open Hyena.Data

open Mono.Addins

module Constants =
    let NAME = "SongKickGeoLocation"

type GeoLocation = JsonProvider<"./GeoLocationSample.json">

type Service() = 
    let name      = Constants.NAME + ".Service"
    let serverUrl = "https://geoip.fedoraproject.org/city"

    interface IExtensionService with
        member this.Initialize() = 
            Log.Information <| name + " is inititalizing"
            let res =
                try
                    Some (GeoLocation.Load(serverUrl))
                with
                   | :? System.Net.WebException -> None
            match res with
            | Some x -> Log.Information <| x.City
            | None   -> Log.Information <| GeoLocation.GetSample().City
        member this.Dispose() = ()
        member this.ServiceName = name