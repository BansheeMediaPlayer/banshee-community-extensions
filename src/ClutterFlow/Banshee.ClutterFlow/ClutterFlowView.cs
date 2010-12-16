//
// ClutterFlowView.cs
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
using System.Collections.Generic;

using Banshee.Gui;
using Banshee.ServiceStack;
using Banshee.Collection;
using Hyena.Data;
using Hyena.Gui;

using Gdk;
using Gtk;
using Cairo;
using Pango;
using Clutter;
using ClutterFlow;
using ClutterFlow.Captions;
using ClutterFlow.Slider;
using ClutterFlow.Alphabet;

using Banshee.ClutterFlow.Buttons;

namespace Banshee.ClutterFlow
{
	
	public class ClutterFlowView : Clutter.Embed
	{
        #region Fields
        #region Active/Current Album Related
		public event EventHandler UpdatedAlbum;

		private AlbumInfo activeAlbum = null;
		public AlbumInfo ActiveAlbum {
			get { return activeAlbum; }
			protected set { activeAlbum = value; }
		}		
		private int activeIndex = -1;
		public int ActiveIndex {
			get { return activeIndex; }
			protected set { activeIndex = value; }
		}
		public int ActiveModelIndex {
			get {
				int ret = AlbumLoader.ConvertIndexToModelIndex (ActiveIndex);
				Hyena.Log.DebugFormat ("ActiveModelIndex_get will return {0}", ret);
				return ret;
			}
		}		
		public AlbumInfo CurrentAlbum {
			get { return albumLoader.CurrentAlbum; }
		}
		public int CurrentIndex {
			get { return albumLoader.CurrentIndex; }
		}
		public int CurrentModelIndex {
			get { return AlbumLoader.ConvertIndexToModelIndex (CurrentIndex); }
		}		
        #endregion

        #region General
		private AlbumLoader albumLoader;
		public AlbumLoader AlbumLoader {
			get { return albumLoader; }
		}
		private CoverManager coverManager;
		public CoverManager CoverManager {
			get { return coverManager; }
		}
		
        public virtual IListModel<AlbumInfo> Model {
            get { return albumLoader.Model; }
        }

        public void SetModel (FilterListModel<AlbumInfo> value)
        {
            albumLoader.Model = value;
        }
        protected bool attached = false;
        public bool Attached {
            get { return attached; }
        }
        #endregion

        #region User Interface & Interaction
		private bool dragging = false;			// wether or not we are currently dragging the viewport around
		private double mouse_x, mouse_y;
		private float drag_x0, drag_y0;		// initial coordinates when the mouse button was pressed down
		private int start_index;
		
		private float drag_sens = 0.3f;
		public float DragSensitivity {
			get { return drag_sens; }
			set {
				if (value<0.01f) value = 0.01f;
				if (value>2.0f) value = 2.0f;
				drag_sens = value;
			}
		}
		
		
		private ClutterFlowSlider slider;
		public ClutterFlowSlider Slider {
			get { return slider;	}
		}
		private ClutterWidgetBar widget_bar;
		public ClutterWidgetBar WidgetBar {
			get { return widget_bar;	}
		}
		private PartyModeButton pm_button;
		public PartyModeButton PMButton {
			get { return pm_button;	}
		}
		private FullscreenButton fs_button;
		public FullscreenButton FSButton {
			get { return fs_button; }
		}
		private SortButton sort_button;
		public SortButton SortButton {
			get { return sort_button; }
		}		
		private CoverCaption caption_cover;
		public CoverCaption LabelCover {
			get { return caption_cover;	}
		}
		private TrackCaption caption_track;
		public TrackCaption LabelTrack {
			get { return caption_track;	}
		}
		
		public bool LabelCoverIsVisible {
			set { if (value) caption_cover.ShowAll(); else caption_cover.HideAll(); }
		}
		public bool LabelTrackIsVisible {
			set { if (value) caption_track.ShowAll(); else caption_track.HideAll(); }
		}

		
		private const float rotSens = 0.00001f;
		private const float viewportMaxAngleX = 10; // maximum X viewport angle
		private const float viewportMinAngleX = -30; // maximum X viewport angle
		private const float viewportMaxAngleY = -15; // maximum Y viewport angle
		private float viewportAngleX = -5f;			 // current X viewport angle
		public float ViewportAngleX {
			get { return viewportAngleX; }
			set {
				if (value!=viewportAngleX) {
					viewportAngleX = value;
					if (viewportAngleX < viewportMinAngleX) viewportAngleX = viewportMinAngleX;
					if (viewportAngleX > viewportMaxAngleX) viewportAngleX = viewportMaxAngleX;

					if (viewportAngleX < -1f && viewportAngleX > -9f)
						coverManager.SetRotation(RotateAxis.Y, -5f, coverManager.Width*0.5f,coverManager.Height*0.5f,coverManager.Behaviour.ZFar);
					else
						coverManager.SetRotation(RotateAxis.X, viewportAngleX, coverManager.Width*0.5f,coverManager.Height*0.5f,coverManager.Behaviour.ZFar);
					
				}
			}
		}
		private float viewportAngleY = 0;			// current Y viewport angle
		public float ViewportAngleY {
			get { return viewportAngleY; }
			set {
				if (value!=viewportAngleY) {
					viewportAngleY = value;
					if (viewportAngleY < viewportMaxAngleY) viewportAngleY = viewportMaxAngleY;
					if (viewportAngleY > -viewportMaxAngleY) viewportAngleY = -viewportMaxAngleY;

					if (viewportAngleY > -4f && viewportAngleY < 4f)
						coverManager.SetRotation(RotateAxis.Y, 0, coverManager.Width*0.5f,coverManager.Height*0.5f,coverManager.Behaviour.ZFar);
					else
						coverManager.SetRotation(RotateAxis.Y, viewportAngleY, coverManager.Width*0.5f,coverManager.Height*0.5f,coverManager.Behaviour.ZFar);
				}
			}
		}
        #endregion
        #endregion

		#region Initialisation
        public ClutterFlowView () : base ()
        {
			SetSizeRequest (300, 200);
			Clutter.Global.MotionEventsEnabled = true;

			coverManager = new CoverManager();
			albumLoader = new AlbumLoader (coverManager);

            AttachEvents ();
			
			SetupViewport ();
			SetupSlider ();
			SetupLabels ();
			SetupWidgetBar ();
		}

        public void AttachEvents ()
        {
            if (attached)
                return;
            attached = true;

            Stage.AllocationChanged += HandleAllocationChanged;
            Stage.ScrollEvent += HandleScroll;
            Stage.ButtonReleaseEvent += HandleButtonReleaseEvent;
            Stage.ButtonPressEvent += HandleButtonPressEvent;
            Stage.MotionEvent += HandleMotionEvent;
			albumLoader.ActorActivated += HandleActorActivated;
        }

        public void DetachEvents ()
        {
            if (!attached)
                return;

            Stage.AllocationChanged -= HandleAllocationChanged;
            Stage.ScrollEvent -= HandleScroll;
            Stage.ButtonReleaseEvent -= HandleButtonReleaseEvent;
            Stage.ButtonPressEvent -= HandleButtonPressEvent;
            Stage.MotionEvent -= HandleMotionEvent;
			albumLoader.ActorActivated -= HandleActorActivated;

            attached = false;
        }

        protected bool disposed = false;
        public override void Dispose ()
        {
            if (disposed)
                return;
            disposed = true;

            DetachEvents ();
            AlbumLoader.Dispose ();
            CoverManager.Dispose ();

            //base.Dispose ();
        }


		protected void SetupViewport ()
		{
			Stage.Color = new Clutter.Color (0x00, 0x00, 0x00, 0xff);
			coverManager.SetRotation (RotateAxis.X, viewportAngleX, Stage.Width/2, Stage.Height/2,0);
			Stage.Add (coverManager);

			
			
			coverManager.EmptyActor.SetToPb(
	            IconThemeUtils.LoadIcon (coverManager.TextureSize, "gtk-stop", "clutterflow-large.png")
            );
            CoverManager.DoubleClickTime = (uint) Gtk.Settings.GetForScreen(this.Screen).DoubleClickTime;
			coverManager.LowerBottom ();
			coverManager.Show ();
		}
		
		protected void SetupSlider ()
		{
			slider = new ClutterFlowSlider (400, 40, coverManager);
			Stage.Add (slider);
		}	
		
		protected void SetupLabels () {
			caption_cover = new CoverCaption (coverManager, "Sans Bold 10", new Clutter.Color(1.0f,1.0f,1.0f,1.0f));
			Stage.Add (caption_cover);

			caption_track = new TrackCaption (coverManager, "Sans Bold 10", new Clutter.Color(1.0f,1.0f,1.0f,1.0f));
			Stage.Add (caption_track);
		}

		protected void SetupWidgetBar ()
		{
			pm_button = new PartyModeButton ();
			fs_button = new FullscreenButton ();
			sort_button = new SortButton ();
			
			widget_bar = new ClutterWidgetBar (new Actor[] { pm_button, fs_button, sort_button });
			widget_bar.ShowAll ();
			Stage.Add (widget_bar);
			widget_bar.SetPosition (5, 5);
		}
		#endregion
		
		#region Rendering
		//Update all elements:
		protected void RedrawInterface ()
		{
			slider.Update ();
			caption_cover.Update ();
			caption_track.Update ();

			widget_bar.SetPosition (5, 5);
			RedrawViewport ();
		}
		
		//Update the coverStage position:
		protected void RedrawViewport ()
		{
			coverManager.UpdateBehaviour();
			coverManager.SetRotation(RotateAxis.X, viewportAngleX, coverManager.Width*0.5f, coverManager.Height*0.5f,0);
			if (!coverManager.IsVisible) coverManager.Show();
			coverManager.LowerBottom();
		}
		#endregion
		
		#region Event Handling

		protected override void OnSizeAllocated (Gdk.Rectangle allocation)
		{
			base.OnSizeAllocated (allocation);
			RedrawInterface();
		}

		
		private void HandleAllocationChanged (object o, AllocationChangedArgs args)
		{	
			RedrawInterface();
		}


        private void HandleActorActivated (ClutterFlowAlbum actor, EventArgs e)
        {
        	UpdateAlbum (actor);
        }
		
		private void HandleButtonPressEvent(object o, Clutter.ButtonPressEventArgs args)
		{
			Clutter.EventHelper.GetCoords (args.Event, out drag_x0, out drag_y0);
			args.RetVal = true;
		}
		
		void HandleButtonReleaseEvent (object o, Clutter.ButtonReleaseEventArgs args)
		{
			//if (args.Event.Button==1 && !dragging  && coverManager.CurrentCover!=null && ActiveAlbum != CurrentAlbum)
			//	UpdateAlbum ();
			if (dragging) Clutter.Ungrab.Pointer ();
			dragging = false;
			args.RetVal = true;
		}
		
		private void HandleMotionEvent (object o, Clutter.MotionEventArgs args)
		{
			if ((args.Event.ModifierState.value__ & Clutter.ModifierType.Button1Mask.value__)!=0) {
				float drag_x; float drag_y;
				Clutter.EventHelper.GetCoords (args.Event, out drag_x, out drag_y);
				if (!dragging) {
					if (Math.Abs(drag_x0 - drag_x) > 2 && Math.Abs(drag_y0 - drag_y) > 2) {
						start_index = CoverManager.TargetIndex;
						Clutter.Grab.Pointer (Stage);
						dragging = true;
					}
				} else {
					if ((args.Event.ModifierState.value__ & Clutter.ModifierType.ControlMask.value__)!=0) {
						if (!dragging) Clutter.Grab.Pointer (Stage);
						ViewportAngleY += (float) (mouse_x - args.Event.X)*rotSens;
						ViewportAngleX += (float) (mouse_y - args.Event.Y)*rotSens;
					} else {
						CoverManager.TargetIndex = start_index + (int) ((drag_x0 - drag_x)*drag_sens);
					}
				}
			} else {
				if (dragging) Clutter.Ungrab.Pointer ();
				dragging = false;
			}
			mouse_x = args.Event.X;
			mouse_y = args.Event.Y;
			
			args.RetVal = dragging;
		}
		
		private void HandleScroll (object o, Clutter.ScrollEventArgs args)
		{
			if (args.Event.Direction==Clutter.ScrollDirection.Down)
				Scroll(true);
			else
				Scroll(false);
		}
		
		public void Scroll (bool Backward)
		{
			if (Backward) coverManager.TargetIndex--;
			else coverManager.TargetIndex++;
		}

		public void UpdateAlbum ()
		{
			UpdateAlbum (albumLoader.CurrentActor);
		}
		
		public void UpdateAlbum (ClutterFlowAlbum actor)
		{
			ActiveAlbum = actor.Album;
			ActiveIndex = actor.Index;
			if (UpdatedAlbum!=null)	UpdatedAlbum (ActiveAlbum, EventArgs.Empty);
		}
		#endregion
		
	}	
}
