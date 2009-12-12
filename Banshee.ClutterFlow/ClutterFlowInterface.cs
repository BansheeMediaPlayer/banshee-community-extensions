
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

			ShowAll();
		}

        private void MoveVideoExternal (bool hidden)
        {
			contents.FullscreenReparent (video_window);
        }
        
        private void MoveVideoInternal ()
        {
			contents.UndoFullscreenReparent ();
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

        private void FullscreenHandler (bool fullscreen)
        {
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
        }
        
#endregion

#region Implement ISourceContents

        protected ITrackModelSource source;
		
        public bool SetSource (ISource source)
        {
			this.source = source as ITrackModelSource;
			if (this.source == null) {
				return false;
			}			
			
            IFilterableSource filterable_source = ServiceManager.SourceManager.MusicLibrary as IFilterableSource;
            if (filterable_source == null) {
            	return false;
            }
            if (filterable_source.CurrentFilters != null) {
                foreach (IListModel model in filterable_source.CurrentFilters) {
                    if (model is IListModel<AlbumInfo>) {
                        contents.FilterView.SetModel (model as IListModel<AlbumInfo>);
					}
                }
			}

			contents.TrackView.SetModel (this.source.TrackModel);
			
			return true;
        }

        public void ResetSource ()
        {
            source = null;
            contents.TrackView.SetModel (null);
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
