//
// AcoustIDSender.fs
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
open System

type AcoustIDSender() = 
    static member private Append(builder : System.Text.StringBuilder, key, value) = 
        if value |> String.IsNullOrEmpty |> not then
            "&{0}={1}"
            |> String.Format
            |> builder.Append
            |> ignore

    static member private AppendObligatory(builder, key, value) = 
        if value |> String.IsNullOrEmpty then
            invalidArg key "value must exists"
        AcoustIDSender.Append (builder, key, value)
        ()

    static member Send (uri : string, duration, bitrate, title, artist, album, album_artist, year, track_no, disc_no) =
        let su = new Hyena.SafeUri (uri)
        let (dur, fileFP) = AcoustIDStorage.LoadFingerprint (su.AbsolutePath)

        if fileFP.IsSome then
            let builder = new System.Text.StringBuilder ("http://api.acoustid.org/v2/submit?format=json&client=")
            AcoustIDReader.AcoustIDKey |> builder.Append |> ignore
            AcoustIDSender.AppendObligatory (builder, "duration", duration.ToString ())
            AcoustIDSender.AppendObligatory (builder, "user", PasswordManager.ReadPassword ())
            AcoustIDSender.AppendObligatory (builder, "fingerprint", fileFP.Value)
            AcoustIDSender.Append (builder, "bitrate", bitrate.ToString ())
            AcoustIDSender.Append (builder, "track", title)
            AcoustIDSender.Append (builder, "artist", artist)
            AcoustIDSender.Append (builder, "album", album)
            AcoustIDSender.Append (builder, "albumartist", album_artist)
            AcoustIDSender.Append (builder, "year", year.ToString ())
            AcoustIDSender.Append (builder, "trackno", track_no.ToString ())
            AcoustIDSender.Append (builder, "discno", disc_no.ToString ())
            
            async {
                FSharp.Data.Http.AsyncRequestString(builder.ToString ()) |> ignore
            } |> Async.Start
        else
            Hyena.Log.Warning ("Cannot read fingerprint")
