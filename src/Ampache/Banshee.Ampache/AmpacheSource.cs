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

namespace Banshee.Ampache
{
    public class AmpacheSource : Source, IBasicPlaybackController, ITrackModelSource
    {    
        private AmpacheSourceContents _contents;
        private TrackListModel _trackModel;
        private PlayQueue _queue;

        public AmpacheSource () : base ("Ampache", "Ampache", 90, "Ampache")
        {
            _trackModel = new MemoryTrackListModel();
            Pixbuf icon = new Pixbuf (System.Reflection.Assembly.GetExecutingAssembly ()
                                      .GetManifestResourceStream ("ampache.png"));
            Properties.Set<Pixbuf> ("Icon.Pixbuf_22", icon.ScaleSimple (22, 22, InterpType.Bilinear));
            //InterfaceActionService svc = Get<InterfaceActionService>("InterfaceActionService");
            //svc.PlaybackActions.ShuffleActions
        }
        
        public override int Count { get { return 0; } }
        
        public override void Activate ()
        {
            if (_contents == null)
            {
                Properties.Set<ISourceContents> ("Nereid.SourceContents", _contents = new AmpacheSourceContents ());
                _contents.View.NewPlayList += Handle_NewPlayList;
            }
            base.Activate ();
            ServiceManager.PlaybackController.NextSource = this;
            ServiceManager.PlayerEngine.ConnectEvent(Next, PlayerEvent.RequestNextTrack);
            ServiceManager.PlaybackController.ShuffleModeChanged += HandleServiceManagerPlaybackControllerShuffleModeChanged;
        }

        void Handle_NewPlayList (object sender, Hyena.EventArgs<PlayQueue> e)
        {
            _queue = e.Value;
            ServiceManager.PlayerEngine.Open(_queue.Current);
            if (ServiceManager.PlaybackController.ShuffleMode != "off") {
                _queue.Shuffle(new object());
            }
        }

        void HandleServiceManagerPlaybackControllerShuffleModeChanged (object sender, Hyena.EventArgs<string> e)
        {
            if (_queue == null)    {
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
        public override void Deactivate ()
        {
            base.Deactivate ();
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
                ServiceManager.PlayerEngine.Open(song);
                _contents.View.SelectPlayingSong(song);
                _queue.Next();
            }
            return true;
        }

        public bool Previous (bool restart)
        {
            if (_queue == null) {
                return false;
            }
            var song = _queue.Previous();
            ServiceManager.PlayerEngine.Open(song);
            _contents.View.SelectPlayingSong(song);
            return true;
        }        
        #endregion
        
        #region ITrackModelSource implementation
        public void Reload ()
        {
            //throw new NotImplementedException ();
        }

        public void RemoveSelectedTracks ()
        {
            //throw new NotImplementedException ();
        }

        public void DeleteSelectedTracks ()
        {
            //throw new NotImplementedException ();
        }

        public TrackListModel TrackModel { get { return _trackModel; } }

        public bool HasDependencies { get { return false; } }

        public bool CanAddTracks { get { return false; } }

        public bool CanRemoveTracks { get { return false; } }

        public bool CanDeleteTracks { get { return false; } }

        public bool ConfirmRemoveTracks { get { return false; } }

        public bool CanRepeat { get { return false; } }

        public bool CanShuffle { get { return true; } }

        public bool ShowBrowser { get { return false; } }

        public bool Indexable { get { return false; } }
        #endregion
    }
}

