//
// KaraokeService.cs
//
// Authors:
//   Frank Ziegler <funtastix@googlemail.com>
//
// Copyright (C) 2011 Frank Ziegler
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

using Banshee.ServiceStack;
using Banshee.Streamrecorder.Gst;
using Banshee.Gui;
using Banshee.Configuration;

namespace Banshee.Karaoke
{
    public class KaraokeService : IExtensionService, IDelayedInitializeService, IDisposable
    {
        Bin audiobin;
        Bin playbin;
        Bin audiotee;
        Element audiokaraoke;

        bool has_karaoke = false;

        private Gtk.ActionGroup actions;
        private InterfaceActionService action_service;
        private uint ui_menu_id = 0;
        private uint ui_button_id = 0;

        private bool karaoke_enabled = true;
        private bool lyrics_enabled = false;

        private float effect_level = 1.0f;
        private float filter_band = 220.0f;
        private float filter_width = 100.0f;

        public static event EventHandler LyricsEnabledChanged;

        public KaraokeService ()
        {
            karaoke_enabled = IsKaraokeEnabledEntry.Get ().Equals ("True") ? true : false;
            lyrics_enabled = IsLyricsEnabledEntry.Get ();
            effect_level = (float)EffectLevelEntry.Get ();
            effect_level = effect_level / 100;
            filter_band = (float)FilterBandEntry.Get ();
            filter_width = (float)FilterWidthEntry.Get ();
        }

        #region IExtensionService implementation
        void IExtensionService.Initialize ()
        {
            Marshaller.Init ();
            has_karaoke = Marshaller.CheckGstPlugin ("audiokaraoke");
            Hyena.Log.Debug ("[Karaoke] GstPlugin audiokaraoke" + (has_karaoke ? "" : " not") + " found");
            if (!has_karaoke) {
                Hyena.Log.Warning ("[Karaoke] audiokaraoke is not available, please install gstreamer-good-plugins");
                return;
            }

            action_service = ServiceManager.Get<InterfaceActionService> ();
            actions = new Gtk.ActionGroup ("Karaoke");

            actions.Add (new Gtk.ActionEntry[] { new Gtk.ActionEntry ("KaraokeAction", null,
                             AddinManager.CurrentLocalizer.GetString ("_Karaoke"), null, null, null),
                             new Gtk.ActionEntry ("KaraokeConfigureAction", Gtk.Stock.Properties,
                                 AddinManager.CurrentLocalizer.GetString ("_Configure"), null,
                                 AddinManager.CurrentLocalizer.GetString ("Configure the Karaoke extension"), OnConfigure) });

            Gdk.Pixbuf icon = new Gdk.Pixbuf (System.Reflection.Assembly.GetExecutingAssembly ()
                                              .GetManifestResourceStream ("microphone.png"));

            Gtk.IconSet iconset = new Gtk.IconSet (icon);
            Gtk.IconFactory iconfactory = new Gtk.IconFactory ();
            iconfactory.Add ("microphone", iconset);
            iconfactory.AddDefault ();

            actions.Add (new Gtk.ToggleActionEntry[] { new Gtk.ToggleActionEntry ("KaraokeEnableAction", "microphone",
                             AddinManager.CurrentLocalizer.GetString ("_Activate Karaoke mode"), null,
                             AddinManager.CurrentLocalizer.GetString ("Activate Karaoke mode"),
                             OnActivateKaraoke, karaoke_enabled) });

            action_service.UIManager.InsertActionGroup (actions, 0);
            ui_menu_id = action_service.UIManager.AddUiFromResource ("KaraokeMenu.xml");
            ui_button_id = action_service.UIManager.AddUiFromResource ("KaraokeButton.xml");
        }

        /// <summary>
        /// Activates or deactivates Karaoke mode
        /// </summary>
        /// <param name="o">
        /// A <see cref="System.Object"/> -- not used
        /// </param>
        /// <param name="ea">
        /// A <see cref="EventArgs"/> -- not used
        /// </param>
        public void OnActivateKaraoke (object o, EventArgs ea)
        {
            karaoke_enabled = !karaoke_enabled;
            IsKaraokeEnabledEntry.Set (karaoke_enabled.ToString ());

            if (!karaoke_enabled) {
                audiokaraoke.SetFloatProperty ("level", 0);
                audiokaraoke.SetFloatProperty ("mono-level", 0);
            } else {
                audiokaraoke.SetFloatProperty ("level", effect_level);
                audiokaraoke.SetFloatProperty ("mono-level", effect_level);
            }
        }

        /// <summary>
        /// Triggers configuration, shows the configuration dialog
        /// </summary>
        /// <param name="o">
        /// A <see cref="System.Object"/> -- not used
        /// </param>
        /// <param name="ea">
        /// A <see cref="EventArgs"/> -- not used
        /// </param>
        public void OnConfigure (object o, EventArgs ea)
        {
            new KaraokeConfigDialog (this, EffectLevel, FilterBand, FilterWidth, IsLyricsEnabled);
        }

        void OnLyricsEnabledChanged ()
        {
            EventHandler handler = LyricsEnabledChanged;
            if (handler != null) {
                handler (this, new EventArgs ());
            }
        }

        void IDelayedInitializeService.DelayedInitialize ()
        {
            if (!has_karaoke) return;

            playbin = new Bin (ServiceManager.PlayerEngine.ActiveEngine.GetBaseElements ()[0]);
            audiobin = new Bin (ServiceManager.PlayerEngine.ActiveEngine.GetBaseElements ()[1]);
            audiotee = new Bin (ServiceManager.PlayerEngine.ActiveEngine.GetBaseElements ()[2]);

            if (playbin.IsNull ()) {
                Hyena.Log.Debug ("[Karaoke] Playbin is not yet initialized, cannot start Karaoke Mode");
            }

            audiokaraoke = audiobin.GetByName ("karaoke");

            if (audiokaraoke.IsNull ()) {
                audiokaraoke = ElementFactory.Make ("audiokaraoke","karaoke");

                //add audiokaraoke to audiobin
                audiobin.Add (audiokaraoke);

                //setting new audiobin sink to audiokaraoke sink
                GhostPad teepad = new GhostPad (audiobin.GetStaticPad ("sink").ToIntPtr ());
                Pad audiokaraokepad = audiokaraoke.GetStaticPad ("sink");
                teepad.SetTarget (audiokaraokepad);

                //link audiokaraoke sink and audiotee sink
                audiokaraoke.Link (audiotee);
            }

            if (!karaoke_enabled) {
                audiokaraoke.SetFloatProperty ("level", 0);
                audiokaraoke.SetFloatProperty ("mono-level", 0);
            } else {
                audiokaraoke.SetFloatProperty ("level", effect_level);
                audiokaraoke.SetFloatProperty ("mono-level", effect_level);
            }
        }
        #endregion

        #region IDisposable implementation
        void IDisposable.Dispose ()
        {
            if (has_karaoke && !playbin.IsNull () && !audiokaraoke.IsNull ()) {
                audiokaraoke.SetFloatProperty ("level", 0);
                audiokaraoke.SetFloatProperty ("mono-level", 0);
            }

            if (ui_menu_id > 0) {
                action_service.UIManager.RemoveUi (ui_menu_id);
            }
            if (ui_button_id > 0) {
                action_service.UIManager.RemoveUi (ui_button_id);
            }
            if (actions != null) {
                action_service.UIManager.RemoveActionGroup (actions);
                actions = null;
            }
        }
        #endregion

        /// <summary>
        /// The service name
        /// </summary>
        string IService.ServiceName {
            get { return "KaraokeService"; }
        }

        public bool IsKaraokeEnabled
        {
            get { return karaoke_enabled; }
        }

        public bool IsLyricsEnabled {
            get { return this.lyrics_enabled; }
            set {
                lyrics_enabled = value;
                OnLyricsEnabledChanged ();
            }
        }

        public void ApplyKaraokeEffectLevel (float new_level)
        {
            if (!karaoke_enabled) {
                return;
            }
            audiokaraoke.SetFloatProperty ("level", new_level);
            audiokaraoke.SetFloatProperty ("mono-level", new_level);
        }

        public float EffectLevel
        {
            get { return effect_level; }
            set
            {
                effect_level = value;
                ApplyKaraokeEffectLevel (effect_level);
            }
        }

        public void ApplyKaraokeFilterBand (float new_band)
        {
            audiokaraoke.SetFloatProperty ("filter-band", new_band);
        }

        public float FilterBand
        {
            get { return filter_band; }
            set
            {
                filter_band = value;
                ApplyKaraokeFilterBand (filter_band);
            }
        }

        public void ApplyKaraokeFilterWidth (float new_width)
        {
            audiokaraoke.SetFloatProperty ("filter-width", new_width);
        }

        public float FilterWidth
        {
            get { return filter_width; }
            set
            {
                filter_width = value;
                ApplyKaraokeFilterWidth (filter_width);
            }
        }

        public static readonly SchemaEntry<string> IsKaraokeEnabledEntry = new SchemaEntry<string> (
               "plugins.karaoke", "karaoke_enabled", "", "Is Karaoke mode enabled", "Is Karaoke mode enabled");

        public static readonly SchemaEntry<bool> IsLyricsEnabledEntry = new SchemaEntry<bool> (
               "plugins.karaoke", "karaokelyrics_enabled", false, "Is Karaoke lyrics display enabled", "Is Karaoke lyrics display enabled");

        public static readonly SchemaEntry<int> EffectLevelEntry = new SchemaEntry<int> (
               "plugins.karaoke", "effect_level", 100, "Effect Level", "Effect Level");

        public static readonly SchemaEntry<int> FilterBandEntry = new SchemaEntry<int> (
               "plugins.karaoke", "filter_band", 220, "Filter Band", "Filter Band");

        public static readonly SchemaEntry<int> FilterWidthEntry = new SchemaEntry<int> (
               "plugins.karaoke", "filter_width", 100, "Filter Width", "Filter Width");

    }
}
