//
// LiveRadioSource.cs
//
// Authors:
//   Frank Ziegler <funtastix@googlemail.com>
//
// Copyright (C) 2010 Frank Ziegler
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

using Banshee.Sources;
using Banshee.Sources.Gui;

using Banshee.ServiceStack;
using Banshee.Gui;

using Gtk;

using Hyena;

using Banshee.LiveRadio.Plugins;
using Banshee.Collection.Database;
using Banshee.Collection;

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

        public LiveRadioSource () : base(Catalog.GetString ("LiveRadio"), Catalog.GetString ("LiveRadio"), "live-radio", sort_order)
        {
            Log.Debug ("[LiveRadioSource]<Constructor> START");
            Properties.SetString ("Icon.Name", "radio");
            TypeUniqueId = "live-radio";
            IsLocal = false;
            
            plugins = new LiveRadioPluginManager ().LoadPlugins ();
            
            foreach (ILiveRadioPlugin plugin in plugins) {
                Log.DebugFormat ("[LiveRadioSource]<Constructor> found plugin: {0}", plugin.Name);
            }
            
            AfterInitialized ();
            
            InterfaceActionService uia_service = ServiceManager.Get<InterfaceActionService> ();

            uia_service.GlobalActions.AddImportant (new ActionEntry[] { new ActionEntry ("LiveRadioAction", null,
                             Catalog.GetString ("Live_Radio"), null, null, null),
                             new ActionEntry ("LiveRadioConfigureAction", Stock.Properties,
                                 Catalog.GetString ("_Configure"), null,
                                 Catalog.GetString ("Configure the LiveRadio plugin"), OnConfigure) });
            
            uia_service.GlobalActions.AddImportant (
                        new ActionEntry ("RefreshLiveRadioAction",
                                          Stock.Add, Catalog.GetString ("Refresh View"), null,
                                          Catalog.GetString ("Refresh View"),
                                          OnRefreshPlugin));
            uia_service.GlobalActions.AddImportant (
                        new ActionEntry ("AddToInternetRadioAction",
                                          Stock.Add, Catalog.GetString ("Add To Internet Radio"), null,
                                          Catalog.GetString ("Add stream as Internet Radio Station"),
                                          OnAddToInternetRadio));

            ui_global_id = uia_service.UIManager.AddUiFromResource ("GlobalUI.xml");
            
            Properties.SetString ("ActiveSourceUIResource", "ActiveSourceUI.xml");
            Properties.Set<bool> ("ActiveSourceUIResourcePropagate", true);
            Properties.Set<System.Reflection.Assembly> ("ActiveSourceUIResource.Assembly", typeof(LiveRadioPluginSource).Assembly);

            Properties.SetString ("GtkActionPath", "/LiveRadioContextMenu");
            
            Properties.Set<bool> ("Nereid.SourceContentsPropagate", false);
            
            if (!SetupSourceContents ())
                ServiceManager.SourceManager.SourceAdded += OnSourceAdded;
            
            Log.Debug ("[LiveRadioSource]<Constructor> END");
        }

        public void OnConfigure (object o, EventArgs ea)
        {
            new LiveRadioConfigDialog (plugins);
            return;
        }

        private void OnSourceAdded (SourceAddedArgs args)
        {
            if (args.Source is LiveRadioSource)
                SetupSourceContents ();
        }

        private bool SetupSourceContents ()
        {
            foreach (ILiveRadioPlugin plugin in plugins) {
                LiveRadioPluginSource plugin_source = new LiveRadioPluginSource (plugin);
                this.AddChildSource (plugin_source);
                this.MergeSourceInput (plugin_source, SourceMergeType.Source);
                plugin.Initialize ();
                plugin_source.UpdateCounts ();
            }
            
            Properties.Set<ISourceContents> ("Nereid.SourceContents", new CustomView ());
            
            ServiceManager.SourceManager.SourceAdded -= OnSourceAdded;
            return true;
        }

        public override int Count {
            get {
                int sum = 0;
                foreach (ISource source in this.Children)
                    sum += source.Count;
                return sum;
            }
        }

        private class CustomView : ISourceContents
        {
            Gtk.Label label = new Gtk.Label ("LiveRadio! This view will be setup to show statistics...");

            public bool SetSource (ISource source)
            {
                return true;
            }
            public void ResetSource ()
            {
            }
            public Gtk.Widget Widget {
                get { return label; }
            }
            public ISource Source {
                get { return null; }
            }
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

        protected void OnRefreshPlugin (object o, EventArgs e)
        {
            LiveRadioPluginSource current_source = ServiceManager.SourceManager.ActiveSource as LiveRadioPluginSource;
            if (current_source == null) return;
            foreach (ILiveRadioPlugin plugin in plugins) {
                if (plugin.PluginSource.Equals (current_source)) {
                    plugin.RetrieveGenreList ();
                    LiveRadioPluginSourceContents source_contents =
                        current_source.Properties.Get<ISourceContents> ("Nereid.SourceContents") as LiveRadioPluginSourceContents;
                    if (source_contents != null)
                        source_contents.InitRefresh ();

                    return;
                }
            }
        }

        protected void OnAddToInternetRadio (object o, EventArgs e)
        {
            Log.Debug ("[LiveRadioSource]<OnAddToInternetRadio> START");
            PrimarySource internet_radio_source = GetInternetRadioSource ();
            PrimarySource current_source = ServiceManager.SourceManager.ActiveSource as PrimarySource;
            if (current_source == null) {
                Log.Debug ("[LiveRadioSource]<OnAddToInternetRadio> ActiveSource not Primary END");
                return;
            }
            if (internet_radio_source == null) {
                Log.Debug ("[LiveRadioSource]<OnAddToInternetRadio> Internet Radio not found END");
                return;
            }

            ITrackModelSource active_track_model_source = (ITrackModelSource) current_source;

            if (active_track_model_source.TrackModel.SelectedItems == null ||
                active_track_model_source.TrackModel.SelectedItems.Count <= 0) {
                return;
            }

            ILiveRadioPlugin current_plugin = null;
            foreach (ILiveRadioPlugin plugin in plugins)
            {
                if (plugin.PluginSource.Equals (current_source))
                {
                    current_plugin = plugin;
                }
            }

            foreach (TrackInfo track in active_track_model_source.TrackModel.SelectedItems) {
                DatabaseTrackInfo station_track = new DatabaseTrackInfo (track as DatabaseTrackInfo);
                if (station_track != null) {
                    station_track.PrimarySource = internet_radio_source;
                    if (current_plugin != null)
                        station_track.Uri = current_plugin.CleanUpUrl (station_track.Uri);
                    station_track.Save ();
                }
            }
            Log.Debug ("[LiveRadioSource]<OnAddToInternetRadio> END");
        }

        public override bool AcceptsInputFromSource (Source source)
        {
            return false;
        }

        public override bool CanDeleteTracks {
            get { return false; }
        }

        public override bool CanSearch {
            get { return false; }
        }

        public override bool ShowBrowser {
            get { return false; }
        }

        public override bool CanRename {
            get { return false; }
        }

        protected override bool HasArtistAlbum {
            get { return false; }
        }

        public override bool HasViewableTrackProperties {
            get { return true; }
        }

        public override bool HasEditableTrackProperties {
            get { return false; }
        }

        protected PrimarySource GetInternetRadioSource ()
        {
            Log.Debug ("[LiveRadioPluginSource] <GetInternetRadioSource> Start");
            
            foreach (Source source in Banshee.ServiceStack.ServiceManager.SourceManager.Sources) {
                Log.DebugFormat ("[LiveRadioPluginSource] <GetInternetRadioSource> Source: {0}", source.GenericName);
                
                if (source.UniqueId.Equals ("InternetRadioSource-internet-radio")) {
                    return (PrimarySource)source;
                }
            }
            
            Log.Debug ("[LiveRadioPluginSource] <GetInternetRadioSource> Not found throwing exception");
            throw new InternetRadioExtensionNotFoundException ();
        }
    }

    public class InternetRadioExtensionNotFoundException : Exception
    {
    }
}
