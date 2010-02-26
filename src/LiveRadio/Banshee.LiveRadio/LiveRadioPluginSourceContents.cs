
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

namespace Banshee.LiveRadio
{
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

        public LiveRadioPluginSourceContents (ILiveRadioPlugin plugin)
        {
            Log.DebugFormat ("[LiveRadioPluginSourceContents\"{0}\"]<Constructor> START", plugin.GetName ());
            base.Name = plugin.GetName ();
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
                Log.DebugFormat ("[LiveRadioPluginSourceContents\"{0}\"]<Constructor> END", plugin.GetName ());
                return;
            }
            
            if (ServiceManager.Contains ("InterfaceActionService")) {
                action_service = ServiceManager.Get<InterfaceActionService> ();
                
                if (action_service.FindActionGroup ("BrowserView") == null) {
                    browser_view_actions = new ActionGroup ("BrowserView");
                    
                    browser_view_actions.Add (new RadioActionEntry [] {
                        new RadioActionEntry ("BrowserLeftAction", null,
                            Catalog.GetString ("Browser on Left"), null,
                            Catalog.GetString ("Show the artist/album browser to the left of the track list"), 0),

                        new RadioActionEntry ("BrowserTopAction", null,
                            Catalog.GetString ("Browser on Top"), null,
                            Catalog.GetString ("Show the artist/album browser above the track list"), 1),
                    }, position == "top" ? 1 : 0, null);

                    browser_view_actions.Add (new ToggleActionEntry [] {
                        new ToggleActionEntry ("BrowserVisibleAction", null,
                            Catalog.GetString ("Show Browser"), "<control>B",
                            Catalog.GetString ("Show or hide the artist/album browser"),
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
            
            Log.DebugFormat ("[LiveRadioPluginSourceContents\"{0}\"]<Constructor> END", plugin.GetName ());
            
        }

        void OnPluginRequestResultRetrieved (object sender,
                                             string request,
                                             LiveRadioRequestType request_type,
                                             List<DatabaseTrackInfo> result)
        {
            Hyena.Log.Debug ("[LiverRadioPluginSourceContenst]<OnPluginRequestResultRetrieved> handling result retrieved");
            if ((request_type == LiveRadioRequestType.ByFreetext)
                || (filter_box.GetSelectedGenre ().Equals (request) && request_type == LiveRadioRequestType.ByGenre))
            {
                if (result.Count > 0) {
                    plugin.GetLiveRadioPluginSource ().SetStations (result);
                    main_scrolled_window.Sensitive = true;
                } else {
                    List<DatabaseTrackInfo> fakeresult = new List<DatabaseTrackInfo> ();
                    DatabaseTrackInfo faketrack = new DatabaseTrackInfo ();
                    faketrack.TrackTitle = Catalog.GetString("Error... Please Reload");
                    faketrack.ArtistName = Catalog.GetString("Error... Please Reload");
                    faketrack.AlbumArtist = Catalog.GetString("Error... Please Reload");
                    faketrack.Uri = new Banshee.Base.SafeUri ("http://test.com/test.pls");

                    fakeresult.Add (faketrack);
                    plugin.GetLiveRadioPluginSource ().SetStations (fakeresult);
                    main_scrolled_window.Sensitive = false;
                }
            }
        }

        void OnPluginGenreListLoaded (object sender, List<string> genres)
        {
            Hyena.Log.Debug ("[LiverRadioPluginSourceContenst]<OnPluginGenreListLoaded> handling genrelistloaded");
            if (genres.Count > 0) {
                filter_box.UpdateGenres (genres);
                filter_box.Sensitive = true;
            } else {
                List<string> fakeresult = new List<string> ();
                fakeresult.Add (Catalog.GetString("Error... Please Reload"));
                filter_box.UpdateGenres (fakeresult);
                filter_box.Sensitive = false;
            }
        }

        protected void InitializeViews ()
        {
            SetupMainView (track_view = new TrackListView ());
        }

        protected void SetupMainView<T> (ListView<T> main_view)
        {
            main_scrolled_window = SetupView (main_view);
            main_scrolled_window.Sensitive = false;
        }

        private ScrolledWindow SetupView (Widget view)
        {
            ScrolledWindow window = null;
            
            //if (!Banshee.Base.ApplicationContext.CommandLine.Contains ("no-smooth-scroll")) {
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

        private void Reset ()
        {
            // Unparent the views' scrolled window parents so they can be re-packed in
            // a new layout. The main container gets destroyed since it will be recreated.
            
            if (container != null && main_scrolled_window != null) {
                container.Remove (main_scrolled_window);
            }
            
            if (container != null) {
                Remove (container);
            }
        }

        private void LayoutLeft ()
        {
            Layout (false);
        }

        private void LayoutTop ()
        {
            Layout (true);
        }

        private void Layout (bool top)
        {
            //Hyena.Log.Information ("ListBrowser LayoutLeft");
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

        void OnViewQuerySent (object sender, string query)
        {
            plugin.ExecuteRequest (LiveRadioRequestType.ByFreetext, query);
        }

        void OnViewGenreSelected (object sender, string genre)
        {
            plugin.ExecuteRequest (LiveRadioRequestType.ByGenre, genre);
        }

        private void ShowPack ()
        {
            PackStart (container, true, true, 0);
            VBox instruct = new VBox ();
            instruct.ExposeEvent += (o, a) => {
                using (Cairo.Context cr = Gdk.CairoHelper.Create (instruct.GdkWindow)) {
                    double radius = 10;
                    int x = a.Event.Area.X;
                    int y = a.Event.Area.Y;
                    int width = a.Event.Area.Width;
                    int height = a.Event.Area.Height;

                    cr.MoveTo (x + radius, y);
                    cr.Arc (x + width - radius, y + radius, radius, Math.PI * 1.5, Math.PI * 2);
                    cr.Arc (x + width - radius, y + height - radius, radius, 0, Math.PI * .5);
                    cr.Arc (x + radius, y + height - radius, radius, Math.PI * .5, Math.PI);
                    cr.Arc (x + radius, y + radius, radius, Math.PI, Math.PI * 1.5);
                    cr.Color = new Cairo.Color (.5, .5, .5, 1);
                    cr.Stroke ();
                }
//                Gdk.Window win = a.Event.Window;
//                Gdk.Rectangle area = a.Event.Area;
//
//                win.DrawRectangle (Style.BaseGC (StateType.Active), false, area);
//
//                a.RetVal = true;
            };
            Label help_label = new Label (
                  Catalog.GetString ("Click a gernre to load/refresh entries or type query, use refresh button to refresh genres."));
            instruct.PackStart(help_label, false, true, 10);
            PackStart (instruct, false, true, 10);
            NoShowAll = false;
            ShowAll ();
            NoShowAll = true;
            browser_container.Visible = ForcePosition != null || BrowserVisible.Get ();
        }

        private Paned GetPane (bool hpane)
        {
            if (hpane)
                return new HPaned ();
            else
                return new VPaned ();
        }

        private void OnViewModeChanged (object o, ChangedArgs args)
        {
            //Hyena.Log.InformationFormat ("ListBrowser mode toggled, val = {0}", args.Current.Value);
            if (args.Current.Value == 0) {
                LayoutLeft ();
                BrowserPosition.Set ("left");
            } else {
                LayoutTop ();
                BrowserPosition.Set ("top");
            }
        }

        private void OnToggleBrowser (object o, EventArgs args)
        {
            ToggleAction action = (ToggleAction)o;
            
            browser_container.Visible = action.Active && ActiveSourceCanHasBrowser;
            BrowserVisible.Set (action.Active);
            
        }

        protected virtual void OnBrowserViewSelectionChanged (object o, EventArgs args)
        {
        }
            /* If the All item is now selected, scroll to the top
            Hyena.Collections.Selection selection = (Hyena.Collections.Selection) o;
            if (selection.AllSelected) {
                // Find the view associated with this selection; a bit yuck; pass view in args?
                foreach (IListView view in filter_views) {
                    if (view.Selection == selection) {
                        view.ScrollTo (0);
                        break;
                    }
                }
            }*/            
        
                protected bool ActiveSourceCanHasBrowser {
            get { return true; }
        }

        protected string ForcePosition {
            get { return "bottom"; }
        }

        #region Implement ISourceContents

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

        public void ResetSource ()
        {
            this.source = null;
            track_view.SetModel (null);
        }

        public ISource Source {
            get { return source; }
        }

        public Widget Widget {
            get { return this; }
        }

        #endregion

        #region ITrackModelSourceContents implementation

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
