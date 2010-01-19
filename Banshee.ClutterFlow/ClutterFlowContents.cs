// 
// ClutterFlowContents.cs
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
using System.Reflection;
using System.Collections.Generic;

using Gtk;
using Mono.Unix;

using Hyena;
using Hyena.Data;
using Hyena.Data.Gui;
using Hyena.Widgets;

using Banshee.ServiceStack;
using Banshee.PlatformServices;
using Banshee.Gui;
using Banshee.Sources.Gui;
using Banshee.Sources;
using Banshee.Collection;
using Banshee.Collection.Gui;
using Banshee.Collection.Database;
using Banshee.Library;
using Banshee.NowPlaying;

using ScrolledWindow=Gtk.ScrolledWindow;

namespace Banshee.ClutterFlow
{

    public class ClutterFlowContents : VBox, ISourceContents, ITrackModelSourceContents
    {
		#region Fields

		//WIDGETS:
		private Paned container;
		public Paned Container {
			get { return container; }
		}
        
		private Hyena.Widgets.RoundedFrame frame;
		private ClutterFlowView filter_view;
		public ClutterFlowView FilterView {
			get { return filter_view; }
		}

		private Gtk.ScrolledWindow main_scrolled_window;
		private TrackListView main_view;
        public TrackListView TrackView {
            get { return main_view; }
        }
		IListView<TrackInfo> ITrackModelSourceContents.TrackView {
            get { return TrackView; }
        }

        public Widget Widget {
            get { return this; }
        }

		//FULLSCREEN HANDLING:
        private Gtk.Window video_window;
        private FullscreenAdapter fullscreen_adapter;
        private ScreensaverManager screensaver;
		
		//SOURCE, TRACKMODEL & (ALBUM) FILTERS:
        protected MusicLibrarySource source;
        public ISource Source {
            get { return source; }
        }
		
        public TrackListModel TrackModel {
            get { return source.TrackModel; }
        }

		protected bool ignore_reload = true;
		
		protected FilterListModel<AlbumInfo> external_filter; //this is actually tied to the MusicLibrary
		protected DatabaseAlbumListModel internal_filter;	//this is a duplicate not added using AddFilter

		//PLAYBACK RELATED:
		private TrackInfo previous_track;
		
		private bool IsActiveSource {
			get { return ServiceManager.SourceManager.ActiveSource == source; }
		}
		private bool IsPlaybackSource {
			get { return ServiceManager.PlaybackController.Source == source; }
		}
		private bool InPlaybackMode {
			get {
				return (IsActiveSource && IsPlaybackSource);
			}
		}
		private bool InPartyMode {
			get {
				return (InPlaybackMode && external_filter.Selection.AllSelected);
			}
		}

		//GENERIC:
		private string name;
		#endregion
		
		#region Initialising
        public ClutterFlowContents ()
        {
			name = "ClutterFlowView";
            InitializeInterface ();		
			Layout ();
			SetupFullscreenHandling ();
			SetupPlaybackHandling ();
            NoShowAll = true;
        }
        
        public override void Dispose ()
        {
			Clutter.Application.Quit();
			if (filter_view != null) {
				filter_view.Dispose();
                filter_view = null;
            }
        }
		#endregion

		#region Packing & Resetting
        private void Layout ()
        {
            Reset ();
            
            container = new VPaned ();

            frame = new Hyena.Widgets.RoundedFrame ();
            frame.SetFillColor (new Cairo.Color (0, 0, 0));
            frame.DrawBorder = false;
			frame.Add (filter_view);
			filter_view.Show();
            frame.Show ();

			container.Pack1 (frame, false, false);			
            container.Pack2 (main_scrolled_window, true, false);

            container.Position = 175;
            PersistentPaneController.Control (container, ControllerName (-1));
			
            ShowPack ();
        }
		
        private string ControllerName (int filter)
        {
            if (filter == -1)
                return String.Format ("{0}.browser.{1}", name, "top");
            else
                return String.Format ("{0}.browser.{1}.{2}", name, "top", filter);
        }
        
        private void ShowPack ()
        {
            PackStart (container, true, true, 0);
            NoShowAll = false;
            ShowAll ();
            NoShowAll = true;
        }

        private void Reset ()
        {
            // The main container gets destroyed since it will be recreated.
            if (container != null) {
				if (frame != null) container.Remove (frame);
				if (main_scrolled_window != null) container.Remove (main_scrolled_window);	
                Remove (container);
            }
        }
		#endregion
		
		#region View Setup
        protected void InitializeInterface ()
        {
            SetupMainView ();
			SetupFilterView ();
        }

        protected void SetupMainView ()
        {
            main_view = new TrackListView ();
            main_scrolled_window = CreateScrollable (main_view);
			main_view.HeaderVisible = true;
        }

        protected void SetupFilterView ()
        {
			if (!GLib.Thread.Supported) GLib.Thread.Init();
			/*Gdk.Threads.Init(); FIXME causes weird lockups */
			Clutter.Threads.Init();
			Clutter.Application.InitForToolkit();
			Clutter.Application.Init();
			
			filter_view = new ClutterFlowView ();
			filter_view.FSButton.IsActive = IsFullscreen;
			filter_view.PMButton.IsActive = InPartyMode;
			FilterView.LabelTrackIsVisible = ClutterFlowSchemas.DisplayTitle.Get () && IsFullscreen;
        }
		
        private ScrolledWindow CreateScrollable (Widget view)
        {
			//TODO make this a collabsable thing so the ClutterFlow widget can be bigger, while not fullscreen
            ScrolledWindow window = null;

            // See revision http://git.gnome.org/browse/banshee/commit/?id=2b7789ea563319fa7196e50b76b2b561d699fd71
            //if (Banshee.Base.ApplicationContext.CommandLine.Contains ("smooth-scroll")) {
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
		#endregion

		#region Fullscreen Handling

		protected void SetupFullscreenHandling ()
		{
			GtkElementsService service = ServiceManager.Get<GtkElementsService> ();
			
            fullscreen_adapter = new FullscreenAdapter ();
            screensaver = new ScreensaverManager ();
			
            video_window = new FullscreenWindow (service.PrimaryWindow);
            video_window.Hidden += OnFullscreenWindowHidden;
            video_window.Realize ();
		}
		
		protected override void OnMapped ()
		{
			OverrideFullscreen ();
			base.OnMapped ();
		}

		protected override void OnUnmapped ()
		{
			RelinquishFullscreen ();
			base.OnUnmapped ();
		}

#region Video Fullscreen Override

        private ViewActions.FullscreenHandler previous_fullscreen_handler;
		
        private void DisableFullscreenAction ()
        {
            InterfaceActionService service = ServiceManager.Get<InterfaceActionService> ();
            Gtk.ToggleAction action = service.ViewActions["FullScreenAction"] as Gtk.ToggleAction;
            if (action != null) {
                action.Active = false;
            }
        }

        internal void OverrideFullscreen ()
        {
            FullscreenHandler (false);
            
            InterfaceActionService service = ServiceManager.Get<InterfaceActionService> (); 
            if (service == null || service.ViewActions == null) {
                return;
            }
            
            previous_fullscreen_handler = service.ViewActions.Fullscreen;
            service.ViewActions.Fullscreen = FullscreenHandler;
            DisableFullscreenAction ();
        }

        internal void RelinquishFullscreen ()
        {
            FullscreenHandler (false);
            
            InterfaceActionService service = ServiceManager.Get<InterfaceActionService> (); 
            if (service == null || service.ViewActions == null) {
                return;
            }
            
            service.ViewActions.Fullscreen = previous_fullscreen_handler;
        }
	
        private void OnFullscreenWindowHidden (object o, EventArgs args)
        {
            MoveVideoInternal ();
            DisableFullscreenAction ();
        }

		private bool handlingFullScreen = false;
        private void FullscreenHandler (bool fullscreen)
        {
			if (!handlingFullScreen) {
				handlingFullScreen = true;
				FilterView.FSButton.IsActive = fullscreen;
				FilterView.LabelTrackIsVisible = ClutterFlowSchemas.DisplayTitle.Get () && fullscreen;
	            if (fullscreen) {
	                MoveVideoExternal (true);
	                video_window.Show ();
	                fullscreen_adapter.Fullscreen (video_window, true);
	                screensaver.Inhibit ();
	            } else {
	                video_window.Hide ();
	                screensaver.UnInhibit ();
	                fullscreen_adapter.Fullscreen (video_window, false);
	                video_window.Hide ();
	            }
				handlingFullScreen = false;
			}
        }
        
#endregion

        private void MoveVideoExternal (bool hidden)
        {
            if (filter_view.Parent != video_window) {
                filter_view.Reparent (video_window);
                filter_view.Show ();
            }
			is_fullscreen = true;
			if (hidden)
				video_window.Hide();
			else
				video_window.Show();
        }
        
        private void MoveVideoInternal ()
        {
            if (filter_view.Parent != frame) {
				if (filter_view.Parent != null)
					(filter_view.Parent as Container).Remove (filter_view);
				frame.Add (filter_view);
				frame.QueueDraw ();
			}
			is_fullscreen = false;
			ShowPack ();
			
        }

		protected bool is_fullscreen = false;
		public bool IsFullscreen {
			get { return is_fullscreen; }
		}
		#endregion

		#region Playback Handling
		
		private void SetupPlaybackHandling ()
		{
			FilterView.UpdatedAlbum += HandleUpdatedAlbum;
			FilterView.PMButton.Toggled += HandlePMButtonToggled;
			FilterView.FSButton.Toggled += HandleFSButtonToggled;

			ServiceManager.SourceManager.ActiveSourceChanged += HandleActiveSourceChanged;
            ServiceManager.PlaybackController.TrackStarted += OnPlaybackControllerTrackStarted;
			ServiceManager.PlaybackController.SourceChanged += OnPlaybackSourceChanged;

		}
		
		private void HandleUpdatedAlbum(object sender, EventArgs e)
		{
			if (!IsActiveSource) ServiceManager.SourceManager.SetActiveSource (source);
			SelectActiveAlbum ();
			UpdatePlayback ();
			FilterView.PMButton.SetSilent (InPartyMode);
		}

		private void HandlePMButtonToggled(object sender, EventArgs e)
		{
			if (!IsActiveSource) ServiceManager.SourceManager.SetActiveSource (source);
			if (FilterView.PMButton.IsActive) {
				SelectAllTracks ();
				UpdatePlayback ();
			} else
				FilterView.UpdateAlbum ();
		}

		private void HandleFSButtonToggled(object sender, EventArgs e)
		{
            InterfaceActionService service = ServiceManager.Get<InterfaceActionService> (); 
            if (service == null || service.ViewActions == null)
                return;
			service.ViewActions.Fullscreen (FilterView.FSButton.IsActive);
		}
		
		private void HandleActiveSourceChanged (SourceEventArgs args)
		{
			FilterView.PMButton.SetSilent (InPartyMode);
		}
        private void OnPlaybackControllerTrackStarted (object o, EventArgs args)
        {			
            CheckForSwitch ();
        }
        private void OnPlaybackSourceChanged (object o, EventArgs args)
        {
			FilterView.PMButton.SetSilent (InPartyMode);
        }
		
		/// <summary>
		/// Checks if we are in PartyMode & if a new song started playing
		/// Called from OnPlaybackControllerTrackStarted
		/// </summary>
        private void CheckForSwitch ()
        {
            TrackInfo current_track = ServiceManager.PlaybackController.CurrentTrack;
            if (current_track != null && previous_track != current_track) {
				if (IsActiveSource)
					FilterView.LabelTrack.SetValueWithAnim (current_track.TrackNumber + " - " + current_track.TrackTitle);
				if (InPartyMode) {
					DatabaseAlbumInfo album = DatabaseAlbumInfo.FindOrCreate (
							DatabaseArtistInfo.FindOrCreate (current_track.AlbumArtist, current_track.AlbumArtistSort),
							current_track.AlbumTitle, current_track.AlbumTitleSort, current_track.IsCompilation);
					FilterView.AlbumLoader.ScrollTo(album);
				}
            }
			previous_track = current_track;
        }
		
		private void UpdatePlayback ()
		{
			if (!ClutterFlowSchemas.InstantPlayback.Get ()) {
				ServiceManager.PlaybackController.NextSource = source;
				if (!ServiceManager.PlayerEngine.IsPlaying()) 
					ServiceManager.PlayerEngine.Play();
			} else {
				ServiceManager.PlaybackController.Source = source;
				if (!ServiceManager.PlayerEngine.IsPlaying()) 
					ServiceManager.PlayerEngine.Play();
				else
					ServiceManager.PlaybackController.Next();
			}
			//source.OnUpdated (); FIXME
		}
		#endregion

		#region Source Handling		
        public bool SetSource (ISource source)
        {
			if ((source as MusicLibrarySource) == null)
				return false;
			if ((source as MusicLibrarySource)==this.source)
				return true;
			else
				ResetSource ();
			
			this.source = (source as MusicLibrarySource);
			this.source.TrackModel.Selection.Clear (false);
			this.source.TracksAdded += HandleTracksAdded;
			this.source.TracksDeleted += HandleTracksDeleted;

			internal_filter = new DatabaseAlbumListModel(this.source, (DatabaseTrackListModel) this.source.TrackModel, ServiceManager.DbConnection, this.source.UniqueId);
			internal_filter.Reload (false);
			
			foreach (IFilterListModel list_model in this.source.CurrentFilters) {
				list_model.Clear (); //clear selections, we need all albums!!
				if (list_model is FilterListModel<AlbumInfo>) {
					external_filter = list_model as FilterListModel<AlbumInfo>;
					break;
				}
			}
			
			TrackView.SetModel (TrackModel);
			FilterView.SetModel (internal_filter);

			TrackModel.Reloaded += HandleModelReload;
			
			return true;
        }

		private void HandleTracksAdded (Source sender, TrackEventArgs args)
		{
			SelectAllTracks ();
			internal_filter.Reload (false);
		}

		private void HandleTracksDeleted (Source sender, TrackEventArgs args)
		{
			SelectAllTracks ();
			internal_filter.Reload (false);
		}
		
		private void HandleModelReload (object o, EventArgs args)
		{
			internal_filter.Reload (true);
			TrackModel.Reloaded -= HandleModelReload;
		}

        public void ResetSource ()
        {
			if (source!=null) {
				TrackModel.Reloaded -= HandleModelReload;
	            source = null;
			}
            TrackView.SetModel (null);
			FilterView.SetModel (null);
        }

        protected void SelectActiveAlbum () // to implement sorting: create a DatabaseAlbumListModel subclass
        {
			AlbumInfo album = FilterView.ActiveAlbum;
			if (album!=null) {
				external_filter.Selection.Clear (false);
				external_filter.Selection.Select (FilterView.ActiveIndex+1);
			}
        }
		protected void SelectAllTracks () 
		{
			if (!external_filter.Selection.AllSelected) {
				external_filter.Selection.SelectAll ();
			}
		}

		// TODO: FilterQueries etc. now need to be set on the TrackListModel inside ClutterFlowContents
		// this should invoke a reload event on the internal_filter also so that it displays the correct albums
		
        /*public override string FilterQuery {
            get { return (clutter_flow_interface.TrackModel as DatabaseTrackListModel).UserQuery / *Parent.Properties.Get<string> ("FilterQuery")* /; }
            set {
				(clutter_flow_interface.TrackModel as DatabaseTrackListModel).UserQuery = value;
				clutter_flow_interface.TrackModel.Reload();
			}
        }
        
        new public TrackFilterType FilterType {
            get { return (TrackFilterType) clutter_flow_interface.Source.Properties.GetInteger ("FilterType"); }
            set { 
				clutter_flow_interface.Source.Properties.SetInteger ("FilterType", (int) value);
				clutter_flow_interface.TrackModel.Reload();
			}
        }*/
		
		#endregion
    }
}
