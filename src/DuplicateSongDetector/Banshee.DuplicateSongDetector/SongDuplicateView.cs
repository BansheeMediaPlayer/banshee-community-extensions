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
    public class SongDuplicateView : ISourceContents
    {
        private Gtk.ScrolledWindow Scroll;
        private Gtk.TreeView Tree;
        private static Gtk.ListStore MusicListStore;
        private Gtk.VBox vbox;

        public SongDuplicateView ()
        {
            Tree = new Gtk.TreeView ();
            vbox = new Gtk.VBox (false, 1);

            Gtk.Label label = new Gtk.Label ("");
            Gtk.HBox hbox = new Gtk.HBox (false, 1);
            Gtk.Button removeButton = new Gtk.Button ();
            removeButton.Label = AddinManager.CurrentLocalizer.GetString ("Remove Selected Songs");
            removeButton.Clicked += OnRemoveCommand;
            Gtk.Button deleteButton = new Gtk.Button ();
            deleteButton.Label = AddinManager.CurrentLocalizer.GetString ("Delete Selected Songs");
            deleteButton.Clicked += OnDeleteCommand;
            Gtk.TreeViewColumn selectColumn = new Gtk.TreeViewColumn ();
            selectColumn.Title = AddinManager.CurrentLocalizer.GetString ("Select");
            Gtk.TreeViewColumn songColumn = new Gtk.TreeViewColumn ();
            songColumn.Title = AddinManager.CurrentLocalizer.GetString ("Song Title");
            Gtk.TreeViewColumn artistColumn = new Gtk.TreeViewColumn ();
            artistColumn.Title = AddinManager.CurrentLocalizer.GetString ("Artist");
            Gtk.TreeViewColumn albumColumn = new Gtk.TreeViewColumn ();
            albumColumn.Title = AddinManager.CurrentLocalizer.GetString ("Album");
            Gtk.TreeViewColumn uriColumn = new Gtk.TreeViewColumn ();
            uriColumn.Title = AddinManager.CurrentLocalizer.GetString ("File URI");
            
            // Remove From Library, Delete From Drive, Song Name, Artist Name, Album Name, Formated URI, Actual URI, Database Track ID
            MusicListStore = new Gtk.ListStore (typeof(bool), typeof(string), typeof(string), typeof(string), typeof(string), typeof(string), typeof(int));
            Tree.Model = MusicListStore;
            
            Gtk.CellRendererToggle selectCell = new Gtk.CellRendererToggle ();
            selectCell.Activatable = true;
            selectCell.Toggled += OnSelectToggled;
            selectColumn.PackStart (selectCell, true);
            
            Gtk.CellRendererText songCell = new Gtk.CellRendererText ();
            songColumn.PackStart (songCell, true);
            Gtk.CellRendererText artistCell = new Gtk.CellRendererText ();
            artistColumn.PackStart (artistCell, true);
            Gtk.CellRendererText albumCell = new Gtk.CellRendererText ();
            albumColumn.PackStart (albumCell, true);
            Gtk.CellRendererText uriCell = new Gtk.CellRendererText ();
            uriColumn.PackStart (uriCell, true);
            
            selectColumn.AddAttribute (selectCell, "active", 0);
            songColumn.AddAttribute (songCell, "text", 1);
            artistColumn.AddAttribute (artistCell, "text", 2);
            albumColumn.AddAttribute (albumCell, "text", 3);
            uriColumn.AddAttribute (uriCell, "text", 4);
            
            Scroll = new Gtk.ScrolledWindow ();
            
            Tree.AppendColumn (selectColumn);
            Tree.AppendColumn (songColumn);
            Tree.AppendColumn (artistColumn);
            Tree.AppendColumn (albumColumn);
            Tree.AppendColumn (uriColumn);
            
            hbox.PackStart (removeButton, false, false, 0);
            hbox.PackStart (deleteButton, false, false, 0);
            hbox.PackStart (label, false, true, 0);
            vbox.PackStart (hbox, false, false, 0);
            vbox.PackStart (Scroll, true, true, 0);
            
            if (Scroll != null) {
                Scroll.AddWithViewport (Tree);
            }
            vbox.ShowAll ();
            
        }

        void OnSelectToggled (object o, ToggledArgs args)
        {
            TreeIter Iter;
            if (MusicListStore.GetIter (out Iter, new TreePath (args.Path))) {
                bool OldValue = (bool)MusicListStore.GetValue (Iter, 0);
                MusicListStore.SetValue (Iter, 0, !OldValue);
                Log.DebugFormat ("Setting Selection Value For Row {0} -> {1}", MusicListStore.GetStringFromIter (Iter), !OldValue);
            }
        }

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
                                string Uri = (string)MusicListStore.GetValue (Iter, 4);
                                Uri = Uri.Replace ("file://", "");
                                RemoveTrack ((int)MusicListStore.GetValue (Iter, 6));
                                DeleteTrack (Uri);
                            } else if ((bool)MusicListStore.GetValue (Iter, 0)) {
                                RemoveTrack ((int)MusicListStore.GetValue (Iter, 6));
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
                header = AddinManager.CurrentLocalizer.GetString ("Deleting Selected Files");
                button_label = "gtk-delete";
            } else {
                message = AddinManager.CurrentLocalizer.GetString ("Are you sure you want to proceed?");
                button_label = "gtk-remove";
            }
            
            
            HigMessageDialog md = new HigMessageDialog (ServiceManager.Get<GtkElementsService> ().PrimaryWindow, DialogFlags.DestroyWithParent, delete ? MessageType.Warning : MessageType.Question, ButtonsType.None, header, message);
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

        public static void AddData (int ID, String song, String artist, String album, String uri)
        {
            string NewUri = Uri.UnescapeDataString (uri).Replace ("file://", "");
            if (File.Exists (NewUri)) {
                MusicListStore.AppendValues (false,  song, artist, album, Uri.UnescapeDataString (uri), uri, ID);
            } else {
                MusicListStore.AppendValues (true,  song, artist, album, Uri.UnescapeDataString (uri), uri, ID);
            }
        }

        public static void ReloadWindow ()
        {
            ClearData ();
            HyenaDataReader reader = new HyenaDataReader (ServiceManager.DbConnection.Query (@"SELECT
                             CT.TrackID,CT.Title,CA.Title, CA.ArtistName,CT.URI
                             FROM CoreTracks CT,CoreAlbums CA ON Ct.AlbumID = CA.AlbumID
                             AND CT.TrackID IN (
                                 SELECT
                                     CT1.TrackID from CoreTracks CT1,CoreTracks CT2 where
                                     CT1.PrimarySourceID=1
                                     AND CT1.TrackID <> CT2.TrackID
                                     AND CT1.TitleLowered = CT2.TitleLowered
                                     AND CT1.TrackNumber = CT2.TrackNumber
                                     AND CT1.AlbumID = CT2.AlbumID
                                     AND CT1.ArtistID = CT2.ArtistID
                             )
                             ORDER BY CT.Title"));
            while (reader.Read ()) {
                int ID = reader.Get<int> (0);
                String Title = reader.Get<String> (1);
                String Album = reader.Get<String> (2);
                String Artist = reader.Get<String> (3);
                String URI = reader.Get<String> (4);
                AddData (ID, Title, Album, Artist, URI);
            }
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

        private void RemoveTrack (int id)
        {
            ServiceManager.DbConnection.Execute (@"Delete From CoreTracks where TrackId = ?", id);
        }
        private void DeleteTrack (string uri)
        {
            File.Delete (uri);
        }
        public bool SetSource (ISource source)
        {
            return true;
        }
        public void ResetSource ()
        {
        }
        public Gtk.Widget Widget {
            get { return vbox; }
        }
        public ISource Source {
            get { return null; }
        }
    }
    
}


