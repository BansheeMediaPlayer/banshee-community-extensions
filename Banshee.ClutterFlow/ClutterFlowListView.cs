
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

namespace Banshee.ClutterFlow
{
	
	public delegate void ForEachCover(CoverGroup o);
	
	public class ClutterFlowListView : Clutter.Embed
	{
		
		public event EventHandler UpdatedAlbum;
		
		public AlbumInfo CurrentAlbum {
			get { return coverManager.CurrentAlbum; }
		}
		
		protected CoverManager coverManager = new CoverManager();
		public CoverManager CoverManager {
			get { return coverManager; }
		}
		
        public virtual IListModel<AlbumInfo> Model {
            get { return coverManager.Model; }
        }
		
        public void SetModel (IListModel<AlbumInfo> model)
        {
            SetModel(model, 0.0);
        }

        public void SetModel (IListModel<AlbumInfo> value, double vpos)
        {
            coverManager.Model = value;
        }

		public bool Enabled {
			get { return coverManager!=null ? coverManager.Enabled : false; }
			set { coverManager.Enabled  = value; }
		}
		
		private bool dragging = false;			// wether or not we are currently dragging the viewport around
		private double mouse_x, mouse_y;
		
		private ClutterFlowSlider slider;
		private CoverCaption caption;
		
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
        public ClutterFlowListView () : base ()
        {
			SetSizeRequest (500, 300);
			Clutter.Global.MotionEventsEnabled = true;
			
			Stage.AllocationChanged += HandleAllocationChanged;
			Stage.ScrollEvent += HandleScroll;
			Stage.ButtonReleaseEvent += HandleButtonReleaseEvent;
			Stage.MotionEvent += HandleMotionEvent;
			
			SetupViewport();
			SetupSlider();
			SetupAlbumText();
		}

		protected void SetupViewport() {
			Stage.Color = new Clutter.Color (0x00, 0x00, 0x00, 0xff);
			
			coverManager.SetRotation(RotateAxis.X, viewportAngleX, Stage.Width/2, Stage.Height/2,0);
			Stage.Add(coverManager);
			coverManager.LowerBottom();
		}
		
		protected void SetupSlider() {
			slider = new ClutterFlowSlider(Stage.Width, Stage.Height, coverManager);
			Stage.Add(slider);
		}
		
		protected void SetupAlbumText() {
			caption = new CoverCaption(coverManager, "Sans Bold 10", new Clutter.Color(1.0f,1.0f,1.0f,1.0f));
			Stage.Add(caption);
		}
		#endregion
		
		#region Rendering
		//Update all elements:
		protected void RedrawInterface () 
		{
			slider.Update ();
			caption.Update ();
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
			if (args.Event.Button==1 && !dragging  && coverManager.CurrentCover!=null && UpdatedAlbum!=null) 
				UpdatedAlbum (coverManager.CurrentCover.CreateClickClone(), EventArgs.Empty);
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
