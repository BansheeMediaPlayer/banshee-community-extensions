//
// FanartService.cs
//
// Author:
//   James Willcox <snorp@novell.com>
//   Gabriel Burt <gburt@novell.com>
//   Tomasz Maczyński <tmtimon@gmail.com>
//
// Copyright 2013 Tomasz Maczyński
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
using Banshee.ServiceStack;
using Hyena;
using Banshee.Configuration;
using Banshee.Sources;
using Hyena.Data.Sqlite;

namespace Banshee.Fanart
{
    public class FanartService : IExtensionService
    {
        private bool disposed;
        private ArtistImageJob job;

        public FanartService ()
        {
        }

        void IExtensionService.Initialize ()
        {
            // TODO: check it:
            // TODO: add disposing
            Banshee.Metadata.MetadataService.Instance.AddProvider (
                new FanartMetadataProvider ());

            if (!ServiceManager.DbConnection.TableExists ("ArtistImageDownloads")) {
                ServiceManager.DbConnection.Execute (@"
                    CREATE TABLE ArtistImageDownloads (
                        MusicBrainzID INTEGER UNIQUE,
                        Downloaded  BOOLEAN,
                        LastAttempt INTEGER NOT NULL
                    )");
            }

            if (!ServiceManager.DbConnection.TableExists ("ArtistMusicBrainz")) {
                ServiceManager.DbConnection.Execute (@"
                    CREATE TABLE ArtistMusicBrainz (
                        ArtistName TEXT UNIQUE,
                        MusicBrainzID INTEGER,
                        LastAttempt INTEGER NOT NULL
                    )");
            }

            if (!ServiceStartup ()) {
                ServiceManager.SourceManager.SourceAdded += OnSourceAdded;
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
            ServiceManager.SourceManager.MusicLibrary.TracksChanged += OnTracksChanged;
            ServiceManager.SourceManager.MusicLibrary.TracksDeleted += OnTracksDeleted;
            FetchArtistImages ();
        }

        public void FetchArtistImages ()
        {
            bool force = false;
            if (!String.IsNullOrEmpty (Environment.GetEnvironmentVariable ("BANSHEE_FORCE_ARTISTS_IMAGES_FETCH"))) {
                Log.Debug ("Forcing artists' images download session");
                force = true;
            }

            FetchArtistImages (force);
        }

        public void FetchArtistImages (bool force)
        {
            if (job == null) {
                var config_variable_name = "last_artist_image_scan";
                DateTime last_scan = DateTime.MinValue;

                if (!force) {
                    try {
                        last_scan = DatabaseConfigurationClient.Client.Get<DateTime> (config_variable_name,
                                                                                      DateTime.MinValue);
                    } catch (FormatException) {
                        Log.Warning (String.Format ("{0} is malformed, resetting to default value", 
                                                    config_variable_name));
                        DatabaseConfigurationClient.Client.Set<DateTime> (config_variable_name,
                                                                          DateTime.MinValue);
                    }
                }

                // TODO: leave only final version
                // this line is just to force update:
                //job = new ArtistImageJob (DateTime.MinValue);
                // final version:
                job = new ArtistImageJob (last_scan);

                job.Finished += delegate {
                    if (!job.IsCancelRequested) {
                        DatabaseConfigurationClient.Client.Set<DateTime> (config_variable_name, DateTime.Now);
                    }
                    job = null;
                };
                job.Start ();
            }
        }

        private void OnSourceAdded (SourceAddedArgs args)
        {
            if (ServiceStartup ()) {
                ServiceManager.SourceManager.SourceAdded -= OnSourceAdded;
            }
        }

        private void OnTracksAdded (Source sender, TrackEventArgs args)
        {
            FetchArtistImages ();
        }

        private void OnTracksChanged (Source sender, TrackEventArgs args)
        {
            if (args.ChangedFields == null) {
                FetchArtistImages ();
            } else {
                foreach (Hyena.Query.QueryField field in args.ChangedFields) {
                    // TODO: check that:
                    if (field == Banshee.Query.BansheeQuery.AlbumField ||
                        field == Banshee.Query.BansheeQuery.ArtistField ||
                        field == Banshee.Query.BansheeQuery.AlbumArtistField) {
                        FetchArtistImages ();
                        break;
                    }
                }
            }
        }

        // TODO: update:
        /*
        private static HyenaSqliteCommand delete_query = new HyenaSqliteCommand (
            "DELETE FROM ArtistImageDownloads WHERE ArtistID NOT IN (SELECT ArtistID FROM CoreArtists)");
        */

        private void OnTracksDeleted (Source sender, TrackEventArgs args)
        {
            // ServiceManager.DbConnection.Execute (delete_query);
        }

        public void Dispose ()
        {
            if (disposed) {
                return;
            }

            ServiceManager.SourceManager.MusicLibrary.TracksAdded -= OnTracksAdded;
            ServiceManager.SourceManager.MusicLibrary.TracksChanged -= OnTracksChanged;
            ServiceManager.SourceManager.MusicLibrary.TracksDeleted -= OnTracksDeleted;

            disposed = true;
        }

        #region IService implementation

        string IService.ServiceName {
            get { return "FanartService"; }
        }

        #endregion

        // public static readonly SchemaEntry<bool> EnabledSchema
        // is skipped
    }
}

