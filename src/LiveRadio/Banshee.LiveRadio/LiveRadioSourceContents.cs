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

using Mono.Unix;

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
    ///
    /// </summary>
    public class LiveRadioSourceContents : VBox, ISourceContents
    {
        //Gtk.Label label = new Gtk.Label ("LiveRadio! This view will be setup to show statistics...");
        private ListView<ILiveRadioPlugin> plugin_view;
        private ListView<LiveRadioStatistic> statistic_view;
        private List<LiveRadioStatistic> statistics;
        Button enable_button = new Button (Stock.Add);
        Button disable_button = new Button (Stock.Remove);
        Button configure_button = new Button (Stock.Preferences);

        ISource source;

        public LiveRadioSourceContents (List<ILiveRadioPlugin> plugins)
        {
            statistics = new List<LiveRadioStatistic> ();
            ConnectPluginEvents(plugins);
            CreateLayout (plugins);
        }

        void ConnectPluginEvents (List<ILiveRadioPlugin> plugins)
        {
            foreach (ILiveRadioPlugin plugin in plugins)
            {
                plugin.ErrorReturned += OnPluginErrorReturned;
                plugin.GenreListLoaded += OnPluginGenreListLoaded;
                plugin.RequestResultRetrieved += OnPluginRequestResultRetrieved;
            }
        }

        void OnPluginRequestResultRetrieved (object sender,
                                             string request,
                                             LiveRadioRequestType request_type,
                                             List<DatabaseTrackInfo> result)
        {
            string short_message = Catalog.GetString ("Requested Results Returned");
            string long_message = Catalog.GetString ("The plugin has returned a list of results for a genre or freetext query");
            LiveRadioStatistic stat = statistics.Find (delegate (LiveRadioStatistic statistic) {
                                           return MessageEqual (statistic,
                                                                short_message,
                                                                long_message);
                                       }) ?? new LiveRadioStatistic (short_message, long_message);
            stat.AddCount (result.Count);
            if (!statistics.Contains (stat)) statistics.Add (stat);
            LiveRadioStatisticListModel model = statistic_view.Model as LiveRadioStatisticListModel;
            model.SetList(statistics);
        }

        void OnPluginGenreListLoaded (object sender, List<Genre> genres)
        {

        }

        void OnPluginErrorReturned (ILiveRadioPlugin plugin, LiveRadioPluginError error)
        {
        }

        private bool MessageEqual (LiveRadioStatistic statistic, string name, string description)
        {
            if (statistic.Name.Equals (name) && statistic.Description.Equals (description))
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
            Column col_name = new Column (new ColumnDescription ("Name", Catalog.GetString ("Plugin"), 100));
            Column col_version = new Column (new ColumnDescription ("Version", Catalog.GetString ("Version"), 100));
            Column col_enabled = new Column (new ColumnDescription ("Enabled", Catalog.GetString ("Enabled"), 100));
            plugin_view.ColumnController = new ColumnController ();
            plugin_view.ColumnController.Add (col_name);
            plugin_view.ColumnController.Add (col_version);
            plugin_view.ColumnController.Add (col_enabled);
            plugin_view.SetModel (new LiveRadioPluginListModel (plugins));
            plugin_view.Model.Selection.FocusChanged += OnPluginViewModelSelectionFocusChanged;;

            List<LiveRadioStatistic> stats = new List<LiveRadioStatistic> ();

            statistic_view = new ListView<LiveRadioStatistic> ();
            Column col_sname = new Column (new ColumnDescription ("Name", Catalog.GetString ("Short Info"), 100));
            Column col_desc = new Column (new ColumnDescription ("Description", Catalog.GetString ("Long Info"), 100));
            Column col_count = new Column (new ColumnDescription ("Count", Catalog.GetString ("Count"), 100));
            Column col_average = new Column (new ColumnDescription ("Average", Catalog.GetString ("Average"), 100));
            Column col_updates = new Column (new ColumnDescription ("Updates", Catalog.GetString ("Updates"), 100));
            statistic_view.ColumnController = new ColumnController ();
            statistic_view.ColumnController.Add (col_sname);
            statistic_view.ColumnController.Add (col_desc);
            statistic_view.ColumnController.Add (col_count);
            statistic_view.ColumnController.Add (col_average);
            statistic_view.ColumnController.Add (col_updates);
            statistic_view.SetModel (new LiveRadioStatisticListModel (stats));
            statistic_view.Model.Selection.FocusChanged += OnStatisticViewModelSelectionFocusChanged;

            enable_button.TooltipText = Catalog.GetString ("Enable Plugin");
            enable_button.Label = Catalog.GetString ("Enable Plugin");
            enable_button.Sensitive = false;
            disable_button.TooltipText = Catalog.GetString ("Disable Plugin");
            disable_button.Label = Catalog.GetString ("Disable Plugin");
            disable_button.Sensitive = false;
            configure_button.TooltipText = Catalog.GetString ("Configure Plugin");
            configure_button.Label = Catalog.GetString ("Configure Plugin");
            configure_button.Sensitive = false;

            HBox button_box = new HBox ();
            button_box.PackStart (configure_button, false, false, 10);
            button_box.PackEnd (enable_button, false, false, 10);
            button_box.PackEnd (disable_button, false, false, 10);

            PackStart (SetupView (plugin_view), true, true, 10);
            PackStart (button_box, false, true, 10);
            PackStart (SetupView (statistic_view), true, true, 10);

            ShowAll ();
        }

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

        void OnStatisticViewModelSelectionFocusChanged (object sender, EventArgs e)
        {

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