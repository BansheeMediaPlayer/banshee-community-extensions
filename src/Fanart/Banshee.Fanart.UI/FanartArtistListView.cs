//
// FanartArtistListView.cs
//
// Author:
//   Tomasz Maczyński <tmtimon@gmail.com>
//   Aaron Bockover <abockover@novell.com>
//   Frank Ziegler <funtastix@googlemail.com>
//
// Copyright 2013 Tomasz Maczyński
// Copyright (C) 2007 Novell, Inc.
// Copyright (C) 2011 Frank Ziegler
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
using Banshee.Collection.Gui;
using Banshee.Collection;
using Hyena.Data.Gui;
using Banshee.Configuration;
using Banshee.ServiceStack;
using Banshee.Gui;
using Gtk;
using Banshee.I18n;
using Hyena;
using Banshee.Sources;

namespace Banshee.Fanart.UI
{
    public class FanartArtistListView : TrackFilterListView<ArtistInfo>
    {
        private FanartArtistColumnCell image_column_cell;
        private Column image_column;
        private Column artist_name_column;

        private InterfaceActionService action_service;
        private ActionGroup viewKindActions;

        private static string menu_xml = @"
            <ui>
              <menubar name=""MainMenu"">
                <menu name=""ViewMenu"" action=""ViewMenuAction"">
                  <placeholder name=""ViewMenuAdditions"">
                    <menu name=""FanartViewKindMenu"" action=""FanartViewKindMenuAction"">
                        <menuitem name=""FanartViewOneColumnKind"" action=""FanartViewOneColumnKindAction"" />
                        <menuitem name=""FanartViewTwoColumnsKind"" action=""FanartViewTwoColumnsKindAction"" />
                    </menu>
                    <separator />
                  </placeholder>
                </menu>
              </menubar>
            </ui>
        ";

        private static FanartArtistListViewKind ViewKind {
            get { 
                return ListViewKindSchema.Get (); 
            }
            set {
                ListViewKindSchema.Set (value);
            }
        }

        protected FanartArtistListView (IntPtr ptr) : base () {}

        public FanartArtistListView () : base ()
        {
            InstallPreferences ();
            SetView (ViewKind);
        }

        private void InstallPreferences ()
        {
            if (ServiceManager.Contains ("InterfaceActionService")) {
                action_service = ServiceManager.Get<InterfaceActionService> ();

                if (action_service.FindActionGroup ("ArtistGridView") == null) {
                    viewKindActions = new ActionGroup ("ArtistGridView");

                    viewKindActions.Add (new Gtk.Action("FanartViewKindMenuAction", "FanartViewKindMenuAction"));

                    viewKindActions.Add (new RadioActionEntry [] {
                        new RadioActionEntry ("FanartViewOneColumnKindAction", null,
                                               "One column mode", null,
                                               "Use one column mode...",
                                               ViewKind == FanartArtistListViewKind.NormalOneColumn ? 1 : 0),

                        new RadioActionEntry ("FanartViewTwoColumnsKindAction", null,
                                               "Two columns mode", null,
                                               "Use two columns mode...",
                                               ViewKind == FanartArtistListViewKind.NormalTwoColumns ? 1 : 0)
                    }, (int)ViewKind, OnViewKindChanged);

                    action_service.AddActionGroup (viewKindActions);
                    action_service.UIManager.AddUiFromString (menu_xml);
                   
                }
            }

            ServiceManager.SourceManager.ActiveSourceChanged += delegate {
                ThreadAssist.ProxyToMain (delegate {
                    if (ServiceManager.SourceManager.ActiveSource is ITrackModelSource) {
                        ITrackModelSource source = ServiceManager.SourceManager.ActiveSource as ITrackModelSource;
                        action_service.FindAction("ArtistGridView.FanartViewOneColumnKindAction").Visible = source.ShowBrowser;
                        action_service.FindAction("ArtistGridView.FanartViewOneColumnKindAction").Sensitive = source.ShowBrowser;
                        action_service.FindAction("ArtistGridView.FanartViewTwoColumnsKindAction").Visible = source.ShowBrowser;
                        action_service.FindAction("ArtistGridView.FanartViewTwoColumnsKindAction").Sensitive = source.ShowBrowser;
                    } else {
                        action_service.FindAction("ArtistGridView.FanartViewOneColumnKindAction").Visible = false;
                        action_service.FindAction("ArtistGridView.FanartViewOneColumnKindAction").Sensitive = false;
                        action_service.FindAction("ArtistGridView.FanartViewTwoColumnsKindAction").Visible = false;
                        action_service.FindAction("ArtistGridView.FanartViewTwoColumnsKindAction").Sensitive = false;
                    }
                });
            };

        }

        private void SwitchToView (FanartArtistListViewKind viewKind)
        {
            ClearColumns ();
            SetView (viewKind);
        }

        void ClearColumns ()
        {
            column_controller.Clear ();
            ColumnController = column_controller;
        }

        private void SetView (FanartArtistListViewKind viewKind)
        {
            switch (viewKind) {
            case FanartArtistListViewKind.NormalOneColumn:
                SetNormalOneColumn ();
                break;
            case FanartArtistListViewKind.NormalTwoColumns:
                SetNormalTwoColumns ();
                break;
            default:
                var msg = "FanartArtistListViewKind enum value with no corresponding method was passed to SetView method";
                Hyena.Log.Debug (msg);
                throw new NotImplementedException (msg);
            }

            QueueDraw ();
        }

        void OnNormalOneColumnSelected (object sender, EventArgs e)
        {
            Hyena.Log.Debug ("OnNormalTwoColumnsSelected");
        }

        void OnNormalTwoColumnsSelected (object sender, EventArgs e)
        {
            Hyena.Log.Debug ("OnNormalTwoColumnsSelected");
        }

        private void OnViewKindChanged (object o, ChangedArgs args)
        {
            var button = o as RadioAction;
            SwitchToView ((FanartArtistListViewKind) button.Value);
        }

        private void SetNormalOneColumn () 
        {
            image_column_cell = new FanartArtistColumnCell () { RenderNameWhenNoImage = true };
            image_column = new Column ("Artist Image", image_column_cell, 1.0);
            column_controller.Add (image_column);

            ColumnController = column_controller;
        }

        private void SetNormalTwoColumns () 
        {
            artist_name_column = new Column ("Artist", new ColumnCellText ("DisplayName", true), 0.65);
            image_column_cell = new FanartArtistColumnCell () { RenderNameWhenNoImage = true };
            image_column = new Column ("Artist Image", image_column_cell, 0.35);

            column_controller.Add (artist_name_column);
            column_controller.Add (image_column);

            ColumnController = column_controller;
        }

        protected override Gdk.Size OnMeasureChild ()
        {
            return new Gdk.Size (0, image_column_cell.ComputeRowHeight (this));
        }

        public enum FanartArtistListViewKind {
            NormalOneColumn,
            NormalTwoColumns,
        }

        private static readonly SchemaEntry<FanartArtistListViewKind> ListViewKindSchema = new SchemaEntry<FanartArtistListViewKind> (
            "player_window", "fanart_artist_listview_kind",
            FanartArtistListViewKind.NormalOneColumn,
            "Desired kind of FanartListView's appearance",
            "Desired kind of FanartListView's appearance"
            );
    }
}

