//
// AcoustIDStorage.fs
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
open System.IO

open Hyena

type AcoustIDStorage() = 
    static let cacheDirectory = Paths.Combine (Paths.ExtensionCacheRoot, "onlinemetadatafixer", "acoustid")
    
    static member private ComputeHash (filename, filesize) =
        let input = filename + filesize.ToString ()
        let md5 = System.Security.Cryptography.MD5.Create()
        input
        |> System.Text.Encoding.ASCII.GetBytes
        |> md5.ComputeHash
        |> Seq.map (fun c -> c.ToString("X2"))
        |> Seq.reduce (+)
    
    static member private GetHashedFilename (filename) =
        let filesize = (new FileInfo(filename)).Length
        let hashedFileName = AcoustIDStorage.ComputeHash(filename, filesize)
        Paths.Combine (cacheDirectory, hashedFileName)
    
    static member SaveFingerprint (fingerprint, filename, duration) =
        let hashedFileName = AcoustIDStorage.GetHashedFilename (filename)
        
        try
            if not (Directory.Exists (cacheDirectory)) then
                cacheDirectory |> Directory.CreateDirectory |> ignore

            let sw = new StreamWriter (hashedFileName, false)
            sw.Write (duration.ToString () + " " + fingerprint.ToString ())
            sw.Close ()
        with
        | :? SystemException as ex -> Hyena.Log.WarningFormat ("Cannot save fingerprint: ", ex.Message)

    static member LoadFingerprint (filename) =
        let hashedFileName = AcoustIDStorage.GetHashedFilename (filename)
        if File.Exists (hashedFileName) then
            try
                let sr = new StreamReader (hashedFileName)
                let data = sr.ReadToEnd ();
                let a = data.Split (' ')
                (Convert.ToInt64 (a. [0]), Some (a. [1]))
            with
            | :? SystemException as ex ->
                Hyena.Log.WarningFormat ("Cannot read fingerprint: ", ex.Message)
                (0L, None)
        else
            (0L, None)
            
    static member FingerprintExists (uri : string) =
        let su = new Hyena.SafeUri (uri)
        match su.IsFile with
        | true ->
            su.AbsolutePath
            |> AcoustIDStorage.GetHashedFilename
            |> File.Exists
        | _ -> false

    static member FingerprintMayExists (uri : string) =
        let su = new Hyena.SafeUri (uri)
        su.IsFile
