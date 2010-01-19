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

using Banshee.ServiceStack;
using Banshee.Gui;
using Banshee.I18n;
using Banshee.Library;
using Banshee.Sources;
using Banshee.Sources.Gui;
using Banshee.Preferences;

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

        private ActionGroup clutterflow_actions;
		private ToggleAction browser_action;
		protected ToggleAction BrowserAction {
			get {
				if (browser_action==null)
					browser_action = (ToggleAction) action_service.FindAction("BrowserView.BrowserVisibleAction"); 
				return browser_action;
			}
		}
		private ToggleAction cfbrows_action;
		protected ToggleAction CfBrowsAction {
			get {
				if (cfbrows_action==null)
					cfbrows_action = (ToggleAction) action_service.FindAction("ClutterFlowView.ClutterFlowVisibleAction"); 
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

		private bool old_value = FilteredListSourceContents.BrowserVisible.Get ();
		private ClutterFlowContents clutter_flow_contents;
		#endregion
		
		#region Initialization
		public ClutterFlowService ()
		{
		}
		
		void IExtensionService.Initialize ()
		{	
			preference_service = ServiceManager.Get<PreferenceService> ();
			action_service = ServiceManager.Get<InterfaceActionService> ();

			source_manager = ServiceManager.SourceManager;
			music_library = source_manager.MusicLibrary;
			
			if (!SetupPreferences () || !SetupInterfaceActions ())
				ServiceManager.ServiceStarted += OnServiceStarted;
			else if (!SetupSourceContents ())
				source_manager.SourceAdded += OnSourceAdded;
				
			
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
				if (!SetupSourceContents ())
					source_manager.SourceAdded += OnSourceAdded;
			}
		}
		
		private void OnSourceAdded (SourceAddedArgs args) 
		{
			if (args.Source is MusicLibrarySource)
				music_library = args.Source as MusicLibrarySource;
			SetupSourceContents ();
		}
		#endregion
		
		#region Setup
		private bool SetupSourceContents ()
		{
			if (music_library==null || preference_service==null || action_service==null)
				return false;		

            clutter_flow_contents = new ClutterFlowContents ();
			clutter_flow_contents.SetSource(music_library);

			if (ClutterFlowSchemas.ShowClutterFlow.Get ()) {
				BrowserAction.Active = false;
				music_library.Properties.Set<ISourceContents> ("Nereid.SourceContents", clutter_flow_contents);
			}
			
			source_manager.SourceAdded -= OnSourceAdded;
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

				clutterflow_actions.Add (new ToggleActionEntry [] {
					new ToggleActionEntry ("ClutterFlowVisibleAction", null,
   					Catalog.GetString ("Show ClutterFlow Browser"), null,
     				Catalog.GetString ("Show or hide the ClutterFlow browser"),
   					null, false)
                });
				
				action_service.AddActionGroup (clutterflow_actions);
            	action_service.UIManager.AddUiFromString (menu_xml);
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
            if (args.Source==music_library)
                clutterflow_actions.Visible = true;
           else
                clutterflow_actions.Visible = false;
        }
                
		private void OnToggleBrowser (object sender, EventArgs e)
		{
			if (BrowserAction.Active) {
				old_value = true;
				CfBrowsAction.Active = false;
				ClutterFlowSchemas.ShowClutterFlow.Set (false);
			}
		}

		
		private void OnToggleClutterFlow (object sender, EventArgs e)
		{
			if (CfBrowsAction.Active) {
				ClutterFlowSchemas.ShowClutterFlow.Set (true);
				old_value = BrowserAction.Active;
				BrowserAction.Active = false;
				Clutter.Threads.Enter ();
				music_library.Properties.Set<ISourceContents> ("Nereid.SourceContents", clutter_flow_contents);
				Clutter.Threads.Leave ();
			} else {
				ClutterFlowSchemas.ShowClutterFlow.Set (false);
				Clutter.Threads.Enter ();
				music_library.Properties.Remove ("Nereid.SourceContents");
				Clutter.Threads.Leave ();
				BrowserAction.Active = old_value;
			}
		}

		private void RemoveClutterFlow ()
		{
			Clutter.Threads.Enter ();
			music_library.Properties.Remove ("Nereid.SourceContents");
			clutter_flow_contents.Dispose ();
			Clutter.Threads.Leave ();

			BrowserAction.Activated -= OnToggleBrowser;
			CfBrowsAction.Activated -= OnToggleClutterFlow;
			
			BrowserAction.Active = old_value;
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
				pref_page = preference_service.Add(new Page("clutterflow", Catalog.GetString ("ClutterFlow"), 10)); 
				
	            general = pref_page.Add (new Section ("general", 
	                Catalog.GetString ("General"), 1));
				
	            general.Add (new SchemaPreference<bool> (ClutterFlowSchemas.InstantPlayback, 
	                ClutterFlowSchemas.InstantPlayback.ShortDescription, ClutterFlowSchemas.InstantPlayback.LongDescription, UpdateLabelVisibility));
	            general.Add (new SchemaPreference<bool> (ClutterFlowSchemas.DisplayLabel, 
	                ClutterFlowSchemas.DisplayLabel.ShortDescription, ClutterFlowSchemas.DisplayLabel.LongDescription, UpdateLabelVisibility));
				general.Add (new SchemaPreference<bool> (ClutterFlowSchemas.DisplayTitle, 
	                ClutterFlowSchemas.DisplayTitle.ShortDescription, ClutterFlowSchemas.DisplayTitle.LongDescription, UpdateTitleVisibility));
				general.Add (new SchemaPreference<int> (ClutterFlowSchemas.VisibleCovers, 
	                ClutterFlowSchemas.VisibleCovers.ShortDescription, ClutterFlowSchemas.VisibleCovers.LongDescription, UpdateVisibleCovers));
				
				dimensions = pref_page.Add (new Section ("dimensions", 
	                Catalog.GetString ("Dimensions"), 2));
	
	            dimensions.Add (new SchemaPreference<int> (ClutterFlowSchemas.MinCoverSize, 
	                ClutterFlowSchemas.MinCoverSize.ShortDescription, ClutterFlowSchemas.MinCoverSize.LongDescription, UpdateMinCoverSize));
				dimensions.Add (new SchemaPreference<int> (ClutterFlowSchemas.MaxCoverSize, 
	                ClutterFlowSchemas.MaxCoverSize.ShortDescription, ClutterFlowSchemas.MaxCoverSize.LongDescription, UpdateMaxCoverSize));
				dimensions.Add (new SchemaPreference<int> (ClutterFlowSchemas.TextureSize, 
	                ClutterFlowSchemas.TextureSize.ShortDescription, ClutterFlowSchemas.TextureSize.LongDescription, UpdateTextureSize));
	
				LoadPreferences ();

				pref_installed = true;
			}
		}

		private void LoadPreferences ()
		{
			UpdateLabelVisibility ();
			UpdateTitleVisibility ();
			UpdateVisibleCovers ();
			UpdateMinCoverSize ();
			UpdateMaxCoverSize ();
			UpdateTextureSize ();
		}
		
		private void UpdateLabelVisibility ()
		{
			if (clutter_flow_contents != null)
				clutter_flow_contents.FilterView.LabelCoverIsVisible = 
					ClutterFlowSchemas.DisplayLabel.Get ();
		}
		
		private void UpdateTitleVisibility ()
		{
			if (clutter_flow_contents != null)
				clutter_flow_contents.FilterView.LabelTrackIsVisible = 
					ClutterFlowSchemas.DisplayTitle.Get ();
		}
		
		private void UpdateVisibleCovers ()
		{
			if (clutter_flow_contents!=null)
				clutter_flow_contents.FilterView.CoverManager.VisibleCovers =
					((ClutterFlowSchemas.VisibleCovers.Get () + 1) * 2 + 1);
		}
		
		private void UpdateMinCoverSize ()
		{
			if (clutter_flow_contents != null)
				clutter_flow_contents.FilterView.CoverManager.MinCoverWidth = 
					ClutterFlowSchemas.MinCoverSize.Get ();
		}

		private void UpdateMaxCoverSize ()
		{
			if (clutter_flow_contents != null)
				clutter_flow_contents.FilterView.CoverManager.MaxCoverWidth = 
					ClutterFlowSchemas.MaxCoverSize.Get ();
		}

		private void UpdateTextureSize ()
		{
			if (clutter_flow_contents != null)
				clutter_flow_contents.FilterView.CoverManager.TextureSize = 
					ClutterFlowSchemas.TextureSize.Get ();
		}
		
        private void UninstallPreferences ()
        {
            pref_page = null;
            general = null;
			dimensions = null;
			pref_installed = false;
        }
		#endregion

		public void Dispose ()
		{
			UninstallPreferences ();
			RemoveClutterFlow ();

			//if they were still setting up:
			ServiceManager.ServiceStarted -= OnServiceStarted;
			source_manager.SourceAdded -= OnSourceAdded;
		}
	}
}
