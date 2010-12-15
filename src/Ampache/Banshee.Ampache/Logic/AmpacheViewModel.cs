// 
// AmpacheViewModel.cs
// 
// Author:
//   John Moore <jcwmoore@gmail.com>
// 
// Copyright (c) 2010 john
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
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Banshee.Ampache
{
    public class AmpacheViewModel : INotifyPropertyChanged, IDisposable
    {
        #region Artists
        
        private ICollection<AmpacheArtist> artists = new HashSet<AmpacheArtist>();
        
        public ICollection<AmpacheArtist> Artists
        {
            get { return artists; }
            set
            {
                if (value == artists){
                    return;
                }
                artists = value;
                if(PropertyChanged != null){
                    PropertyChanged(this, new PropertyChangedEventArgs("Artists"));
                }
            }
        }
        
        #endregion

        #region SelectedArtist

        private AmpacheArtist selectedArtist = null;

        public AmpacheArtist SelectedArtist
        {
            get { return selectedArtist; }
            set
            {
                if (value == selectedArtist){
                    return;
                }
                selectedArtist = value;
                if(PropertyChanged != null){
                    PropertyChanged(this, new PropertyChangedEventArgs("SelectedArtist"));
                }
            }
        }
        
        #endregion

        #region Albums

        private ICollection<AmpacheAlbum> albums = new HashSet<AmpacheAlbum>();

        public ICollection<AmpacheAlbum> Albums
        {
            get { return albums; }
            set
            {
                if (value == albums){
                    return;
                }
                albums = value;
                if(PropertyChanged != null){
                    PropertyChanged(this, new PropertyChangedEventArgs("Albums"));
                }
            }
        }

        #endregion

        #region SelectedAlbum

        private AmpacheAlbum selectedAlbum = null;

        public AmpacheAlbum SelectedAlbum
        {
            get { return selectedAlbum; }
            set
            {
                if (value == selectedAlbum){
                    return;
                }
                selectedAlbum = value;
                if(PropertyChanged != null){
                    PropertyChanged(this, new PropertyChangedEventArgs("SelectedAlbum"));
                }
            }
        }

        #endregion

        #region Playlists

        private ICollection<AmpachePlaylist> playlists = new HashSet<AmpachePlaylist>();

        public ICollection<AmpachePlaylist> Playlists
        {
            get { return playlists; }
            set
            {
                if (value == playlists){
                    return;
                }
                playlists = value;
                if(PropertyChanged != null){
                    PropertyChanged(this, new PropertyChangedEventArgs("Playlists"));
                }
            }
        }

        #endregion

        #region SelectedPlaylist

        private AmpachePlaylist selectedplaylist = null;

        public AmpachePlaylist SelectedPlaylist
        {
            get { return selectedplaylist; }
            set
            {
                if (value == selectedplaylist){
                    return;
                }
                selectedplaylist = value;
                if(PropertyChanged != null){
                    PropertyChanged(this, new PropertyChangedEventArgs("Selectedplaylist"));
                }
            }
        }

        #endregion

        #region Songs

        private ICollection<AmpacheSong> songs = new HashSet<AmpacheSong>();

        public ICollection<AmpacheSong> Songs
        {
            get { return songs; }
            set
            {
                if (value == songs){
                    return;
                }
                songs = value;
                if(PropertyChanged != null){
                    PropertyChanged(this, new PropertyChangedEventArgs("Songs"));
                }
            }
        }

        #endregion

        #region SelectedSong

        private AmpacheSong selectedSong = null;

        public AmpacheSong SelectedSong
        {
            get { return selectedSong; }
            set
            {
                if (value == selectedSong){
                    return;
                }
                selectedSong = value;
                if(PropertyChanged != null){
                    PropertyChanged(this, new PropertyChangedEventArgs("SelectedSong"));
                }
            }
        }

        #endregion

        #region IsConnected

        private bool isConnected = false;

        public bool IsConnected
        {
            get { return isConnected; }
            set
            {
                if (value == isConnected){
                    return;
                }
                isConnected = value;
                if(PropertyChanged != null){
                    PropertyChanged(this, new PropertyChangedEventArgs("IsConnected"));
                }
            }
        }

        #endregion

        #region IsLoading

        private bool isLoading = false;

        public bool IsLoading
        {
            get { return isLoading; }
            set
            {
                if (value == isLoading){
                    return;
                }
                isLoading = value;
                if(PropertyChanged != null){
                    PropertyChanged(this, new PropertyChangedEventArgs("IsLoading"));
                }
            }
        }

        #endregion

        public Action LoadSongsCommand { get; private set; }
        public Action ConnectCommand { get; private set; }

        private Timer pingTimer;
        private AmpacheSelectionFactory factory;

        public AmpacheViewModel ()
        {
            ConnectCommand = LoadAmpache;
            LoadSongsCommand = LoadSongs;
        }

        private void LoadAmpache()
        {
            if(!IsConnected) {
                try {
                    IsLoading = true;
                    var tmp = new Authenticate(AmpacheSource.AmpacheRootAddress.Get(), AmpacheSource.UserName.Get(), AmpacheSource.UserPassword.Get());
                    factory = new AmpacheSelectionFactory(tmp);
                    pingTimer = new System.Threading.Timer((o) => tmp.Ping(), null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
                    Artists = factory.GetInstanceSelectorFor<AmpacheArtist>().SelectAll().ToList();
                    Albums = factory.GetInstanceSelectorFor<AmpacheAlbum>().SelectAll().ToList();
                    Playlists = factory.GetInstanceSelectorFor<AmpachePlaylist>().SelectAll().ToList();
                    IsConnected = true;
                    // hydrate albums
                    var artists = Artists.ToDictionary(k=>k.Id, v=>v);
                    foreach (var alb in Albums) {
                        if (artists.ContainsKey(alb.ArtistId)) {
                            alb.Hydrate(artists[alb.ArtistId]);
                        }
                    }
                }
                catch (Exception e) {
                    Hyena.Log.ErrorFormat("{0}: message {1}\n{2}", e.GetType().Name, e.Message, e.StackTrace);
                    IsConnected = false;
                }
                IsLoading = false;
            }
        }

        private void LoadSongs()
        {
            var sel = factory.GetInstanceSelectorFor<AmpacheSong>();
            IEnumerable<AmpacheSong> songs = Enumerable.Empty<AmpacheSong>();
            if (SelectedPlaylist != null) {
                songs = sel.SelectBy(SelectedPlaylist);
            }
            else if (SelectedAlbum != null) {
                songs = sel.SelectBy(SelectedAlbum);
            }
            else if (SelectedArtist != null) {
                songs = sel.SelectBy(SelectedArtist);
            }
            var artists = Artists.ToDictionary(k=>k.Id, v=>v);
            var albums = Albums.ToDictionary(k=>k.Id, v=>v);
            foreach (AmpacheSong item in songs) {
                item.Hydrate(albums[item.AlbumId], artists[item.ArtistId]);
            }
            Songs = new HashSet<AmpacheSong>(songs);
        }

        #region INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion

        #region IDisposable implementation
        public void Dispose ()
        {
            if (pingTimer != null) {
                pingTimer.Change(Timeout.Infinite, Timeout.Infinite);
                pingTimer.Dispose();
                pingTimer = null;
            }
            factory = null;
            IsConnected = false;
        }
        #endregion
    }
}

