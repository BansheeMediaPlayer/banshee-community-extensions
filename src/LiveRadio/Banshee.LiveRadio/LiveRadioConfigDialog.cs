//
// LiveRadioConfigDialog.cs
//
// Authors:
//   Frank Ziegler <funtastix@googlemail.com>
//
// Copyright (C) 2010 Frank Ziegler
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
using Banshee.LiveRadio.Plugins;

using Gtk;
using Mono.Unix;
using System.Collections.Generic;

namespace Banshee.LiveRadio
{

    /// <summary>
    /// A dynamic configuration dialog for LiveRadio plugins
    /// </summary>
    public class LiveRadioConfigDialog : Gtk.Dialog
    {

        Gtk.Notebook notebook = new Gtk.Notebook ();
        Gtk.Image preferences_image = new Gtk.Image ();
        Gtk.Label header_label = new Gtk.Label ();
        Gtk.Label description_label = new Gtk.Label ();
        Gtk.Button cancel_button = new Gtk.Button (Gtk.Stock.Cancel);
        Gtk.Button apply_button = new Gtk.Button (Gtk.Stock.Apply);
        Gtk.Button save_button = new Gtk.Button (Gtk.Stock.Save);

        private List<ILiveRadioPlugin> plugins;

        /// <summary>
        /// Constructor -- builds a basic dialog structure and adds each plugins configuration widget into a notebook
        /// </summary>
        /// <param name="plugins">
        /// A <see cref="List<ILiveRadioPlugin>"/> -- the list of plugins
        /// </param>
        public LiveRadioConfigDialog (List<ILiveRadioPlugin> plugins)
        {
            this.plugins = plugins;

            preferences_image.Yalign = 0f;
            preferences_image.IconName = "gtk-preferences";
            preferences_image.IconSize = (int)IconSize.Dialog;
            preferences_image.Show ();
            header_label.Text = String.Format (Catalog.GetString ("{0}LiveRadio configuration\n{1}"), "<span weight=\"bold\" size=\"larger\">", "</span>");
            header_label.UseMarkup = true;
            header_label.Wrap = true;
            header_label.Yalign = 0f;
            header_label.Xalign = 0f;

            description_label.Text = Catalog.GetString ("Please set your preferences for your LiveRadio plugins\n");
            description_label.Yalign = 0f;
            description_label.Xalign = 0f;

            foreach (ILiveRadioPlugin plugin in plugins)
            {
                Widget plugin_widget = plugin.ConfigurationWidget;
                if (plugin_widget != null)
                    notebook.AppendPage(plugin_widget, new Label (plugin.Name));
            }
            cancel_button.Label = Catalog.GetString ("_Cancel");
            cancel_button.Image = new Image ("gtk-cancel", IconSize.Button);
            apply_button.Label = Catalog.GetString ("_Apply");
            apply_button.Image = new Image ("gtk-apply", IconSize.Button);
            save_button.Label = Catalog.GetString ("_Save");
            save_button.Image = new Image ("gtk-save", IconSize.Button);

            HBox main_container = new HBox ();
            VBox action_container = new VBox ();

            main_container.Spacing = 12;
            main_container.BorderWidth = 6;
            
            action_container.PackStart (header_label, true, true, 0);
            action_container.PackStart (description_label, true, true, 0);

            main_container.PackStart (preferences_image, true, true, 5);
            main_container.PackEnd (action_container, true, true, 5);
            this.VBox.PackStart (main_container, true, true, 5);

            this.VBox.PackStart (notebook, true, true, 5);

            AddActionWidget (cancel_button, 0);
            AddActionWidget (apply_button, 0);
            AddActionWidget (save_button, 0);

            cancel_button.Clicked += new EventHandler (OnCancelButtonClicked);
            apply_button.Clicked += new EventHandler (OnApplyButtonClicked);
            save_button.Clicked += new EventHandler (OnSaveButtonClicked);
            
            Title = "LiveRadio configuration";
            IconName = "gtk-preferences";
            Resizable = false;
            BorderWidth = 6;
            HasSeparator = false;
            this.VBox.Spacing = 12;
            
            ShowAll ();
        }

        /// <summary>
        /// Handles when the user presses the Cancel Button
        /// </summary>
        /// <param name="o">
        /// A <see cref="System.Object"/> -- not used
        /// </param>
        /// <param name="a">
        /// A <see cref="EventArgs"/> -- not used
        /// </param>
        private void OnCancelButtonClicked (object o, EventArgs a)
        {
            Destroy ();
        }

        /// <summary>
        /// Handles when the user presses the Save Button
        /// </summary>
        /// <param name="o">
        /// A <see cref="System.Object"/> -- not used
        /// </param>
        /// <param name="a">
        /// A <see cref="EventArgs"/> -- not used
        /// </param>
        private void OnSaveButtonClicked (object o, EventArgs a)
        {
            SaveConfiguration();
            Destroy ();
        }

        /// <summary>
        /// Handles when the user presses the Apply Button
        /// </summary>
        /// <param name="o">
        /// A <see cref="System.Object"/> -- not used
        /// </param>
        /// <param name="a">
        /// A <see cref="EventArgs"/> -- not used
        /// </param>
        private void OnApplyButtonClicked (object o, EventArgs a)
        {
            SaveConfiguration();
        }

        /// <summary>
        /// Initiates each plugin's SaveConfiguration method
        /// </summary>
        private void SaveConfiguration()
        {
            foreach (ILiveRadioPlugin plugin in plugins)
            {
                plugin.SaveConfiguration ();
            }
        }

    }
}
