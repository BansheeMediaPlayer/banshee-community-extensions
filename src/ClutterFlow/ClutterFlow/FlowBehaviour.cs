// 
// FlowBehaviour.cs
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
using Clutter;

namespace ClutterFlow
{

	public class FlowBehaviour : IDisposable {
				
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

        /*protected List<ClutterFlowBaseActor> visible_actors = new List<ClutterFlowBaseActor> ();
        public List<ClutterFlowBaseActor> VisibleActors {
            get { return visible_actors; }
        }*/

        public bool TransitionAnimationBusy {
            get { return (transition_score!=null && transition_score.IsPlaying); }
        }
        
		public bool HoldUpdates {
			get { return CoverManager.Timeline.IsPaused || TransitionAnimationBusy || transition_queue.Count > 0; }
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
		protected float CenterMargin = 75;
		protected float SideMargin = 50;
		protected float CenterWidth = 150;
		protected float XStep = 20;
		protected float SideWidth = 130;
		public float CoverWidth {
			get { return coverWidth; }
			set {
				if (value!=coverWidth) {
					coverWidth = value;
					CenterMargin = value*0.75f;
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

        #region Initialisation
		public FlowBehaviour (CoverManager coverManager)
		{
			this.CoverManager = coverManager;
		}
        public virtual void Dispose ()
        {
            CoverManager = null;
            if (fadeInAnim!=null) {
                fadeInAnim.CompleteAnimation ();
                fadeInAnim = null;
            }
            if (fadeOutAnim!=null) {
                fadeOutAnim.CompleteAnimation ();
                fadeOutAnim = null;
            }
        }
        #endregion

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
			if (!HoldUpdates && coverManager.IsVisible) {
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
					CoverManager.ForSomeCovers (HideActor, cub, pub);
				else if (ccb>pcb)
					CoverManager.ForSomeCovers (HideActor, plb, clb);

				CoverManager.ForSomeCovers (UpdateActor, clb, cub);
				CoverManager.SortDepthOrder ();
				
				previousProgress = currentProgress;
				pcb = ccb;
				plb = clb;
				pub = cub;
			}
		}
		protected void SetPreviousBounds (double progress)
        {
            pcb = Math.Min (coverManager.TotalCovers-1, Math.Max (0, (int) (progress * (CoverManager.TotalCovers-1))));
            plb = Math.Min (coverManager.TotalCovers-1, Math.Max (0, (int) (pcb - (CoverManager.HalfVisCovers + 1))));
            pub = Math.Min (coverManager.TotalCovers-1, Math.Max (0, (int) (pcb + (CoverManager.HalfVisCovers + 1))));                
        }
		protected void HideActor (ClutterFlowBaseActor actor)
		{
			actor.Hide ();
		}
		
		protected void UpdateActor (ClutterFlowBaseActor actor)
		{
			UpdateActor (actor, Progress);
		}
		
		protected void UpdateActor (ClutterFlowBaseActor actor, double progress)
		{
			UpdateActorWithAlpha (actor, AlphaFunc(actor));
            
		}

        private double AlphaFunc (ClutterFlowBaseActor actor) {
/*            if (!actor.Data.ContainsKey ("alpha_ini"))
                actor.Data.Add*/
            if (actor.Index < 0)
                actor.Data["alpha_ini"] = 0;
            else {
                double previous_alpha = (CoverManager.HalfVisCovers - (CoverManager.TotalCovers-1) 
                                  * CoverManager.Timeline.Progress + actor.Index) 
                                  / (CoverManager.VisibleCovers-1);
                if (previous_alpha<0) previous_alpha=0;
                if (previous_alpha>1) previous_alpha=1;
                actor.Data["alpha_ini"] = previous_alpha;
            }
            return (double) actor.Data["alpha_ini"];
        }
        
		protected void UpdateActorWithAlpha (ClutterFlowBaseActor actor, double alpha) {
			float ratio = Math.Min (0.75f * (float) coverManager.Timeline.Delta / (float) coverManager.VisibleCovers, 1.25f);
			OffsetRotationAngle = (float) ((rotationAngle / 5) * ratio);
			OffsetCenterX = -(float) ((CenterMargin / 2) * ratio);
			OffsetZNear = (int) (-(ZNear-ZFar) * 1.5 * ratio);
			if (coverManager.Timeline.Direction!=TimelineDirection.Forward) {
				OffsetRotationAngle = -OffsetRotationAngle;
				OffsetCenterX = -OffsetCenterX;
			}
			
			actor.SetScale (coverWidth/actor.Width, coverWidth/actor.Width);
			if (alpha!=0 || alpha!=1) {
                //if (!visible_actors.Contains (actor)) visible_actors.Add (actor);
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
			} else {
                //if (visible_actors.Contains (actor)) visible_actors.Remove (actor);
                actor.Hide ();
            }
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

        protected const int fade_slow = 1;
        Score transition_score;
        Timeline t_slide;
        protected struct TransitionStruct {            
            public List<ClutterFlowBaseActor> OldCovers;
            public List<ClutterFlowBaseActor> PersistentCovers;
            public List<ClutterFlowBaseActor> NewCovers;
            public EventHandler OnCompletedHandler;
            public double NewProgress;
            
            public TransitionStruct (List<ClutterFlowBaseActor> old_covers, List<ClutterFlowBaseActor> persistent_covers, List<ClutterFlowBaseActor> new_covers, double new_progress, EventHandler on_completed) {
                OldCovers = old_covers;
                PersistentCovers = persistent_covers;
                NewCovers = new_covers;
                OnCompletedHandler = on_completed;
                NewProgress = new_progress;
            }

            public bool IsFilled {
                get {
                    return OldCovers!=null && 
                           PersistentCovers!=null &&
                           NewCovers !=null &&
                           OnCompletedHandler != null &&
                           !double.IsNaN(NewProgress);
                }
            }
        }
        Queue<TransitionStruct> transition_queue = new Queue<TransitionStruct>();
        protected void FadeCoversInAndOut (TransitionStruct trans) {
            FadeCoversInAndOut (trans.OldCovers, 
                                trans.PersistentCovers, 
                                trans.NewCovers,
                                trans.NewProgress, trans.OnCompletedHandler);
        }
		public void FadeCoversInAndOut (List<ClutterFlowBaseActor> old_covers, 
                                        List<ClutterFlowBaseActor> persistent_covers, 
                                        List<ClutterFlowBaseActor> new_covers, 
                                        double newProgress, EventHandler onCompleted)
		{
            if (old_covers==null || persistent_covers==null || new_covers==null) return;
            if (TransitionAnimationBusy) {
                transition_queue.Enqueue(new TransitionStruct(old_covers, persistent_covers, new_covers, newProgress, onCompleted));
            }
			if (fadeOutAnim!=null) { fadeOutAnim.CompleteAnimation (); fadeOutAnim = null; }
			if (fadeInAnim!=null) { fadeInAnim.CompleteAnimation (); fadeInAnim = null; }
            if (t_slide!=null) { t_slide.Advance(t_slide.Duration); t_slide = null; }
            
			//Fade out removed covers
			foreach (ClutterFlowBaseActor cover in old_covers) {
				if (cover!=null) {
					FadeOutActor (cover);
                    fadeOutAnim.Timeline.Pause ();
				}
			}
            //Set up code for sliding of persistent covers:
            foreach (ClutterFlowBaseActor cover in persistent_covers) {
                if (cover!=null) {
                    if (cover.Data.ContainsKey ("alpha_ini")) cover.Data.Remove ("alpha_ini");
                    if (cover.Data.ContainsKey ("alpha_end")) cover.Data.Remove ("alpha_end");
                    cover.Data.Add ("alpha_ini", cover.LastAlpha);
                    cover.Data.Add ("alpha_end", AlphaFunc(cover));
                   // Console.WriteLine ("alpha_ini = " + cover.Data["alpha_ini"] + " and alpha_end = " + cover.Data["alpha_end"] + " for " + cover.Label.Replace ("\n", " - "));
                }
            }
            t_slide = new Timeline (CoverManager.MaxAnimationSpan*fade_slow);
            t_slide.NewFrame += delegate (object o, NewFrameArgs args) {
                foreach (ClutterFlowBaseActor cover in persistent_covers) {
                    if (cover!=null && cover.Data.ContainsKey ("alpha_ini") && cover.Data.ContainsKey ("alpha_end")) {
                        double alpha_end = (double) cover.Data["alpha_end"];
                        double alpha_ini = (double) cover.Data["alpha_ini"];
                        UpdateActorWithAlpha (cover, alpha_ini + (alpha_end - alpha_ini) * t_slide.Progress);
                    }
                }
            };
            //Prepare to fade in new covers
            foreach (ClutterFlowBaseActor cover in new_covers) {
                if (cover!=null) {
                    UpdateActorWithAlpha (cover, AlphaFunc(cover));
                    FadeInActor (cover);
                    fadeInAnim.Timeline.Pause ();
                }
            }
            if (transition_score!=null) {
                GC.SuppressFinalize (transition_queue);
                transition_score.Completed -= HandleScoreCompleted;
                transition_score.Stop ();
                transition_score.Dispose ();
                transition_score = null;
            }
            transition_score = new Score();
            transition_score.Completed += HandleScoreCompleted;

            Timeline parent = null;
            if (fadeOutAnim!=null && fadeOutAnim.Timeline!=null) {
                transition_score.Append(parent, fadeOutAnim.Timeline);
                parent = fadeOutAnim.Timeline;
            }
            if (t_slide!=null) {
                transition_score.Append(parent, t_slide);
                parent = t_slide;
            }
            if (fadeInAnim!=null && fadeInAnim.Timeline!=null) {
                transition_score.Append(parent, fadeInAnim.Timeline);
                parent = fadeInAnim.Timeline;
            }
            
            SetPreviousBounds (newProgress);
            CoverManager.SortDepthOrder ();
            if (parent!=null) {
                transition_score.Completed += onCompleted;
                transition_score.Start ();
            } else {
                onCompleted (this, EventArgs.Empty);
            }
		}

        private void HandleScoreCompleted(object sender, EventArgs e)
        {
            if (transition_queue != null && transition_queue.Count > 0)
                FadeCoversInAndOut (transition_queue.Dequeue());
        }
		
		public void CreateClickedCloneAnimation (ClutterFlowBaseActor actor) {
			if (actor.Parent!=null) {
				Clone clone = new Clone(actor);
				MoveAndRotateActorCentrally (clone, 0);
				double scaleX, scaleY; actor.GetScale (out scaleX, out scaleY); clone.SetScale (scaleX, scaleY);

				((Container) actor.Parent).Add (clone);
				clone.ShowAll ();
				clone.Opacity = 255;
				clone.Raise (actor);
				Animation anmn = clone.Animatev ((ulong) AnimationMode.EaseInExpo.value__, CoverManager.MaxAnimationSpan*4, new string[] { "opacity" }, new GLib.Value ((byte) 50));
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
                GC.SuppressFinalize (actor);
                actor.Destroy ();
                actor = null;
				(sender as Animation).Completed -= HandleClickedCloneCompleted;
			}
		}
		
		protected Animation fadeOutAnim;		
		public void FadeOutActor (Actor actor)
		{
			actor.Show ();
			if (fadeOutAnim==null)
				fadeOutAnim = actor.Animatev ((ulong) Clutter.AnimationMode.Linear.value__, CoverManager.MaxAnimationSpan*fade_slow, new string[] { "opacity" }, new GLib.Value ((byte) 0));
			else
				actor.AnimateWithTimelinev ((ulong) Clutter.AnimationMode.Linear.value__, fadeOutAnim.Timeline, new string[] { "opacity" }, new GLib.Value ((byte) 0));
		}
		
		protected Animation fadeInAnim;
		public void FadeInActor (Actor actor)
		{
            byte opacity = actor.Opacity;
            actor.Opacity = 0;
            actor.Show();
			if (fadeInAnim==null)
				fadeInAnim = actor.Animatev ((ulong) Clutter.AnimationMode.Linear.value__, CoverManager.MaxAnimationSpan*fade_slow, new string[] { "opacity" }, new GLib.Value ((byte) opacity));
			else
                actor.AnimateWithTimelinev ((ulong) Clutter.AnimationMode.Linear.value__, fadeInAnim.Timeline, new string[] { "opacity" }, new GLib.Value ((byte) opacity));
		}
		#endregion
	}	
}
