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

namespace Banshee.Ampache
{
    [System.ComponentModel.ToolboxItem(true)]
    public partial class AmpacheView : Gtk.Bin
    {
        bool _connected;
        private readonly NodeStore _artistStore;
        private readonly NodeStore _albumStore;
        private readonly NodeStore _songStore;
        private readonly NodeStore _playlistStore;
        private Dictionary<int, AmpacheArtist> _artists = new Dictionary<int, AmpacheArtist>();
        private Dictionary<int, AmpacheAlbum> _albums = new Dictionary<int, AmpacheAlbum>();
        private Dictionary<int, AmpacheSong> _songs = new Dictionary<int, AmpacheSong>();
        private Dictionary<int, AmpachePlaylist> _playlists = new Dictionary<int, AmpachePlaylist>();
        internal event EventHandler<Hyena.EventArgs<PlayQueue>> NewPlayList;

        // Short alias for the translations
        private static string _(string s)
        {
            return Mono.Addins.AddinManager.CurrentLocalizer.GetString (s);
        }

        public AmpacheView ()
        {
            this.Build ();
            _artistStore = new NodeStore(typeof(ArtistLabel));
            ndvArtists.NodeStore = _artistStore;
            ndvArtists.AppendColumn(_("Name"), new CellRendererText(), "text", 0);
            ndvArtists.NodeSelection.Changed += HandleNdvArtistsNodeSelectionChanged;
            _albumStore = new NodeStore(typeof(AlbumLabel));
            ndvAlbums.NodeStore = _albumStore;
            ndvAlbums.AppendColumn(_("Name"), new CellRendererText(), "text", 0);
            ndvAlbums.NodeSelection.Changed += HandleNdvAlbumsNodeSelectionChanged;
            _songStore = new NodeStore(typeof(SongLabel));
            ndvSongs.NodeStore = _songStore;
            ndvSongs.AppendColumn(_("Name"), new CellRendererText(), "text", 0);
            ndvSongs.AppendColumn(_("Artist"), new CellRendererText(), "text", 1);
            ndvSongs.AppendColumn(_("Album"), new CellRendererText(), "text", 2);
            ndvSongs.NodeSelection.Changed += HandleNdvSongsNodeSelectionChanged;
            _playlistStore = new NodeStore(typeof(PlaylistLabel));
            ndvPlaylists.AppendColumn(_("Name "), new CellRendererText(), "text", 0);
            ndvPlaylists.NodeStore = _playlistStore;
            ndvPlaylists.NodeSelection.Changed += HandleNdvPlaylistsNodeSelectionChanged;
        }

        void HandleNdvPlaylistsNodeSelectionChanged(object sender, EventArgs e)
        {
            var sel = sender as Gtk.NodeSelection;
            if (sel == null || sel.SelectedNode == null) {
                return;
            }
            var ply = (PlaylistLabel)sel.SelectedNode;
            ndvAlbums.NodeSelection.UnselectAll();
            ndvArtists.NodeSelection.UnselectAll();
            _songStore.Clear();
            _playlists[ply.AmpacheId].Songs.ToList().ForEach(s=>_songStore.AddNode(new SongLabel(s)));
        }

        private void HandleNdvAlbumsNodeSelectionChanged (object sender, EventArgs e)
        {
            var sel = sender as Gtk.NodeSelection;
            if (sel == null || sel.SelectedNode == null) {
                return;
            }
            var alb = (AlbumLabel)sel.SelectedNode;
            _songStore.Clear();
            _songs.Values.Where(a=>a.AlbumId == alb.AmpacheId).ToList().ForEach(s=>_songStore.AddNode(new SongLabel(s)));
            ndvPlaylists.NodeSelection.UnselectAll();
        }

        private void HandleNdvSongsNodeSelectionChanged (object sender, EventArgs e)
        {
            var sel = sender as Gtk.NodeSelection;
            if (sel == null || sel.SelectedNode == null || ServiceManager.PlayerEngine.CurrentState == PlayerState.Playing) {
                return;
            }
            var lbls = _songStore.Cast<SongLabel>().ToList();
            var songs = lbls.Select(l => _songs[l.AmpacheId]).ToList();
            var skip = lbls.IndexOf((SongLabel)sel.SelectedNode);
            var tmp = new PlayQueue(songs.Skip(skip), songs.Take(skip));
            if (NewPlayList != null) {
                NewPlayList(this, new Hyena.EventArgs<PlayQueue>(tmp));
            }
        }

        //todo: change to event
        internal void SelectPlayingSong(TrackInfo song)
        {
            var tmp = _songStore.Cast<SongLabel>().FirstOrDefault(s=>s.Url == song.Uri.AbsoluteUri);
            if(tmp != null) {
                ndvSongs.NodeSelection.Changed -= HandleNdvSongsNodeSelectionChanged;
                ndvSongs.NodeSelection.SelectNode(tmp);
                ndvSongs.NodeSelection.Changed += HandleNdvSongsNodeSelectionChanged;
            }
        }

        private void HandleNdvArtistsNodeSelectionChanged (object sender, EventArgs e)
        {
            var sel = sender as Gtk.NodeSelection;
            if (sel == null || sel.SelectedNode == null) {
                return;
            }
            var art = (ArtistLabel)sel.SelectedNode;
            _albumStore.Clear();
            _albums.Values.Where(a=>a.ArtistId == art.ArtistId).ToList().ForEach(a=>_albumStore.AddNode(new AlbumLabel{AmpacheId = a.Id, ArtistId = a.ArtistId, Name = a.DisplayTitle}));
            _songStore.Clear();
            _songs.Values.Where(a=>a.ArtistId == art.ArtistId).ToList().ForEach(s=>_songStore.AddNode(new SongLabel(s)));
            ndvPlaylists.NodeSelection.UnselectAll();
        }

        protected virtual void btnConnect_OnClicked (object sender, System.EventArgs e)
        {
            if (!_connected) {
                System.Threading.ThreadPool.QueueUserWorkItem(LoadAmpache);
                _artistStore.AddNode(new ArtistLabel{Name = _("Loading...")});
                _albumStore.AddNode(new AlbumLabel{Name = _("Loading...")});
                _playlistStore.AddNode(new PlaylistLabel{Name = _("Loading...")});
                btnConnect.Hide();
            }
        }
        // todo: this does not belong in the UI class
        private void LoadAmpache(object ob)
        {
            if(!_connected) {
                try {
                    var tmp = new Authenticate(entUrl.Text, entUser.Text, entPassword.Text);
                    AmpacheSelectionFactory.Initialize(tmp);
                    _artists = AmpacheSelectionFactory.GetSelectorFor<AmpacheArtist>().SelectAll().ToDictionary(k=>k.Id, v=>v);
                    _albums = AmpacheSelectionFactory.GetSelectorFor<AmpacheAlbum>().SelectAll().ToDictionary(k=>k.Id, v=>v);
                    _songs = AmpacheSelectionFactory.GetSelectorFor<AmpacheSong>().SelectAll().ToDictionary(k=>k.Id, v=>v);
                    _playlists = AmpacheSelectionFactory.GetSelectorFor<AmpachePlaylist>().SelectAll().ToDictionary(k=>k.Id, v=>v);
                    _connected = true;
                    // hydrate albums and songs
                    foreach (var alb in _albums.Values) {
                        if (_artists.ContainsKey(alb.ArtistId)) {
                            alb.Hydrate(_artists[alb.ArtistId]);
                        }
                    }
                    foreach (AmpacheSong song in _songs.Values.Union(_playlists.Values.SelectMany(p => p.Songs))) {
                        if (_albums.ContainsKey(song.AlbumId)) {
                            song.Hydrate(_albums[song.AlbumId], _artists[song.ArtistId]);
                        }
                    }

                }
                catch (Exception e) {
                    Hyena.Log.Error(e.Message);
                    _connected = false;
                }
                Gtk.Application.Invoke((o,e) => LoadNodeLists());
            }
        }

        private void LoadNodeLists()
        {
            if (_connected)
            {
                LoginArea.Hide();
            }
            else
            {
                btnConnect.Show();
            }

            _artistStore.Clear();
            _artists.Values.ToList().ForEach(a => _artistStore.AddNode(new ArtistLabel{ArtistId = a.Id, Name = a.Name}));
            _albumStore.Clear();
            _albums.Values.ToList().ForEach(a =>_albumStore.AddNode(new AlbumLabel{AmpacheId = a.Id, ArtistId = a.ArtistId, Name = a.DisplayTitle}));
            _songStore.Clear();
            _songs.Values.ToList().ForEach(s =>_songStore.AddNode(new SongLabel(s)));
            _playlistStore.Clear();
            _playlists.Values.ToList().ForEach(p => _playlistStore.AddNode(new PlaylistLabel(p)));
        }
    }
}

