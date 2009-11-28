
using System;
using System.Collections.Generic;
using Clutter;

namespace Banshee.ClutterFlow
{

	public class FlowBehaviour {
				
		#region Fields
		private int zFar = 50;
		public int ZFar {
			get { return zFar; }
			set {
				if (value!=zFar) {
					zFar = value;
					UpdateActors();
				}
			}
		}
		private int zNear = 100;
		public int ZNear {
			get { return zNear; }
			set {
				if (value!=zNear) {
					zNear = value;
					UpdateActors ();
				}
			}
		}
		
		protected int OffsetZNear = 0;
		
		protected double rotationAngle = 60;
		public double RotationAngle {
			get { return rotationAngle; }
			set {
				if (value!=rotationAngle) {
					rotationAngle = value;
					UpdateActors ();
				}
			}
		}
		
		protected float OffsetRotationAngle = 0;
		
		protected float width = 450;
		public float Width {
			get { return width; }
			set {
				if (value!=width) {
					width = value;
					centerX = value*0.5f;
					UpdateXStepAndSideWidth ();
					UpdateActors ();
				}
			}
		}

		protected float height = 200;
		public float Height {
			get { return height; }
			set {
				if (value!=height) {
					height = value;
					centerY = value*0.45f;
					UpdateActors ();
				}
			}
		}
		
		protected float centerY = 100;
		public float CenterY {
			get { return centerY; }
		}

		protected float OffsetCenterX = 0;
		protected float centerX = 225;
		public float CenterX {
			get { return centerX; }
		}
		
		private float CoverCount {
			get { return coverManager.VisibleCovers; }
		}
		
		protected bool holdUpdates = false;
		public bool HoldUpdates {
			get { return holdUpdates; }
			set { holdUpdates = value; }
		}
		
		protected void UpdateXStepAndSideWidth ()
		{
			XStep = (Width - CenterWidth - SideMargin) / CoverCount;
			SideWidth = Width*0.5f - (CenterMargin+XStep+SideMargin);
			if (SideWidth>coverWidth*0.5f) {
			 	SideMargin += SideWidth-coverWidth*0.5f;
				XStep = (Width - CenterWidth - SideMargin) / CoverCount;
				SideWidth = Width*0.5f - (CenterMargin+XStep+SideMargin);
			}
		}
		
		protected float coverWidth = 100;
		protected float CenterMargin = 70;
		protected float SideMargin = 50;
		protected float CenterWidth = 150;
		protected float XStep = 20;
		protected float SideWidth = 130;
		public float CoverWidth {
			get { return coverWidth; }
			set {
				if (value!=coverWidth) {
					coverWidth = value;
					CenterMargin = value*0.7f;
				 	SideMargin = value*0.5f;
					CenterWidth = CenterMargin*2;
					UpdateXStepAndSideWidth ();
					UpdateActors ();
				}
			}
		}
		
		protected float AlphaStep {
			get { return 1 / CoverCount; }
		}

		protected double previousProgress = 0.0;
		
		protected double Progress {
			get { return coverManager.Timeline.Progress; }
		}
		
		protected CoverManager coverManager;
		public CoverManager CoverManager {
			get { return coverManager; }
			set {
				if (value!=coverManager) {
					if (coverManager!=null) {
						coverManager.VisibleCoversChanged -= HandleVisibleCoversChanged;
						coverManager.TargetIndexChanged -= HandleTargetIndexChanged;
						coverManager.Timeline.NewFrame -= HandleNewFrame;
					}
					coverManager = value;
					if (coverManager!=null) {
						coverManager.VisibleCoversChanged += HandleVisibleCoversChanged;
						coverManager.TargetIndexChanged += HandleTargetIndexChanged;
						coverManager.Timeline.NewFrame += HandleNewFrame;
					}
				}
			}
		}
		#endregion
		
		public FlowBehaviour (CoverManager coverManager)
		{
			this.CoverManager = coverManager;
		}
		
		#region Event Handling
		void HandleNewFrame (object sender, NewFrameEventArgs e)
		{
			UpdateActors ();
		}
			
		void HandleTargetIndexChanged (object sender, EventArgs e)
		{
			UpdateActors ();
		}
		
		protected void HandleVisibleCoversChanged (object o, EventArgs args)
		{
			UpdateActors ();
		}
		#endregion
			
		#region Actor Handling (animation)

		public void UpdateActors () 
		{
			if (!holdUpdates) {
				//only update covers that were visible at the previous & current progress:
				
				double currentProgress = Progress;

				int ccb = Math.Min (coverManager.TotalCovers-1, Math.Max (0, (int) (currentProgress * (CoverManager.TotalCovers-1))));
				int clb = Math.Min (coverManager.TotalCovers-1, Math.Max (0, (int) (ccb - (CoverManager.HalfVisCovers + 1))));
				int cub = Math.Min (coverManager.TotalCovers-1, Math.Max (0, (int) (ccb + (CoverManager.HalfVisCovers + 1))));
				
				int pcb = Math.Min (coverManager.TotalCovers-1, Math.Max (0, (int) (previousProgress * (CoverManager.TotalCovers-1))));
				int plb = Math.Min (coverManager.TotalCovers-1, Math.Max (0, (int) (pcb - (CoverManager.HalfVisCovers + 1))));
				int pub = Math.Min (coverManager.TotalCovers-1, Math.Max (0, (int) (pcb + (CoverManager.HalfVisCovers + 1))));
						
				if (ccb<pcb)
					coverManager.ForSomeCovers (HideActor, cub, pub);
				else
					coverManager.ForSomeCovers (HideActor, plb, clb);
				
				coverManager.ForSomeCovers (UpdateActor, clb, cub);
				coverManager.SortDepthOrder ();
				
				previousProgress = currentProgress;
			}
		}
		
		protected void HideActor (CoverGroup cover)
		{
			cover.Hide ();
		}
		
		protected void UpdateActor (CoverGroup cover)
		{
			UpdateActor (cover, Progress);
		}
		
		protected void UpdateActor (CoverGroup cover, double progress)
		{
			UpdateActorWithAlpha (cover, cover.AlphaFunction(progress));
		}
		
		protected void UpdateActorWithAlpha (CoverGroup cover, double alpha) {
			float ratio = Math.Min (0.5f * (float) coverManager.Timeline.Delta / (float) coverManager.VisibleCovers, 1.0f);
			OffsetRotationAngle = (float) ((rotationAngle / 5) * ratio);
			OffsetCenterX = -(float) ((CenterMargin / 2) * ratio);
			OffsetZNear = (int) (-(ZNear-ZFar) * 1.5 * ratio);
			if (coverManager.Timeline.Direction!=TimelineDirection.Forward) {
				OffsetRotationAngle = -OffsetRotationAngle;
				OffsetCenterX = -OffsetCenterX;
			}
			
			cover.SetScale (coverWidth/cover.Width, coverWidth/cover.Width);
			if (alpha!=0 || alpha!=1) {
				if (alpha<AlphaStep) {
					MoveAndFadeOutActor			(cover, (float) (alpha / AlphaStep), true);
				} else if (alpha>=AlphaStep && alpha<=0.5-AlphaStep) {
					MoveActorSideways			(cover, (float) ((alpha - AlphaStep) / (0.5f - 2*AlphaStep)), true);
				} else if (alpha>0.5f-AlphaStep && alpha<0.5f+AlphaStep) {
					MoveAndRotateActorCentrally (cover, (float) ((alpha - 0.5f) / AlphaStep));
				} else if (alpha>=0.5f+AlphaStep && alpha<=1-AlphaStep) {
					MoveActorSideways			(cover, (float) ((1 - AlphaStep - alpha) / (0.5f - 2*AlphaStep)), false);
				} else if (alpha>1-AlphaStep) {
					MoveAndFadeOutActor			(cover,  (float) ((1 - alpha) / AlphaStep), false);
				}
				cover.Show ();
			} else cover.Hide ();
		}
		
		private void MoveAndFadeOutActor (CoverGroup cover, float progress, bool left)
		{
			MoveAndFadeOutActor ((Actor) cover, progress, left);
			cover.SetShade (255, left);
		}
		
		private void MoveAndFadeOutActor (Actor actor, float progress,  bool left) 
		{
			actor.SetPosition ((left ? 0 : Width) + (SideMargin + progress * XStep)*(left ? 1 : -1), CenterY);
			actor.Depth = zFar - 3 + progress;
			actor.SetRotation (Clutter.RotateAxis.Y, (left ? 1 : -1) * rotationAngle + OffsetRotationAngle, 0, 0, 0);
			actor.Opacity = (byte) (progress * 223);
		}
		
		private void MoveActorSideways (CoverGroup cover, float progress, bool left)
		{
			MoveActorSideways ((Actor) cover, progress, left);
			cover.SetShade (255, left);
		}
		
		private void MoveActorSideways (Actor actor, float progress, bool left)
		{
			actor.SetPosition (CenterX + OffsetCenterX - (left ? 1 : -1) * ((1-progress)*(SideWidth + OffsetCenterX*(left ? 1 : -1)) + CenterMargin), CenterY);
			actor.Depth = zFar - 2 + progress;
			actor.SetRotation (Clutter.RotateAxis.Y, (left ? 1 : -1) * rotationAngle + OffsetRotationAngle, 0, 0, 0);
			actor.Opacity = (byte) (255 - (1-progress)*32);
		}
		
		private void MoveAndRotateActorCentrally (CoverGroup cover, float progress)
		{
			MoveAndRotateActorCentrally ((Actor) cover, progress);
			cover.SetShade ((byte) Math.Abs(255*progress), (progress < 0));
		}
		
		private void MoveAndRotateActorCentrally (Actor actor, float progress) 
		{
			actor.SetPosition ((float) (CenterX + OffsetCenterX + CenterMargin*progress), CenterY);
			actor.Depth = (float) (zFar + (zNear + OffsetZNear - zFar ) * (1 - Math.Abs(progress)));
			actor.SetRotation (Clutter.RotateAxis.Y, -progress * rotationAngle + OffsetRotationAngle, 0, 0, 0);
			actor.Opacity = 255;
		}
		
		public void FadeCoversInAndOut (List<CoverGroup> old_covers, List<CoverGroup> new_covers, double newProgress, EventHandler onCompleted) 
		{
			if (fadeOutAnim!=null) { fadeOutAnim.Dispose (); fadeOutAnim = null; }
			if (fadeInAnim!=null) { fadeInAnim.Dispose (); fadeInAnim = null; }

			//Fade out removed covers, slide still visible ones
			List<CoverGroup> sliding_covers = new List<CoverGroup> ();
			foreach (CoverGroup cover in old_covers) {
				if (cover!=null) {
					if (!new_covers.Contains (cover)) {
						FadeOutActor (cover);
					} else {
						if (cover.Data.ContainsKey ("fromAlpha")) cover.Data.Remove ("fromAlpha");
						cover.Data.Add ("fromAlpha", cover.LastAlpha);
						sliding_covers.Add (cover);
						new_covers.Remove (cover);
					}
				}
			}

			//Clear fromAlpha hashes from new covers (could still be there from older searches
			foreach (CoverGroup cover in new_covers) {
				if (cover!=null && cover.Data.ContainsKey ("fromAlpha")) cover.Data.Remove ("fromAlpha");
			}

			//Add both arrays together:
			new_covers.AddRange (sliding_covers);

			//Set up slide animation:
			Timeline tSlide = new Timeline (CoverManager.MaxAnimationSpan);
			tSlide.NewFrame += delegate (object o, NewFrameArgs args) {
				foreach (CoverGroup cover in new_covers) {
					if (cover!=null) {
						double toAlpha = cover.AlphaFunction (newProgress);
						double fromAlpha = 0;
						if (cover.Data.ContainsKey ("fromAlpha"))
							fromAlpha = (double) cover.Data["fromAlpha"];
						else if (toAlpha > 0.5)
							fromAlpha = 1.0;
						UpdateActorWithAlpha (cover, fromAlpha + (toAlpha - fromAlpha) * tSlide.Progress);
					}
				}
			};

			//Chain slide in:
			EventHandler wrappedOnFadeOutCompleted = delegate (object sender, EventArgs e) {
				tSlide.Start ();
			};
			if (fadeOutAnim!=null) fadeOutAnim.Completed += wrappedOnFadeOutCompleted;
			else wrappedOnFadeOutCompleted (this, EventArgs.Empty);

			if (tSlide!=null)
				tSlide.Completed += onCompleted;
			else if (fadeOutAnim!=null)
				fadeOutAnim.Completed += onCompleted;
			else onCompleted (this, EventArgs.Empty);
		}
		
		public void CreateClickedCloneAnimation (CoverGroup cover) {		
			if (cover.Parent!=null) {
				Clone clone = new Clone (cover);
				MoveAndRotateActorCentrally (clone, 0);
				double scaleX, scaleY; cover.GetScale (out scaleX, out scaleY); clone.SetScale (scaleX, scaleY);
				
				((Group) cover.Parent).Add (clone);
				clone.ShowAll ();
				clone.Opacity = 255;
				clone.Raise (cover);
				Animation anmn = clone.Animatev ((ulong) AnimationMode.EaseInExpo.value__, 1000, new string[] { "opacity" }, new GLib.Value ((byte) 50));
				anmn.Completed += HandleClickedCloneCompleted;
				clone.AnimateWithTimelinev ((ulong) AnimationMode.EaseInExpo.value__, anmn.Timeline, new string[] { "scale-x" }, new GLib.Value (scaleX*2));
				clone.AnimateWithTimelinev ((ulong) AnimationMode.EaseInExpo.value__, anmn.Timeline, new string[] { "scale-y" }, new GLib.Value (scaleY*2));
				clone.AnimateWithTimelinev ((ulong) AnimationMode.EaseInExpo.value__, anmn.Timeline, new string[] { "fixed::anchor-x" }, new GLib.Value (clone.Width/2));
				clone.AnimateWithTimelinev ((ulong) AnimationMode.EaseInExpo.value__, anmn.Timeline, new string[] { "fixed::anchor-y" }, new GLib.Value (clone.Height/4));
			}
		}
		
		protected void HandleClickedCloneCompleted (object sender, EventArgs e)
		{
			if (sender is Animation && (sender as Animation).Object is Actor) {
				Actor actor = (Actor) (sender as Animation).Object;
				actor.Destroy ();
				actor.Dispose ();
				(sender as Animation).Completed -= HandleClickedCloneCompleted;
			}
		}
		
		protected Animation fadeOutAnim;		
		public void FadeOutActor (Actor actor)
		{
			actor.Show ();
			if (fadeOutAnim==null)
				fadeOutAnim = actor.Animatev ((ulong) Clutter.AnimationMode.Linear.value__, CoverManager.MaxAnimationSpan, new string[] { "opacity" }, new GLib.Value ((byte) 0));
			else
				actor.AnimateWithTimelinev ((ulong) Clutter.AnimationMode.Linear.value__, fadeOutAnim.Timeline, new string[] { "opacity" }, new GLib.Value ((byte) 0));
		}
		
		protected Animation fadeInAnim;
		public void FadeInActor (Actor actor)
		{
			actor.Show();
			byte opacity = actor.Opacity;
			actor.Opacity = 0;
			if (fadeInAnim==null)
				fadeInAnim = actor.Animatev ((ulong) Clutter.AnimationMode.Linear.value__, CoverManager.MaxAnimationSpan, new string[] { "opacity" }, new GLib.Value ((byte) opacity));
			else
				actor.AnimateWithTimelinev ((ulong) Clutter.AnimationMode.Linear.value__, fadeInAnim.Timeline, new string[] { "opacity" }, new GLib.Value ((byte) opacity));
		}
		#endregion
	}	
}
