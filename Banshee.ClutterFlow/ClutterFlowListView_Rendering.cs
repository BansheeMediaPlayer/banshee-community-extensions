
using System;
using System.Collections.Generic;

using Banshee.ServiceStack;
using Banshee.Collection;
//using Banshee.Collection.Gui;
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
		
		private bool disable_scroll = false;	// wether or not we should disable queueing positions when scrolled
		private bool dragging = false;			// wether or not we are currently dragging the viewport around
		private double mouse_x, mouse_y;
		
		private const int visibleCovers = 15;						// # visible covers
		private const int halfVisCovers = (visibleCovers - 1)/2;	// # covers displayed at the sides
		private ClutterFlowAnmMgr anim_mgr = new ClutterFlowAnmMgr(visibleCovers);
		private Group coverStage = new Group();
		private ClutterSlider slider;
		private Clutter.Text srf_text;
		private Animation text_fade;
		
		private const float rotSens = 0.00001f;
		private const float viewportMaxAngleX = -15;// maximum X viewport angle
		private const float viewportMaxAngleY = -5;	// maximum Y viewport angle
		private float viewportAngleX = 0;			// current X viewport angle
		private float viewportAngleY = 0;			// current Y viewport angle
		private float viewportOffsetZ {
			get { return coverWidth*4; }
		}
		
		private float coverWidth = 150;			// width of covers in pixels
		
		private List<CoverGroup> covers;		// list with cover actors
		private int targetIndex = 0;			// curent targetted cover index
		public int TargetIndex {
			get { return targetIndex; }
			set {
				if (value!=targetIndex) {
					targetIndex = value;
					if (targetIndex < 0) targetIndex = 0;
					else if (targetIndex >= TotalCovers) targetIndex = TotalCovers-1;
					UpdatePositions();
					slider.HandlePostionFromIndex = (uint) TargetIndex;
				}
			}
		}
		
		private int TotalCovers					// number of covers or zero if null
		{
			get {
				return (covers != null) ? covers.Count : 0;
			}
		}			
		

		#region Initialisation
		private void ReloadCovers() {
			if (covers==null || covers.Count==0) {
				Hyena.Log.Information("Loading Covers");
				disable_scroll = true;
				LoadCovers(null);
				UpdatePositions(false);
				disable_scroll = false;
			} else if (!disable_scroll) {
				disable_scroll = true;
				Hyena.Log.Information("Reloading Covers");
				
				//Step 1: setup new and old lists:
				int old_current_index = covers.IndexOf(current_cover);
				float current_ipos = (float) old_current_index / (float) (covers.Count-1);
				foreach (CoverGroup cover in covers) {
					anim_mgr.ApplyLast(cover);
					cover.Hide();
				}
				bool keep_current = false;
				LoadCovers(delegate (CoverGroup cover) {
					if (cover==current_cover) keep_current = true;
					//cover.ClearPosQueue();
					cover.Hide();
				});
				TargetIndex = keep_current ? covers.IndexOf(current_cover) : (int) Math.Round(current_ipos * (covers.Count-1));
				List<CoverGroup> new_covers = new List<CoverGroup>(SafeGetRange(covers,TargetIndex - halfVisCovers, visibleCovers));

				for (int i=(int)anim_mgr.MiddlePosition-1; i >= 0; i--) {
					if (new_covers[i]!=null)
							new_covers[i].ApplyPosition((uint) i);
				}
				for (int i=(int)anim_mgr.MiddlePosition; i < new_covers.Count; i++) {
					if (new_covers[i]!=null)
						new_covers[i].ApplyPosition((uint) i);
				}
				disable_scroll = false;
			}
			
			slider.UpdateBounds((uint) covers.Count,(uint) TargetIndex);
		}
		
		private void LoadCovers(System.Action<CoverGroup> method_call) {
			covers = new List<CoverGroup>();
			Hyena.Log.Information("Loading new covers");
			for (int i = 1; i < model.Count; i++) {
				AlbumInfo album = (model as IListModel<AlbumInfo>)[i];
				CoverGroup cover = AddCover(album, i);
				if (method_call!=null) method_call(cover);
			}
		}
		private IEnumerable<CoverGroup> SafeGetRange(List<CoverGroup> list, int index, int count) {
			for (int i = index; i < index + count; i++) {
				CoverGroup cover;
				try {
					cover = list[i];
				} catch {
					cover = null;
				}
				yield return cover;
			}
			yield break;
		}
		
		private void ForEachCover(ForEachCover method_call) {
			if (covers!=null) {
				IEnumerator<CoverGroup> enumerator = covers.GetEnumerator();
				while (enumerator.MoveNext())
					method_call(enumerator.Current); 
			}
		}
		
		public CoverGroup AddCover(AlbumInfo album) {
			return AddCover(album, covers.Count);
		}
			
		public CoverGroup AddCover(AlbumInfo album, int index) {
			CoverGroup cover = anim_mgr.CoverCache.GetCoverGroupFromAlbum(album, coverWidth);
			if (index >= covers.Count) covers.Add(cover);
			else covers.Insert(index, cover);
			if (cover.Parent!=coverStage)
				coverStage.Add(cover);
			cover.Hide();
			return cover;
		}
		
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

			anim_mgr.NewFrame += HandleNewFrame;
			
			CoverGroupHelper.NewCurrentCover += HandleNewCurrentCover;
		}

		protected void SetupViewport() {
			Stage.Color = new Clutter.Color (0x00, 0x00, 0x00, 0xff);
			
			coverStage.SetRotation(RotateAxis.X, viewportAngleX, Stage.Width/2, Stage.Height/2,0);
			//coverStage.SetPosition(viewportOffsetX, viewportOffsetY);
			coverStage.Depth = viewportOffsetZ;
			Stage.Add(coverStage);
			coverStage.Show();
			coverStage.LowerBottom();
		}
		
		protected void SetupSlider() {
			slider = new ClutterSlider(Stage.Width, Stage.Height);
			slider.SliderHasChanged += HandleSliderHasChanged;
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
			coverStage.SetRotation(RotateAxis.X, viewportAngleX, Stage.Width/2, Stage.Height/2,0);
			coverStage.Depth = viewportOffsetZ;
			if (!coverStage.IsVisible) coverStage.Show();
			coverStage.LowerBottom();
		}
		
		//Update coverStage rotation with intervals:
		public void DeltaRotationX(float delta) {
			viewportAngleX += delta*rotSens;
			if (viewportAngleX < viewportMaxAngleX) viewportAngleX = viewportMaxAngleX;
			if (viewportAngleX > 0) viewportAngleX = 0;
			coverStage.SetRotation(RotateAxis.X, viewportAngleX, coverStage.Width*0.5f,coverStage.Height*0.5f,0);
		}
		public void DeltaRotationY(float delta) {
			viewportAngleY -= delta*rotSens;
			if (viewportAngleY < viewportMaxAngleY) viewportAngleY = viewportMaxAngleY;
			if (viewportAngleY > -viewportMaxAngleY) viewportAngleY = -viewportMaxAngleY;
			coverStage.SetRotation(RotateAxis.Y, viewportAngleY, coverStage.Width*0.5f,coverStage.Height*0.5f,0);
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
			srf_text.SetPosition(Stage.Width*0.5f, Stage.Height*0.125f);
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
			coverWidth = Stage.Height*0.5f;			
			
			anim_mgr.SetScale(Stage.Width, Stage.Height, coverWidth);
			ForEachCover(delegate (CoverGroup cover) {
				cover.SetScale(coverWidth/cover.Width,coverWidth/cover.Width);
			});
			RedrawInterface();
			
			if (!anim_mgr.IsAnimating) UpdatePositions();
		}
		
		void HandleButtonReleaseEvent(object o, Clutter.ButtonReleaseEventArgs args)
		{
			if (!dragging && args.Event.Button==1) {
				if (UpdatedAlbum!=null) UpdatedAlbum (current_cover, EventArgs.Empty);
				anim_mgr.CreateClickedCloneAnimation (current_cover);
			}
		}
		
		private void HandleMotionEvent(object o, Clutter.MotionEventArgs args)
		{			
			if ((args.Event.ModifierState.value__ & Clutter.ModifierType.Button1Mask.value__)!=0 && (args.Event.ModifierState.value__ & Clutter.ModifierType.ControlMask.value__)!=0) {
				dragging = true;
				
				DeltaRotationY( (float) (mouse_x - args.Event.X));
				DeltaRotationX( (float) (mouse_y - args.Event.Y));
			} else dragging = false;
			mouse_x = args.Event.X;
			mouse_y = args.Event.Y;
		}
		
		private void HandleSliderHasChanged(object sender, EventArgs e)
		{
			TargetIndex = (int) slider.HandlePostionFromIndex;
		}
		
		private void HandleScroll (object o, Clutter.ScrollEventArgs args)
		{
			if (args.Event.Direction==Clutter.ScrollDirection.Down)
				Scroll(true);
			else
				Scroll(false);
		}
		
		public void Scroll(bool Backward) {
			if (!disable_scroll) {
				if (Backward) TargetIndex--;
				else TargetIndex++;
			}
		}
		
		void HandleNewFrame (object o, NewFrameArgs args)
		{
			coverStage.SortDepthOrder ();
		}
		
        void HandleNewCurrentCover (CoverGroup cover, EventArgs e)
        {
        	current_cover = cover;
			UpdateAlbumText ();
        }
		#endregion
		
		public void UpdatePositions() {
			UpdatePositions(true);
		}
		
		public void UpdatePositions(bool animate) {
			uint pos;
			for (int i=0; i < TotalCovers; i++) {
				if (i >= TargetIndex+halfVisCovers) { // move off-screen to right side 
					if (animate) {
						if (covers[i].IsVisible) covers[i].Position = (uint) visibleCovers-1;
					} else covers[i].ApplyPosition((uint) visibleCovers-1);
				} else if (i <= TargetIndex-halfVisCovers) { //move off-screen to left side
					if (animate) {
						if (covers[i].IsVisible) covers[i].Position = 0;
					} else covers[i].ApplyPosition(0);
				} else { //put it somewhere in between
					pos = (uint) (i-TargetIndex+halfVisCovers);
					if (animate) {
						//if (!covers[i].IsVisible) covers[i].ApplyPosition( pos <= halfVisCovers ? 0 : (uint) visibleCovers-1 );
						covers[i].Position = pos;
					} else covers[i].ApplyPosition(pos);
				}
			}
		}
		
	}	
}
