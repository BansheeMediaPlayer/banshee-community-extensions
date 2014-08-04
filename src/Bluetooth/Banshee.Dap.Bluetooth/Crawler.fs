//
// Crawler.fs
//
// Author:
//   Nicholas Little <arealityfarbetween@googlemail.com>
//
// Copyright (c) 2014 Nicholas Little
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
module Banshee.Dap.Bluetooth.Ftp

open System
open System.Threading
open System.Collections
open System.Collections.Generic
open System.Text

open Banshee.Dap.Bluetooth.ObexApi
open Banshee.Dap.Bluetooth.Client
open Banshee.Dap.Bluetooth.Wrappers
open Banshee.Dap.Bluetooth.SupportApi

open Hyena.Log

open DBus

let DEFAULT_TRIES_MAX = 60us

type NodeType = | Folder | File

type RemoteNode = {
    path  : string list
    name  : string
    ntype : NodeType
    size  : uint64
    ctime : uint64
    atime : uint64
    mtime : uint64
    }

let rec PrefixOf x y =
    match (x, y) with
    | ([],ys) -> Some ys
    | (xn::xs,yn::ys) when xn = yn -> PrefixOf xs ys
    | (_,_) -> None
let SuffixOf x y =
    let xr = List.rev x
    let yr = List.rev y
    PrefixOf xr yr
let NodeTypeOf x = match x.ToString() with
                   | "file" -> File
                   | "folder" -> Folder
                   | _ -> failwith "Invalid Node Type: %s" x
let RemoteNodeOf path (x: Map<string,_>) =
    let UInt64Of x = let v = ref 0UL
                     UInt64.TryParse(x.ToString(), v) |> ignore
                     !v
    let LookupOrDefaultOf tv key def =
        let v = Map.tryFind key x
        match v with
        | Some v -> tv v
        | None -> def
    let nm = x.["Name"].ToString()
    let nt = x.["Type"].ToString() |> NodeTypeOf
    let size = LookupOrDefaultOf UInt64Of "Size" 0UL
    let ctime = LookupOrDefaultOf UInt64Of "Created" 0UL
    let atime = LookupOrDefaultOf UInt64Of "Accessed" 0UL
    let mtime = LookupOrDefaultOf UInt64Of "Modified" 0UL
    { path = path;
      name = nm;
      ntype = nt;
      size = size;
      ctime = ctime;
      atime = atime;
      mtime = mtime }
let rec PrintMap x =
    let PrintVal (v: obj) = match v with
                            | :? IDictionary as vd -> "{ " + PrintMap vd + " }"
                            | _ -> sprintf "%s" (v.ToString())
    let sb = StringBuilder()
    for key in x.Keys do
        sb.AppendLine(sprintf "%s => %s" (key.ToString()) (PrintVal x.[key])) |> ignore
    sb.ToString()
let DictConv x = x |> Seq.map (|KeyValue|) |> Map.ofSeq
let ToDict (x: KeyValuePair<_,_>[]) = x :> seq<_> |> DictConv
let ToNodeSeq path nodes =
    nodes |> Seq.map (fun afi -> ToDict afi |> RemoteNodeOf path)

type ICrawler =
    abstract Init : unit -> bool
    abstract Up : unit -> unit
    abstract Up : int -> unit
    abstract Root : unit -> unit
    abstract List : unit -> RemoteNode seq
    abstract Drop : unit -> unit
    abstract Down : string -> bool
    abstract Make : string -> bool
    abstract MkDn : string -> unit
    abstract Delete : string -> unit
    abstract PutFile : string -> string -> ObjectPath
    abstract Path : string list with get, set

type rooter = unit -> IFileTransfer

type Crawler(addr: string, cm: ClientManager, max_tries: uint16) =
    let mutable fails = max_tries
    let mutable path : string list = []
    let mutable ops : ObjectPath = Unchecked.defaultof<_>
    let drop () =
        try
          match cm.Session ops with
          | Some _ -> cm.RemoveSession ops
          | None -> ()
        with
        | _ -> ()
        ops <- Unchecked.defaultof<_>
    let record (e: Exception) =
        Warnf "Dap.Bluetooth: Fails = %d: %s => %s" fails (e.GetType().FullName) e.Message
        if max_tries <= fails then raise e
        fails <- 1us + fails
        drop ()
    let check (e: Exception) =
        Debugf "Dap.Bluetooth: Fails = %d: %s => %s" fails (e.GetType().FullName) e.Message
        if IsNull e.Message then record e
        else
          let parts = e.Message.Split (":".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)
          match parts.[0].Trim() with
          | "org.bluez.obex.Error.Failed" ->
            match parts.[1].Trim() with
            | "Not Found" -> ()
            | "Unable to find service record"
            | "The transport is not connected"
            | _ -> record e
          | "org.freedesktop.DBus.Error.NoReply"
          | "Object reference not set to an instance of an object"
          | _ -> raise e
    let up (root: rooter) =
        match path with
        | [] -> ()
        | hd::tl ->
          try
            Debugf "Dap.Bluetooth: cd: .."
            root().ChangeFolder ".."
            path <- tl
          with
          | e -> check e
    let down (root: rooter) y =
        try
          Debugf "Dap.Bluetooth: cd: %s" y
          root().ChangeFolder y
          path <- y::path
          true
        with
        | e -> check e
               false
    let make (root: rooter) y =
        try
          Debugf "Dap.Bluetooth: md: %s" y
          root().CreateFolder y
          path <- y::path
          true
        with
        | e -> check e
               false
    let mkdn root y = (down root y || make root y) |> ignore
    let restore root =
        path |> List.rev |> List.iter (fun dir -> mkdn root dir)
    let rec crawl root dest =
        let suffix = SuffixOf path dest
        Debugf "Dap.Bluetooth: suffix: %A" suffix
        match suffix with
        // In the directory
        | Some [] -> ()
        // In a parent directory
        | Some d -> List.iter (fun i -> mkdn root i) d
        // Elsewhere
        | None -> up root
                  crawl root dest
    let rec root () =
        try
          if 0us < fails then TimeSpan.FromSeconds 0.5 |> Thread.Sleep
          match (fails, cm.Session ops) with
          | (0us, Some s) -> s
          | (_, Some s) ->
            Debugf "Dap.Bluetooth: Setting Path %A" path
            fails <- 0us
            restore root
            s
          | (_, None) ->
            Debugf "Dap.Bluetooth: Requesting Session"
            ops <- cm.CreateSession addr Ftp
            root()
        with
        | e -> check e
               root()
    member x.Init () =
        try
          fails <- max_tries
          root() |> ignore
          true
        with
        | _ -> false
    member x.Up () = up root
    member x.Up y = for i in [0..y] do x.Up()
    member x.Root () = x.Up path.Length
    member x.List () =
        try
          root().ListFolder() |> ToNodeSeq path
        with
        | e -> check e; Seq.empty
    member x.Drop () = drop ()
    member x.Down y = down root y
    member x.Make y = make root y
    member x.MkDn y = mkdn root y
    member x.Delete y =
        try
          Debugf "Dap.Bluetooth: rm: %s" y
          root().Delete y
        with
        | e -> check e
    member x.PutFile y z =
        try
          Debugf "Dap.Bluetooth: put: %s => %s" y z
          root().PutFile y z
        with
        | e -> check e
               Unchecked.defaultof<_>
    member x.Path with get () = path
                  and set v = crawl root v
    interface ICrawler with
        member x.Init () = x.Init ()
        member x.Up () = x.Up ()
        member x.Up y = x.Up y
        member x.Root () = x.Root ()
        member x.List () = x.List ()
        member x.Drop () = x.Drop ()
        member x.Down y = x.Down y
        member x.Make y = x.Make y
        member x.MkDn y = x.MkDn y
        member x.Delete y = x.Delete y
        member x.PutFile y z = x.PutFile y z
        member x.Path with get () = x.Path
                      and set v = x.Path <- v
    new (addr, cm) = Crawler(addr, cm, DEFAULT_TRIES_MAX)
