
//
// LiveRadioPluginSourceContents.cs
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

using Gtk;
using ScrolledWindow = Gtk.ScrolledWindow;

using Mono.Addins;

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

namespace Banshee.LiveRadio
{

    /// <summary>
    /// The source contents for a plugin source. It creates a view with a genre choose box, a query field
    /// and the view for the retrieved station tracks.
    /// The source contents is also the connector between plugin and source, as it handles just about all
    /// refresh/notify events coming from either side.
    /// </summary>
    public class LiveRadioPluginSourceContents : VBox, ISourceContents, ITrackModelSourceContents
    {
        private TrackListView track_view;
        private Gtk.ScrolledWindow main_scrolled_window;
        protected ISource source;

        private Paned container;
        private Widget browser_container;
        private InterfaceActionService action_service;
        private ActionGroup browser_view_actions;

        private ILiveRadioPlugin plugin;
        private LiveRadioFilterView filter_box;

        private static string menu_xml = @"
            <ui>
              <menubar name=""MainMenu"">
                <menu name=""ViewMenu"" action=""ViewMenuAction"">
                  <placeholder name=""BrowserViews"">
                    <menuitem name=""BrowserVisible"" action=""BrowserVisibleAction"" />
                    <separator />
                    <menuitem name=""BrowserTop"" action=""BrowserTopAction"" />
                    <menuitem name=""BrowserLeft"" action=""BrowserLeftAction"" />
                    <separator />
                  </placeholder>
                </menu>
              </menubar>
            </ui>
        ";

        /// <summary>
        /// Constructor -- creates the source contents for a plugin and sets up the event handlers for the view
        /// and the plugin refresh events.
        /// </summary>
        /// <param name="plugin">
        /// A <see cref="ILiveRadioPlugin"/> -- the plugin to set up the source contents for.
        /// </param>
        public LiveRadioPluginSourceContents (ILiveRadioPlugin plugin)
        {
            base.Name = plugin.Name;
            this.plugin = plugin;

            InitializeViews ();

            string position = ForcePosition == null ? BrowserPosition.Get () : ForcePosition;
            if (position == "top") {
                LayoutTop ();
            } else {
                LayoutLeft ();
            }

            plugin.GenreListLoaded += OnPluginGenreListLoaded;
            plugin.RequestResultRetrieved += OnPluginRequestResultRetrieved;

            if (ForcePosition != null) {
                return;
            }

            if (ServiceManager.Contains ("InterfaceActionService")) {
                action_service = ServiceManager.Get<InterfaceActionService> ();

                if (action_service.FindActionGroup ("BrowserView") == null) {
                    browser_view_actions = new ActionGroup ("BrowserView");

                    browser_view_actions.Add (new RadioActionEntry [] {
                        new RadioActionEntry ("BrowserLeftAction", null,
                            AddinManager.CurrentLocalizer.GetString ("Browser on Left"), null,
                            AddinManager.CurrentLocalizer.GetString ("Show the artist/album browser to the left of the track list"), 0),

                        new RadioActionEntry ("BrowserTopAction", null,
                            AddinManager.CurrentLocalizer.GetString ("Browser on Top"), null,
                            AddinManager.CurrentLocalizer.GetString ("Show the artist/album browser above the track list"), 1),
                    }, position == "top" ? 1 : 0, null);

                    browser_view_actions.Add (new ToggleActionEntry [] {
                        new ToggleActionEntry ("BrowserVisibleAction", null,
                            AddinManager.CurrentLocalizer.GetString ("Show Browser"), "<control>B",
                            AddinManager.CurrentLocalizer.GetString ("Show or hide the artist/album browser"),
                            null, BrowserVisible.Get ())
                    });


                    action_service.AddActionGroup (browser_view_actions);
                    action_service.UIManager.AddUiFromString (menu_xml);
                }

                (action_service.FindAction ("BrowserView.BrowserLeftAction") as RadioAction).Changed += OnViewModeChanged;
                (action_service.FindAction ("BrowserView.BrowserTopAction") as RadioAction).Changed += OnViewModeChanged;
                action_service.FindAction ("BrowserView.BrowserVisibleAction").Activated += OnToggleBrowser;
            }

            ServiceManager.SourceManager.ActiveSourceChanged += delegate {
                ThreadAssist.ProxyToMain (delegate {
                    browser_container.Visible = ActiveSourceCanHasBrowser ? BrowserVisible.Get () : false;
                });
            };

            NoShowAll = true;

        }

        /// <summary>
        /// Initiate a refesh of the contents: clear the genre list, add a fake "loading" entry and
        /// prohibit interaction with the elements
        /// </summary>
        public void InitRefresh ()
        {
            main_scrolled_window.Sensitive = false;
            List<Genre> fakeresult = new List<Genre> ();
            fakeresult.Add (new Genre(AddinManager.CurrentLocalizer.GetString("Loading...")));
            filter_box.UpdateGenres (fakeresult);
            filter_box.Sensitive = false;
        }

        /// <summary>
        /// Handles when a new result for a previous query request is received from the corresponding plugin. Transfers the
        /// received result to the plugin's source, and if there are none, set up a fake entry.
        /// </summary>
        /// <param name="sender">
        /// A <see cref="System.Object"/> -- The plugin that sent the results. Implements ILiveRadioPlugin.
        /// </param>
        /// <param name="request">
        /// A <see cref="System.String"/> -- The requested query string, either a genre name or the freetext (see request_type).
        /// </param>
        /// <param name="request_type">
        /// A <see cref="LiveRadioRequestType"/> -- The type of the request.
        /// </param>
        /// <param name="result">
        /// A <see cref="List<DatabaseTrackInfo>"/> -- A list of DatabaseTrackInfo objects that fulfil the query.
        /// </param>
        void OnPluginRequestResultRetrieved (object sender,
                                             string request,
                                             LiveRadioRequestType request_type,
                                             List<DatabaseTrackInfo> result)
        {
            if ((request_type == LiveRadioRequestType.ByFreetext)
                || (filter_box.GetSelectedGenre ().Name.Equals (request) && request_type == LiveRadioRequestType.ByGenre))
            {
                if (result == null)
                {
                    SetFakeTrack (AddinManager.CurrentLocalizer.GetString("Error... Please Reload"));
                } else if (result.Count > 0) {
                    plugin.PluginSource.SetStations (result);
                    main_scrolled_window.Sensitive = true;
                } else {
                    string message = (request_type == LiveRadioRequestType.ByGenre ? "Error... Please Reload" : "No Results");
                    SetFakeTrack (AddinManager.CurrentLocalizer.GetString(message));
                }
            }
        }

        /// <summary>
        /// Adds a fake track to the source and disables interaction with the track view.
        /// </summary>
        /// <param name="info">
        /// A <see cref="System.String"/> -- The information to display will be set as the fake track title and artist.
        /// </param>
        protected void SetFakeTrack (string info)
        {
            List<DatabaseTrackInfo> fakeresult = new List<DatabaseTrackInfo> ();
            DatabaseTrackInfo faketrack = new DatabaseTrackInfo ();
            faketrack.TrackTitle = info;
            faketrack.ArtistName = info;
            faketrack.AlbumArtist = info;
            faketrack.Uri = new SafeUri ("http://test.com/test.pls");

            fakeresult.Add (faketrack);
            plugin.PluginSource.SetStations (fakeresult);
            main_scrolled_window.Sensitive = false;
        }

        /// <summary>
        /// Handles when a genre list has been retrieved by the plugin. Fills the genre choose box with the results or
        /// adds a info message in case of an empty result and disables interaction with the control.
        /// </summary>
        /// <param name="sender">
        /// A <see cref="System.Object"/>
        /// </param>
        /// <param name="genres">
        /// A <see cref="List<Genre>"/>
        /// </param>
        void OnPluginGenreListLoaded (object sender, List<Genre> genres)
        {
            Hyena.ThreadAssist.ProxyToMain (delegate {
                if (genres.Count > 0) {
                    filter_box.UpdateGenres (genres);
                    filter_box.Sensitive = true;
                } else {
                    List<Genre> fakeresult = new List<Genre> ();
                    fakeresult.Add (new Genre(AddinManager.CurrentLocalizer.GetString("Error... Please Reload")));
                    filter_box.UpdateGenres (fakeresult);
                    filter_box.Sensitive = false;
                }
            });
        }

        /// <summary>
        /// Initialize the main track view
        /// </summary>
        protected void InitializeViews ()
        {
            SetupMainView (track_view = new TrackListView ());
        }


        /// <summary>
        /// Setup the main track view and disable interaction
        /// </summary>
        /// <param name="main_view">
        /// A <see cref="ListView<T>"/> -- the main track view to set up
        /// </param>
        protected void SetupMainView<T> (ListView<T> main_view)
        {
            main_scrolled_window = SetupView (main_view);
            main_scrolled_window.Sensitive = false;
        }

        /// <summary>
        /// Capsules a widget in a scrolled window to add scrolling.
        /// </summary>
        /// <param name="view">
        /// A <see cref="Widget"/> -- the view to capsule
        /// </param>
        /// <returns>
        /// A <see cref="ScrolledWindow"/> -- the scrolled window containing the capsuled view
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

        /// <summary>
        /// Unparent the views' scrolled window parents so they can be re-packed in
        /// a new layout. The main container gets destroyed since it will be recreated.
        /// </summary>
        private void Reset ()
        {
            if (container != null && main_scrolled_window != null) {
                container.Remove (main_scrolled_window);
            }

            if (container != null) {
                Remove (container);
            }
        }

        /// <summary>
        /// Make a layout with the filter views on the left hand side
        /// </summary>
        private void LayoutLeft ()
        {
            Layout (false);
        }

        /// <summary>
        /// Make a layout with the filter views on top
        /// </summary>
        private void LayoutTop ()
        {
            Layout (true);
        }

        /// <summary>
        /// Create the contents of the source contents in the desired layout.
        /// </summary>
        /// <param name="top">
        /// A <see cref="System.Boolean"/> -- whether to create a LayoutTop (true) or a LayoutLeft (false)
        /// </param>
        private void Layout (bool top)
        {
            Reset ();

            container = GetPane (!top);
            filter_box = new LiveRadioFilterView ();
            filter_box.Sensitive = false;
            filter_box.GenreSelected += OnViewGenreSelected;
            filter_box.GenreActivated += OnViewGenreSelected;
            filter_box.QuerySent += OnViewQuerySent;

            container.Pack1 (filter_box, false, false);
            container.Pack2 (main_scrolled_window, true, false);
            browser_container = filter_box;

            container.Position = top ? 175 : 275;
            ShowPack ();
        }

        /// <summary>
        /// Handles when the user sends a query through the Entry box of the UI and initiates request execution
        /// in the plugin object. The track view is reset with a fake entry.
        /// </summary>
        /// <param name="sender">
        /// A <see cref="System.Object"/> -- the sender of the event
        /// </param>
        /// <param name="query">
        /// A <see cref="System.String"/> -- the query string the user entered
        /// </param>
        void OnViewQuerySent (object sender, string query)
        {
            SetFakeTrack (AddinManager.CurrentLocalizer.GetString("Loading..."));
            plugin.ExecuteRequest (LiveRadioRequestType.ByFreetext, query);
        }

        /// <summary>
        /// Handles when the user activates a genre in the genre choose box and initiates request execution
        /// in the plugin object. The track view is reset with a fake entry.
        /// </summary>
        /// <param name="sender">
        /// A <see cref="System.Object"/> -- the sender of the event
        /// </param>
        /// <param name="genre">
        /// A <see cref="Genre"/> -- the genre the user selected
        /// </param>
        void OnViewGenreSelected (object sender, Genre genre)
        {
            SetFakeTrack (AddinManager.CurrentLocalizer.GetString("Loading..."));
            plugin.ExecuteRequest (LiveRadioRequestType.ByGenre, genre.Name);
        }
        /// <summary>
        /// Draw the source contents
        /// </summary>
        private void ShowPack ()
        {
            PackStart (container, true, true, 0);
            VBox instruct = new VBox ();
            //instruct.ExposeEvent += (o, a) => {
            //    using (Cairo.Context cr = Gdk.CairoHelper.Create (instruct.GdkWindow)) {
            //        double radius = 10;
            //        int x = a.Event.Area.X;
            //        int y = a.Event.Area.Y;
            //        int width = a.Event.Area.Width;
            //        int height = a.Event.Area.Height;

            //        cr.MoveTo (x + radius, y);
            //        cr.Arc (x + width - radius, y + radius, radius, Math.PI * 1.5, Math.PI * 2);
            //        cr.Arc (x + width - radius, y + height - radius, radius, 0, Math.PI * .5);
            //        cr.Arc (x + radius, y + height - radius, radius, Math.PI * .5, Math.PI);
            //        cr.Arc (x + radius, y + radius, radius, Math.PI, Math.PI * 1.5);
            //        cr.Color = new Cairo.Color (.5, .5, .5, 1);
            //        cr.Stroke ();
            //    }
                // not yet sure which is the better code.
                //                Gdk.Window win = a.Event.Window;
                //                Gdk.Rectangle area = a.Event.Area;
                //
                //                win.DrawRectangle (Style.BaseGC (StateType.Active), false, area);
                //
                //                a.RetVal = true;
            //};
            Label help_label = new Label (
                  AddinManager.CurrentLocalizer.GetString ("Click a genre to load/refresh entries or type a query. Use the refresh button to refresh genres."));
            instruct.PackStart(help_label, false, true, 10);
            PackStart (instruct, false, true, 10);
            NoShowAll = false;
            ShowAll ();
            NoShowAll = true;
            browser_container.Visible = ForcePosition != null || BrowserVisible.Get ();
        }

        /// <summary>
        /// Helper function to make a new Paned with the correct layout
        /// </summary>
        /// <param name="hpane">
        /// A <see cref="System.Boolean"/>
        /// </param>
        /// <returns>
        /// A <see cref="Paned"/>
        /// </returns>
        private Paned GetPane (bool hpane)
        {
            if (hpane)
                return new HPaned ();
            else
                return new VPaned ();
        }

        /// <summary>
        /// Handles when the user selects a different view mode.
        /// </summary>
        /// <param name="o">
        /// A <see cref="System.Object"/>
        /// </param>
        /// <param name="args">
        /// A <see cref="ChangedArgs"/>
        /// </param>
        private void OnViewModeChanged (object o, ChangedArgs args)
        {
            if (args.Current.Value == 0) {
                LayoutLeft ();
                BrowserPosition.Set ("left");
            } else {
                LayoutTop ();
                BrowserPosition.Set ("top");
            }
        }

        /// <summary>
        /// Handles when browser visibility is toggled
        /// </summary>
        /// <param name="o">
        /// A <see cref="System.Object"/>
        /// </param>
        /// <param name="args">
        /// A <see cref="EventArgs"/>
        /// </param>
        private void OnToggleBrowser (object o, EventArgs args)
        {
            ToggleAction action = (ToggleAction)o;

            browser_container.Visible = action.Active && ActiveSourceCanHasBrowser;
            BrowserVisible.Set (action.Active);

        }

        protected bool ActiveSourceCanHasBrowser {
            get { return true; }
        }

        protected string ForcePosition {
            get { return "bottom"; }
        }

        #region Implement ISourceContents

        /// <summary>
        /// Sets the source and the track model for the source contents
        /// </summary>
        /// <param name="source">
        /// A <see cref="ISource"/>
        /// </param>
        /// <returns>
        /// A <see cref="System.Boolean"/>
        /// </returns>
        public bool SetSource (ISource source)
        {
            DatabaseSource track_source = source as DatabaseSource;
            if (track_source == null) {
                return false;
            }

            this.source = source;

            track_view.SetModel (track_source.TrackModel);

            return true;
        }

        /// <summary>
        /// Sets the source and track model references to null
        /// </summary>
        public void ResetSource ()
        {
            this.source = null;
            track_view.SetModel (null);
        }

        /// <summary>
        /// The ISource source of this source contents
        /// </summary>
        public ISource Source {
            get { return source; }
        }

        /// <summary>
        /// The Widget of the source contents
        /// </summary>
        public Widget Widget {
            get { return this; }
        }

        #endregion

        #region ITrackModelSourceContents implementation

        /// <summary>
        /// returns the track view of this source contents
        /// </summary>
        public IListView<TrackInfo> TrackView {
            get { return track_view; }
        }

        #endregion

        public static readonly SchemaEntry<bool> BrowserVisible = new SchemaEntry<bool> (
            "browser", "visible",
            true,
            "Artist/Album Browser Visibility",
            "Whether or not to show the Artist/Album browser"
        );

        public static readonly SchemaEntry<string> BrowserPosition = new SchemaEntry<string> (
            "browser", "position",
            "left",
            "Artist/Album Browser Position",
            "The position of the Artist/Album browser; either 'top' or 'left'"
        );
    }
}
