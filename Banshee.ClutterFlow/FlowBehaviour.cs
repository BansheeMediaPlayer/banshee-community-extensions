
using System;
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
		private int zNear = 150;
		public int ZNear {
			get { return zNear; }
			set {
				if (value!=zNear) {
					zNear = value;
					UpdateActors();
				}
			}
		}
		
		protected double rotationAngle = 50;
		public double RotationAngle {
			get { return rotationAngle; }
			set {
				if (value!=rotationAngle) {
					rotationAngle = value;
					UpdateActors();
				}
			}
		}
		
		protected float width = 450;
		protected float CenterX = 225;
		public float Width {
			get { return width; }
			set {
				if (value!=width) {
					width = value;
					CenterX = value*0.5f;
					XStep = (Width - CenterWidth - SideMargin) / CoverCount;
					SideWidth = Width*0.5f - (CenterMargin+XStep+SideMargin);
					UpdateActors();
				}
			}
		}
		protected float height = 200;
		protected float CenterY = 100;
		public float Height {
			get { return height; }
			set {
				if (value!=height) {
					height = value;
					CenterY = value*0.5f;
					UpdateActors();
				}
			}
		}
		
		private float CoverCount {
			get { return coverManager.VisibleCovers; }
		}
		
		protected float coverWidth = 100;
		protected float CenterMargin = 100;
		protected float SideMargin = 50;
		protected float CenterWidth = 150;
		protected float XStep = 20;
		protected float SideWidth = 130;
		public float CoverWidth {
			get { return coverWidth; }
			set {
				if (value!=coverWidth) {
					coverWidth = value;
					CenterMargin = value;
				 	SideMargin = value*0.5f;
					CenterWidth = CenterMargin*2;
					XStep = (Width - CenterWidth - SideMargin) / CoverCount;
					SideWidth = Width*0.5f - (CenterMargin+XStep+SideMargin);
					UpdateActors();
				}
			}
		}
		
		protected float AlphaStep {
			get { return 1 / CoverCount; }
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
		
		public FlowBehaviour (CoverManager coverManager) {
			this.CoverManager = coverManager;
		}
		
		#region Event Handling
		void HandleNewFrame(object sender, NewFrameEventArgs e)
		{
			UpdateActors();
		}
			
		void HandleTargetIndexChanged (object sender, EventArgs e)
		{
			UpdateActors();
		}
		
		protected void HandleVisibleCoversChanged (object o, EventArgs args) {		
			UpdateActors();
		}
		#endregion
			
		#region Actor Handling (animation)
		
		protected double previousProgress = 0.0;
		
		public void UpdateActors () 
		{
			double currentProgress = Progress;
			
			int ccb = Math.Min(coverManager.TotalCovers , Math.Max(0, (int) (currentProgress * (CoverManager.TotalCovers-1))));
			int clb = Math.Min(coverManager.TotalCovers , Math.Max(0, (int) (ccb - (CoverManager.HalfVisCovers + 1))));
			int cub = Math.Min(coverManager.TotalCovers , Math.Max(0, (int) (ccb + (CoverManager.HalfVisCovers + 1))));
			
			int pcb = Math.Min(coverManager.TotalCovers , Math.Max(0, (int) (previousProgress * (CoverManager.TotalCovers-1))));
			int plb = Math.Min(coverManager.TotalCovers , Math.Max(0, (int) (pcb - (CoverManager.HalfVisCovers + 1))));
			int pub = Math.Min(coverManager.TotalCovers , Math.Max(0, (int) (pcb + (CoverManager.HalfVisCovers + 1))));
					
			if (ccb<pcb)
				coverManager.ForSomeCovers(HideActor, cub, pub);
			else
				coverManager.ForSomeCovers(HideActor, plb, clb);
			
			
			coverManager.ForSomeCovers(UpdateActor, clb, cub);
			coverManager.SortDepthOrder();
			
			previousProgress = currentProgress;
		}
		
		protected double Progress {
			get { return coverManager.Timeline.Progress; }
		}
		
		protected void HideActor (CoverGroup cover)
		{
			cover.Hide();
		}
		
		protected void UpdateActor (CoverGroup cover)
		{
			double alpha = cover.AlphaFunction(Progress);
			cover.SetScale(coverWidth/cover.Width,coverWidth/cover.Width);
			//coverManager.Timeline.Frequency
			if (alpha!=0 || alpha!=1) {
				if (alpha<AlphaStep) {
					MoveAndFadeOutActor (cover, (float) (alpha / AlphaStep), true);
				} else if (alpha>=AlphaStep && alpha<=0.5-AlphaStep) {
					MoveActorSideways(cover, (float) ((alpha - AlphaStep) / (0.5f - 2*AlphaStep)), true);
				} else if (alpha>0.5f-AlphaStep && alpha<0.5f+AlphaStep) {
					MoveAndRotateActorCentrally (cover, (float) ((alpha - 0.5f) / AlphaStep));
				} else if (alpha>=0.5f+AlphaStep && alpha<=1-AlphaStep) {
					MoveActorSideways(cover, (float) ((1 - AlphaStep - alpha) / (0.5f - 2*AlphaStep)), false);
				} else if (alpha>1-AlphaStep) {
					MoveAndFadeOutActor (cover,  (float) ((1 - alpha) / AlphaStep), false);
				}
				cover.Show();
			} else cover.Hide();
		}
		
		public void CreateClickedCloneAnimation(CoverGroup cover) {		
			if (cover.Parent!=null) {
				Clone clone = new Clone(cover);
				MoveAndRotateActorCentrally(clone, 0);
				double scaleX, scaleY; cover.GetScale(out scaleX, out scaleY); clone.SetScale(scaleX, scaleY);
				
				((Group) cover.Parent).Add(clone);
				clone.ShowAll();
				clone.Opacity = 255;
				clone.Raise(cover);
				Animation anmn = clone.Animatev((ulong) AnimationMode.EaseInExpo.value__, 1000, new string[] { "opacity" }, new GLib.Value((byte) 50));
				anmn.Completed += HandleClickedCloneCompleted;
				clone.AnimateWithTimelinev((ulong) AnimationMode.EaseInExpo.value__,anmn.Timeline,new string[] { "scale-x" }, new GLib.Value(scaleX*2));
				clone.AnimateWithTimelinev((ulong) AnimationMode.EaseInExpo.value__,anmn.Timeline,new string[] { "scale-y" }, new GLib.Value(scaleY*2));
				clone.AnimateWithTimelinev((ulong) AnimationMode.EaseInExpo.value__,anmn.Timeline,new string[] { "fixed::anchor-x" }, new GLib.Value(clone.Width/2));
				clone.AnimateWithTimelinev((ulong) AnimationMode.EaseInExpo.value__,anmn.Timeline,new string[] { "fixed::anchor-y" }, new GLib.Value(clone.Height/4));
			}
		}
		
		protected void HandleClickedCloneCompleted(object sender, EventArgs e)
		{
			if (sender is Animation && (sender as Animation).Object is Actor) {
				Actor actor = (Actor) (sender as Animation).Object;
				actor.Destroy();
				actor.Dispose();
				(sender as Animation).Completed -= HandleClickedCloneCompleted;
			}
		}
		
		public static void FadeOutActor (Actor actor)
		{
			actor.Animatev ((ulong) Clutter.AnimationMode.Linear.value__, CoverManager.MaxAnimationSpan, new string[] { "opacity" }, new GLib.Value((byte) 0));
		}
		
		public static void FadeInActor (Actor actor)
		{
			actor.Animatev ((ulong) Clutter.AnimationMode.Linear.value__, CoverManager.MaxAnimationSpan, new string[] { "opacity" }, new GLib.Value((byte) 255));
		}
		
		private void MoveAndFadeOutActor (Actor actor, float progress,  bool left) 
		{
			actor.SetPosition ((left ? 0 : Width) + (SideMargin + progress * XStep)*(left ? 1 : -1), CenterY);
			actor.Depth = zFar - 3 + progress;
			actor.SetRotation (Clutter.RotateAxis.Y, (left ? 1 : -1) * rotationAngle, 0, 0, 0);
			actor.Opacity = (byte) Math.Max(0, progress * (255 + 32) - 32);
		}
		
		private void MoveAndRotateActorCentrally (Actor actor, float progress) 
		{
			actor.SetPosition ((float) (CenterX + CenterMargin*progress), CenterY);
			actor.Depth = (float) (zFar + (zNear - zFar) * (1 - Math.Abs(progress)));
			actor.SetRotation (Clutter.RotateAxis.Y, -progress*rotationAngle, 0, 0, 0);
			actor.Opacity = 255;
		}
		
		private void MoveActorSideways (Actor actor, float progress, bool left)
		{
			actor.SetPosition (CenterX - (left ? 1 : -1) * ((1-progress)*SideWidth + CenterMargin), CenterY);
			actor.Depth = zFar - 2 + progress;
			actor.SetRotation (Clutter.RotateAxis.Y, (left ? 1 : -1) * rotationAngle, 0, 0, 0);
			actor.Opacity = 255;
		}
		#endregion
	}	
}
