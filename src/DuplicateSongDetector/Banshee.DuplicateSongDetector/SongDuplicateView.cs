//
// DuplicateSongView.cs
// 
// Author:
//   Kevin Anthony <Kevin@NoSideRacing.com>
// 
// Copyright (c) 2011 Kevin Anthony
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
using System.IO;

using Mono.Addins;
using Mono.Unix;

using Gtk;

using Hyena;
using Hyena.Data.Sqlite;
using Hyena.Widgets;

using Banshee.Base;
using Banshee.Library;
using Banshee.Sources;
using Banshee.Sources.Gui;
using Banshee.ServiceStack;
using Banshee.Gui;

namespace Banshee.DuplicateSongDetector
{
    public class SongDuplicateView : RoundedFrame, ISourceContents
    {
        private static Gtk.ListStore MusicListStore;

        public SongDuplicateView ()
        {
            Gtk.ScrolledWindow Scroll = new Gtk.ScrolledWindow ();
            Gtk.TreeView Tree = new Gtk.TreeView ();
            Gtk.VBox vbox = new Gtk.VBox (false, 1);
            Gtk.HBox hbox = new Gtk.HBox (false, 1);
            Tree.RowActivated += OnRowClicked;
            //Buttons For Header
            Gtk.Button removeButton = new Gtk.Button ();
            removeButton.Label = AddinManager.CurrentLocalizer.GetString ("Remove Selected Songs");
            removeButton.Clicked += OnRemoveCommand;
            Gtk.Button deleteButton = new Gtk.Button ();
            deleteButton.Label = AddinManager.CurrentLocalizer.GetString ("Delete Selected Songs");
            deleteButton.Clicked += OnDeleteCommand;

            //Create 5 columns, first column is a checkbox, next 4 are text boxes
            Gtk.CellRendererToggle selectCell = new Gtk.CellRendererToggle ();
            selectCell.Activatable = true;
            selectCell.Toggled += OnSelectToggled;
            Tree.AppendColumn(AddinManager.CurrentLocalizer.GetString ("Select"),selectCell, "active", 0);
            Tree.AppendColumn(AddinManager.CurrentLocalizer.GetString ("Track Number"),new Gtk.CellRendererText (), "text", 1);
            Tree.AppendColumn(AddinManager.CurrentLocalizer.GetString ("Song Title"),new Gtk.CellRendererText (), "text", 2);
            Tree.AppendColumn(AddinManager.CurrentLocalizer.GetString ("Artist"),new Gtk.CellRendererText (), "text", 3);
            Tree.AppendColumn(AddinManager.CurrentLocalizer.GetString ("Album"),new Gtk.CellRendererText (), "text", 4);
            Tree.AppendColumn(AddinManager.CurrentLocalizer.GetString ("File"),new Gtk.CellRendererText (), "text", 5);
            // Remove From Library, Delete From Drive, Song Name, Artist Name, Album Name, Formated URI, Actual URI, Database Track ID
            MusicListStore = new Gtk.ListStore (typeof(bool),typeof(string), typeof(string), typeof(string), typeof(string), typeof(string), typeof(string), typeof(int));
            Tree.Model = MusicListStore;
            //Pack the Tree in a scroll window
            Scroll.Add (Tree);
            //Pack the buttons in a hbox
            hbox.PackStart (removeButton, false, false, 0);
            hbox.PackStart (deleteButton, false, false, 0);
            //pack the hbox->buttons and Scroll->Tree in a Vbox, tell the Scroll window to Expand and Fill the vbox
            vbox.PackStart (hbox, false, false, 0);
            vbox.PackStart (Scroll, true, true, 0);
            //pack the vbox in the Rounded Frame
            Add(vbox);
            //Finally, show everything
            ShowAll();
            
        }
#region ToggleBoxHandling
        void OnRowClicked (object o, RowActivatedArgs args)
        {
            TreeIter Iter;
            if ((o as TreeView).Selection.GetSelected(out Iter)){
                bool OldValue = (bool)MusicListStore.GetValue (Iter, 0);
                MusicListStore.SetValue (Iter, 0, !OldValue);
                Log.DebugFormat ("Setting Selection Value For Row {0} -> {1}", MusicListStore.GetStringFromIter (Iter), !OldValue);
            }
        }

        void OnSelectToggled (object o, ToggledArgs args)
        {
            //get the toggled row, pull out the value for the select box, and store the opposite.
            TreeIter Iter;
            if (MusicListStore.GetIter (out Iter, new TreePath (args.Path))) {
                bool OldValue = (bool)MusicListStore.GetValue (Iter, 0);
                MusicListStore.SetValue (Iter, 0, !OldValue);
                Log.DebugFormat ("Setting Selection Value For Row {0} -> {1}", MusicListStore.GetStringFromIter (Iter), !OldValue);
            }
        }
#endregion
#region ButtonPushed
        void OnRemoveCommand (object o, EventArgs args)
        {
            OnExecuteCommand (false);
        }

        void OnDeleteCommand (object o, EventArgs args)
        {
            OnExecuteCommand (true);
        }

        void OnExecuteCommand (bool Delete)
        {
            if (ConfirmRemove (Delete)) {
                MusicLibrarySource Library = ServiceManager.SourceManager.MusicLibrary;
                if (Library.CanRemoveTracks && Library.CanDeleteTracks) {
                    Gtk.TreeIter Iter = new Gtk.TreeIter ();
                    if (MusicListStore.GetIterFirst (out Iter)) {
                        do {
                            if (Delete && (bool)MusicListStore.GetValue (Iter, 0)) {
                                //delete
                                string Uri = (string)MusicListStore.GetValue (Iter, 5);
                                Uri = Uri.Replace ("file://", "");
                                RemoveTrack ((int)MusicListStore.GetValue (Iter, 7));
                                DeleteTrack (Uri);
                            } else if ((bool)MusicListStore.GetValue (Iter, 0)) {
                                RemoveTrack ((int)MusicListStore.GetValue (Iter, 7));
                            }
                        } while (MusicListStore.IterNext (ref Iter));
                        Library.Reload ();
                    } else {
                        Log.Warning ("Please Don't Click Execute with nothing selected");
                    }
                } else {
                    Log.Warning ("Can not remove or delete any tracks");
                }
                ReloadWindow ();
            }
        }

        private static bool ConfirmRemove (bool delete)
        {
            bool ret = false;
            string header = null;
            string message = null;
            string button_label = null;

            if (delete) {
                header = AddinManager.CurrentLocalizer.GetString (
                    "Are you sure you want to permanently delete the selected items?");
                message = AddinManager.CurrentLocalizer.GetString (
                    "If you delete the selection, it will be permanently lost.");
                button_label = "gtk-delete";
            } else {
                header = AddinManager.CurrentLocalizer.GetString (
                    "Remove selection from Library?");
                message = AddinManager.CurrentLocalizer.GetString (
                    "Are you sure you want to remove the selected items from your Library?");
                button_label = "gtk-remove";
            }

            HigMessageDialog md = new HigMessageDialog (ServiceManager.Get<GtkElementsService> ().PrimaryWindow,
                DialogFlags.DestroyWithParent, delete ? MessageType.Warning : MessageType.Question,
                ButtonsType.None, header, message);
            // Delete from Disk defaults to Cancel and the others to OK/Confirm.
            md.AddButton ("gtk-cancel", ResponseType.No, delete);
            md.AddButton (button_label, ResponseType.Yes, !delete);
            
            try {
                if (md.Run () == (int)ResponseType.Yes) {
                    ret = true;
                }
            } finally {
                md.Destroy ();
            }
            return ret;
        }
#endregion
#region DataHandlers
        private static void AddData (int ID, String track_number,String song, String artist, String album, String uri)
        {
            string NewUri = Uri.UnescapeDataString (uri).Replace ("file://", "");
            if (File.Exists (NewUri)) {
                MusicListStore.AppendValues (false,  track_number,song, artist, album, Uri.UnescapeDataString (uri), uri, ID);
            } else {
                MusicListStore.AppendValues (true,  track_number,song, artist, album, Uri.UnescapeDataString (uri), uri, ID);
            }
        }

        public static void ReloadWindow ()
        {
            ClearData ();
            HyenaDataReader reader = new HyenaDataReader (ServiceManager.DbConnection.Query (@"SELECT
                             CT.TrackID, CT.Title, CA.ArtistName, CA.Title, CT.URI, CT.TrackNumber
                             FROM CoreTracks CT,CoreAlbums CA ON Ct.AlbumID = CA.AlbumID
                             AND CT.TrackID IN (
                                 SELECT
                                     CT1.TrackID from CoreTracks CT1,CoreTracks CT2 where
                                     CT1.PrimarySourceID=1
                                     AND CT1.TrackID <> CT2.TrackID
                                     AND CT1.TitleLowered = CT2.TitleLowered
                                     AND CT1.AlbumID = CT2.AlbumID
                                     AND CT1.ArtistID = CT2.ArtistID
                                     AND CT1.Disc = CT2.Disc
                                     AND CT1.Duration = CT2.Duration
                             )
                             ORDER BY CT.Title,CT.ArtistID,CT.TrackNumber"));
            while (reader.Read ()) {
                int ID = reader.Get<int> (0);
                String Title = reader.Get<String> (1);
                String Artist = reader.Get<String> (2);
                String Album = reader.Get<String> (3);
                String URI = reader.Get<String> (4);
                String TrackNumber = reader.Get<String> (5);
                AddData (ID, TrackNumber,Title, Artist, Album, URI);
            }
        }

        public void RefreshWindow(){
            ReloadWindow();
        }

        public static void ClearData ()
        {
            Gtk.TreeIter Iter = new Gtk.TreeIter ();
            if (MusicListStore.GetIterFirst (out Iter)) {
                do {
                    MusicListStore.Remove (ref Iter);
                } while (MusicListStore.IterIsValid (Iter));
            }
        }
#endregion
#region RemoveWorkhorse
        //I would love to replace the database and File.Delete commands with MusicLibrary.RemoveTracks and MusicLibrary.DeleteTracks
        //But i have to wait until better documentation comes out
        private void RemoveTrack (int id)
        {
            ServiceManager.DbConnection.Execute (@"Delete From CoreTracks where TrackId = ?", id);
        }
        private void DeleteTrack (string uri)
        {
            File.Delete (uri);
        }
#endregion
#region ISourceContents
        private MusicLibrarySource source;
        public bool SetSource (ISource source)
        {
            this.source = source as MusicLibrarySource;
            return this.source != null;
        }

        public ISource Source {
            get { return source; }
        }

        public void ResetSource ()
        {
            source = null;
        }

        public Widget Widget {
            get { return this; }
        }
#endregion

    } //End of Class
    
} // End of NameSpace


