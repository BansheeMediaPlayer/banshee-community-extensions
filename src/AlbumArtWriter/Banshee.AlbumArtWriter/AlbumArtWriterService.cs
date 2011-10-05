//
// AlbumArtWriterService.cs
//
// Authors:
//   Kevin Anthony <Kevin.S.Anthony@gmail.com>
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
using Mono.Addins;

using Banshee.ServiceStack;
using Banshee.Configuration;
using Banshee.Sources;
using Banshee.Gui;
using Hyena;
using Banshee.MediaEngine;

namespace Banshee.AlbumArtWriter
{
    public class AlbumArtWriterService : IExtensionService, IDisposable
    {
        private bool disposed;
        private AlbumArtWriterJob job;
        private InterfaceActionService action_service;
        private ActionGroup actions;
        private uint ui_manager_id;
        private bool forced;

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
            DatabaseConfigurationClient.Client.Set<int>("AlbumArtWriter", "Version", 1);
            }
	        if (DatabaseConfigurationClient.Client.Get<int> ("AlbumArtWriter", "Version", 0) < 2) {
                ServiceManager.DbConnection.Execute (@"ALTER TABLE AlbumArtWriter ADD COLUMN LastUpdated INTEGER");
                DatabaseConfigurationClient.Client.Set<int>("AlbumArtWriter", "Version", 2);
            }  
            if (!ServiceStartup ()) {
                ServiceManager.SourceManager.SourceAdded += OnSourceAdded;
            }

            action_service = ServiceManager.Get<InterfaceActionService> ();

            actions = new ActionGroup ("AlbumArtWriter");

            actions.Add (new ActionEntry [] {
                new ActionEntry ("AlbumArtWriterAction", null,
                    AddinManager.CurrentLocalizer.GetString ("Album Art Writer"), null,
                    null, null),
                new ActionEntry ("AlbumArtWriterConfigureAction", Stock.Properties,
                    AddinManager.CurrentLocalizer.GetString ("_Configure..."), null,
                    AddinManager.CurrentLocalizer.GetString ("Configure the Album Art Writer plugin"), OnConfigure),
                new ActionEntry ("AlbumArtWriterForceAction", Stock.Refresh,
                    AddinManager.CurrentLocalizer.GetString ("Force Copy"), null,
                    AddinManager.CurrentLocalizer.GetString ("Force Recopy of all Album Art"), onForce)
            });

            action_service.UIManager.InsertActionGroup (actions, 0);
            ui_manager_id = action_service.UIManager.AddUiFromResource ("GlobalUI.xml");
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

            action_service.UIManager.RemoveUi (ui_manager_id);
            action_service.UIManager.RemoveActionGroup (actions);
            actions = null;

            ServiceManager.SourceManager.MusicLibrary.TracksAdded -= OnTracksAdded;
            /* Setting SavedOrTried to 0 where SavedOrTried = 1 allows album art to be written next time */
            ServiceManager.DbConnection.Execute (@"UPDATE AlbumArtWriter SET SavedOrTried = 0 WHERE SavedOrTried = 1");
            disposed = true;
        }

        public void StartWriterJob ()
        {
            if (job == null) {
                job = new AlbumArtWriterJob (this);
                job.Finished += delegate {
                    job = null;
                    forced = false;
                };
                job.Start ();
            }
        }
        #region Actons
        private void onForce(object o, EventArgs args){
            Log.Information("Forcing Copy of album art");
            forced = true;
            StartWriterJob();
        }
        private void OnConfigure (object o, EventArgs args)
        {
            ConfigurationDialog dialog = new ConfigurationDialog (this);
            dialog.Run ();
            dialog.Destroy ();
        }
        #endregion
        #region Configuration properties
        internal bool AlbumArtWriterEnabled
        {
            get { return ConfigurationSchema.IsEnabled.Get (); }
            set { ConfigurationSchema.IsEnabled.Set (value); }
        }       internal bool JPG
        {
            get { return ConfigurationSchema.JPG.Get (); }
            set { ConfigurationSchema.JPG.Set (value); }
        }
        internal bool PNG
        {
            get { return ConfigurationSchema.PNG.Get (); }
            set { ConfigurationSchema.PNG.Set (value); }
        }
        internal string ArtName
        {
            get { return ConfigurationSchema.ArtName.Get (); }
            set { ConfigurationSchema.ArtName.Set (value); }
        }
        internal bool ForceRecopy{
            get {return this.forced;}
            set {this.forced = value;}
        }
        #endregion
        private void OnTracksAdded (Source sender, TrackEventArgs args)
        {
            StartWriterJob ();
        }

        string IService.ServiceName {
            get { return "AlbumArtWriterService"; }
        }
    }
}
