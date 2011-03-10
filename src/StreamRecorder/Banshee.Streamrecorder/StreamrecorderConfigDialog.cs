//
// StreamrecorderConfigDialog.cs
//
//   Frank Ziegler
//   based on Banshee-Streamripper by Akseli Mantila <aksu@paju.oulu.fi>
//
// Copyright (C) 2009 Akseli Mantila
// Copyright (C) 2009 Frank Ziegler
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
using System.IO;

using Mono.Addins;

using Gtk;

using Banshee.ServiceStack;
using Banshee.I18n;

namespace Banshee.Streamrecorder
{

    /// <summary>
    /// A dialog to control StreamRecorder options
    /// </summary>
    public class StreamrecorderConfigDialog : Gtk.Dialog
    {
        StreamrecorderService streamrecorder_service;
        Gtk.Image preferences_image = new Gtk.Image ();
        Gtk.Label header_label = new Gtk.Label ();
        Gtk.Label description_label = new Gtk.Label ();
        Gtk.Label choose_folder_label = new Gtk.Label ();
        Gtk.Label choose_encoder_label = new Gtk.Label ();
        Gtk.Entry output_folder = new Gtk.Entry ();
        Gtk.Button choose_output_folder_button = new Gtk.Button (Gtk.Stock.Open);
        Gtk.CheckButton enable_import_ripped_songs = new Gtk.CheckButton ();
        Gtk.CheckButton enable_automatic_splitting = new Gtk.CheckButton ();
        Gtk.Button cancel_button = new Gtk.Button (Gtk.Stock.Cancel);
        Gtk.Button save_button = new Gtk.Button (Gtk.Stock.Save);
        Gtk.ComboBox encoderbox = new Gtk.ComboBox ();

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="service">
        /// The <see cref="StreamrecorderService"/> that is being configured
        /// </param>
        /// <param name="previous_output_folder">
        /// A <see cref="System.String"/> containing the previously configured output directory
        /// </param>
        /// <param name="previous_encoder">
        /// A <see cref="System.String"/> containing the previously configured encoder
        /// </param>
        /// <param name="is_importing_enabled">
        /// A <see cref="System.Boolean"/> indicating whether file scanning was previously enabled
        /// </param>
        /// <param name="is_splitting_enabled">
        /// A <see cref="System.Boolean"/> indicating whether file splitting was previously enabled
        /// </param>
        public StreamrecorderConfigDialog (StreamrecorderService service, string previous_output_folder, string previous_encoder, bool is_importing_enabled, bool is_splitting_enabled)
        {
            streamrecorder_service = service;

            preferences_image.Yalign = 0f;
            preferences_image.IconName = "gtk-preferences";
            preferences_image.IconSize = (int)IconSize.Dialog;
            preferences_image.Show ();
            header_label.Text = String.Format (AddinManager.CurrentLocalizer.GetString ("{0}Streamrecorder configuration\n{1}"), "<span weight=\"bold\" size=\"larger\">", "</span>");
            header_label.UseMarkup = true;
            header_label.Wrap = true;
            header_label.Yalign = 0f;
            header_label.Xalign = 0f;
            description_label.Text = AddinManager.CurrentLocalizer.GetString ("Please select output folder for ripped files and if ripped\n" + "files should be imported to media library.\n");
            description_label.Yalign = 0f;
            description_label.Xalign = 0f;
            choose_folder_label.Text = AddinManager.CurrentLocalizer.GetString ("Output folder:");
            choose_encoder_label.Text = AddinManager.CurrentLocalizer.GetString ("Encoder:");
            output_folder.Text = previous_output_folder;
            choose_output_folder_button.Label = AddinManager.CurrentLocalizer.GetString ("_Browse");
            choose_output_folder_button.Image = new Image ("gtk-directory", IconSize.Button);
            choose_output_folder_button.ShowAll ();
            cancel_button.Label = AddinManager.CurrentLocalizer.GetString ("_Cancel");
            cancel_button.Image = new Image ("gtk-cancel", IconSize.Button);
            save_button.Label = AddinManager.CurrentLocalizer.GetString ("_Save");
            save_button.Image = new Image ("gtk-save", IconSize.Button);
            enable_import_ripped_songs.Label = AddinManager.CurrentLocalizer.GetString ("Import files to media library");
            enable_import_ripped_songs.Active = StreamrecorderService.IsImportingEnabledEntry.Get ().Equals ("True") ? true : false;
            enable_automatic_splitting.Label = AddinManager.CurrentLocalizer.GetString ("Enable automatic files splitting by Metadata");
            enable_automatic_splitting.Active = StreamrecorderService.IsFileSplittingEnabledEntry.Get ().Equals ("True") ? true : false;

            encoderbox.Clear ();
            CellRendererText cell = new CellRendererText ();
            encoderbox.PackStart (cell, false);
            encoderbox.AddAttribute (cell, "text", 0);
            ListStore store = new ListStore (typeof(string));
            encoderbox.Model = store;

            int row = -1;
            int chosen_row = -1;
            foreach (string encoder in streamrecorder_service.GetEncoders ()) {
                row++;
                store.AppendValues (encoder);
                if (encoder.Equals (previous_encoder)) {
                    chosen_row = row;
                    Hyena.Log.DebugFormat ("[StreamrecorderConfigDialog] found active encoder in row {1}: {0}", encoder, chosen_row);
                }
            }

            if (chosen_row > -1) {
                Gtk.TreeIter iter;
                encoderbox.Model.IterNthChild (out iter, chosen_row);
                encoderbox.SetActiveIter (iter);
            } else {
                Gtk.TreeIter iter;
                encoderbox.Model.GetIterFirst (out iter);
                encoderbox.SetActiveIter (iter);
            }

            HBox main_container = new HBox ();
            VBox action_container = new VBox ();

            main_container.Spacing = 12;
            main_container.BorderWidth = 6;

            action_container.PackStart (header_label, true, true, 0);
            action_container.PackStart (description_label, true, true, 0);
            VBox choosing_labels = new VBox ();
            choosing_labels.PackStart (choose_folder_label, true, true, 5);
            choosing_labels.PackStart (choose_encoder_label, true, true, 5);
            HBox folder_choosing = new HBox ();
            folder_choosing.PackStart (output_folder, true, true, 5);
            folder_choosing.PackStart (choose_output_folder_button, true, true, 0);
            VBox box_choosing = new VBox ();
            box_choosing.PackStart (folder_choosing, true, true, 0);
            box_choosing.PackStart (encoderbox, true, true, 5);
            HBox all_choosing = new HBox ();
            all_choosing.PackStart (choosing_labels, true, true, 0);
            all_choosing.PackStart (box_choosing, true, true, 0);

            action_container.PackStart (all_choosing, true, true, 5);
            action_container.PackStart (enable_automatic_splitting, true, true, 5);
            action_container.PackStart (enable_import_ripped_songs, true, true, 5);

            main_container.PackStart (preferences_image, true, true, 5);
            main_container.PackEnd (action_container, true, true, 5);
            this.VBox.PackStart (main_container, true, true, 5);

            AddActionWidget (cancel_button, 0);
            AddActionWidget (save_button, 0);

            choose_output_folder_button.Clicked += new EventHandler (OnChooseOutputFolderButtonClicked);
            cancel_button.Clicked += new EventHandler (OnCancelButtonClicked);
            save_button.Clicked += new EventHandler (OnSaveButtonClicked);

            Title = "Streamrecorder configuration";
            IconName = "gtk-preferences";
            Resizable = false;
            BorderWidth = 6;
            HasSeparator = false;
            this.VBox.Spacing = 12;

            ShowAll ();
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

            StreamrecorderService.IsImportingEnabledEntry.Set (enable_import_ripped_songs.Active.ToString ());
            streamrecorder_service.IsImportingEnabled = enable_import_ripped_songs.Active.ToString ().Equals ("True") ? true : false;

            StreamrecorderService.IsFileSplittingEnabledEntry.Set (enable_automatic_splitting.Active.ToString ());
            streamrecorder_service.IsFileSplittingEnabled = enable_automatic_splitting.Active.ToString ().Equals ("True") ? true : false;

            if (ValidateOutputFolderField ()) {
                streamrecorder_service.OutputDirectory = output_folder.Text.Trim ();
                StreamrecorderService.OutputDirectoryEntry.Set (output_folder.Text.Trim ());
            }

            streamrecorder_service.ActiveEncoder = encoderbox.ActiveText;
            StreamrecorderService.ActiveEncoderEntry.Set (encoderbox.ActiveText);

            Destroy ();
        }

        /// <summary>
        /// Handles click to the output folder choose button
        /// </summary>
        /// <param name="o">
        /// A <see cref="System.Object"/> -- not used
        /// </param>
        /// <param name="a">
        /// A <see cref="EventArgs"/> -- not used
        /// </param>
        private void OnChooseOutputFolderButtonClicked (object o, EventArgs a)
        {
            FileChooserDialog output_folder_chooser = new FileChooserDialog ("Choose output folder", this, FileChooserAction.SelectFolder, "Cancel", ResponseType.Cancel, "Open", ResponseType.Accept);

            ResponseType response = (ResponseType)output_folder_chooser.Run ();

            if (response == ResponseType.Accept) {
                output_folder.Text = output_folder_chooser.Filename;
            }

            output_folder_chooser.Destroy ();
        }

        /// <summary>
        /// Validates the choosen output folder
        /// </summary>
        /// <returns>
        /// A <see cref="System.Boolean"/> returns true if the output folder is valid, false otherwise
        /// </returns>
        private bool ValidateOutputFolderField ()
        {
            if (Banshee.IO.Directory.Exists (output_folder.Text.Trim ())) {
                return true;
            }
            try {
                Banshee.IO.Directory.Create (output_folder.Text);
                return true;
            } catch {
                Hyena.Log.Debug ("[StreamrecorderConfigDialog] <ValidateOutputFolderField> NOT VALID!");
                return false;
            }
        }
    }
}
