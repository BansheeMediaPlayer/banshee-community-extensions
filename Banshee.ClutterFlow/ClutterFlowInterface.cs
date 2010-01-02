
using System;

using Gtk;

using Hyena.Data;
using Hyena.Data.Gui;

using Banshee.ServiceStack;
using Banshee.PlatformServices;
using Banshee.Gui;
using Banshee.Sources;
using Banshee.Sources.Gui;
using Banshee.Collection;
using Banshee.NowPlaying;

namespace Banshee.ClutterFlow
{
	
	
	public class ClutterFlowInterface : VBox, ISourceContents, ITrackModelSourceContents
	{

        private Gtk.Window video_window;
		private ClutterFlowContents contents;
        private FullscreenAdapter fullscreen_adapter;
        private ScreensaverManager screensaver;

		
		public ClutterFlowInterface()
		{

            GtkElementsService service = ServiceManager.Get<GtkElementsService> ();
            
            contents = new ClutterFlowContents ();
			
            video_window = new FullscreenWindow (service.PrimaryWindow);
            video_window.Hidden += OnFullscreenWindowHidden;
            video_window.Realize ();         
            
            PackStart (contents, true, true, 0);
			
            fullscreen_adapter = new FullscreenAdapter ();
            screensaver = new ScreensaverManager ();

			FilterView.FSButton.Toggled += HandleFSButtonToggled;
			
			ShowAll();
		}
		
        private void MoveVideoExternal (bool hidden)
        {
			contents.FullscreenReparent (video_window);
			if (hidden)
				video_window.Hide();
			else
				video_window.Show();
        }
        
        private void MoveVideoInternal ()
        {
			contents.UndoFullscreenReparent ();
        }

#region Video Fullscreen Override

        private ViewActions.FullscreenHandler previous_fullscreen_handler;

		public bool IsFullscreen {
			get { return contents.IsFullscreen; }
		}
		
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
		private void HandleFSButtonToggled(object sender, EventArgs e)
		{
			FullscreenHandler (FilterView.FSButton.IsActive);
		}
        
#endregion

#region Implement ISourceContents

        protected ClutterFlowSource source;
		
        public bool SetSource (ISource source)
        {
			this.source = source as ClutterFlowSource;
			if (this.source == null) {
				return false;
			}			
			contents.FilterView.SetModel (this.source.AlbumModel);
			contents.TrackView.SetModel (this.source.TrackModel);
			
			return true;
        }

        public void ResetSource ()
        {
            source = null;
            contents.TrackView.SetModel (null);
			contents.FilterView.SetModel (null);
        }		
		
        public ISource Source {
            get { return source; }
        }

        public Widget Widget {
            get { return this; }
        }

#endregion

#region Implement ITrackModelSourceContents
		IListView<TrackInfo> ITrackModelSourceContents.TrackView {
            get { return contents.TrackView; }
        }
#endregion

		public ClutterFlowWidget FilterView {
			get { return contents.FilterView; }
		}
	}
}
