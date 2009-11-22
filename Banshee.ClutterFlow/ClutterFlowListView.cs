
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
	
	public partial class ClutterFlowListView : Clutter.Embed
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
		private Clutter.Text srf_text;
		private Animation text_fade;
		
		private const float rotSens = 0.00001f;
		private const float viewportMaxAngleX = -40;// maximum X viewport angle
		private const float viewportMaxAngleY = -5;	// maximum Y viewport angle
		private float viewportAngleX = -15;			// current X viewport angle
		public float ViewportAngleX {
			get { return viewportAngleX; }
			set {
				if (value!=viewportAngleX) {
					viewportAngleX = value;
					if (viewportAngleX < viewportMaxAngleX) viewportAngleX = viewportMaxAngleX;
					if (viewportAngleX > 0) viewportAngleX = 0;
					coverManager.SetRotation(RotateAxis.X, viewportAngleX, coverManager.Width*0.5f,coverManager.Height*0.5f,coverManager.Behaviour.ZNear);
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
					coverManager.SetRotation(RotateAxis.Y, viewportAngleY, coverManager.Width*0.5f,coverManager.Height*0.5f,coverManager.Behaviour.ZNear);
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
			
			coverManager.NewCurrentCover += HandleNewCurrentCover;
		}

		protected void SetupViewport() {
			Stage.Color = new Clutter.Color (0x00, 0x00, 0x00, 0xff);
			
			coverManager.SetRotation(RotateAxis.X, viewportAngleX, Stage.Width/2, Stage.Height/2,0);
			//coverManager.Depth = viewportOffsetZ;
			Stage.Add(coverManager);
			coverManager.LowerBottom();
		}
		
		protected void SetupSlider() {
			slider = new ClutterFlowSlider(Stage.Width, Stage.Height, coverManager);
			Stage.Add(slider);
		}
		
		protected void SetupAlbumText() {
			srf_text = new Text("Sans Bold 7.5", "Unkown Artist\nUnkown Album", new Clutter.Color(1.0f,1.0f,1.0f,1.0f));
			Stage.Add(srf_text);
			srf_text.Show();
			srf_text.Depth = 200;
			srf_text.Editable = false;
			srf_text.Selectable = false;
			srf_text.Activatable = false;
			srf_text.CursorVisible = false;
			srf_text.LineAlignment = Pango.Alignment.Center;
			RedrawAlbumText();
		}
		#endregion
		
		#region Rendering
		//Update all elements:
		protected void RedrawInterface() {
			RedrawSlider();
			RedrawAlbumText();
			RedrawViewport();
		}
		
		//Update the coverStage position:
		protected void RedrawViewport() {
			coverManager.UpdateBehaviour();
			coverManager.SetRotation(RotateAxis.X, viewportAngleX, coverManager.Width*0.5f, coverManager.Height*0.5f,0);
			if (!coverManager.IsVisible) coverManager.Show();
			coverManager.LowerBottom();
		}
		
		//Fades text out and in:
		protected void UpdateAlbumText() {
			if (text_fade==null || !text_fade.Timeline.IsPlaying) {
				text_fade = srf_text.Animatev((ulong) AnimationMode.Linear.value__, 150, new string[] { "opacity" }, new GLib.Value((byte) 0));
				text_fade.Completed += delegate(object sender, EventArgs e) {
					RedrawAlbumText();
					srf_text.Animatev((ulong) AnimationMode.Linear.value__, 150, new string[] { "opacity" }, new GLib.Value((byte) 255));
				};
			}
		}
		//Updates the text surface:
		protected void RedrawAlbumText() {
			if (CurrentAlbum!=null)
				srf_text.Value = CurrentAlbum.ArtistName + "\n" + CurrentAlbum.Title;
			else
				srf_text.Value = "Unkown Artist\nUnkown Album";
			//srf_text.SetScale(coverWidth*fontScale/srf_text.Height, coverWidth*fontScale/srf_text.Height);
			srf_text.SetAnchorPoint(srf_text.Width*0.5f, 0);
			srf_text.SetPosition(Stage.Width*0.5f, 5 + Stage.Height*0.125f);
		}

		//Redraws and positions the slider:
		protected void RedrawSlider() {
			slider.Update();
			slider.SetAnchorPoint(slider.Width*0.5f,slider.Height*0.5f);
			slider.SetPosition(Stage.Width*0.5f, Stage.Height - 20);
		}
		#endregion
		
		#region Event Handling
		
		private void HandleAllocationChanged(object o, AllocationChangedArgs args)
		{	
			RedrawInterface();
		}
		
		void HandleButtonReleaseEvent(object o, Clutter.ButtonReleaseEventArgs args)
		{
			if (!dragging && args.Event.Button==1 && UpdatedAlbum!=null) UpdatedAlbum (coverManager.CurrentCover.CreateClickClone(), EventArgs.Empty);
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
		
        void HandleNewCurrentCover (CoverGroup cover, EventArgs e)
        {
			UpdateAlbumText (); //TODO suprimate this by creating a special AlbumText class + needs to fade out seperately
        }
		#endregion
		
	}	
}
