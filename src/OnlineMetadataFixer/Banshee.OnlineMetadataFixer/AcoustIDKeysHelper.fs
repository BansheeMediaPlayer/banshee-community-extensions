//
// AcoustIDKeysHelper.fs
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

open Gnome.Keyring

type AcoustIDKeysHelper() = 
    static let keyring = "banshee-acoustid"
    
    static member private CheckIsKeyringAvailable () =
        if not (Ring.Available) then
            "The gnome-keyring-daemon cannot be reached." |> Hyena.Log.Error
        Ring.Available

    static member SaveAcoustIDKey (password) =
        if AcoustIDKeysHelper.CheckIsKeyringAvailable () then
            try
                Ring.CreateKeyring (keyring, String.Empty);
            with :? KeyringException as ex -> () // keyring might already exists

            Ring.CreateItem (keyring, ItemType.GenericSecret, "acoustid-key", new System.Collections.Hashtable(), password, true) |> ignore

    static member ReadAcoustIDKey () =
        if AcoustIDKeysHelper.CheckIsKeyringAvailable () then
            try
                Ring.GetItemInfo(keyring, 1).Secret
            with :? KeyringException as ex -> // keyring might not exists
                Hyena.Log.Warning (ex);
                String.Empty
        else
            String.Empty
