//
// UbuntuOneMusicStoreSource.cs
//
// Authors:
//   Jo Shields <directhex@apebox.org>
//
// Copyright (C) 2010 Jo Shields
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;

using Mono.Unix;
using Gdk;

using Hyena;

using Banshee.Base;
using Banshee.Collection;
using Banshee.Gui;
using Banshee.ServiceStack;
using Banshee.Sources;
using Banshee.Sources.Gui;

namespace Banshee.UbuntuOneMusicStore
{
    public class UbuntuOneMusicStoreSource : Source
    {
        // In the sources TreeView, sets the order value for this source, small on top
        const int sort_order = 190;

        public UbuntuOneMusicStoreSource () : base (Catalog.GetString ("Ubuntu One Music Store"), Catalog.GetString ("Ubuntu One Music Store"), sort_order)
        {
            Pixbuf icon = new Pixbuf (System.Reflection.Assembly.GetExecutingAssembly ()
                                      .GetManifestResourceStream ("ubuntuone.png"));
            Properties.Set<Pixbuf> ("Icon.Pixbuf_22", icon.ScaleSimple (22, 22, InterpType.Bilinear));
            Properties.Set<ISourceContents> ("Nereid.SourceContents", new CustomView ());

            Log.Debug ("U1MS: Initialized");
        }

        // A count of 0 will be hidden in the source TreeView
        public override int Count {
            get { return 0; }
        }

        public class StoreWrapper: UbuntuOne.U1MusicStore, IDisableKeybindings
        {
            string U1LibraryLocation = System.IO.Path.Combine (System.IO.Path.Combine (System.Environment.GetFolderPath (System.Environment.SpecialFolder.Personal), ".ubuntuone"), "Purchased from Ubuntu One");

            public StoreWrapper (): base ()
            {
                this.PreviewMp3 += PlayMP3Preview;
                this.DownloadFinished += AddDownloadToLibrary;
                this.PlayLibrary += PlayU1MSLibrary;
                this.UrlLoaded += U1MSUrlLoaded;
            }

            private void PlayMP3Preview (object Sender, UbuntuOne.PreviewMp3Args a)
            {
                Log.Debug ("U1MS: Playing preview: ", a.Url );
                TrackInfo PreviewTrack = new TrackInfo ();
                PreviewTrack.TrackTitle = a.Title;
                PreviewTrack.ArtistName = Catalog.GetString ("Track Preview");
                PreviewTrack.AlbumTitle = Catalog.GetString ("Ubuntu One Music Store");
                PreviewTrack.Uri = new SafeUri (a.Url);
                ServiceManager.PlayerEngine.OpenPlay (PreviewTrack);
                ServiceManager.PlaybackController.StopWhenFinished = true;
            }

            private void AddDownloadToLibrary (object Sender, UbuntuOne.DownloadFinishedArgs a)
            {
                Log.Debug ("U1MS: Track downloaded: ", a.Path);
                ServiceManager.Get<Banshee.Library.LibraryImportManager> ().ImportTrack (new SafeUri (a.Path));
                ServiceManager.Get<Banshee.Library.LibraryImportManager> ().NotifyAllSources ();
            }

            private void PlayU1MSLibrary (object Sender, UbuntuOne.PlayLibraryArgs a)
            {
                Log.Debug ("U1MS: Playing from library: ", a.Path);
                Log.Debug ("U1MS: U1 library location: ", U1LibraryLocation);
                int track_id = Banshee.Collection.Database.DatabaseTrackInfo.GetTrackIdForUri (System.IO.Path.Combine (U1LibraryLocation, a.Path));
                if (track_id > 0)
                {
                    var track = Banshee.Collection.Database.DatabaseTrackInfo.Provider.FetchSingle (track_id);
                    ServiceManager.PlaybackController.NextSource = ServiceManager.SourceManager.MusicLibrary;
                    ServiceManager.PlayerEngine.OpenPlay (track);
                }
            }

            private void U1MSUrlLoaded (object Sender, UbuntuOne.UrlLoadedArgs a)
            {
                Log.Debug ("U1MS: Url Loaded: ", a.Url);
            }
        }

        private class CustomView : ISourceContents
        {
            StoreWrapper store = new StoreWrapper ();

            public bool SetSource (ISource source) { return true; }
            public void ResetSource () { }
            public Gtk.Widget Widget { get { return store; } }
            public ISource Source { get { return null; } }
        }
    }
}
