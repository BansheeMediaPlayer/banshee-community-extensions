//
// AcoustIDPreferences.fs
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

open Banshee.Preferences;
open Banshee.ServiceStack;

open Mono.Unix;

open Gtk;

open Gnome.Keyring;

type AcoustIDPreferences private () as x = 
    let api_key_url = "https://acoustid.org/api-key"
    let apikey_section = new Section ("acoustid-acount", "AcoustID", 20)
    static let instance = new AcoustIDPreferences ()
    let install_handler =  new EventHandler(fun sender eventargs -> x.OnPreferencesServiceInstallWidgetAdapters(sender, eventargs ))
    do
        let service = ServiceManager.Get<PreferenceService> ();
      
        if obj.ReferenceEquals (service, null) |> not then
            apikey_section.Add (new VoidPreference ("acoustid-pref")) |> ignore
            service.InstallWidgetAdapters.AddHandler (install_handler)
            
    interface IDisposable with
        member x.Dispose () =
            let service = ServiceManager.Get<PreferenceService> ()
            if obj.ReferenceEquals (service, null) |> not then
                service.InstallWidgetAdapters.RemoveHandler (install_handler)
            ()
    
    member x.Section with get () = apikey_section
    
    member x.OnPreferencesServiceInstallWidgetAdapters (sender, args) =
        if obj.ReferenceEquals (apikey_section, null) |> not then
            let align = new Alignment (0.5f, 0.5f, 1.0f, 1.0f);
            align.LeftPadding <- 20u
            align.RightPadding <- 20u
            align.TopPadding <- 5u

            let button_box = new HBox (Spacing = 6)

            button_box.PackStart (new Label (Catalog.GetString ("_API Key")), false, false, 0u);

            let apikey_entry = new Entry (Text = PasswordManager.ReadAcoustIDKey ())
            apikey_entry.FocusOutEvent.AddHandler (fun s o -> PasswordManager.SaveAcoustIDKey (apikey_entry.Text));
            apikey_entry.GrabFocus ()
            button_box.PackStart (apikey_entry, true, true, 10u)

            let signup_button = new Gtk.LinkButton (api_key_url, Catalog.GetString ("Get your API key"))
            signup_button.Xalign <- 0.0f
            button_box.PackStart (signup_button, false, false, 0u)

            align.Add (button_box);

            align.ShowAll ();
            apikey_section. ["acoustid-pref"].DisplayWidget <- align;
            
    static member Instance with get () = instance
