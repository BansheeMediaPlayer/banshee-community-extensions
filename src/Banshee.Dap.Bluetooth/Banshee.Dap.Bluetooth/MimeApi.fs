//
// MimeApi.fs
//
// Author:
//   Nicholas J. Little <arealityfarbetween@googlemail.com>
//
// Copyright (c) 2014 Nicholas J. Little
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

namespace Banshee.Dap.Bluetooth.Mime

open System
open System.Text
open System.Linq
open Microsoft.FSharp.Collections

type MediaType =
    | Unrecognised 
    | Asf | M3u | Pls 
    | Mp3 | Ogg | Wma | Aac | Wav | Flac | M4a
    | Avi | Mp4 | Mpeg | Qt | Wmv 

module Constants =
    let PlaylistTypes = Set [ Asf; M3u; Pls ]
    let AudioTypes = Set [ Mp3; Ogg; Wma; Aac; Wav; Flac; M4a ]
    let VideoTypes = Set [ Wmv; Mp4; Avi; Mpeg; Qt ]

[<System.Runtime.CompilerServices.Extension>]
module Extensions =
    [<System.Runtime.CompilerServices.Extension>]
    let IsKnown(this: MediaType) = MediaType.Unrecognised = this
    [<System.Runtime.CompilerServices.Extension>]
    let IsPlaylist(this: MediaType) = Constants.PlaylistTypes.Contains this
    [<System.Runtime.CompilerServices.Extension>]
    let IsAudio(this: MediaType) = Constants.AudioTypes.Contains this
    [<System.Runtime.CompilerServices.Extension>]
    let IsVideo(this: MediaType) = Constants.VideoTypes.Contains this
    [<System.Runtime.CompilerServices.Extension>]
    let ToString(this: MediaType) = match this with
                                    | MediaType.Mp3  -> "audio/mpeg"
                                    | MediaType.Ogg  -> "audio/ogg"
                                    | MediaType.Wma  -> "audio/x-ms-wma"
                                    | MediaType.Wmv  -> "audio/x-ms-wmv"
                                    | MediaType.Asf  -> "audio/x-ms-asf"
                                    | MediaType.Aac  -> "audio/x-aac"
                                    | MediaType.Mp4  -> "video/mp4"
                                    | MediaType.Avi  -> "video/avi"
                                    | MediaType.Wav  -> "video/wav"
                                    | MediaType.Mpeg -> "video/mpeg"
                                    | MediaType.Flac -> "audio/flac"
                                    | MediaType.Qt   -> "video/quicktime"
                                    | MediaType.M4a  -> "audio/mp4"
                                    | MediaType.M3u  -> "audio/x-mpegurl"
                                    | MediaType.Pls  -> "audio/x-scpls"
                                    | _ -> null
    [<System.Runtime.CompilerServices.Extension>]
    let ToMimeType(this: string) = 
        let is x = this.EndsWith(x, StringComparison.InvariantCultureIgnoreCase)
        match this with
        | this when is "mp3"  -> MediaType.Mp3
        | this when is "ogg"  -> MediaType.Ogg
        | this when is "wma"  -> MediaType.Wma
        | this when is "wmv"  -> MediaType.Wmv
        | this when is "asf"  -> MediaType.Asf
        | this when is "aac"  -> MediaType.Aac
        | this when is "mp4"  -> MediaType.Mp4
        | this when is "avi"  -> MediaType.Avi
        | this when is "wav"  -> MediaType.Wav
        | this when is "mpg"  -> MediaType.Mpeg
        | this when is "flac" -> MediaType.Flac
        | this when is "mov"  -> MediaType.Qt
        | this when is "m4a"  -> MediaType.M4a
        | this when is "m4v"  -> MediaType.Mp4
        | this when is "m3u"  -> MediaType.M3u
        | this when is "pls"  -> MediaType.Pls
        | _ -> MediaType.Unrecognised
