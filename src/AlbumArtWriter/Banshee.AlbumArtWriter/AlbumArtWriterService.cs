//
// AlbumArtWriterService.cs
//
// Authors:
//   Kevin@NosideRacing.com
//
// Copyright (C) 2011 Kevin Anthony
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

using Gtk;
using Mono.Unix;

using Hyena;

using Banshee.Base;
using Banshee.Collection;
using Banshee.Collection.Database;
using Banshee.ServiceStack;
using Banshee.Configuration;
using Banshee.Gui;
using Banshee.Collection.Gui;
using Banshee.Library;
using Banshee.Metadata;
using Banshee.Networking;
using Banshee.Sources;

namespace Banshee.AlbumArtWriter
{
    /* TODO: I would like to every 7 days or so, check to make sure the album art is actually still in the folder */
    public class AlbumArtWriterService : IExtensionService
    {
        private bool disposed;
        private AlbumArtWriterJob job;

        public AlbumArtWriterService ()
        {
        }

        void IExtensionService.Initialize ()
        {
            /*
             * if SavedOrTried = 0, try and download the art
             * if SavedOrTried = 1, we have tried this session
             * if SavedOrTried = 2, art is already in folder
             * if SavedOrTired = 3, we were successful in writing art to folder
             */
            if (!ServiceManager.DbConnection.TableExists ("AlbumArtWriter")) {
                ServiceManager.DbConnection.Execute (@"
                        CREATE TABLE AlbumArtWriter (
                            AlbumID     INTEGER UNIQUE,
                            SavedOrTried INTEGER
                    )");
            }

            if (!ServiceStartup ()) {
                ServiceManager.SourceManager.SourceAdded += OnSourceAdded;
            }
        }

        private void OnSourceAdded (SourceAddedArgs args)
        {
            if (ServiceStartup ()) {
                ServiceManager.SourceManager.SourceAdded -= OnSourceAdded;
            }
        }

        private bool ServiceStartup ()
        {
            if (ServiceManager.SourceManager.MusicLibrary == null) {
                return false;
            }

            Initialize ();

            return true;
        }

        private void Initialize ()
        {
            ServiceManager.SourceManager.MusicLibrary.TracksAdded += OnTracksAdded;
            StartWriterJob ();
        }

        public void Dispose ()
        {
            if (disposed) {
                return;
            }

            ServiceManager.SourceManager.MusicLibrary.TracksAdded -= OnTracksAdded;
            /* Setting SavedOrTried to 0 where SavedOrTried = 1 allows album art to be written next time */
            ServiceManager.DbConnection.Execute (@"UPDATE AlbumArtWriter SET SavedOrTried = 0 WHERE SavedOrTried = 1");
            disposed = true;
        }

        public void StartWriterJob ()
        {
            if (job == null) {
                job = new AlbumArtWriterJob ();
                job.Finished += delegate {
                    job = null;
                };
                job.Start ();
            }
        }

        private void OnTracksAdded (Source sender, TrackEventArgs args)
        {
            StartWriterJob ();
        }

        string IService.ServiceName {
            get { return "AlbumArtWriterService"; }
        }
    }
}
