//
// CoverArtListView.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//   Frank Ziegler <funtastix@googlemail.com>
//   Tomasz Maczyński <tmtimon@gmail.com>
//
// Copyright (C) 2007 Novell, Inc.
// Copyright (C) 2011 Frank Ziegler
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

using Hyena.Data;
using Hyena.Data.Gui;

using Banshee.Collection;
using Banshee.ServiceStack;
using Banshee.Gui;
using Banshee.I18n;
using Banshee.Configuration;
using Banshee.MediaEngine;
using Banshee.Sources;
using Gtk;
using Hyena;
using Banshee.Collection.Gui;

namespace Banshee.Fanart.UI
{
    public class CoverArtArtistListView : TrackFilterListView<ArtistInfo>
    {
        private CoverArtArtistColumnCell renderer = null;
        private Column current_layout = null;
        private bool? artist_grid_rendered = null;
        private bool? small_images_used = null;

        private InterfaceActionService action_service;
        private ActionGroup artist_grid_view_actions;

        private static string menu_xml = @"
            <ui>
              <menubar name=""MainMenu"">
                <menu name=""ViewMenu"" action=""ViewMenuAction"">
                  <placeholder name=""ViewMenuAdditions"">
                    <menu name=""ArtistGridMenu"" action=""ArtistGridMenuAction"">
                        <menuitem name=""DisableArtistGrid"" action=""DisableArtistGridAction"" />
                        <menuitem name=""ArtistGridUseSmallImages"" action=""ArtistGridUseSmallImagesAction"" />
                    </menu>
                    <separator />
                  </placeholder>
                </menu>
              </menubar>
            </ui>
        ";

        protected CoverArtArtistListView (IntPtr ptr) : base () {}

        public CoverArtArtistListView () : base ()
        {
            column_controller.Updated += ColumnCotrollerUpdated;

            InstallInterfaceActions ();

            ServiceManager.PlayerEngine.ConnectEvent (OnPlayerEvent, PlayerEvent.TrackInfoUpdated);
            Banshee.Metadata.MetadataService.Instance.ArtworkUpdated += OnArtworkUpdated;
        }

        public override void Dispose ()
        {
            ServiceManager.PlayerEngine.DisconnectEvent (OnPlayerEvent);
            Banshee.Metadata.MetadataService.Instance.ArtworkUpdated -= OnArtworkUpdated;
        }

        protected void InstallInterfaceActions ()
        {
            if (ServiceManager.Contains ("InterfaceActionService")) {
                action_service = ServiceManager.Get<InterfaceActionService> ();

                if (action_service.FindActionGroup ("ArtistGridView") == null) {
                    artist_grid_view_actions = new ActionGroup ("ArtistGridView");

                    artist_grid_view_actions.Add (new ActionEntry [] {
                        new ActionEntry ("ArtistGridMenuAction", null,
                            Catalog.GetString ("Artist List"), null,
                            Catalog.GetString ("Artist List"), null)
                    });

                    artist_grid_view_actions.Add (new ToggleActionEntry [] {
                        new ToggleActionEntry ("DisableArtistGridAction", null,
                            Catalog.GetString ("Disable Album Covers"), null,
                            Catalog.GetString ("Disable album covers and display a text list instead"),
                            null, DisableArtistGrid.Get ())
                    });
                    artist_grid_view_actions.Add (new ToggleActionEntry [] {
                        new ToggleActionEntry ("ArtistGridUseSmallImagesAction", null,
                            Catalog.GetString ("Single Line Mode"), null,
                            Catalog.GetString ("Use small images and a single line to save space"),
                            null, ArtistGridUseSmallImages.Get ())
                    });

                    action_service.AddActionGroup (artist_grid_view_actions);
                    action_service.UIManager.AddUiFromString (menu_xml);

                    action_service.FindAction("ArtistGridView.DisableArtistGridAction").Activated += OnToggleArtistGrid;
                    action_service.FindAction("ArtistGridView.ArtistGridUseSmallImagesAction").Activated += OnToggleUseSmallImages;
                }
            }

            ServiceManager.SourceManager.ActiveSourceChanged += delegate {
                ThreadAssist.ProxyToMain (delegate {
                    if (ServiceManager.SourceManager.ActiveSource is ITrackModelSource) {
                        ITrackModelSource source = ServiceManager.SourceManager.ActiveSource as ITrackModelSource;
                        action_service.FindAction("ArtistGridView.DisableArtistGridAction").Visible = source.ShowBrowser;
                        action_service.FindAction("ArtistGridView.DisableArtistGridAction").Sensitive = source.ShowBrowser;
                        action_service.FindAction("ArtistGridView.ArtistGridUseSmallImagesAction").Visible = source.ShowBrowser;
                        action_service.FindAction("ArtistGridView.ArtistGridUseSmallImagesAction").Sensitive = source.ShowBrowser;
                    } else {
                        action_service.FindAction("ArtistGridView.DisableArtistGridAction").Visible = false;
                        action_service.FindAction("ArtistGridView.DisableArtistGridAction").Sensitive = false;
                        action_service.FindAction("ArtistGridView.ArtistGridUseSmallImagesAction").Visible = false;
                        action_service.FindAction("ArtistGridView.ArtistGridUseSmallImagesAction").Sensitive = false;
                    }
                });
            };
        }

        private void ColumnCotrollerUpdated (object sender, EventArgs e)
        {
            var artwork_manager = ServiceManager.Get<ArtworkManager> ();
            if (artwork_manager != null && column_controller.Count > 0)
                artwork_manager.ChangeCacheSize (renderer.ImageSize, column_controller.Count * 3);
        }

        protected override bool OnWidgetEvent (Gdk.Event evnt)
        {
            if (artist_grid_rendered == null && evnt.Type == Gdk.EventType.Expose) {
                ToggleArtistGrid ();
            }
            if (small_images_used == null && evnt.Type == Gdk.EventType.Expose) {
                ToggleUseSmallImages ();
            }
            return base.OnWidgetEvent (evnt);
        }

        protected override Gdk.Size OnMeasureChild ()
        {
            return artist_grid_rendered.HasValue && artist_grid_rendered.Value
                ? new Gdk.Size (0, renderer.ComputeRowHeight (this))
                : base.OnMeasureChild ();
        }

        private void OnToggleArtistGrid (object o, EventArgs args)
        {
            ToggleAction action = (ToggleAction)o;

            artist_grid_rendered = !action.Active;
            DisableArtistGrid.Set (action.Active);

            DisabledArtistGrid = DisableArtistGrid.Get ();
        }

        private void OnToggleUseSmallImages (object o, EventArgs args)
        {
            ToggleAction action = (ToggleAction)o;

            small_images_used = action.Active;
            ArtistGridUseSmallImages.Set (action.Active);

            ArtistGridUsedSmallImages = ArtistGridUseSmallImages.Get ();
        }

        private void ToggleArtistGrid ()
        {
            if (artist_grid_rendered.HasValue &&
                !DisableArtistGrid.Get ().Equals (artist_grid_rendered.Value)) {
                return;
            }

            DisabledArtistGrid = DisableArtistGrid.Get ();
        }

        private void ToggleUseSmallImages ()
        {
            if (small_images_used.HasValue &&
                ArtistGridUseSmallImages.Get ().Equals (small_images_used.Value)) {
                return;
            }

            ArtistGridUsedSmallImages = ArtistGridUseSmallImages.Get ();
        }

        private bool DisabledArtistGrid {
            get { return DisableArtistGrid.Get (); }
            set {
                DisableArtistGrid.Set (value);
                if (renderer == null)
                    renderer = new CoverArtArtistColumnCell (small_images_used.HasValue && small_images_used.Value);
                if (value) {
                    if (current_layout != null)
                        column_controller.Remove (current_layout);
                    current_layout = new Column ("Artist", new ColumnCellText ("DisplayName", true), 1.0);
                    column_controller.Add (current_layout);
                    ColumnController = null;
                    ColumnController = column_controller;
                } else {
                    if (current_layout != null)
                        column_controller.Remove (current_layout);
                    current_layout = new Column ("Artist", renderer, 1.0);
                    column_controller.Add (current_layout);
                    ColumnController = null;
                    ColumnController = column_controller;
                }
                artist_grid_rendered = !value;
                ViewLayout = null;
            }
        }

        private bool ArtistGridUsedSmallImages {
            get { return ArtistGridUseSmallImages.Get (); }
            set {
                ArtistGridUseSmallImages.Set (value);
                small_images_used = value;
                renderer = new CoverArtArtistColumnCell (small_images_used.HasValue && small_images_used.Value);
                ViewLayout = null;
            }
        }

        private static readonly SchemaEntry<bool> DisableArtistGrid = new SchemaEntry<bool> (
            "player_window", "disable_artist_grid",
            false,
            "Disable artist grid",
            "Disable artist grid and show the classic layout instead"
        );

        private static readonly SchemaEntry<bool> ArtistGridUseSmallImages = new SchemaEntry<bool> (
            "player_window", "artist_grid_use_small_images",
            false,
            "Use small images in artist grid",
            "Use small images in artist grid to save space"
        );

        private void OnPlayerEvent (PlayerEventArgs args)
        {
            QueueDraw ();
        }

        private void OnArtworkUpdated (IBasicTrackInfo track)
        {
            ViewLayout = null;
         }

            // TODO add context menu for artists/albums...probably need a Banshee.Gui/ArtistActions.cs file.  Should
            // make TrackActions.cs more generic with regards to the TrackSelection stuff, using the new properties
            // set on the sources themselves that give us access to the IListView<T>.
            /*protected override bool OnPopupMenu ()
        {
            ServiceManager.Get<InterfaceActionService> ().TrackActions["TrackContextMenuAction"].Activate ();
            return true;
        }*/
    }
}

