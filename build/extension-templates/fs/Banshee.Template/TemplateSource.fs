//
// EXTENSION-NAMESource.fs 
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

namespace Banshee.EXTENSION-NAME

open Banshee.ServiceStack
open Banshee.Sources

open Hyena
open Hyena.Data

open Mono.Addins

module Constants =
    let SORT = 190
    let NAME = "EXTENSION-NAME"

type Source() as this = 
    inherit Banshee.Sources.Source (AddinManager.CurrentLocalizer.GetString (Constants.NAME),
                                    AddinManager.CurrentLocalizer.GetString (Constants.NAME),
                                    Constants.SORT,
                                    "extension-unique-id")
    let name = Constants.NAME + ".Source"
    do 
        this.Properties.SetStringList ("Icon.Name", "multimedia-player")
        Log.DebugFormat ("Instantiating {0}", name)

//type Service() =
//    let name = Constants.NAME + ".Service"
//    do Log.DebugFormat ("Instantiating {0}", name)
//    interface IExtensionService with
//        member x.Dispose () = ()
//        member x.ServiceName = name
//        member x.Initialize () = Log.DebugFormat ("Initializing {0}", name)

