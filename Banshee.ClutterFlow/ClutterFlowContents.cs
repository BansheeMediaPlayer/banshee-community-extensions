//
// CompositeTrackSourceContents.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2007 Novell, Inc.
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
using System.Reflection;
using System.Collections.Generic;

using Gtk;
using Mono.Unix;

using Hyena.Data;
using Hyena.Data.Gui;
using Hyena.Widgets;

using Banshee.Sources.Gui;
using Banshee.Sources;
using Banshee.ServiceStack;
using Banshee.Collection;
using Banshee.Library;
using Banshee.Gui;
using Banshee.Collection.Gui;

using ScrolledWindow=Gtk.ScrolledWindow;

namespace Banshee.ClutterFlow
{

    public class ClutterFlowContents : VBox
    {

		private string name;
		
		private Paned container;
		private Hyena.Widgets.RoundedFrame frame;
        private ClutterFlowWidget filter_view;
		private Gtk.ScrolledWindow main_scrolled_window;
        private TrackListView main_view;

        public ClutterFlowContents ()
        {
			name = "ClutterFlowView";
            InitializeViews ();		
			Layout ();
            NoShowAll = true;
        }
        
        public override void Dispose ()
        {
			Clutter.Application.Quit();
			if (filter_view != null) {
				filter_view.Destroy();
				filter_view.Dispose();
                filter_view = null;
            }
        }
		
        protected void InitializeViews ()
        {
            SetupMainView (main_view = new TrackListView ());
			SetupFilterView ();
        }

        protected void SetupMainView (TrackListView main_view)
        {
            this.main_view = main_view;
            main_scrolled_window = SetupView (main_view);
			main_view.HeaderVisible = true;
        }

        protected void SetupFilterView ()
        {
			if (!GLib.Thread.Supported) GLib.Thread.Init();
			/*Gdk.Threads.Init();*/
			Clutter.Threads.Init();
			Clutter.Application.InitForToolkit();
			Clutter.Application.Init();
			filter_view = new ClutterFlowWidget ();
        }
		
        private ScrolledWindow SetupView (Widget view)
        {
            ScrolledWindow window = null;

            if (Banshee.Base.ApplicationContext.CommandLine.Contains ("smooth-scroll")) {
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
            // The main container gets destroyed since it will be recreated.
            if (container != null) {
				if (frame != null) container.Remove (frame);
				if (main_scrolled_window != null) container.Remove (main_scrolled_window);	
                Remove (container);
            }
        }

		public ClutterFlowWidget FilterView {
			get { return filter_view; }
		}

		public void UndoFullscreenReparent ()
		{
            if (filter_view.Parent != frame) {
				if (filter_view.Parent != null)
					(filter_view.Parent as Container).Remove (filter_view);
				frame.Add (filter_view);
			}
			is_fullscreen = false;
			ShowPack ();
		}
		
		public void FullscreenReparent (Widget new_parent)
		{
            if (filter_view.Parent != new_parent) {
                filter_view.Reparent (new_parent);
                filter_view.Show ();
            }
			is_fullscreen = true;
		}

		protected bool is_fullscreen = false;
		public bool IsFullscreen {
			get { return is_fullscreen; }
		}

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

        protected bool ActiveSourceCanHasBrowser {
            get {
                if (!(ServiceManager.SourceManager.ActiveSource is ITrackModelSource)) {
                    return false;
                }
                
                return ((ITrackModelSource)ServiceManager.SourceManager.ActiveSource).ShowBrowser;
            }
        }

        public TrackListView TrackView {
            get { return main_view; }
        }

    }
}
