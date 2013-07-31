//
// SongKickSource.cs
//
// Authors:
//   Tomasz Maczyński
//
// Copyright (C) 2011 Tomasz Maczyński
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

using Mono.Addins;

using Banshee.Base;
using Banshee.Sources;
using Banshee.Sources.Gui;

// Other namespaces you might want:
using Banshee.ServiceStack;
using Banshee.Preferences;
using Banshee.MediaEngine;
using Banshee.PlaybackController;
using Banshee.SongKick.Recommendations;
using Hyena.Jobs;
using Banshee.SongKick.Network;
using Banshee.SongKick.UI;

using System.Linq;

namespace Banshee.SongKick
{
    // We are inheriting from Source, the top-level, most generic type of Source.
    // Other types include (inheritance indicated by indentation):
    //      DatabaseSource - generic, DB-backed Track source; used by PlaylistSource
    //        PrimarySource - 'owns' tracks, used by DaapSource, DapSource
    //          LibrarySource - used by Music, Video, Podcasts, and Audiobooks

    public class SongKickSource : Source, IDisposable
    {
        // In the sources TreeView, sets the order value for this source, small on top
        const int sort_order = 190;


        public SongKickSource () : base (AddinManager.CurrentLocalizer.GetString ("SongKick"),
                                               AddinManager.CurrentLocalizer.GetString ("SongKick"),
		                                       sort_order,
		                                       "extension-unique-id")
        {
            //Change comment in lines below to see described behaviour:
            //Properties.Set<ISourceContents> ("Nereid.SourceContents", new SongKickSourceContents ());
            Properties.Set<ISourceContents> ("Nereid.SourceContents", new LazyLoadSourceContents <SongKickSourceContents> ());

            // set logo:
            // TODO: fix that so that it works with various resolutions
            Properties.SetStringList ("Icon.Name", "songkick_logo");

            // For testing purpose only:
            /*
            System.Threading.Thread thread = 
                new System.Threading.Thread(
                    new System.Threading.ThreadStart( 
                        () => {
                            var list = new Banshee.SongKick.Network.SongKickDownloader(SongKickCore.APIKey)
                               .findLocationBasedOnIP(Locations.GetLocationListResultsDelegate)
                               .results
                               .ToList<Banshee.SongKick.Recommendations.Location>();
                            foreach(var elem in list) {
                                Hyena.Log.Debug(elem.ToString());
                            }
                        }));
            thread.Start();
            */

            Hyena.Log.Information ("SongKick source has been instantiated!");
        }

        // A count of 0 will be hidden in the source TreeView
        public override int Count {
            get { return 0; }
        }

        /*
        private class CustomView : ISourceContents
        {
            Gtk.Label label = new Gtk.Label ("Custom view for SongKick extension is working!");

            public bool SetSource (ISource source) { return true; }
            public void ResetSource () { }
            public Gtk.Widget Widget { get { return label; } }
            public ISource Source { get { return null; } }
        }
        */

        #region IDisposable implementation

        public void Dispose ()
        {
            // no resources that need to be disposed
        }
        #endregion
    }
}
