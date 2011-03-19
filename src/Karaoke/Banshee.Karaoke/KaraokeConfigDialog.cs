// 
// KaraokeConfigDialog.cs
// 
// Author:
//   Frank Ziegler <funtastix@googlemail.com>
// 
// Copyright (c) 2011 Frank Ziegler
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

using System;

using Gtk;
using Mono.Addins;

using Banshee.Gui.Dialogs;

namespace Banshee.Karaoke
{

    public class KaraokeConfigDialog : BansheeDialog
    {
        KaraokeService karaoke_service;

        private double default_filter_width = 100;
        private double default_filter_band = 220;

        Gtk.Image preferences_image = new Gtk.Image ();
        Gtk.Label header_label = new Gtk.Label ();
        Gtk.Label description_label = new Gtk.Label ();

        Gtk.Label choose_level_label = new Gtk.Label ();
        Gtk.Label choose_band_label = new Gtk.Label ();
        Gtk.Label choose_width_label = new Gtk.Label ();

        Gtk.HScale level_scale = new Gtk.HScale (0, 100, 1);
        Gtk.HScale band_scale = new Gtk.HScale (0, 441, 1);
        Gtk.HScale width_scale = new Gtk.HScale (0, 100, 1);

        Gtk.Button default_button = new Gtk.Button (Gtk.Stock.Home);

        Gtk.Button cancel_button = new Gtk.Button (Gtk.Stock.Cancel);
        Gtk.Button save_button = new Gtk.Button (Gtk.Stock.Save);

        Gtk.CheckButton enable_lyrics_display = new Gtk.CheckButton ();

        float initial_level;
        float initial_band;
        float initial_width;

        public KaraokeConfigDialog (KaraokeService service,
                                    float current_level,
                                    float current_band,
                                    float current_width,
                                    bool lyrics_enabled)
        {
            karaoke_service = service;

            initial_level = current_level;
            initial_band = current_band;
            initial_width = current_width;

            preferences_image.Yalign = 0f;
            preferences_image.IconName = "gtk-preferences";
            preferences_image.IconSize = (int)IconSize.Dialog;

            header_label.Text = String.Format ("<span weight=\"bold\" size=\"larger\">{0}</span>",
                                               AddinManager.CurrentLocalizer.GetString ("Karaoke configuration"));
            header_label.UseMarkup = true;
            header_label.Yalign = 0f;
            header_label.Xalign = 0f;
            description_label.Text = AddinManager.CurrentLocalizer.GetString ("You can alter the parameters of the karaoke effect here and "
                                                                              + "define custom filter-band, filter-width and effect level.");
            description_label.Yalign = 0f;
            description_label.Xalign = 0f;
            description_label.Wrap = true;

            choose_level_label.Text = AddinManager.CurrentLocalizer.GetString ("Effect Level:");
            choose_band_label.Text = AddinManager.CurrentLocalizer.GetString ("Filter Band:");
            choose_width_label.Text = AddinManager.CurrentLocalizer.GetString ("Filter Width:");
            choose_level_label.Xalign = choose_band_label.Xalign = choose_width_label.Xalign = 0f;

            level_scale.Digits = 0;
            level_scale.DrawValue = true;
            level_scale.TooltipText = AddinManager.CurrentLocalizer.GetString ("The effect level defines to what degree the voice "
                                                                              + "frequencies are reduced.");
            level_scale.ValuePos = PositionType.Right;
            level_scale.Value = current_level * 100;
            level_scale.ValueChanged += OnLevelValueChanged;
            band_scale.Digits = 0;
            band_scale.DrawValue = true;
            band_scale.TooltipText = AddinManager.CurrentLocalizer.GetString ("The filter band defines position of filter within "
                                                                              + "the frequency spectrum.");
            band_scale.ValuePos = PositionType.Right;
            band_scale.Value = current_band;
            band_scale.ValueChanged += OnBandValueChanged;
            width_scale.Digits = 0;
            width_scale.DrawValue = true;
            width_scale.TooltipText = AddinManager.CurrentLocalizer.GetString ("The filter width defines how wide the spectrum "
                                                                              + "of the filtered frequencies is.");
            width_scale.ValuePos = PositionType.Right;
            width_scale.Value = current_width;
            width_scale.ValueChanged += OnWidthValueChanged;

            enable_lyrics_display.Label = AddinManager.CurrentLocalizer.GetString ("Enable lyrics display in Context Pane");
            enable_lyrics_display.Active = lyrics_enabled;

            default_button.Label = AddinManager.CurrentLocalizer.GetString ("Restore _defaults");
            default_button.Image = new Image ("gtk-home", IconSize.Button);

            cancel_button.Label = AddinManager.CurrentLocalizer.GetString ("_Cancel");
            cancel_button.Image = new Image ("gtk-cancel", IconSize.Button);
            save_button.Label = AddinManager.CurrentLocalizer.GetString ("_Save");
            save_button.Image = new Image ("gtk-save", IconSize.Button);

            HBox main_container = new HBox ();
            VBox action_container = new VBox ();

            action_container.PackStart (header_label, true, true, 0);
            action_container.PackStart (description_label, true, true, 0);

            Table table = new Table (4, 2, false);
            table.RowSpacing = 6;
            table.ColumnSpacing = 12;
            table.Attach (choose_level_label, 0, 1, 0, 1,
                          AttachOptions.Fill, AttachOptions.Fill, 0, 0);
            table.Attach (level_scale, 1, 2, 0, 1,
                          AttachOptions.Expand | AttachOptions.Fill, AttachOptions.Fill, 0, 0);
            table.Attach (choose_band_label, 0, 1, 1, 2,
                          AttachOptions.Fill, AttachOptions.Fill, 0, 0);
            table.Attach (band_scale, 1, 2, 1, 2,
                          AttachOptions.Expand | AttachOptions.Fill, AttachOptions.Fill, 0, 0);
            table.Attach (choose_width_label, 0, 1, 2, 3,
                          AttachOptions.Fill, AttachOptions.Fill, 0, 0);
            table.Attach (width_scale, 1, 2, 2, 3,
                          AttachOptions.Expand | AttachOptions.Fill, AttachOptions.Fill, 0, 0);
            table.Attach (enable_lyrics_display, 0, 2, 3, 4,
                          AttachOptions.Expand | AttachOptions.Fill, AttachOptions.Fill, 0, 0);

            action_container.PackStart (table, true, true, 5);

            main_container.PackStart (preferences_image, false, true, 5);
            main_container.PackEnd (action_container, true, true, 5);
            this.VBox.PackStart (main_container, true, true, 5);

            AddActionWidget (default_button, ResponseType.None);

            AddActionWidget (cancel_button, 0);
            AddActionWidget (save_button, 0);

            default_button.Clicked += new EventHandler (OnDefaultButtonClicked);
            cancel_button.Clicked += new EventHandler (OnCancelButtonClicked);
            save_button.Clicked += new EventHandler (OnSaveButtonClicked);

            Title = "Karaoke configuration";
            IconName = "gtk-preferences";
            HeightRequest = 260;
            WidthRequest = 500;

            ShowAll ();
        }

        void OnLevelValueChanged (object sender, EventArgs e)
        {
            karaoke_service.ApplyKaraokeEffectLevel ((float)(level_scale.Value / 100));
        }

        void OnBandValueChanged (object sender, EventArgs e)
        {
            karaoke_service.ApplyKaraokeFilterBand ((float)band_scale.Value);
        }

        void OnWidthValueChanged (object sender, EventArgs e)
        {
            karaoke_service.ApplyKaraokeFilterWidth ((float)width_scale.Value);
        }

        /// <summary>
        /// Handles click on the Default button
        /// </summary>
        /// <param name="o">
        /// A <see cref="System.Object"/> -- not used
        /// </param>
        /// <param name="a">
        /// A <see cref="EventArgs"/> -- not used
        /// </param>
        private void OnDefaultButtonClicked (object o, EventArgs a)
        {
            level_scale.Value = 100;
            band_scale.Value = default_filter_band;
            width_scale.Value = default_filter_width;
        }

        /// <summary>
        /// Handles click on the Cancel button
        /// </summary>
        /// <param name="o">
        /// A <see cref="System.Object"/> -- not used
        /// </param>
        /// <param name="a">
        /// A <see cref="EventArgs"/> -- not used
        /// </param>
        private void OnCancelButtonClicked (object o, EventArgs a)
        {
            karaoke_service.ApplyKaraokeEffectLevel (initial_level);
            karaoke_service.ApplyKaraokeFilterBand (initial_band);
            karaoke_service.ApplyKaraokeFilterWidth (initial_width);

            Destroy ();
        }

        /// <summary>
        /// Sets the configuration and saves it to SchemaEntries
        /// </summary>
        /// <param name="o">
        /// A <see cref="System.Object"/> -- not used
        /// </param>
        /// <param name="a">
        /// A <see cref="EventArgs"/> -- not used
        /// </param>
        private void OnSaveButtonClicked (object o, EventArgs a)
        {
            KaraokeService.EffectLevelEntry.Set ((int)level_scale.Value);
            karaoke_service.EffectLevel = (float)(level_scale.Value / 100);

            KaraokeService.FilterBandEntry.Set ((int)band_scale.Value);
            karaoke_service.FilterBand = (float)band_scale.Value;

            KaraokeService.FilterWidthEntry.Set ((int)width_scale.Value);
            karaoke_service.FilterWidth = (float)width_scale.Value;

            KaraokeService.IsLyricsEnabledEntry.Set (enable_lyrics_display.Active);
            karaoke_service.IsLyricsEnabled = enable_lyrics_display.Active;

            Destroy ();
        }

    }
}

