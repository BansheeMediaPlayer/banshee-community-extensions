//
// SoundMenuService.cs
//
// Authors:
//   Bertrand Lorentz <bertrand.lorentz@gmail.com>
//
// Copyright (C) 2010 Bertrand Lorentz
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;

using Mono.Addins;
using Mono.Unix;

using Banshee.Base;
using Banshee.Configuration;
using Banshee.Gui;
using Banshee.ServiceStack;
using Banshee.Preferences;

using Indicate;

namespace Banshee.SoundMenu
{
    public class SoundMenuService : IExtensionService
    {
        private GtkElementsService elements_service;
        private InterfaceActionService interface_action_service;
        private Server server;

        public SoundMenuService ()
        {
        }

        void IExtensionService.Initialize ()
        {
            elements_service = ServiceManager.Get<GtkElementsService> ();
            interface_action_service = ServiceManager.Get<InterfaceActionService> ();

            if (!ServiceStartup ()) {
                ServiceManager.ServiceStarted += OnServiceStarted;
            }
        }

        private void OnServiceStarted (ServiceStartedArgs args)
        {
            if (args.Service is Banshee.Gui.InterfaceActionService) {
                interface_action_service = (InterfaceActionService)args.Service;
            } else if (args.Service is GtkElementsService) {
                elements_service = (GtkElementsService)args.Service;
            }

            ServiceStartup ();
        }

        private bool ServiceStartup ()
        {
            if (elements_service == null || interface_action_service == null) {
                return false;
            }

            InstallPreferences ();
            server = Server.RefDefault ();

            ServiceManager.ServiceStarted -= OnServiceStarted;

            return true;
        }

        public void Dispose ()
        {
            UninstallPreferences ();

            elements_service = null;
            interface_action_service = null;
        }

        public void Register ()
        {
            server.SetType ("music.banshee");
            server.Show ();
        }

        public void Unregister ()
        {
            server.Hide ();
        }

#region Preferences

        private PreferenceBase enabled_pref;

        private void InstallPreferences ()
        {
            PreferenceService service = ServiceManager.Get<PreferenceService> ();
            if (service == null) {
                return;
            }

            enabled_pref = service["general"]["misc"].Add (
                new SchemaPreference<bool> (EnabledSchema,
                    Catalog.GetString ("_Show Banshee in the sound menu"),
                    Catalog.GetString ("Control Banshee through the sound menu"),
                    delegate { Enabled = EnabledSchema.Get (); })
            );
        }

        private void UninstallPreferences ()
        {
            ServiceManager.SourceManager.MusicLibrary.PreferencesPage["misc"].Remove (enabled_pref);
        }

        public bool Enabled {
            get { return EnabledSchema.Get (); }
            set {
                EnabledSchema.Set (value);
                if (value) {
                    Register ();
                } else {
                    Unregister ();
                }
            }
        }

        private static readonly SchemaEntry<bool> EnabledSchema = new SchemaEntry<bool> (
            "plugins.soundmenu", "enabled",
            false,
            "Show Banshee in the sound menu",
            "Show Banshee in the sound menu"
        );

#endregion

        string IService.ServiceName {
            get { return "SoundMenuService"; }
        }
    }
}