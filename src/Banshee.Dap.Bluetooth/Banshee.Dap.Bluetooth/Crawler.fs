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
namespace Banshee.Dap.Bluetooth.Ftp

open System
open System.Collections
open System.Collections.Generic
open System.Text

open Banshee.Dap.Bluetooth.ObexApi
open Banshee.Dap.Bluetooth.Client
open Banshee.Dap.Bluetooth.Wrappers
open Banshee.Dap.Bluetooth.SupportApi

open DBus

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

module Functions =
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

type Crawler(addr: string, cm: ClientManager) =
    let mutable path : string list = []
    let mutable ops : ObjectPath = Unchecked.defaultof<_>
    let check (e: Exception) =
        match e.Message with
        | "org.bluez.obex.Error.Failed: Not Found" -> ()
        | "org.bluez.obex.Error.Failed: Unable to find service record"
        | "org.bluez.obex.Error.Failed: The transport is not connected" ->
          printfn "%s: %s" (e.GetType().FullName) e.Message
          ops <- Unchecked.defaultof<_>
          path <- []
        | "org.freedesktop.DBus.Error.NoReply: Message did not receive a reply (timeout by message bus)"
        | "Object reference not set to an instance of an object" ->
          raise e
        | m -> printfn "%s: %s" (e.GetType().FullName) m
               raise e
    let rec root () =
        try
          match (Functions.IsNull ops, cm.Session ops) with
          | (_, Some s) -> s
          | (true, None) -> printfn "Creating Session"
                            ops <- cm.CreateSession addr Ftp
                            root()
          | (false, None) -> System.Threading.Thread.Sleep 500
                             root()
        with
        | e -> check e
               root()
    member x.Up () =
        match path with
        | [] -> ()
        | hd::tl -> try
                      printfn "cd: .."
                      root().ChangeFolder ".."
                      path <- tl
                    with
                    | e -> check e
    member x.Up y = for i in [0..y] do x.Up()
    member x.Root () = x.Up path.Length
    member x.List () =
        try
          root().ListFolder() |> Functions.ToNodeSeq path
        with
        | e -> check e; Seq.empty
    member x.Drop () =
        match cm.Session ops with
        | Some _ -> cm.RemoveSession ops
        | None -> ()
    member x.Down y =
        try
          printfn "cd: %s" y
          root().ChangeFolder y
          path <- y::path
          true
        with
        | e -> check e
               false
    member x.Make y =
        try
          printfn "md: %s" y
          root().CreateFolder y
          path <- y::path
          true
        with
        | e -> check e
               false
    member x.MkDn y = (x.Down y || x.Make y) |> ignore
    member private x.Crawl dest =
        let suffix = Functions.SuffixOf path dest
        printfn "%A" suffix
        match suffix with
        // In the directory
        | Some [] -> ()
        // In a parent directory
        | Some d -> List.iter (fun i -> x.MkDn i) d
        // Elsewhere
        | None -> x.Up()
                  x.Crawl dest
    member x.Delete y =
        try
          printfn "rm: %s" y
          root().Delete y
        with
        | e -> check e
    member x.PutFile y z =
        try
          printfn "put: %s => %s" y z
          root().PutFile y z
        with
        | e -> check e
               Unchecked.defaultof<_>
    member x.Path with get () = path
                  and set v = x.Crawl v
    interface ICrawler with
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
