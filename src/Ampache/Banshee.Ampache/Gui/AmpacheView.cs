//
// AmpacheView.cs
//
// Author:
//       John Moore <jcwmoore@gmail.com>
//
// Copyright (c) 2010 John Moore
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
using Gtk;
using System.Linq;

using Banshee.Base;
using Banshee.Widgets;
using Banshee.Database;
using Banshee.Sources;
using Banshee.Metadata;
using Banshee.MediaEngine;
using Banshee.Collection;
using Banshee.ServiceStack;
using Banshee.PlaybackController;
using Hyena.Data.Gui;
using Hyena.Data;

namespace Banshee.Ampache
{
    public class AmpacheView : Gtk.VBox
    {
        private readonly ScrolledWindow trackWindow = new ScrolledWindow();
        private readonly ListView<AmpacheSong> lvTracks = new ListView<AmpacheSong>();
        private readonly ScrolledWindow artistsWindow = new ScrolledWindow();
        private readonly ListView<AmpacheArtist> lvArtists = new ListView<AmpacheArtist>();
        private readonly ScrolledWindow albumWindow = new ScrolledWindow();
        private readonly ListView<AmpacheAlbum> lvAlbums = new ListView<AmpacheAlbum>();
        private readonly ScrolledWindow playlistWindow = new ScrolledWindow();
        private readonly ListView<AmpachePlaylist> lvPlaylists = new ListView<AmpachePlaylist>();
        private readonly HBox filterBox = new HBox();
        private readonly Button btnConnect = new Button();
        private readonly MemoryListModel<AmpacheSong> trackModel = new MemoryListModel<AmpacheSong>();
        private readonly MemoryListModel<AmpacheArtist> artistModel = new MemoryListModel<AmpacheArtist>();
        private readonly MemoryListModel<AmpacheAlbum> albumModel = new MemoryListModel<AmpacheAlbum>();
        private readonly MemoryListModel<AmpachePlaylist> playlistModel = new MemoryListModel<AmpachePlaylist>();
        private readonly AmpacheViewModel viewModel;
        private bool ignoreChanges = false;

        private static string _(string s)
        {
            return Mono.Addins.AddinManager.CurrentLocalizer.GetString (s);
        }
        public AmpacheView (AmpacheViewModel vm)
        {
            viewModel = vm;
            vm.PropertyChanged += HandleVmPropertyChanged;
            btnConnect.Label = _("Connect");
            PackStart(btnConnect, false, false, 1);
            artistsWindow.Add(lvArtists);
            filterBox.PackStart(artistsWindow, true, true, 1);
            albumWindow.Add(lvAlbums);
            filterBox.PackStart(albumWindow, true, true, 1);
            playlistWindow.Add(lvPlaylists);
            filterBox.PackStart(playlistWindow, true, true, 1);
            PackStart(filterBox, true, true, 1);
            trackWindow.Add(lvTracks);
            PackStart(trackWindow, true, true, 1);

            lvArtists.ColumnController = BuildDisplayColumnController(_("Artist"));
            lvArtists.SetModel(artistModel);
            lvAlbums.ColumnController = BuildDisplayColumnController(_("Album"));
            lvAlbums.SetModel(albumModel);
            lvPlaylists.ColumnController = BuildDisplayColumnController(_("Playlist"));
            lvPlaylists.SetModel(playlistModel);
            lvTracks.ColumnController = BuildDisplayColumnController(_("Title"));
            lvTracks.ColumnController.Add(new Column(new ColumnDescription("DisplayArtistName", _("Artist"), 100)));
            lvTracks.ColumnController.Add(new Column(new ColumnDescription("DisplayAlbumTitle", _("Album"), 100)));
            lvTracks.SetModel(trackModel);
            ShowAll();

            lvArtists.Model.Selection.Changed += HandleLvArtistsModelSelectionChanged;
            lvAlbums.Model.Selection.Changed += HandleLvAlbumsModelSelectionChanged;
            lvPlaylists.Model.Selection.Changed += HandleLvPlaylistsModelSelectionChanged;
            lvTracks.Model.Selection.Changed += HandleLvTracksModelSelectionChanged;

            if (!string.IsNullOrEmpty(AmpacheSource.UserName.Get())) {
                btnConnect.Hide();
                System.Threading.ThreadPool.QueueUserWorkItem((o)=> viewModel.ConnectCommand() );
            }
        }

        void HandleVmPropertyChanged (object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (ignoreChanges) {
                return;
            }
            switch (e.PropertyName) {
            case "Artists":
                Gtk.Application.Invoke(RefreshArtists);
                break;
            case "Albums":
                Gtk.Application.Invoke(RefreshAlbums);
                break;
            case "Playlists":
                Gtk.Application.Invoke(RefreshPlaylists);
                break;
            case "Songs":
                Gtk.Application.Invoke(RefreshTracks);
                break;
            case "SelectedSong":
                Gtk.Application.Invoke(SelectPlayingSong);
                break;
            case "SelectedArtist":
                Gtk.Application.Invoke(SelectArtist);
                break;
            case "SelectedAlbum":
                Gtk.Application.Invoke(SelectAlbum);
                break;
            default:
                break;
            }
        }

        void HandleLvTracksModelSelectionChanged (object sender, EventArgs e)
        {
            var sng = trackModel.SelectedItems.FirstOrDefault();
            if (sng != null) {
                ignoreChanges = true;
                viewModel.SelectedSong = sng;
                ignoreChanges = false;
            }
        }

        void HandleLvPlaylistsModelSelectionChanged (object sender, EventArgs e)
        {
            if (playlistModel.SelectedItems.Count != 1) {
                viewModel.SelectedPlaylist = null;
                return;
            }
            ignoreChanges = true;
            viewModel.SelectedPlaylist = playlistModel.SelectedItems.First();
            ignoreChanges = false;
            viewModel.SelectedAlbum = null;
            viewModel.SelectedArtist = null;
            System.Threading.ThreadPool.QueueUserWorkItem((o) => viewModel.LoadSongsCommand());
        }

        void HandleLvAlbumsModelSelectionChanged (object sender, EventArgs e)
        {
            if (albumModel.SelectedItems.Count != 1) {
                viewModel.SelectedAlbum = null;
                return;
            }
            ignoreChanges = true;
            viewModel.SelectedAlbum = albumModel.SelectedItems.First();
            ignoreChanges = false;
            viewModel.SelectedArtist = viewModel.Artists.FirstOrDefault(a => a.Id == viewModel.SelectedAlbum.ArtistId);
            System.Threading.ThreadPool.QueueUserWorkItem((o) => viewModel.LoadSongsCommand());
        }

        private void HandleLvArtistsModelSelectionChanged (object sender, EventArgs e)
        {
            if (artistModel.SelectedItems.Count != 1) {
                viewModel.SelectedArtist = null;
                return;
            }
            ignoreChanges = true;
            playlistModel.Selection.Clear();
            viewModel.SelectedArtist = artistModel.SelectedItems.First();
            if (viewModel.SelectedAlbum != null && viewModel.SelectedAlbum.ArtistId != viewModel.SelectedArtist.Id){
                albumModel.Selection.Clear();
            }
            ignoreChanges = false;
            System.Threading.ThreadPool.QueueUserWorkItem((o) => viewModel.LoadSongsCommand());
        }
        
        private ColumnController BuildDisplayColumnController(string localizedTitle)
        {
            var result = new ColumnController();
            result.Add(new Column(new ColumnDescription("DisplayName", localizedTitle, 100)));
            return result;
        }

        #region Populate Methods

        private void RefreshArtists(object sender, EventArgs e)
        {
            artistModel.Clear();
            viewModel.Artists.ToList().ForEach(a=>artistModel.Add(a));
            artistModel.Reload();
        }

        private void RefreshAlbums(object sender, EventArgs e)
        {
            albumModel.Clear();
            viewModel.Albums.ToList().ForEach(a=>albumModel.Add(a));
            albumModel.Reload();
        }

        private void RefreshPlaylists(object sender, EventArgs e)
        {
            playlistModel.Clear();
            viewModel.Playlists.ToList().ForEach(a=>playlistModel.Add(a));
            playlistModel.Reload();
        }

        private void RefreshTracks(object sender, EventArgs e)
        {
            trackModel.Clear();
            viewModel.Songs.ToList().ForEach(a=>trackModel.Add(a));
            trackModel.Reload();
        }

        private void SelectPlayingSong(object sender, EventArgs e)
        {
            var index = trackModel.IndexOf(viewModel.SelectedSong);
            if(index > 0) {
                trackModel.Selection.Changed -= HandleLvTracksModelSelectionChanged;
                trackModel.Selection.Clear();
                trackModel.Selection.Select(index);
                lvTracks.ScrollTo(index);
                trackModel.Selection.Changed += HandleLvTracksModelSelectionChanged;
            }
        }

        private void SelectArtist(object sender, EventArgs e)
        {
            var index = artistModel.IndexOf(viewModel.SelectedArtist);
            artistModel.Selection.Changed -= HandleLvArtistsModelSelectionChanged;
            artistModel.Selection.Clear();
            artistModel.Selection.Changed += HandleLvArtistsModelSelectionChanged;
            if (index > 0) {
                artistModel.Selection.Select(index);
                lvArtists.ScrollTo(index);
            }
        }

        private void SelectAlbum(object sender, EventArgs e)
        {
            var index = albumModel.IndexOf(viewModel.SelectedAlbum);
            albumModel.Selection.Clear(false);
            if (index > 0) {
                albumModel.Selection.Select(index);
                lvAlbums.ScrollTo(index);
            }
        }

        #endregion
    }
    public static class IListModelExtensions
    {
        public static IEnumerable<T> AsEnumerable<T>(this IListModel<T> model)
        {
            for (int i = 0; i < model.Count; i++) {
                yield return model[i];
            }
        }
    }
}

