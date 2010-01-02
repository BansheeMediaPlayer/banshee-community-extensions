
using System;
using System.Collections.Generic;

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

namespace Banshee.ClutterFlow
{
	
	public class ClutterFlowWidget : Clutter.Embed
	{
		
		public event EventHandler UpdatedAlbum;

		private AlbumInfo activeAlbum = null;
		public AlbumInfo ActiveAlbum {
			get { return activeAlbum; }
			protected set { activeAlbum = value; }
		}
		public AlbumInfo CurrentAlbum {
			get { return albumLoader.CurrentAlbum; }
		}

		protected AlbumLoader albumLoader;
		public AlbumLoader AlbumLoader {
			get { return albumLoader; }
		}
		protected CoverManager coverManager = new CoverManager();
		public CoverManager CoverManager {
			get { return coverManager; }
		}
		
        public virtual IListModel<AlbumInfo> Model {
            get { return albumLoader.Model; }
        }
		
        public void SetModel (FilterListModel<AlbumInfo> model)
        {
            SetModel(model, 0.0);
        }

        public void SetModel (FilterListModel<AlbumInfo> value, double vpos)
        {
            albumLoader.Model = value;
        }

		public bool Enabled {
			get { return coverManager!=null ? coverManager.Enabled : false; }
			set { coverManager.Enabled  = value; }
		}
		
		private bool dragging = false;			// wether or not we are currently dragging the viewport around
		private double mouse_x, mouse_y;
		
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
		private const float viewportMaxAngleX = -30;// maximum X viewport angle
		private const float viewportMaxAngleY = -5;	// maximum Y viewport angle
		private float viewportAngleX = -5;			// current X viewport angle
		public float ViewportAngleX {
			get { return viewportAngleX; }
			set {
				if (value!=viewportAngleX) {
					viewportAngleX = value;
					if (viewportAngleX < viewportMaxAngleX) viewportAngleX = viewportMaxAngleX;
					if (viewportAngleX > 0) viewportAngleX = 0;
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
					coverManager.SetRotation(RotateAxis.Y, viewportAngleY, coverManager.Width*0.5f,coverManager.Height*0.5f,coverManager.Behaviour.ZFar);
				}
			}
		}

		#region Initialisation
        public ClutterFlowWidget () : base ()
        {
			SetSizeRequest (500, 300);
			Clutter.Global.MotionEventsEnabled = true;

			albumLoader = new AlbumLoader (coverManager);
			
			Stage.AllocationChanged += HandleAllocationChanged;
			Stage.ScrollEvent += HandleScroll;
			Stage.ButtonReleaseEvent += HandleButtonReleaseEvent;
			Stage.MotionEvent += HandleMotionEvent;
			
			SetupViewport ();
			SetupSlider ();
			SetupLabels ();
			SetupWidgetBar ();
		}

		protected void SetupViewport ()
		{
			Stage.Color = new Clutter.Color (0x00, 0x00, 0x00, 0xff);
			
			coverManager.SetRotation (RotateAxis.X, viewportAngleX, Stage.Width/2, Stage.Height/2,0);
			Stage.Add (coverManager);
			coverManager.LowerBottom ();
			coverManager.Show ();
		}
		
		protected void SetupSlider ()
		{
			slider = new ClutterFlowSlider (Stage.Width, Stage.Height, coverManager);
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
			
			widget_bar = new ClutterWidgetBar (new Actor[] { pm_button, fs_button });
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
		protected void RedrawViewport() {
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

		
		private void HandleAllocationChanged(object o, AllocationChangedArgs args)
		{	
			RedrawInterface();
		}
		
		void HandleButtonReleaseEvent(object o, Clutter.ButtonReleaseEventArgs args)
		{
			if (args.Event.Button==1 && !dragging  && coverManager.CurrentCover!=null && activeAlbum != CurrentAlbum) {
				ActiveAlbum = CurrentAlbum;
				if (UpdatedAlbum!=null)	UpdatedAlbum (coverManager.CurrentCover.CreateClickClone(), EventArgs.Empty);
			}
		}
		
		private void HandleMotionEvent(object o, Clutter.MotionEventArgs args)
		{			
			if ((args.Event.ModifierState.value__ & Clutter.ModifierType.Button1Mask.value__)!=0 && (args.Event.ModifierState.value__ & Clutter.ModifierType.ControlMask.value__)!=0) {
				dragging = true;
				ViewportAngleY += (float) (mouse_x - args.Event.X)*rotSens;
				ViewportAngleX += (float) (mouse_y - args.Event.Y)*rotSens;
			} else dragging = false;
			mouse_x = args.Event.X;
			mouse_y = args.Event.Y;
		}
		
		private void HandleScroll (object o, Clutter.ScrollEventArgs args)
		{
			if (args.Event.Direction==Clutter.ScrollDirection.Down)
				Scroll(true);
			else
				Scroll(false);
		}
		
		public void Scroll(bool Backward) {
			if (Backward) coverManager.TargetIndex--;
			else coverManager.TargetIndex++;
		}
		#endregion
		
	}	
}
