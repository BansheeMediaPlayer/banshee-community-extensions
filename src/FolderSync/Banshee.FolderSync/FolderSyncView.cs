//
// Copyright (c) 2011 Timo Dörr <timo.doerr@latecrew.de>
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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

using Gtk;
using Mono.Addins;

using Hyena.Widgets;
using Banshee.Sources.Gui;

namespace Banshee.FolderSync
{
    public class OptionsModel
    {
        public bool OverwriteExisting;
        public bool CreateM3u;
        public uint SubfolderDepth;
        public string TargetFolder;
        public List<Playlist> SelectedPlaylists;
    }

    public class FolderSyncView : Frame
    {
        public List<Playlist> SelectedPlaylists;

        public OptionsModel GetOptions ()
        {
            var options = new OptionsModel {
                 OverwriteExisting = overwrite_existing.Active,
                 CreateM3u = create_m3u.Active,
                 SubfolderDepth = 0,
                 TargetFolder = target_chooser.Uri
             };
            // if activated, we create subfolder until given depth
            if (create_subfolders.Active)
                options.SubfolderDepth = (uint)subfolder_depth.Value;

            // make sure target always has ending directory seperator '/'
            if (options.TargetFolder.Last () != System.IO.Path.DirectorySeparatorChar)
                options.TargetFolder += System.IO.Path.DirectorySeparatorChar;

            // get the currently user-selected playlists
            options.SelectedPlaylists = new List<Playlist> ();
            Gtk.TreeIter iter;
            Gtk.TreeModel model;
            var tree_paths = playlist_tree.Selection.GetSelectedRows (out model);
            foreach (var path in tree_paths) {
                model.GetIter (out iter, path);
                Hyena.Log.DebugFormat ("Selected Playlist: ID {0}, Name: {1}", model.GetValue (iter, 0),
                     model.GetValue (iter, 1));

                options.SelectedPlaylists.Add (new Playlist ((uint)model.GetValue (iter, 0)));
            }
            return options;
        }

        public Gtk.ProgressBar Progress = new Gtk.ProgressBar () {
            Orientation = ProgressBarOrientation.LeftToRight, DoubleBuffered = true };
        public Gtk.Button StartSyncButton = new Button () {
            Label = AddinManager.CurrentLocalizer.GetString("Start sync") };

        // the liststore is populated from outside so have it public
        // store PlaylistID and PlaylistName
        public Gtk.ListStore PlaylistStore = new Gtk.ListStore (typeof(uint), typeof(string));
        Gtk.TreeView playlist_tree = new Gtk.TreeView ();
        Gtk.FileChooserButton target_chooser = new Gtk.FileChooserButton (
            AddinManager.CurrentLocalizer.GetString ("Choose target sync folder"),
            FileChooserAction.SelectFolder);
        Gtk.CheckButton create_m3u =
            new Gtk.CheckButton (AddinManager.CurrentLocalizer.GetString ("Create M3U Playlist"));
        Gtk.CheckButton overwrite_existing =
            new Gtk.CheckButton (AddinManager.CurrentLocalizer.GetString ("Overwrite existing files"));
        Gtk.CheckButton create_subfolders =
            new Gtk.CheckButton (AddinManager.CurrentLocalizer.GetString ("Create subfolders"));
        Gtk.SpinButton subfolder_depth = new Gtk.SpinButton (0f, 10f, 1f) {
            Text = AddinManager.CurrentLocalizer.GetString("Subfolder depth"),
            Sensitive = false
        };

#region private fields
        Gtk.VBox vbox_main = new VBox (false, 1);
        // hbox for filechooser & label
        Gtk.HBox hbox_chooser = new HBox (false, 1);
        Gtk.HPaned main_hpane = new Gtk.HPaned ();
        // hbox for subfolder depth
        Gtk.HBox hbox_subfolder = new HBox (false, 1);
        Gtk.VBox vbox_folder_and_option = new VBox (false, 1);
        Gtk.VBox vbox_checkbox = new VBox (false, 1);
        Gtk.Frame options_frame = new Gtk.Frame (AddinManager.CurrentLocalizer.GetString ("Options")) {
            ShadowType = ShadowType.Out };
        Gtk.Alignment frame_alignment = new Gtk.Alignment (0.5f, 0.5f, 1.0f, 1.0f);
        Gtk.Alignment startbutton_alignment = new Gtk.Alignment (0.25f, 0.25f, 1.0f, 1.0f);
#endregion

        public void Reload ()
        {
            Progress.Fraction = 0f;
            Progress.Text = null;
            playlist_tree.Model = PlaylistStore;
            // FIXME find a better way
            // rebind the model to force refreshing
            Gtk.Main.IterationDo (false);
        }

        public FolderSyncView ()
        {
            vbox_main.PackStart (main_hpane, true, true, 1);
            vbox_main.PackStart (Progress, false, false, 1);
            main_hpane.Pack1 (playlist_tree, true, true);
            // right hand side is folder select and options
            //hbox_main.PackStart (vbox_folder_and_option, true, true, 0);
            main_hpane.Pack2 (vbox_folder_and_option, true, true);
            hbox_chooser.PackStart (
                new Gtk.Label (AddinManager.CurrentLocalizer.GetString ("Sync to directory") + ":"),
                false, false, 1);
            hbox_chooser.PackStart (target_chooser, true, true, 1);
            vbox_folder_and_option.PackStart (hbox_chooser, false, false, 1);
            vbox_folder_and_option.PackStart (options_frame, false, false, 1);

            options_frame.Add (frame_alignment);

            frame_alignment.Add (vbox_checkbox);
            vbox_checkbox.PackStart (create_m3u, true, true, 1);
            vbox_checkbox.PackStart (overwrite_existing, true, true, 1);
            vbox_checkbox.PackStart (hbox_subfolder, true, true, 1);
            hbox_subfolder.PackStart (create_subfolders, true, true, 1);
            hbox_subfolder.PackStart (
                new Gtk.Label (AddinManager.CurrentLocalizer.GetString ("Subfolder depth") + ":"), true, true, 1);
            hbox_subfolder.PackStart (subfolder_depth, true, true, 1);

            subfolder_depth.Value = 1;
            create_subfolders.Clicked += delegate(object sender, EventArgs e) {
                subfolder_depth.Sensitive = create_subfolders.Active;
            };

            startbutton_alignment.Add (StartSyncButton);
            vbox_folder_and_option.PackStart (startbutton_alignment, false, false, 0);

            Add (vbox_main);

            // PLAYLIST TREEVIEW stuff
            // connect data model to the TreeView
            playlist_tree.Model = PlaylistStore;
            playlist_tree.Selection.Mode = Gtk.SelectionMode.Multiple;

            // new column & renderer for the playlists
            var playlist_column = new TreeViewColumn ();
            var playlist_cell_renderer = new Gtk.CellRendererText ();
            playlist_column.Title = "Playlists";
            // the the cell renderer, set to type text, and choose 1st position
            // from the model (counting starts on 0)
            playlist_column.PackStart (playlist_cell_renderer, true);
            playlist_column.AddAttribute (playlist_cell_renderer, "text", 1);
            var select_column = new TreeViewColumn ();
            var select_cell_renderer = new Gtk.CellRendererToggle ();
            select_column.Title = "Sync";
            select_column.PackStart (select_cell_renderer, false);
            select_column.AddAttribute (select_cell_renderer, "active", 0);

            //TODO enable checkbox in the selection window
            //playlist_tree.AppendColumn (select_column);
            // order of Append matters, so first add select, then the playlist
            playlist_tree.AppendColumn (playlist_column);

            // show all the widgets in this window
            ShowAll ();
        }
    }
}
