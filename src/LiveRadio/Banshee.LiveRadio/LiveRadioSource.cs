//
// LiveRadioSource.cs
//
// Authors:
//   Cool Extension Author <cool.extension@author.com>
//
// Copyright (C) 2010 Cool Extension Author
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
using System.Collections.Generic;

using Mono.Unix;

using Banshee.Base;
using Banshee.Sources;
using Banshee.Sources.Gui;

using Banshee.ServiceStack;
using Banshee.Preferences;
using Banshee.Streaming;
using Banshee.MediaEngine;
using Banshee.PlaybackController;
using Banshee.Collection;
using Banshee.Collection.Database;
using Banshee.Configuration;
using Banshee.Gui;

using Gtk;

using Hyena;

using Banshee.LiveRadio.Plugins;
using Banshee.Library;

namespace Banshee.LiveRadio
{
    // We are inheriting from Source, the top-level, most generic type of Source.
    // Other types include (inheritance indicated by indentation):
    //      DatabaseSource - generic, DB-backed Track source; used by PlaylistSource
    //        PrimarySource - 'owns' tracks, used by DaapSource, DapSource
    //          LibrarySource - used by Music, Video, Podcasts, and Audiobooks
    public class LiveRadioSource : PrimarySource, IDisposable
    {
        // In the sources TreeView, sets the order value for this source, small on top
        const int sort_order = 190;
        private List<ILiveRadioPlugin> plugins;
        private uint ui_global_id;

        public LiveRadioSource () : base (Catalog.GetString ("LiveRadio"), Catalog.GetString ("LiveRadio"), "live-radio", sort_order)
        {
            Log.Debug("[LiveRadioSource]<Constructor> START");
            Properties.SetString ("Icon.Name", "radio");
            TypeUniqueId = "live-radio";
            IsLocal = false;

            plugins = new LiveRadioPluginManager ().LoadPlugins ();

            foreach(ILiveRadioPlugin plugin in plugins)
            {
                Log.DebugFormat("[LiveRadioSource]<Constructor> found plugin: {0}", plugin.GetName ());
            }

            AfterInitialized ();

            InterfaceActionService uia_service = ServiceManager.Get<InterfaceActionService> ();
            uia_service.GlobalActions.AddImportant (
                new ActionEntry ("AddToFavoritesAction", Stock.Add,
                    Catalog.GetString ("Add To Favorites"), null,
                    Catalog.GetString ("Add stream to Favorites"),
                    OnAddToFavorites)
            );
            uia_service.GlobalActions.AddImportant (
                new ActionEntry ("AddToInternetRadioAction", Stock.Add,
                    Catalog.GetString ("Add To Internet Radio"), null,
                    Catalog.GetString ("Add stream as Internet Radio Station"),
                    OnAddToInternetRadio)
            );
            uia_service.GlobalActions.AddImportant (
                new ActionEntry ("FindSimilarStreamsAction", Stock.Find,
                    Catalog.GetString ("Find Similar Streams"), null,
                    Catalog.GetString ("Find Streams Similar to this one"),
                    OnFindSimilar)
            );

            ui_global_id = uia_service.UIManager.AddUiFromResource ("GlobalUI.xml");

            Properties.SetString ("ActiveSourceUIResource", "ActiveSourceUI.xml");
            Properties.Set<bool> ("ActiveSourceUIResourcePropagate", true);
            Properties.Set<System.Reflection.Assembly> ("ActiveSourceUIResource.Assembly", typeof(LiveRadioPluginSource).Assembly);


            Properties.SetString ("GtkActionPath", "/LiveRadioContextMenu");

            Properties.Set<bool> ("Nereid.SourceContentsPropagate", true);

            if (!SetupSourceContents ())
                ServiceManager.SourceManager.SourceAdded += OnSourceAdded;

            Log.Debug("[LiveRadioSource]<Constructor> END");
        }

        private void OnSourceAdded (SourceAddedArgs args)
        {
            if (args.Source is LiveRadioSource)
                SetupSourceContents ();
        }

        private bool SetupSourceContents ()
        {
            foreach (ILiveRadioPlugin plugin in plugins)
            {
                LiveRadioPluginSource plugin_source = new LiveRadioPluginSource (plugin);
                this.AddChildSource(plugin_source);
                this.MergeSourceInput(plugin_source, SourceMergeType.All);
                plugin.Initialize ();
            }

            Properties.Set<ISourceContents> ("Nereid.SourceContents", new CustomView ());

            ServiceManager.SourceManager.SourceAdded -= OnSourceAdded;
            return true;
        }

        public override int Count {
            get
            {
                int sum = 0;
                foreach (ISource source in this.Children)
                    sum += source.Count;
                return sum;
            }
        }

        private class CustomView : ISourceContents
        {
            Gtk.Label label = new Gtk.Label ("LiveRadio! This view will be setup to show statistics...");

            public bool SetSource (ISource source) { return true; }
            public void ResetSource () { }
            public Gtk.Widget Widget { get { return label; } }
            public ISource Source { get { return null; } }
        }

        public override void Dispose ()
        {
            base.Dispose ();

            InterfaceActionService uia_service = ServiceManager.Get<InterfaceActionService> ();
            if (uia_service == null) {
                return;
            }

            if (ui_global_id > 0) {
                uia_service.UIManager.RemoveUi (ui_global_id);
                ui_global_id = 0;
            }

        }

        protected void OnAddToFavorites(object o, EventArgs e)
        {
            return;
        }

        protected void OnAddToInternetRadio(object o, EventArgs e)
        {
            return;
        }

        protected void OnFindSimilar(object o, EventArgs e)
        {
            return;
        }

        public override bool AcceptsInputFromSource (Source source)
        {
            return false;
        }

        public override bool CanDeleteTracks {
            get { return false; }
        }

        public override bool ShowBrowser {
            get { return true; }
        }

        public override bool CanRename {
            get { return false; }
        }

        protected override bool HasArtistAlbum {
            get { return false; }
        }

        public override bool HasViewableTrackProperties {
            get { return false; }
        }

    }

}