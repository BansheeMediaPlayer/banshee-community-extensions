
using System;
using System.Collections.Generic;
using Clutter;

namespace ClutterFlow
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
		protected int pcb = -1; protected int plb; protected int pub;
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
			if (!holdUpdates && coverManager.Enabled && coverManager.IsVisible) {
				//only update covers that were visible at the previous & current progress:
				
				double currentProgress = Progress;
				int ccb = Math.Min (coverManager.TotalCovers-1, Math.Max (0, (int) (currentProgress * (CoverManager.TotalCovers-1))));
				int clb = Math.Min (coverManager.TotalCovers-1, Math.Max (0, (int) (ccb - (CoverManager.HalfVisCovers + 1))));
				int cub = Math.Min (coverManager.TotalCovers-1, Math.Max (0, (int) (ccb + (CoverManager.HalfVisCovers + 1))));
				
				if (pcb==-1) {
					pcb = Math.Min (coverManager.TotalCovers-1, Math.Max (0, (int) (previousProgress * (CoverManager.TotalCovers-1))));
					plb = Math.Min (coverManager.TotalCovers-1, Math.Max (0, (int) (pcb - (CoverManager.HalfVisCovers + 1))));
					pub = Math.Min (coverManager.TotalCovers-1, Math.Max (0, (int) (pcb + (CoverManager.HalfVisCovers + 1))));
				}
				
				if (ccb<pcb)
					coverManager.ForSomeCovers (HideActor, cub, pub);
				else if (ccb>pcb)
					coverManager.ForSomeCovers (HideActor, plb, clb);
				
				coverManager.ForSomeCovers (UpdateActor, clb, cub);
				coverManager.SortDepthOrder ();
				
				previousProgress = currentProgress;
				pcb = Math.Min (coverManager.TotalCovers-1, Math.Max (0, (int) (previousProgress * (CoverManager.TotalCovers-1))));
				plb = Math.Min (coverManager.TotalCovers-1, Math.Max (0, (int) (pcb - (CoverManager.HalfVisCovers + 1))));
				pub = Math.Min (coverManager.TotalCovers-1, Math.Max (0, (int) (pcb + (CoverManager.HalfVisCovers + 1))));				
			}
		}
		
		protected void HideActor (ClutterFlowActor actor)
		{
			actor.Hide ();
		}
		
		protected void UpdateActor (ClutterFlowActor actor)
		{
			UpdateActor (actor, Progress);
		}
		
		protected void UpdateActor (ClutterFlowActor actor, double progress)
		{
			UpdateActorWithAlpha (actor, actor.AlphaFunction(progress));
		}
		
		protected void UpdateActorWithAlpha (ClutterFlowActor actor, double alpha) {
			float ratio = Math.Min (0.5f * (float) coverManager.Timeline.Delta / (float) coverManager.VisibleCovers, 1.0f);
			OffsetRotationAngle = (float) ((rotationAngle / 5) * ratio);
			OffsetCenterX = -(float) ((CenterMargin / 2) * ratio);
			OffsetZNear = (int) (-(ZNear-ZFar) * 1.5 * ratio);
			if (coverManager.Timeline.Direction!=TimelineDirection.Forward) {
				OffsetRotationAngle = -OffsetRotationAngle;
				OffsetCenterX = -OffsetCenterX;
			}
			
			actor.SetScale (coverWidth/actor.Width, coverWidth/actor.Width);
			if (alpha!=0 || alpha!=1) {
				actor.Show ();
				if (alpha<AlphaStep) {
					MoveAndFadeOutActor			(actor, (float) (alpha / AlphaStep), true);
				} else if (alpha>=AlphaStep && alpha<=0.5-AlphaStep) {
					MoveActorSideways			(actor, (float) ((alpha - AlphaStep) / (0.5f - 2*AlphaStep)), true);
				} else if (alpha>0.5f-AlphaStep && alpha<0.5f+AlphaStep) {
					MoveAndRotateActorCentrally (actor, (float) ((alpha - 0.5f) / AlphaStep));
				} else if (alpha>=0.5f+AlphaStep && alpha<=1-AlphaStep) {
					MoveActorSideways			(actor, (float) ((1 - AlphaStep - alpha) / (0.5f - 2*AlphaStep)), false);
				} else if (alpha>1-AlphaStep) {
					MoveAndFadeOutActor			(actor,  (float) ((1 - alpha) / AlphaStep), false);
				}
			} else actor.Hide ();
		}
			
		private void MoveAndFadeOutActor (Actor actor, float progress,  bool left) 
		{
			actor.SetPosition ((left ? 0 : Width) + (SideMargin + progress * XStep)*(left ? 1 : -1), CenterY);
			actor.Depth = zFar - 3 + progress;
			actor.SetRotation (Clutter.RotateAxis.Y, (left ? 1 : -1) * rotationAngle + OffsetRotationAngle, 0, 0, 0);
			actor.Opacity = (byte) (progress * 223);
			if (actor is ClutterFlowActor) (actor as ClutterFlowActor).SetShade (255, left);
		}
			
		private void MoveActorSideways (Actor actor, float progress, bool left)
		{
			actor.SetPosition (CenterX + OffsetCenterX - (left ? 1 : -1) * ((1-progress)*(SideWidth + OffsetCenterX*(left ? 1 : -1)) + CenterMargin), CenterY);
			actor.Depth = zFar - 2 + progress;
			actor.SetRotation (Clutter.RotateAxis.Y, (left ? 1 : -1) * rotationAngle + OffsetRotationAngle, 0, 0, 0);
			actor.Opacity = (byte) (255 - (1-progress)*32);
			if (actor is ClutterFlowActor) (actor as ClutterFlowActor).SetShade (255, left);
		}
		
		private void MoveAndRotateActorCentrally (Actor actor, float progress)
		{
			actor.SetPosition ((float) (CenterX + OffsetCenterX + CenterMargin*progress), CenterY);
			actor.Depth = (float) (zFar + (zNear + OffsetZNear - zFar ) * (1 - Math.Abs(progress)));
			actor.SetRotation (Clutter.RotateAxis.Y, -progress * rotationAngle + OffsetRotationAngle, 0, 0, 0);
			actor.Opacity = 255;
			if (actor is ClutterFlowActor) (actor as ClutterFlowActor).SetShade ((byte) Math.Abs(255*progress), (progress < 0));
		}
		
		public void FadeCoversInAndOut (List<ClutterFlowActor> old_covers, List<ClutterFlowActor> new_covers, double newProgress, EventHandler onCompleted) 
		{
			if (fadeOutAnim!=null) { fadeOutAnim.Dispose (); fadeOutAnim = null; }
			if (fadeInAnim!=null) { fadeInAnim.Dispose (); fadeInAnim = null; }

			//Fade out removed covers, slide still visible ones
			List<ClutterFlowActor> sliding_covers = new List<ClutterFlowActor> ();
			foreach (ClutterFlowActor cover in old_covers) {
				if (cover!=null) {
					if (cover.Index==-1) {
						FadeOutActor (cover);
					} else {
						if (cover.Data.ContainsKey ("fromAlpha")) cover.Data.Remove ("fromAlpha");
						cover.Data.Add ("fromAlpha", cover.LastAlpha);
						sliding_covers.Add (cover);
						new_covers.Remove (cover);
					}
				}
			}

			//Clear fromAlpha hashes from new covers (could still be there from older searches)
			foreach (ClutterFlowActor cover in new_covers) {
				if (cover!=null && cover.Data.ContainsKey ("fromAlpha")) cover.Data.Remove ("fromAlpha");
			}

			//Add both arrays together:
			new_covers.AddRange (sliding_covers);

			//Set up slide animation:
			Timeline tSlide = new Timeline (CoverManager.MaxAnimationSpan);
			tSlide.NewFrame += delegate (object o, NewFrameArgs args) {
				foreach (ClutterFlowActor cover in new_covers) {
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
		
		public void CreateClickedCloneAnimation (ClutterFlowActor actor) {
			if (actor.Parent!=null) {
				Clone clone = new Clone(actor);
				MoveAndRotateActorCentrally (clone, 0);
				double scaleX, scaleY; actor.GetScale (out scaleX, out scaleY); clone.SetScale (scaleX, scaleY);

				((Group) actor.Parent).Add (clone);
				clone.ShowAll ();
				clone.Opacity = 255;
				clone.Raise (actor);
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
