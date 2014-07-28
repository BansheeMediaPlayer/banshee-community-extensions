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


type AcoustIDSubmissionStatus = FSharp.Data.JsonProvider<"Resources/AcoustIDSubmissionStatus.json", EmbeddedResource="AcoustIDTrackInfo.json">

type AcoustIDSender() = 
    static member private Append(builder : System.Text.StringBuilder, key, value, ?additional_condition) = 
        let ac = defaultArg additional_condition String.Empty
        if value |> String.IsNullOrEmpty |> not && value.Equals(ac) |> not then
            ("&{0}={1}", key, value)
            |> String.Format
            |> builder.Append
            |> ignore

    static member private AppendObligatory(builder, key, value, ?additional_condition) = 
        let ac = defaultArg additional_condition String.Empty
        if value |> String.IsNullOrEmpty || value.Equals(ac) then
            invalidArg key "value must exists"
        AcoustIDSender.Append (builder, key, value, ac)
        ()

    static member Send (uri : string, duration, bitrate, title, artist, album, album_artist, year, track_no, disc_no) =
        let su = new Hyena.SafeUri (uri)
        let (dur, fileFP) = AcoustIDStorage.LoadFingerprint (su.AbsolutePath)

        if fileFP.IsSome then
            let builder = new System.Text.StringBuilder ("http://api.acoustid.org/v2/submit?format=json&client=")
            AcoustIDReader.AcoustIDKey |> builder.Append |> ignore
            AcoustIDSender.AppendObligatory (builder, "duration", (duration/1000).ToString (), "0")
            AcoustIDSender.AppendObligatory (builder, "user", PasswordManager.ReadPassword ())
            AcoustIDSender.AppendObligatory (builder, "fingerprint", fileFP.Value)
            AcoustIDSender.Append (builder, "bitrate", bitrate.ToString (), "0")
            AcoustIDSender.Append (builder, "track", title)
            AcoustIDSender.Append (builder, "artist", artist)
            AcoustIDSender.Append (builder, "album", album)
            AcoustIDSender.Append (builder, "albumartist", album_artist)
            AcoustIDSender.Append (builder, "year", year.ToString (), "0")
            AcoustIDSender.Append (builder, "trackno", track_no.ToString (), "0")
            AcoustIDSender.Append (builder, "discno", disc_no.ToString (), "0")
            builder.ToString () |> AcoustIDSender.WebRequest
        else
            Hyena.Log.Warning (String.Format("Cannot read fingerprint of file {0}", uri))
            
    static member private WebRequest (url : string) =
        // Workaround a FSharp.Data bug. See here: https://github.com/fsharp/FSharp.Data/issues/642
        let webClient = new System.Net.WebClient ()
        let jsonProvider = AcoustIDSubmissionStatus.Parse (webClient.DownloadString (url))
            
        Hyena.Log.Debug ("Submitting metadata to AcoustID status: " + jsonProvider.Status)
        
        if jsonProvider.Status.Equals("ok") then
            for result in jsonProvider.Submissions do
                Hyena.Log.Debug (String.Format ("Submission status: {0}, ID: {1}", result.Status, result.Id))
                Hyena.Log.Debug (String.Format ("Look up the status of submission on the website: http://api.acoustid.org/v2/submission_status?client={0}&id={1}", AcoustIDReader.AcoustIDKey, result.Id))
