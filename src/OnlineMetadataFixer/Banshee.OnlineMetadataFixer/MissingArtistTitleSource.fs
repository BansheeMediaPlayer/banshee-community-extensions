//
// MissingArtistTitleSource.fs
//
// Author:
//   Marcin Kolny <marcin.kolny@gmail.com>
//
// Copyright (c) 2014 Marcin Kolny
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

namespace Banshee.OnlineMetadataFixer

open System;
open System.Linq;
open System.Collections.Generic;

open Hyena.Data.Sqlite

open Banshee.ServiceStack

open Mono.Unix;

type MissingArtistTitleSource () = 
    inherit Banshee.Fixup.Solver()
    let mutable findCmd = null
    do
        base.Id <- "missing-artist-online-fix";
        base.Name <- Catalog.GetString ("Missing Artist and Titles Fix");
        base.Description <- Catalog.GetString ("Displayed are tracks loaded in Banshee");

    override this.IdentifyCore () =              
        ServiceManager.DbConnection.Execute ("DELETE FROM CoreAlbums WHERE AlbumID NOT IN (SELECT DISTINCT(AlbumID) FROM CoreTracks)") |> ignore;
        Gst.Application.Init ()
        let reader = new Banshee.OnlineMetadataFixer.AcoustIDReader ()
        reader.GetID ("/home/loganek/Desktop/owieczka.mp3")
        ()

    override this.Fix (problems) =
        ()