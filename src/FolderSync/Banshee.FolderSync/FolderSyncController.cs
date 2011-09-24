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
using System.Text;
using System.Threading;

using Mono.Addins;

using Hyena;
using Hyena.Data.Sqlite;
using Banshee.Gui;
using Banshee.ServiceStack;
using Banshee.Sources;
using Banshee.Sources.Gui;

namespace Banshee.FolderSync
{
    public class FolderSyncAction : BansheeActionGroup
    {
        private FolderSyncSource source = null;

        public FolderSyncAction () : base (AddinManager.CurrentLocalizer.GetString ("Sync to Folder"))
        {
            Add (new Gtk.ActionEntry ("FolderSyncAction", Gtk.Stock.Refresh,
                AddinManager.CurrentLocalizer.GetString ("Synchronise to Folder"), null, null, onLoadSync));
            AddUiFromFile ("MenuUI.xml");
        }

        public void onLoadSync (object o, EventArgs args)
        {
            source = new FolderSyncSource ();
            ServiceManager.SourceManager.MusicLibrary.AddChildSource (source);
            ServiceManager.SourceManager.SetActiveSource (source);
        }
    }

    public class FolderSyncController : ISourceContents
    {
        public FolderSyncView View = new FolderSyncView ();
        private Thread syncThread;

#region ISourceContents implementation
        public bool SetSource (ISource source)
        {
            return true;
        }

        public void ResetSource ()
        {
            Reload ();
        }

        public Gtk.Widget Widget {
            get {
                return View;
            }
        }

        public ISource Source { get { return null; } }
#endregion

        public FolderSyncController ()
        {
            //hookup click event
            View.StartSyncButton.Clicked += onStartSync;
            Reload ();
        }

        public void Reload ()
        {
            // fill the database-stored playlist into the treeview
            var playlists = Playlist.GetAllPlaylists ();
            // remove older playlist entries
            View.PlaylistStore.Clear ();
            foreach (var playlist in playlists) {
                View.PlaylistStore.AppendValues (playlist.ID, playlist.Name);
            }
            View.Reload ();
        }

        public void StopSync ()
        {
            if (syncThread != null && syncThread.IsAlive) {
                Hyena.Log.Debug ("aborting sync thread due to user request");
                syncThread.Abort ();
                Reload ();
            }
        }

        private void onStartSync (object o, EventArgs args)
        {
            Hyena.Log.Debug ("Start of Sync triggered!");

            var options = View.GetOptions ();

            // target directory to copy to
            Hyena.Log.DebugFormat ("Target folder is set to: {0}", options.TargetFolder);

            // count all files for progress bar
            int totalFiles = 0;
            foreach (var playlist in options.SelectedPlaylists) {
                totalFiles += playlist.Tracks.Count ();
            }
            View.Progress.Text = AddinManager.CurrentLocalizer.GetString ("Preparing sync");
            var progress_step = 1f / totalFiles;
            var current_progress = 0f;

            // begin sync worker thread
            ThreadStart syncStart = delegate()
            {
                Hyena.Log.Debug ("Sync thread started!");
                // foreach playlist
                foreach (var playlist in options.SelectedPlaylists) {

                    Stream m3u_stream = null;
                    StreamWriter m3u_writer = null;

                    if (options.CreateM3u) {
                        var m3u_fileUri = new StringBuilder ().Append (options.TargetFolder)
                         .Append (Path.DirectorySeparatorChar).Append (playlist.Name)
                         .Append (".m3u").ToString ();

                        m3u_stream = Banshee.IO.File.OpenWrite (new SafeUri (m3u_fileUri), true);
                        Log.DebugFormat ("opened m3u playlist for writing: {0}", m3u_fileUri);
                        m3u_writer = new StreamWriter (m3u_stream, System.Text.Encoding.UTF8);
                    }

                    // for each contained file
                    foreach (var track in playlist.Tracks) {
                        // get filename part of path
                        var dest_path_suffix = track.GetFilepathSuffix (options.SubfolderDepth);

                        // we dont want %20 etc. in the m3u since some android devices delete
                        // playlists with that encoding in it (i.e. galaxy S)
                        Hyena.Log.DebugFormat ("filename for m3u file is {0}", dest_path_suffix);

                        // assemble new Uri of target track
                        var destUri = new SafeUri (new StringBuilder ().Append (options.TargetFolder)
                            .Append (Path.DirectorySeparatorChar)
                            .Append (dest_path_suffix).ToString ());

                        // create subfolders if necessary
                        string dest_path = options.TargetFolder;
                        var folders = track.GetSubdirectories (options.SubfolderDepth);
                        try {
                            for (int i=0; i < folders.Count (); i++) {
                                dest_path += folders [i] + "/";
                                Hyena.Log.DebugFormat ("creating folder {0}", dest_path);
                                if (!Banshee.IO.Directory.Exists (dest_path))
                                    Banshee.IO.Directory.Create (new SafeUri (dest_path));
                            }
                        } catch {
                            // folder creation failed, this is fatal, stop
                            // TODO display a error popup
                            break;
                        }

                        // copy file to selected folder
                        try {
                            if (options.OverwriteExisting || !Banshee.IO.File.Exists (destUri)) {
                                Banshee.IO.File.Copy (track.Uri, destUri, true);
                                Hyena.Log.DebugFormat ("Copying {0} to {1}", track.Uri, destUri);
                            } else
                                Hyena.Log.Debug ("Not overwriting existing file {0}", destUri);
                        } catch {
                            Hyena.Log.ErrorFormat ("error copying file {0} to {1}, skipping", track.Uri, destUri);
                        }

                        // increment the progressbar
                        current_progress += progress_step;
                        if (current_progress > 1.0f)
                            current_progress = 1.0f;

                        Gtk.Application.Invoke (delegate {
                            View.Progress.Fraction = current_progress;
                            View.Progress.Text = AddinManager.CurrentLocalizer.GetString ("Copying") + " " + track.Filepath;
                            Gtk.Main.IterationDo (false);
                        });

                        if (options.CreateM3u) {
                            m3u_writer.Write (track.CreateM3uEntry (options.SubfolderDepth));
                        }
                    }
                    // close the m3u file before processing next playlist
                    if (options.CreateM3u) {
                        Hyena.Log.Debug ("closing m3u filedescriptor");
                        m3u_writer.Close ();
                        m3u_writer.Dispose ();
                    }
                    Hyena.Log.Debug ("sync process finished");
                }

                Gtk.Application.Invoke (delegate {
                    View.Progress.Text = AddinManager.CurrentLocalizer.GetString ("Done!");
                    View.Progress.Fraction = 1f;
                    Gtk.Main.IterationDo (false);
                });
                Hyena.Log.Debug ("sync DONE, returning");
                return;
            };
            // end of sync worker thread

            syncThread = new Thread (syncStart);
            syncThread.Start ();
            return;
        }
    }

    // small helper classes
    public class Track
    {
        public uint ID;
        public SafeUri Uri;

        public string Filepath {
            get {
                return SafeUri.UriToFilename (Uri);
            }
        }

        public string Filename {
            get {
                return Filepath.Split (Path.DirectorySeparatorChar).Last ();
            }
        }

        public Track ()
        { }

        public string CreateM3uEntry (uint subfolder_depth)
        {
            // TODO add #EXTIF additional metadata
            return GetFilepathSuffix (subfolder_depth) + "\n";
        }

        public string GetFilepathSuffix (uint subfolder_depth)
        {
            if (subfolder_depth == 0)
                return Filename;

            string path = "";
            foreach (var folder in GetSubdirectories (subfolder_depth))
                path += folder + "/";

            return path + Filename;
        }

        public List<string> GetSubdirectories (uint level)
        {
            var folders = this.Filepath.Split (Path.DirectorySeparatorChar).ToList ();

            // drop last segment as its not a folder but the filename
            folders.RemoveAt (folders.Count - 1);
            int skip = (int)(folders.Count - level);
            if (skip < 0)
                skip = 0;

            return folders.Skip (skip).ToList ();
        }
    }

    public class Playlist
    {
        public uint ID;
        public string Name;

        public Playlist ()
        { }

        public Playlist (uint id)
        {
            HyenaDataReader reader = new HyenaDataReader (ServiceManager.DbConnection.Query
                ("SELECT PlaylistID, Name from CorePlaylists WHERE PlaylistID = ?", id));

            if (!reader.Read ())
                throw new Exception ("No such playlist!");

            ID = reader.Get<uint> (0);
            Name = reader.Get<string> (1);
        }

        public List<Track> Tracks {
            get {
                return GetAllTracks (this.ID);
            }
        }

        public static List<Playlist> GetAllPlaylists ()
        {
            // get all the Uri from database
            HyenaDataReader reader = new HyenaDataReader (ServiceManager.DbConnection.Query
                ("SELECT PlaylistID, Name from CorePlaylists"));

            var playlists = new List<Playlist> ();

            while (reader.Read()) {
                playlists.Add (new Playlist {
                    ID = reader.Get<uint> (0),
                    Name = reader.Get<string> (1)
                });
            }
            return playlists;
        }

        // returns filenames of all included files in playlist
        public static List<Track> GetAllTracks (uint playlist_id)
        {
            HyenaDataReader reader = new HyenaDataReader (ServiceManager.DbConnection.Query (
                "SELECT t.TrackID, t.Uri from CorePlaylistEntries e JOIN CoreTracks t ON(e.TrackID = t.TrackID) " +
                "WHERE e.PlaylistID = ?", playlist_id));

            // package the Uris into a List for convenience
            List<Track > tracks = new List<Track> ();
            while (reader.Read()) {
                tracks.Add (new Track () {
                    ID = reader.Get<uint> (0),
                    Uri = new SafeUri(reader.Get<string> (1))
                });
            }
            return tracks;
        }
    }
}
