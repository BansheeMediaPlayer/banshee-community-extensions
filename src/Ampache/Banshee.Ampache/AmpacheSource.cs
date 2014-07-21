//
// AmpacheSource.cs
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
using System.Threading;
using System.Linq;

using Banshee.Configuration;
using Banshee.Collection;
using Banshee.Gui;
using Banshee.Base;
using Banshee.MediaEngine;
using Banshee.ServiceStack;
using Banshee.Sources;
using Banshee.Sources.Gui;
using Banshee.PlaybackController;
using Banshee.Streaming;

using Gdk;
using Banshee.Collection.Database;

namespace Banshee.Ampache
{
    public class AmpacheSource : PrimarySource, IBasicPlaybackController, ITrackModelSource, IDisposable
    {
        private AmpacheSourceContents _contents;
        private TrackListModel _trackModel;
        private PlayQueue _queue;
        private AmpachePreferences preferences;
        private bool ignoreChanges;

        public AmpacheSource () : base ("Ampache", "Ampache", "Ampache", 90)
        {
            _trackModel = new MemoryTrackListModel();
            Pixbuf icon = new Pixbuf (System.Reflection.Assembly.GetExecutingAssembly ()
                                      .GetManifestResourceStream ("ampache.png"));
            Properties.Set<Pixbuf> ("Icon.Pixbuf_16", icon.ScaleSimple (16, 16, InterpType.Bilinear));
            ServiceManager.SourceManager.AddSource(this);
            preferences = new AmpachePreferences(this);
        }

        public override int Count { get { return 0; } }

        public override string PreferencesPageId {
            get {
                return preferences.PageId;
            }
        }

        public override void Activate ()
        {
            if (_contents == null)
            {
                Properties.Set<ISourceContents> ("Nereid.SourceContents", _contents = new AmpacheSourceContents ());
                _contents.ViewModel.PropertyChanged += Handle_contentsViewModelPropertyChanged;
            }
            base.Activate ();
            ServiceManager.PlaybackController.NextSource = this;
            ServiceManager.PlayerEngine.ConnectEvent(Next, PlayerEvent.RequestNextTrack);
            ServiceManager.PlaybackController.ShuffleModeChanged += HandleServiceManagerPlaybackControllerShuffleModeChanged;
        }

        private void Handle_contentsViewModelPropertyChanged (object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "SelectedSong" && !ignoreChanges && ServiceManager.PlayerEngine.CurrentState != PlayerState.Playing) {
                var sngs = _contents.ViewModel.Songs.ToList();
                var skip = sngs.IndexOf(_contents.ViewModel.SelectedSong);
                _queue = new PlayQueue(sngs.Skip(skip), sngs.Take(skip));
                //Console.WriteLine (_queue.Current.DisplayName);
                if (ServiceManager.PlaybackController.ShuffleMode != "off") {
                    _queue.Shuffle(null);
                }
                //Console.WriteLine (_queue.Current.DisplayName);
                ServiceManager.PlayerEngine.OpenPlay(_queue.Current);
            }
        }

        void HandleServiceManagerPlaybackControllerShuffleModeChanged (object sender, Hyena.EventArgs<string> e)
        {
            if (_queue == null) {
                return;
            }
            if (e.Value == "off") {
                _queue.Unshuffle(new object());
            }
            else {
                _queue.Shuffle(new object());
            }
        }

        private void Next (PlayerEventArgs args)
        {
            Next(true, true);
        }

        #region IBasicPlaybackController implementation
        public bool First ()
        {
            return false;
        }

        public bool Next (bool restart, bool changeImmediately)
        {
            if (_queue == null) {
                return false;
            }
            var song = _queue.PeekNext();
            if (changeImmediately) {
                ignoreChanges = true;
                ServiceManager.PlayerEngine.OpenPlay(song);
                _queue.Next();
                _contents.ViewModel.SelectedSong = song;
                ignoreChanges = false;
            }
            return true;
        }

        public bool Previous (bool restart)
        {
            if (_queue == null) {
                return false;
            }
            ignoreChanges = true;
            var song = _queue.Previous();
            ServiceManager.PlayerEngine.Open(song);
            ignoreChanges = false;
            return true;
        }
        #endregion

        #region ITrackModelSource implementation
        public override void Reload ()
        {}

        public new TrackListModel TrackModel { get { return _trackModel; } }

        public override bool HasDependencies { get { return false; } }

        public override bool CanAddTracks { get { return false; } }

        public override bool CanRemoveTracks { get { return false; } }

        public override bool CanDeleteTracks { get { return false; } }

        public override bool ConfirmRemoveTracks { get { return false; } }

        public override bool CanRepeat { get { return false; } }

        public override bool CanShuffle { get { return true; } }

        public override bool ShowBrowser { get { return false; } }

        public override bool Indexable { get { return false; } }

        #endregion

        #region Schema Entries

        public static readonly SchemaEntry<string> UserName = new SchemaEntry<string>(
            "plugins.ampache", "username", string.Empty, "Ampache user", "Ampache user name"
        );

        private const string DEFAULT_AMPACHE_URL = "http://nameofserver/ampache";
        public static readonly SchemaEntry<string> AmpacheRootAddress = new SchemaEntry<string>(
           "plugins.ampache", "address", DEFAULT_AMPACHE_URL, "Ampache root address", "The address of your Ampache server"
        );

        // TODO: is this really best practice?
        public static readonly SchemaEntry<string> UserPassword = new SchemaEntry<string>(
            "plugins.ampache", "password", string.Empty, "User's password", "User's password"
        );
        #endregion

        #region IDisposable implementation
        public override void Dispose ()
        {
            base.Dispose ();
           // AmpacheSelectionFactory.TearDown();
            this.preferences.Dispose();
        }
        #endregion
    }
}