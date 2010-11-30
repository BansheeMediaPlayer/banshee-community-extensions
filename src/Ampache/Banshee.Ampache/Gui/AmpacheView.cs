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

        private bool connected = false;
        private Dictionary<int, AmpacheArtist> artists = new Dictionary<int, AmpacheArtist>();
        private Dictionary<int, AmpacheAlbum> albums = new Dictionary<int, AmpacheAlbum>();
        private Dictionary<int, AmpacheSong> songs = new Dictionary<int, AmpacheSong>();
        private Dictionary<int, AmpachePlaylist> playlists = new Dictionary<int, AmpachePlaylist>();

        internal event EventHandler<Hyena.EventArgs<PlayQueue>> NewPlayList;

        private static string _(string s)
        {
            return Mono.Addins.AddinManager.CurrentLocalizer.GetString (s);
        }
        public AmpacheView ()
        {
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
                System.Threading.ThreadPool.QueueUserWorkItem(LoadAmpache);
            }
        }

        internal void SelectPlayingSong(AmpacheSong song)
        {
            var index = trackModel.IndexOf(song);
            if(index > 0) {
                trackModel.Selection.Changed -= HandleLvTracksModelSelectionChanged;
                trackModel.Selection.Clear();
                trackModel.Selection.ToggleSelect(index);
                trackModel.Selection.Changed += HandleLvTracksModelSelectionChanged;
            }
        }

        void HandleLvTracksModelSelectionChanged (object sender, EventArgs e)
        {
            var sngs = trackModel.AsEnumerable().ToList();
            if (sngs.Count == 0 || ServiceManager.PlayerEngine.CurrentState == PlayerState.Playing) {
                SelectPlayingSong(ServiceManager.PlayerEngine.CurrentTrack as AmpacheSong);
                return;
            }
            lvTracks.HasFocus = false;
            var skip = trackModel.Selection.FirstIndex;
            var tmp = new PlayQueue(sngs.Skip(skip), sngs.Take(skip));
            if (NewPlayList != null) {
                NewPlayList(this, new Hyena.EventArgs<PlayQueue>(tmp));
            }
        }

        void HandleLvPlaylistsModelSelectionChanged (object sender, EventArgs e)
        {
            var plys = playlistModel.SelectedItems.ToList();
            if (plys.Count == 0) {
                return;
            }
            trackModel.Clear();
            trackModel.Selection.Clear();
            var sngs = plys.SelectMany(p => p.Songs).ToList();
            sngs.ForEach(s => trackModel.Add(s));
            trackModel.Reload();
        }

        void HandleLvAlbumsModelSelectionChanged (object sender, EventArgs e)
        {
            var albIds = albumModel.SelectedItems.Select(a => a.Id).ToList();
            if (albIds.Count == 0) {
                //LoadModels();
                return;
            }
            trackModel.Clear();
            trackModel.Selection.Clear();
            songs.Values.Where(s=> albIds.Contains(s.AlbumId)).ToList().ForEach(s=>trackModel.Add(s));
            trackModel.Reload();
        }

        private void HandleLvArtistsModelSelectionChanged (object sender, EventArgs e)
        {
            var artIds = artistModel.SelectedItems.Select(a=> a.Id).ToList();
            if (artIds.Count == 0) {
                //LoadModels();
                return;
            }
            trackModel.Clear();
            trackModel.Selection.Clear();
            songs.Values.Where(s=> artIds.Contains(s.ArtistId)).ToList().ForEach(s=>trackModel.Add(s));
            albumModel.Clear();
            albumModel.Selection.Clear();
            playlistModel.Selection.Clear();
            albums.Values.Where(a => artIds.Contains(a.ArtistId)).ToList().ForEach(a => albumModel.Add(a));
            trackModel.Reload();
            albumModel.Reload();
        }
        
        private ColumnController BuildDisplayColumnController(string localizedTitle)
        {
            var result = new ColumnController();
            result.Add(new Column(new ColumnDescription("DisplayName", localizedTitle, 100)));
            return result;
        }

        private void LoadAmpache(object ob)
        {
            if(!connected) {
                try {
                    var tmp = new Authenticate(AmpacheSource.AmpacheRootAddress.Get(), AmpacheSource.UserName.Get(), AmpacheSource.UserPassword.Get());
                    AmpacheSelectionFactory.Initialize(tmp);
                    artists = AmpacheSelectionFactory.GetSelectorFor<AmpacheArtist>().SelectAll().ToDictionary(k=>k.Id, v=>v);
                    albums = AmpacheSelectionFactory.GetSelectorFor<AmpacheAlbum>().SelectAll().ToDictionary(k=>k.Id, v=>v);
                    songs = AmpacheSelectionFactory.GetSelectorFor<AmpacheSong>().SelectAll().ToDictionary(k=>k.Id, v=>v);
                    playlists = AmpacheSelectionFactory.GetSelectorFor<AmpachePlaylist>().SelectAll().ToDictionary(k=>k.Id, v=>v);
                    connected = true;
                    // hydrate albums and songs
                    foreach (var alb in albums.Values) {
                        if (artists.ContainsKey(alb.ArtistId)) {
                            alb.Hydrate(artists[alb.ArtistId]);
                        }
                    }
                    foreach (AmpacheSong song in songs.Values.Union(playlists.Values.SelectMany(p => p.Songs))) {
                        if (albums.ContainsKey(song.AlbumId)) {
                            song.Hydrate(albums[song.AlbumId], artists[song.ArtistId]);
                        }
                    }
                }
                catch (Exception e) {
                    Hyena.Log.ErrorFormat("{0}: message {1}\n{2}", e.GetType().Name, e.Message, e.StackTrace);
                    connected = false;
                }
                Gtk.Application.Invoke((o,e) => LoadModels());
            }
        }

        private void LoadModels()
        {
            if (!connected) {
                btnConnect.Show();
            }
            artists.Values.ToList().ForEach(a=>artistModel.Add(a));
            artistModel.Reload();
            albums.Values.ToList().ForEach(a=>albumModel.Add(a));
            albumModel.Reload();
            playlists.Values.ToList().ForEach(p=>playlistModel.Add(p));
            playlistModel.Reload();
            trackModel.Clear();
        }
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

