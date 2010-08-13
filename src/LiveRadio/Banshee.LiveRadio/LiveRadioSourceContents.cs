//
// LiveRadioSourceContents.cs
//
// Author:
//   Frank Ziegler <funtastix@googlemail.com>
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2008 Novell, Inc.
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

using Gtk;
using ScrolledWindow = Gtk.ScrolledWindow;

using Hyena;
using Hyena.Widgets;
using Hyena.Data.Gui;

using Banshee.Configuration;
using Banshee.Sources;
using Banshee.Sources.Gui;
using Banshee.ServiceStack;
using Banshee.Collection;
using Banshee.Collection.Database;
using Banshee.Collection.Gui;
using Banshee.Gui;
using Banshee.LiveRadio.Plugins;
using Hyena.Data;

namespace Banshee.LiveRadio
{

    /// <summary>
    /// A Widget containing ILiveRadioPlugin overview and control options as well as a view on statistics messages
    /// from ILiveRadioPlugin objects. The SourceContents will hook up to events coming from registered plugins
    /// </summary>
    public class LiveRadioSourceContents : VPaned, ISourceContents
    {
        private ListView<ILiveRadioPlugin> plugin_view;
        private ListView<LiveRadioStatistic> statistic_view;
        private List<LiveRadioStatistic> statistics;
        Button enable_button = new Button (Stock.Add);
        Button disable_button = new Button (Stock.Remove);
        Button configure_button = new Button (Stock.Preferences);

        ISource source;

        /// <summary>
        /// Constructor -- connects all plugin events
        /// </summary>
        /// <param name="plugins">
        /// A <see cref="List<ILiveRadioPlugin>"/> -- the list of plugins
        /// </param>
        public LiveRadioSourceContents (List<ILiveRadioPlugin> plugins)
        {
            statistics = new List<LiveRadioStatistic> ();
            foreach (ILiveRadioPlugin plugin in plugins)
            {
                ConnectPluginEvents (plugin);
            }
            CreateLayout (plugins);
        }

        /// <summary>
        /// Connects class functions to plugin events
        /// </summary>
        /// <param name="plugin">
        /// A <see cref="ILiveRadioPlugin"/> the events of which will be connected
        /// </param>
        public void ConnectPluginEvents (ILiveRadioPlugin plugin)
        {
            plugin.ErrorReturned += OnPluginErrorReturned;
            plugin.GenreListLoaded += OnPluginGenreListLoaded;
            plugin.RequestResultRetrieved += OnPluginRequestResultRetrieved;
        }

        /// <summary>
        /// Disconnects class functions from plugin events
        /// </summary>
        /// <param name="plugin">
        /// A <see cref="ILiveRadioPlugin"/> the events of which will be disconnected
        /// </param>
        public void DisconnectPluginEvents (ILiveRadioPlugin plugin)
        {
            plugin.ErrorReturned -= OnPluginErrorReturned;
            plugin.GenreListLoaded -= OnPluginGenreListLoaded;
            plugin.RequestResultRetrieved -= OnPluginRequestResultRetrieved;
        }

        /// <summary>
        /// Adds a statistic or updates an existing one if a matching statistic is found
        /// </summary>
        /// <param name="type">
        /// A <see cref="LiveRadioStatisticType"/> that defines the type of the statistic
        /// </param>
        /// <param name="plugin_name">
        /// A <see cref="System.String"/> -- the name of the originating plugin
        /// </param>
        /// <param name="short_message">
        /// A <see cref="System.String"/> with the short info for the statistic
        /// </param>
        /// <param name="long_message">
        /// A <see cref="System.String"/> with the description of the statistic
        /// </param>
        /// <param name="count">
        /// A <see cref="System.Int32"/> -- the number of elements associated with the message
        /// </param>
        private void AddStatistic(LiveRadioStatisticType type,
                                  string plugin_name,
                                  string short_message,
                                  string long_message,
                                  int count)
        {
            LiveRadioStatistic stat = statistics.Find (delegate (LiveRadioStatistic statistic) {
                                           return MessageEqual (statistic,
                                                                type,
                                                                plugin_name,
                                                                short_message,
                                                                long_message);
                                       }) ?? new LiveRadioStatistic (type,
                                                                     plugin_name,
                                                                     short_message,
                                                                     long_message);
            stat.AddCount (count);
            if (!statistics.Contains (stat)) statistics.Add (stat);
            LiveRadioStatisticListModel model = statistic_view.Model as LiveRadioStatisticListModel;
            model.SetList(statistics);
        }

        /// <summary>
        /// Event Handler for the event that a plugin has retreived a result list for a query
        /// </summary>
        /// <param name="sender">
        /// A <see cref="System.Object"/> -- the plugin that has retrieved the results
        /// </param>
        /// <param name="request">
        /// A <see cref="System.String"/> -- the original request string, either freetext or the genre name
        /// </param>
        /// <param name="request_type">
        /// A <see cref="LiveRadioRequestType"/> -- the original request type
        /// </param>
        /// <param name="result">
        /// A <see cref="List<DatabaseTrackInfo>"/> -- the results list
        /// </param>
        void OnPluginRequestResultRetrieved (object sender,
                                             string request,
                                             LiveRadioRequestType request_type,
                                             List<DatabaseTrackInfo> result)
        {
            if (result != null)
            {
                ILiveRadioPlugin plugin = sender as ILiveRadioPlugin;
                AddStatistic (LiveRadioStatisticType.Message,
                              plugin.Name,
                              AddinManager.CurrentLocalizer.GetString ("Requested Results Returned"),
                              AddinManager.CurrentLocalizer.GetString ("The plugin has returned a list of results for a genre or freetext query"),
                              result.Count);
            }
        }

        /// <summary>
        /// Event Handler for the event that a plugin has retrieved a list of genres
        /// </summary>
        /// <param name="sender">
        /// A <see cref="System.Object"/> -- the plugin that has retrieved the genre list
        /// </param>
        /// <param name="genres">
        /// A <see cref="List<Genre>"/> -- the list of genres that has been retrieved
        /// </param>
        void OnPluginGenreListLoaded (object sender, List<Genre> genres)
        {
            ILiveRadioPlugin plugin = sender as ILiveRadioPlugin;
            AddStatistic (LiveRadioStatisticType.Message,
                          plugin.Name,
                          AddinManager.CurrentLocalizer.GetString ("Genre List Retrieved"),
                          AddinManager.CurrentLocalizer.GetString ("The plugin has returned the list of genres"),
                          genres.Count);
        }

        /// <summary>
        /// Event Handler for the event that a plugin has returned an error
        /// </summary>
        /// <param name="plugin">
        /// A <see cref="ILiveRadioPlugin"/> -- the plugin that has encountered the error
        /// </param>
        /// <param name="error">
        /// A <see cref="LiveRadioPluginError"/> containing information about the error
        /// </param>
        void OnPluginErrorReturned (ILiveRadioPlugin plugin, LiveRadioPluginError error)
        {
            AddStatistic (LiveRadioStatisticType.Error,
                          plugin.Name,
                          AddinManager.CurrentLocalizer.GetString (error.Message),
                          AddinManager.CurrentLocalizer.GetString (error.LongMessage),
                          1);
        }

        /// <summary>
        /// Tests if the messages and types of a LiveRadioStatistic matches with the given parameters
        /// </summary>
        /// <param name="statistic">
        /// A <see cref="LiveRadioStatistic"/> to test
        /// </param>
        /// <param name="type">
        /// A <see cref="LiveRadioStatisticType"/> type to match
        /// </param>
        /// <param name="origin">
        /// A <see cref="System.String"/> containing the plugin name to match
        /// </param>
        /// <param name="short_description">
        /// A <see cref="System.String"/> containing the short info to match
        /// </param>
        /// <param name="long_description">
        /// A <see cref="System.String"/> containing the long info to match
        /// </param>
        /// <returns>
        /// A <see cref="System.Boolean"/> true, if all parameters match the properties of the tested statistic, false otherwise
        /// </returns>
        private bool MessageEqual (LiveRadioStatistic statistic,
                                   LiveRadioStatisticType type,
                                   string origin,
                                   string short_description,
                                   string long_description)
        {
            if (statistic.ShortDescription.Equals (short_description)
                && statistic.LongDescription.Equals (long_description)
                && statistic.Origin.Equals (origin)
                && statistic.Type.Equals (type))
                return true;
            return false;
        }

        /// <summary>
        /// creates the layout of the widget
        /// </summary>
        /// <param name="plugins">
        /// A List<ILiveRadioPlugin> -- the plugin list
        /// </param>
        protected void CreateLayout (List<ILiveRadioPlugin> plugins)
        {
            plugin_view = new ListView<ILiveRadioPlugin> ();
            Column col_name = new Column (new ColumnDescription ("Name", AddinManager.CurrentLocalizer.GetString ("Plugin"), 100));
            Column col_version = new Column (new ColumnDescription ("Version", AddinManager.CurrentLocalizer.GetString ("Version"), 100));
            Column col_enabled = new Column (new ColumnDescription ("IsEnabled", AddinManager.CurrentLocalizer.GetString ("Enabled"), 100));
            plugin_view.ColumnController = new ColumnController ();
            plugin_view.ColumnController.Add (col_name);
            plugin_view.ColumnController.Add (col_version);
            plugin_view.ColumnController.Add (col_enabled);
            plugin_view.SetModel (new LiveRadioPluginListModel (plugins));
            plugin_view.Model.Selection.FocusChanged += OnPluginViewModelSelectionFocusChanged;;

            List<LiveRadioStatistic> stats = new List<LiveRadioStatistic> ();

            statistic_view = new ListView<LiveRadioStatistic> ();
            Column col_type = new Column (new ColumnDescription ("TypeName", AddinManager.CurrentLocalizer.GetString ("Type"), 100));
            Column col_origin = new Column (new ColumnDescription ("Origin", AddinManager.CurrentLocalizer.GetString ("Origin"), 100));
            Column col_short = new Column (new ColumnDescription ("ShortDescription", AddinManager.CurrentLocalizer.GetString ("Short Info"), 100));
            Column col_long = new Column (new ColumnDescription ("LongDescription", AddinManager.CurrentLocalizer.GetString ("Long Info"), 100));
            Column col_count = new Column (new ColumnDescription ("Count", AddinManager.CurrentLocalizer.GetString ("Count"), 100));
            Column col_average = new Column (new ColumnDescription ("Average", AddinManager.CurrentLocalizer.GetString ("Average"), 100));
            Column col_updates = new Column (new ColumnDescription ("Updates", AddinManager.CurrentLocalizer.GetString ("Updates"), 100));
            statistic_view.ColumnController = new ColumnController ();
            statistic_view.ColumnController.Add (col_type);
            statistic_view.ColumnController.Add (col_origin);
            statistic_view.ColumnController.Add (col_short);
            statistic_view.ColumnController.Add (col_long);
            statistic_view.ColumnController.Add (col_count);
            statistic_view.ColumnController.Add (col_average);
            statistic_view.ColumnController.Add (col_updates);
            statistic_view.SetModel (new LiveRadioStatisticListModel (stats));

            enable_button.TooltipText = AddinManager.CurrentLocalizer.GetString ("Enable Plugin");
            enable_button.Label = AddinManager.CurrentLocalizer.GetString ("Enable Plugin");
            enable_button.Sensitive = false;
            disable_button.TooltipText = AddinManager.CurrentLocalizer.GetString ("Disable Plugin");
            disable_button.Label = AddinManager.CurrentLocalizer.GetString ("Disable Plugin");
            disable_button.Sensitive = false;
            configure_button.TooltipText = AddinManager.CurrentLocalizer.GetString ("Configure Plugin");
            configure_button.Label = AddinManager.CurrentLocalizer.GetString ("Configure Plugin");
            configure_button.Sensitive = false;

            enable_button.Clicked += new EventHandler(OnEnableButtonClicked);
            disable_button.Clicked += new EventHandler (OnDisableButtonClicked);
            configure_button.Clicked += new EventHandler (OnConfigureButtonClicked);

            HBox button_box = new HBox ();
            button_box.PackStart (configure_button, false, false, 10);
            button_box.PackEnd (enable_button, false, false, 10);
            button_box.PackEnd (disable_button, false, false, 10);

            Label stat_label = new Label ();
            stat_label.UseMarkup = true;
            stat_label.Markup = "<b>" + AddinManager.CurrentLocalizer.GetString ("Plugin Statistics") + "</b>";

            Position = 275;

            VBox pack1 = new VBox ();
            VBox pack2 = new VBox ();


            pack1.PackStart (SetupView (plugin_view), true, true, 10);
            pack1.PackStart (button_box, false, true, 10);
            pack2.PackStart (stat_label, false, true, 20);
            pack2.PackStart (SetupView (statistic_view), true, true, 10);

            Pack1 (pack1, true, true);
            Pack2 (pack2, true, true);

            ShowAll ();
        }

        /// <summary>
        /// Activated when the user clicks the "Enable" button. Enables the selected,
        /// previously inactive plugin
        /// </summary>
        /// <param name="sender">
        /// A <see cref="System.Object"/> -- not used
        /// </param>
        /// <param name="e">
        /// A <see cref="EventArgs"/> -- not used
        /// </param>
        void OnEnableButtonClicked (object sender, EventArgs e)
        {
            LiveRadioPluginListModel model = plugin_view.Model as LiveRadioPluginListModel;
            ILiveRadioPlugin plugin = model[plugin_view.Model.Selection.FocusedIndex];
            if (plugin.Enabled) return;
            LiveRadioSource source = this.Source as LiveRadioSource;
            if (source == null) return;
            source.AddPlugin (plugin);
            enable_button.Sensitive = false;
            disable_button.Sensitive = true;
            plugin_view.GrabFocus ();
        }

        /// <summary>
        /// Activated when the user clicks the "Disable" button. Disables the selected,
        /// previously active plugin
        /// </summary>
        /// <param name="sender">
        /// A <see cref="System.Object"/> -- not used
        /// </param>
        /// <param name="e">
        /// A <see cref="EventArgs"/> -- not used
        /// </param>
        void OnDisableButtonClicked (object sender, EventArgs e)
        {
            LiveRadioPluginListModel model = plugin_view.Model as LiveRadioPluginListModel;
            ILiveRadioPlugin plugin = model[plugin_view.Model.Selection.FocusedIndex];
            if (!plugin.Enabled) return;
            LiveRadioSource source = plugin.PluginSource.Parent as LiveRadioSource;
            if (source == null) return;
            source.RemovePlugin (plugin);
            enable_button.Sensitive = true;
            disable_button.Sensitive = false;
            plugin_view.GrabFocus ();
        }

        /// <summary>
        /// Activated when the user clicks the "Configure" button. Opens a new dialog containing the configuration
        /// widget of the selected plugin
        /// </summary>
        /// <param name="sender">
        /// A <see cref="System.Object"/> -- not used
        /// </param>
        /// <param name="e">
        /// A <see cref="EventArgs"/> -- not used
        /// </param>
        void OnConfigureButtonClicked (object sender, EventArgs e)
        {
            LiveRadioPluginListModel model = plugin_view.Model as LiveRadioPluginListModel;
            ILiveRadioPlugin plugin = model[plugin_view.Model.Selection.FocusedIndex];

            Dialog dialog = new Dialog ();
            dialog.Title = String.Format ("LiveRadio Plugin {0} configuration", plugin.Name);
            dialog.IconName = "gtk-preferences";
            dialog.Resizable = false;
            dialog.BorderWidth = 6;
            dialog.HasSeparator = false;
            dialog.VBox.Spacing = 12;

            dialog.VBox.PackStart (plugin.ConfigurationWidget);

            Button save_button = new Button (Stock.Save);
            Button cancel_button = new Button (Stock.Cancel);

            dialog.AddActionWidget (cancel_button, 0);
            dialog.AddActionWidget (save_button, 0);

            cancel_button.Clicked += delegate { dialog.Destroy (); };
            save_button.Clicked += delegate {
                plugin.SaveConfiguration ();
                dialog.Destroy ();
            };

            dialog.ShowAll ();
        }

        /// <summary>
        /// Activated when the focus in the plugin list changes. Sets the enable and disable button sensitivity
        /// </summary>
        /// <param name="sender">
        /// A <see cref="System.Object"/> -- not used
        /// </param>
        /// <param name="e">
        /// A <see cref="EventArgs"/> -- not used
        /// </param>
        void OnPluginViewModelSelectionFocusChanged (object sender, EventArgs e)
        {
            configure_button.Sensitive = true;
            LiveRadioPluginListModel model = plugin_view.Model as LiveRadioPluginListModel;
            if (model[plugin_view.Model.Selection.FocusedIndex].Enabled)
            {
                enable_button.Sensitive = false;
                disable_button.Sensitive = true;
            } else {
                enable_button.Sensitive = true;
                disable_button.Sensitive = false;
            }
        }

        /// <summary>
        /// Capsules a Gtk Widget in a scolled window to add scrolling
        /// </summary>
        /// <param name="view">
        /// A <see cref="Widget"/> -- the widget to be capsuled
        /// </param>
        /// <returns>
        /// A <see cref="ScrolledWindow"/> -- the scrolled window containing the capsuled widget
        /// </returns>
        private ScrolledWindow SetupView (Widget view)
        {
            ScrolledWindow window = null;

            if (ApplicationContext.CommandLine.Contains ("smooth-scroll")) {
                window = new SmoothScrolledWindow ();
            } else {
                window = new ScrolledWindow ();
            }

            window.Add (view);
            window.HscrollbarPolicy = PolicyType.Automatic;
            window.VscrollbarPolicy = PolicyType.Automatic;

            return window;
        }


        public bool SetSource (ISource source)
        {
            this.source = source;
            return true;
        }

        public void ResetSource ()
        {
        }

        public Gtk.Widget Widget {
            get { return this; }
        }

        public ISource Source {
            get { return source; }
        }
    }

}