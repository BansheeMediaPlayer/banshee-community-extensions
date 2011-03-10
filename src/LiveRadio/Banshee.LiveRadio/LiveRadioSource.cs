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

using Mono.Addins;

using Banshee.Sources;
using Banshee.Sources.Gui;

using Banshee.ServiceStack;
using Banshee.Gui;

using Gtk;

using Hyena;

using Banshee.LiveRadio.Plugins;
using Banshee.Collection.Database;
using Banshee.Collection;
using Banshee.Configuration;
using System.Text;

namespace Banshee.LiveRadio
{

    /// <summary>
    /// The main class of the LiveRadio Extension. It creates a new primary source in the source trees and adds
    /// child sources for each internet radio plugin detected.
    ///
    /// TODO by priority:
    /// * combined search/results for all plugins
    /// * add option to use system proxy -- move to banshee.io.httprequest
    /// * use Mono.Addins for plugins
    /// * save/cancel/apply button in config -> close -> save changes immediately
    /// ** not sure if this can be done safely, configuration widget delivered
    ///    by plugins, might need to extend interface or move logic completely to plugin
    /// </summary>
    public class LiveRadioSource : PrimarySource, IDisposable
    {
        // In the sources TreeView, sets the order value for this source, small on top
        const int sort_order = 190;
        private List<ILiveRadioPlugin> plugins;
        private uint ui_global_id;
        private List<string> enabled_plugins;
        private LiveRadioSourceContents source_contents;

        /// <summary>
        /// Constructor -- creates a new LiveRadio parent source
        /// </summary>
        public LiveRadioSource () : base(AddinManager.CurrentLocalizer.GetString ("LiveRadio"),
                                         AddinManager.CurrentLocalizer.GetString ("LiveRadio"), "live-radio", sort_order)
        {
            Properties.SetString ("Icon.Name", "radio");
            TypeUniqueId = "live-radio";
            IsLocal = false;

            string enabled_plugin_names = EnabledPluginsEntry.Get ();
            if (String.IsNullOrEmpty (enabled_plugin_names))
                enabled_plugins = new List<string> ();
            else
                enabled_plugins = new List<string> (enabled_plugin_names.Split (','));

            //Load internet directory parser plugins
            plugins = new LiveRadioPluginManager ().LoadPlugins ();
            foreach (ILiveRadioPlugin plugin in plugins) {
                Log.DebugFormat ("[LiveRadioSource]<Constructor> found plugin: {0}, enabled: {1}",
                                  plugin.Name,
                                 (EnabledPlugins.Contains (plugin.Name)));
            }

            AfterInitialized ();

            //setting up interface actions
            InterfaceActionService uia_service = ServiceManager.Get<InterfaceActionService> ();

            uia_service.GlobalActions.AddImportant (new ActionEntry[] { new ActionEntry ("LiveRadioAction", null,
                             AddinManager.CurrentLocalizer.GetString ("Live_Radio"), null, null, null),
                             new ActionEntry ("LiveRadioConfigureAction", Stock.Properties,
                                 AddinManager.CurrentLocalizer.GetString ("_Configure"), null,
                                 AddinManager.CurrentLocalizer.GetString ("Configure the LiveRadio plugin"), OnConfigure) });

            uia_service.GlobalActions.AddImportant (
                        new ActionEntry ("RefreshLiveRadioAction",
                                          Stock.Refresh, AddinManager.CurrentLocalizer.GetString ("Refresh View"), null,
                                          AddinManager.CurrentLocalizer.GetString ("Refresh View"),
                                          OnRefreshPlugin));
            uia_service.GlobalActions.AddImportant (
                        new ActionEntry ("AddToInternetRadioAction",
                                          Stock.Add, AddinManager.CurrentLocalizer.GetString ("Add To Internet Radio"), null,
                                          AddinManager.CurrentLocalizer.GetString ("Add stream as Internet Radio Station"),
                                          OnAddToInternetRadio));

            ui_global_id = uia_service.UIManager.AddUiFromResource ("GlobalUI.xml");

            Properties.SetString ("ActiveSourceUIResource", "ActiveSourceUI.xml");
            Properties.Set<bool> ("ActiveSourceUIResourcePropagate", true);
            Properties.Set<System.Reflection.Assembly> ("ActiveSourceUIResource.Assembly", typeof(LiveRadioPluginSource).Assembly);

            Properties.SetString ("GtkActionPath", "/LiveRadioContextMenu");
            Properties.Set<bool> ("Nereid.SourceContentsPropagate", false);

            if (!SetupSourceContents ())
                ServiceManager.SourceManager.SourceAdded += OnSourceAdded;

        }

        /// <summary>
        /// Handles LiveRadioConfigureAction. Creates a new LiveRadioConfigDialog.
        /// </summary>
        /// <param name="o">
        /// A <see cref="System.Object"/> -- not used
        /// </param>
        /// <param name="ea">
        /// A <see cref="EventArgs"/> -- not used
        /// </param>
        public void OnConfigure (object o, EventArgs ea)
        {
            new LiveRadioConfigDialog (this, plugins);
            return;
        }

        /// <summary>
        /// Handles SourceAdded Event of SourceManager. When this source is added, its contents are setup.
        /// </summary>
        /// <param name="args">
        /// A <see cref="SourceAddedArgs"/> -- not used
        /// </param>
        private void OnSourceAdded (SourceAddedArgs args)
        {
            if (args.Source is LiveRadioSource)
                SetupSourceContents ();
        }

        /// <summary>
        /// Setup the source contents of the LiveRadioSource, i.e. create a childsource for each enabled plugin found
        /// and initialize the overview widget.
        /// </summary>
        /// <returns>
        /// A <see cref="System.Boolean"/> -- always returns true
        /// </returns>
        private bool SetupSourceContents ()
        {
            foreach (ILiveRadioPlugin plugin in plugins) {
                if (EnabledPlugins.Contains (plugin.Name))
                {
                    AddPlugin (plugin);
                }
            }

            source_contents = new LiveRadioSourceContents (plugins);
            Properties.Set<ISourceContents> ("Nereid.SourceContents", source_contents);

            ServiceManager.SourceManager.SourceAdded -= OnSourceAdded;
            return true;
        }

        /// <summary>
        /// Register an external plugin with the main source, so it can be added as a child via AddPlugin method.
        /// Assembly plugins are autodetected/autoregistered
        /// </summary>
        /// <param name="plugin">
        /// A <see cref="ILiveRadioPlugin"/> -- an external plugin
        /// </param>
        public void RegisterPlugin (ILiveRadioPlugin plugin)
        {
            if (plugins.Contains (plugin))
            {
                Log.DebugFormat ("[LiveRadioSource]<RegisterPlugin> plugin {0} already registered", plugin.Name);
            } else {
                plugins.Add (plugin);
                source_contents.ConnectPluginEvents (plugin);
            }
        }

        /// <summary>
        /// Add and initialize a plugin -- if it is not an assembly plugin, it must be registered first, assembly
        /// plugins are auto registered
        /// </summary>
        /// <param name="plugin">
        /// A <see cref="ILiveRadioPlugin"/> -- the plugin to initialize
        /// </param>
        public void AddPlugin (ILiveRadioPlugin plugin)
        {
            if (plugins.Contains (plugin))
            {
                LiveRadioPluginSource plugin_source = new LiveRadioPluginSource (plugin);
                this.AddChildSource (plugin_source);
                this.MergeSourceInput (plugin_source, SourceMergeType.Source);
                plugin.Initialize ();
                plugin_source.UpdateCounts ();
                if (!enabled_plugins.Contains (plugin.Name))
                {
                    enabled_plugins.Add(plugin.Name);
                    SetEnabledPluginsEntry ();
                }
            } else {
                Log.DebugFormat ("[LiveRadioSource]<AddPlugin> plugin {0} not registered, cannot add", plugin.Name);
            }
        }

        /// <summary>
        /// Removes a plugin source
        /// </summary>
        /// <param name="plugin">
        /// A <see cref="ILiveRadioPlugin"/> the plugin which to remove the source for
        /// </param>
        public void RemovePlugin(ILiveRadioPlugin plugin)
        {
            if (plugins.Contains (plugin))
            {
                LiveRadioPluginSource plugin_source = plugin.PluginSource;
                plugin.Disable ();
                this.RemoveChildSource(plugin_source);
                plugin_source.Dispose ();
                this.UpdateCounts ();
                if (enabled_plugins.Contains (plugin.Name))
                {
                    enabled_plugins.Remove(plugin.Name);
                    SetEnabledPluginsEntry ();
                }
            } else {
                Log.DebugFormat ("[LiveRadioSource]<RemovePlugin> plugin {0} not registered, cannot remove", plugin.Name);
            }
        }

        /// <summary>
        /// Returns the sum of the child sources' counts
        /// </summary>
        public override int Count {
            get {
                int sum = 0;
                foreach (ISource source in this.Children)
                    sum += source.Count;
                return sum;
            }
        }

        /// <summary>
        /// Dispose interface actions and remove UI.
        /// </summary>
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
            uia_service.GlobalActions.Remove ("AddToInternetRadioAction");
            uia_service.GlobalActions.Remove ("RefreshLiveRadioAction");
            uia_service.GlobalActions.Remove ("LiveRadioConfigureAction");
            uia_service.GlobalActions.Remove ("LiveRadioAction");
        }

        /// <summary>
        /// Handles the RefreshLiveRadioAction. Initiates a reload of the active source/plugin's genre list
        /// and resets the active child source's view
        /// </summary>
        /// <param name="o">
        /// A <see cref="System.Object"/> -- not used
        /// </param>
        /// <param name="e">
        /// A <see cref="EventArgs"/> -- not used
        /// </param>
        protected void OnRefreshPlugin (object o, EventArgs e)
        {
            LiveRadioPluginSource current_source = ServiceManager.SourceManager.ActiveSource as LiveRadioPluginSource;
            if (current_source == null) return;
            foreach (ILiveRadioPlugin plugin in plugins) {
                if (plugin.PluginSource != null && plugin.PluginSource.Equals (current_source)) {
                    plugin.RetrieveGenreList ();
                    LiveRadioPluginSourceContents source_contents =
                        current_source.Properties.Get<ISourceContents> ("Nereid.SourceContents") as LiveRadioPluginSourceContents;
                    if (source_contents != null)
                        source_contents.InitRefresh ();

                    return;
                }
            }
        }

        /// <summary>
        /// Adds the currently selected item(s) of the active source to the internet radio source
        /// as new stations. Any session data (as in live365 with activated user login) will previously
        /// be cleared.
        /// </summary>
        /// <param name="o">
        /// A <see cref="System.Object"/> -- not used
        /// </param>
        /// <param name="e">
        /// A <see cref="EventArgs"/> -- not used
        /// </param>
        protected void OnAddToInternetRadio (object o, EventArgs e)
        {
            PrimarySource internet_radio_source = GetInternetRadioSource ();
            PrimarySource current_source = ServiceManager.SourceManager.ActiveSource as PrimarySource;
            if (current_source == null) {
                Log.Debug ("[LiveRadioSource]<OnAddToInternetRadio> ActiveSource not Primary");
                return;
            }
            if (internet_radio_source == null) {
                Log.Debug ("[LiveRadioSource]<OnAddToInternetRadio> Internet Radio not found");
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
                if (plugin.PluginSource != null && plugin.PluginSource.Equals (current_source))
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
        }

        public override bool AcceptsInputFromSource (Source source)
        {
            return false;
        }

        public override bool CanDeleteTracks {
            get { return false; }
        }

        /// <summary>
        /// We must set this to false as we don't want a hotkey pressed in the query field to activate search
        /// </summary>
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

        /// <summary>
        /// We set this to false to prohibit editing the track properties, as they are pulled from a live catalog.
        /// </summary>
        public override bool HasEditableTrackProperties {
            get { return false; }
        }

        /// <summary>
        /// Gets the Internet Radio Source object
        /// IDEA: Call this method once upon initialization and store the object. If it is not found, remove
        /// AddToInternetRadioAction
        /// </summary>
        /// <returns>
        /// A <see cref="PrimarySource"/> -- the Internet Radio source, or throws an exception, if it cannot be found.
        /// </returns>
        protected PrimarySource GetInternetRadioSource ()
        {
            foreach (Source source in Banshee.ServiceStack.ServiceManager.SourceManager.Sources) {

                if (source.UniqueId.Equals ("InternetRadioSource-internet-radio")) {
                    return (PrimarySource)source;
                }
            }

            throw new InternetRadioExtensionNotFoundException ();
        }

        /// <summary>
        /// Sets the EnablePluginsEntry schema entry correctly from the internal object data
        /// </summary>
        private void SetEnabledPluginsEntry ()
        {
            if (EnabledPlugins.Count > 0)
            {
                StringBuilder sb = new StringBuilder (EnabledPlugins[0]);
                int i = 1;
                while (EnabledPlugins.Count > i)
                {
                    sb.Append (",");
                    sb.Append (EnabledPlugins[i]);
                    i++;
                }
                LiveRadioSource.EnabledPluginsEntry.Set (sb.ToString ());
            } else {
                LiveRadioSource.EnabledPluginsEntry.Set (String.Empty);
            }
        }

        /// <summary>
        /// List of enabled plugins. If updated, newly enabled plugins get a source added
        /// and newly disabled plugin will have their source removed
        /// </summary>
        public List<string> EnabledPlugins
        {
            get { return enabled_plugins; }
            set
            {
                foreach (ILiveRadioPlugin plugin in plugins)
                {
                    if (enabled_plugins.Contains (plugin.Name)
                        && !value.Contains (plugin.Name))
                        this.RemovePlugin (plugin);
                    if (!enabled_plugins.Contains (plugin.Name)
                        && value.Contains (plugin.Name))
                        this.AddPlugin (plugin);
                }
                enabled_plugins = value;
            }
        }

        public static readonly SchemaEntry<string> EnabledPluginsEntry = new SchemaEntry<string> (
        "plugins.liveradio" , "enbaled_plugins", "", "List of enabled Plugins", "List of enabled Plugins");

    }

    public class InternetRadioExtensionNotFoundException : Exception
    {
    }
}
