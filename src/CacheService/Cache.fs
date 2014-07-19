//
// Cache.fs
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

namespace CacheService

open System
open System.IO
open System.Collections.Generic

open Hyena

[<Measure>] type hours

module Constants = 
    let cache_root = Paths.ExtensionCacheRoot
    let timeout    = 12<hours>
    let separator  = "\n$\n"
    let splitter   = '$'

type internal CacheItem = {
    key : string
    value : obj
    created : DateTime
    expiration_timeout : float<hours>
}

type CacheItemAction = Added | Removed | Expired

type CacheChangedArgs (s : CacheItemAction, k : string, v : obj) = 
    inherit EventArgs ()
    member x.State = s
    member x.Key   = k
    member x.Value = v

type Cache internal (nmspace : string) =
    let CacheChanged = Event<_> ()
    do
        if not (Banshee.IO.Directory.Exists nmspace)
        then Directory.CreateDirectory (nmspace) |> ignore

    member private x.ComputeHashCode key  = key.GetHashCode () ^^^ nmspace.GetHashCode ()

    member private x.GetPathToKey key =
        let hash = x.ComputeHashCode key
        nmspace + hash.ToString ()

    member private x.IsItemExpired (createdTime : DateTime) (expTime : float<hours>) =
        DateTime.UtcNow > createdTime.AddHours ((float) expTime)

    member private x.ReadCacheItemFromFile (key : string) =
        let path     = x.GetPathToKey key
        use sr       = new StreamReader (path)
        let data     = sr.ReadToEnd ()
        let databits = data.Split (Constants.splitter)
        if (databits.Length = 3) then
            try
                Some <|
                { key = key
                  value = databits.[0]
                  created = DateTime.Parse (databits.[1])
                  expiration_timeout = ((float) databits.[2] * 1.0<hours>) }
            with
                | :? System.FormatException as e -> Hyena.Log.Error (e)
                                                    None
        else
            None

    member private x.WriteValueToFile (key : string) (value : 'a) = 
        let path = x.GetPathToKey key
        use sw   = new StreamWriter (path, false)
        sw.Write (value.ToString ()           + Constants.separator +
                  DateTime.UtcNow.ToString () + Constants.separator +
                  Constants.timeout.ToString ())

    member x.Add (key : string) (value : 'a) =
        x.WriteValueToFile key value
        Log.DebugFormat ("In cache was added a value with key: {0}", key)
        CacheChanged.Trigger (CacheChangedArgs (Added, key, value))

    member x.Get (key : string) =
        if File.Exists (x.GetPathToKey key) then
           match x.ReadCacheItemFromFile key with 
           | Some c when x.IsItemExpired c.created c.expiration_timeout ->
                         Log.DebugFormat ("Time Expired: cannot return a cached value with key: {0}", key)
                         CacheChanged.Trigger (CacheChangedArgs (Expired, key, c.value))
                         None
           | Some c -> Log.DebugFormat ("Got cached value with key: {0}", key)
                       Some c.value
           | None   -> Log.DebugFormat ("Cannot read cached value with key:", key)
                       None
        else None

    member x.Remove (key : string) =
        let path = x.GetPathToKey key
        if File.Exists path then
           File.Delete path
           Log.DebugFormat ("Deleted cached value with key: ", key)
           CacheChanged.Trigger (CacheChangedArgs (Removed, key, null))

    member x.Clear () =
        for file in Directory.EnumerateFiles nmspace do
          let tempPath = System.IO.Path.Combine (nmspace, file)
          File.Delete (tempPath)
        Log.DebugFormat ("Cleared cache from: {0}" + nmspace)

    member x.CacheStateChanged = CacheChanged.Publish

    interface ICache with
        member x.Add k v    = x.Add k v
        member x.Get k      = x.Get k
        member x.Remove k   = x.Remove k
        member x.Clear ()   = x.Clear ()
        member x.Dispose () = ()
