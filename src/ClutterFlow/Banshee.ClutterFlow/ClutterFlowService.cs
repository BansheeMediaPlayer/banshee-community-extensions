//
// ClutterFlowService.cs
//
// Author:
//       Mathijs Dumon <mathijsken@hotmail.com>
//
// Copyright (c) 2010 Mathijs Dumon
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
using Gtk;
using Mono.Addins;

using Banshee.ServiceStack;
using Banshee.Gui;
using Banshee.Library;
using Banshee.Sources;
using Banshee.Sources.Gui;
using Banshee.Preferences;
using Banshee.Preferences.Gui;

using ClutterFlow;

namespace Banshee.ClutterFlow
{
    public class ClutterFlowService : IExtensionService, IDisposable
    {
        #region Fields
        string IService.ServiceName {
            get { return "ClutterFlowService"; }
        }

        private SourceManager source_manager;
        private MusicLibrarySource music_library;

        private PreferenceService preference_service;
        private InterfaceActionService action_service;

        private uint ui_manager_id;
        private ActionGroup clutterflow_actions;
        private ToggleAction browser_action;
        protected ToggleAction BrowserAction {
            get {
                if (browser_action==null) {
                    browser_action = (ToggleAction) action_service.FindAction("BrowserView.BrowserVisibleAction");
                }
                return browser_action;
            }
        }
        private ToggleAction cfbrows_action;
        protected ToggleAction CfBrowsAction {
            get {
                if (cfbrows_action==null) {
                    cfbrows_action = (ToggleAction) action_service.FindAction("ClutterFlowView.ClutterFlowVisibleAction");
                }
                return cfbrows_action;
            }
        }
        private static string menu_xml = @"
            <ui>
                <menubar name=""MainMenu"">
                    <menu name=""ViewMenu"" action=""ViewMenuAction"">
                        <placeholder name=""BrowserViews"">
                            <menuitem name=""ClutterFlow"" action=""ClutterFlowVisibleAction"" />
                        </placeholder>
                    </menu>
                </menubar>
            </ui>
        ";

        private ClutterFlowContents clutter_flow_contents;
        #endregion

        #region Initialization

        public ClutterFlowService ()
        { }

        void IExtensionService.Initialize ()
        {
            ClutterHelper.Init ();

            preference_service = ServiceManager.Get<PreferenceService> ();
            action_service = ServiceManager.Get<InterfaceActionService> ();

            source_manager = ServiceManager.SourceManager;
            music_library = source_manager.MusicLibrary;

            if (!SetupPreferences () || !SetupInterfaceActions ()) {
                ServiceManager.ServiceStarted += OnServiceStarted;
            } else if (!SetupSourceContents ()) {
                source_manager.SourceAdded += OnSourceAdded;
            }

            //--> TODO Banshee.ServiceStack.Application. register Exit event to close threads etc.
        }

        private void OnServiceStarted (ServiceStartedArgs args)
        {
            if (args.Service is Banshee.Preferences.PreferenceService) {
                preference_service = (PreferenceService)args.Service;
                SetupPreferences ();
            } else if (args.Service is Banshee.Gui.InterfaceActionService) {
                action_service = (InterfaceActionService)args.Service;
                SetupInterfaceActions ();
            }

            if (!(preference_service==null || action_service==null)) {
                ServiceManager.ServiceStarted -= OnServiceStarted;
                if (!SetupSourceContents ()) {
                    source_manager.SourceAdded += OnSourceAdded;
                }
            }
        }

        private void OnSourceAdded (SourceAddedArgs args)
        {
            if (args.Source is MusicLibrarySource) {
                music_library = args.Source as MusicLibrarySource;
            }
            SetupSourceContents ();
        }
        #endregion

        #region Setup
        private bool SetupSourceContents ()
        {
            if (music_library == null || preference_service == null || action_service == null
                || ServiceManager.SourceManager.ActiveSource == null) {
                return false;
            }
            source_manager.SourceAdded -= OnSourceAdded;

            clutter_flow_contents = new ClutterFlowContents ();
            clutter_flow_contents.SetSource (music_library);

            if (ClutterFlowSchemas.ShowClutterFlow.Get ()) {
                BrowserAction.Active = false;
                music_library.Properties.Set<ISourceContents> ("Nereid.SourceContents", clutter_flow_contents);
            }

            LoadPreferences ();

            return true;
        }

        private bool SetupPreferences ()
        {
            InstallPreferences ();

            return true;
        }

        private bool SetupInterfaceActions ()
        {

            action_service = ServiceManager.Get<InterfaceActionService> ();

            if (action_service.FindActionGroup ("ClutterFlowView") == null) {
                clutterflow_actions = new ActionGroup ("ClutterFlowView");

                ToggleActionEntry [] tae = new ToggleActionEntry [] { new ToggleActionEntry ("ClutterFlowVisibleAction", null,
                    AddinManager.CurrentLocalizer.GetString ("Show ClutterFlow Browser"), null,
                    AddinManager.CurrentLocalizer.GetString ("Show or hide the ClutterFlow browser"),
                    null, ClutterFlowSchemas.ShowClutterFlow.Get ()) };
                clutterflow_actions.Add (tae);

                action_service.AddActionGroup (clutterflow_actions);
                ui_manager_id = action_service.UIManager.AddUiFromString (menu_xml);
            }

            source_manager.ActiveSourceChanged += HandleActiveSourceChanged;

            BrowserAction.Activated += OnToggleBrowser;
            CfBrowsAction.Activated += OnToggleClutterFlow;

            return true;
        }
        #endregion

        #region Action Handling
        private void HandleActiveSourceChanged (SourceEventArgs args)
        {
            if (args.Source==music_library) {
                clutterflow_actions.Visible = true;
            } else {
                clutterflow_actions.Visible = false;
            }
        }

        private void OnToggleBrowser (object sender, EventArgs e)
        {
            if (BrowserAction.Active) {
                ClutterFlowSchemas.OldShowBrowser.Set (true);
                CfBrowsAction.Active = false;
                ClutterFlowSchemas.ShowClutterFlow.Set (false);
            }
        }


        private void OnToggleClutterFlow (object sender, EventArgs e)
        {
            if (CfBrowsAction.Active) {
                ClutterFlowSchemas.ShowClutterFlow.Set (true);
                ClutterFlowSchemas.OldShowBrowser.Set (BrowserAction.Active);
                BrowserAction.Active = false;
                Clutter.Threads.Enter ();
                music_library.Properties.Set<ISourceContents> ("Nereid.SourceContents", clutter_flow_contents);
                Clutter.Threads.Leave ();
            } else {
                ClutterFlowSchemas.ShowClutterFlow.Set (false);
                Clutter.Threads.Enter ();
                music_library.Properties.Remove ("Nereid.SourceContents");
                Clutter.Threads.Leave ();
                BrowserAction.Active = ClutterFlowSchemas.OldShowBrowser.Get ();
            }
        }

        private void RemoveClutterFlow ()
        {
            Clutter.Threads.Enter ();
            music_library.Properties.Remove ("Nereid.SourceContents");
            Clutter.Threads.Leave ();
            clutter_flow_contents.Dispose ();
            clutter_flow_contents = null;

            source_manager.ActiveSourceChanged -= HandleActiveSourceChanged;
            BrowserAction.Activated -= OnToggleBrowser;
            BrowserAction.Active = ClutterFlowSchemas.OldShowBrowser.Get ();
            CfBrowsAction.Activated -= OnToggleClutterFlow;
            CfBrowsAction.Visible = false;

            action_service.RemoveActionGroup ("ClutterFlowView");
            action_service.UIManager.RemoveUi (ui_manager_id);
            clutterflow_actions = null;
            cfbrows_action = null;

            preference_service = null;
            source_manager = null;
            music_library = null;
            action_service = null;
            browser_action = null;
            cfbrows_action = null;
        }
        #endregion

        #region Preferences

        private bool pref_installed = false;
        private Page pref_page;
        private Section general;
        private Section dimensions;

        protected void InstallPreferences ()
        {
            if (!pref_installed) {
                preference_service.InstallWidgetAdapters += OnPreferencesServiceInstallWidgetAdapters;

                pref_page = preference_service.Add(new Page("clutterflow",
                                                            AddinManager.CurrentLocalizer.GetString ("ClutterFlow"), 10));

                general = pref_page.Add (new Section ("general",
                    AddinManager.CurrentLocalizer.GetString ("General"), 1));
                ClutterFlowSchemas.AddToSection (general, ClutterFlowSchemas.InstantPlayback, null);
                ClutterFlowSchemas.AddToSection (general, ClutterFlowSchemas.DisplayLabel, UpdateLabelVisibility);
                ClutterFlowSchemas.AddToSection (general, ClutterFlowSchemas.DisplayTitle, UpdateTitleVisibility);
                ClutterFlowSchemas.AddToSection (general, ClutterFlowSchemas.VisibleCovers, UpdateVisibleCovers);
                ClutterFlowSchemas.AddToSection (general, ClutterFlowSchemas.DragSensitivity, UpdateDragSensitivity);

                dimensions = pref_page.Add (new Section ("dimensions",
                    AddinManager.CurrentLocalizer.GetString ("Dimensions"), 2));
                dimensions.Add (new VoidPreference ("dimensions-desc"));
                ClutterFlowSchemas.AddToSection (dimensions, ClutterFlowSchemas.MinCoverSize, UpdateMinCoverSize);
                ClutterFlowSchemas.AddToSection (dimensions, ClutterFlowSchemas.MaxCoverSize, UpdateMaxCoverSize);
                ClutterFlowSchemas.AddToSection (dimensions, ClutterFlowSchemas.TextureSize, UpdateTextureSize);

                pref_installed = true;
            }
        }

        private void LoadPreferences ()
        {
            UpdateDragSensitivity ();
            UpdateLabelVisibility ();
            UpdateTitleVisibility ();
            UpdateVisibleCovers ();
            UpdateMinCoverSize ();
            UpdateMaxCoverSize ();
            UpdateTextureSize ();
        }

        private void UpdateDragSensitivity ()
        {
            if (clutter_flow_contents != null) {
                clutter_flow_contents.FilterView.DragSensitivity =
                    (float) ClutterFlowSchemas.DragSensitivity.Get () * 0.1f;
            }
        }

        private void UpdateLabelVisibility ()
        {
            if (clutter_flow_contents != null) {
                clutter_flow_contents.FilterView.LabelCoverIsVisible =
                    ClutterFlowSchemas.DisplayLabel.Get ();
            }
        }

        private void UpdateTitleVisibility ()
        {
            if (clutter_flow_contents != null) {
                clutter_flow_contents.FilterView.LabelTrackIsVisible =
                    ClutterFlowSchemas.DisplayTitle.Get ();
            }
        }

        private void UpdateVisibleCovers ()
        {
            if (clutter_flow_contents!=null) {
                clutter_flow_contents.FilterView.CoverManager.VisibleCovers =
                    ((ClutterFlowSchemas.VisibleCovers.Get () + 1) * 2 + 1);
            }
        }

        private void UpdateMinCoverSize ()
        {
            if (clutter_flow_contents != null) {
                clutter_flow_contents.FilterView.CoverManager.Behaviour.MinCoverWidth =
                    ClutterFlowSchemas.MinCoverSize.Get ();
            }
        }

        private void UpdateMaxCoverSize ()
        {
            if (clutter_flow_contents != null) {
                clutter_flow_contents.FilterView.CoverManager.Behaviour.MaxCoverWidth =
                    ClutterFlowSchemas.MaxCoverSize.Get ();
            }
        }

        private void UpdateTextureSize ()
        {
            if (clutter_flow_contents != null) {
                clutter_flow_contents.FilterView.CoverManager.TextureSize =
                    ClutterFlowSchemas.TextureSize.Get ();
            }
        }

        private void UninstallPreferences ()
        {
            preference_service.InstallWidgetAdapters -= OnPreferencesServiceInstallWidgetAdapters;

            preference_service.Remove (pref_page);
            pref_page = null;
            general = null;
            dimensions = null;
            pref_installed = false;
        }

        private void OnPreferencesServiceInstallWidgetAdapters (object o, EventArgs args)
        {
            if (dimensions == null) {
                return;
            }

            var description_label = new DescriptionLabel (AddinManager.CurrentLocalizer.GetString (
                "For changes to these values to take effect, you need to restart Banshee"));
            dimensions["dimensions-desc"].ShowLabel = false;
            dimensions["dimensions-desc"].DisplayWidget = description_label;
        }

        #endregion

        private bool disposed = false;
        public void Dispose ()
        {
            if (disposed) {
                return;
            }
            disposed = true;

            ServiceManager.ServiceStarted -= OnServiceStarted;
            source_manager.SourceAdded -= OnSourceAdded;

            UninstallPreferences ();
            RemoveClutterFlow ();

            ClutterHelper.Quit ();
         }
    }
}
