
using System;
using System.Collections.Generic;
using Banshee.Collection;
using Hyena.Data;

using Clutter;
using GLib;

namespace Banshee.ClutterFlow
{
	
	public delegate void CoverEventHandler(CoverGroup cover, EventArgs e);
	
	public class CoverManager : Clutter.Group {
			
		public event CoverEventHandler NewCurrentCover;
		protected void InvokeNewCurrentCover(CoverGroup cover ) {
			if (NewCurrentCover!=null) {
				NewCurrentCover(cover, EventArgs.Empty);
			}
		}
		
		public event EventHandler<EventArgs> CoversChanged;
		protected void InvokeCoversChanged() {
			if (CoversChanged!=null) CoversChanged(this, EventArgs.Empty);
		}
		
		public event EventHandler<EventArgs> TargetIndexChanged;
		protected void InvokeTargetIndexChanged() {
			if (TargetIndexChanged!=null) TargetIndexChanged(this, EventArgs.Empty);
		}
		
		protected ClutterFlowTimeline timeline;
		public ClutterFlowTimeline Timeline {
			get { 
				if (timeline==null) UpdateTimeline ();
				return (timeline==null) ? null : timeline; 
			}
		}
		
		private List<CoverGroup> covers;		// list with cover actors
		private CoverGroup currentCover = null; // currently centered cover
		public CoverGroup CurrentCover {
			get { return currentCover; }
			set {
				if (value!=currentCover) {
					currentCover = value;
					InvokeNewCurrentCover (currentCover);
				}
			}
		}
		public AlbumInfo CurrentAlbum {
			get { return CurrentCover==null ? null : CurrentCover.Album; }
		}
		public int TotalCovers	{				// number of covers or zero if null
			get { return (covers != null) ? covers.Count : 0; }
		}	
		
		protected CoverGroupCache cover_cache;
		public virtual CoverGroupCache CoverCache {
			get { return cover_cache; }
		}
		
		protected FlowBehaviour behaviour;
		public FlowBehaviour Behaviour {
			get { return behaviour; }
		}
		
		private IListModel<AlbumInfo> model;
		public IListModel<AlbumInfo> Model {
			get { return model; }
			set {
				if (value!=model) {
		            if (model != null) {
		                model.Cleared -= OnModelClearedHandler;
		                model.Reloaded -= OnModelReloadedHandler;
		            }
					
					model = value;
		
		            if (model != null) {
		                model.Cleared += OnModelClearedHandler;
		                model.Reloaded += OnModelReloadedHandler;
		            }
		            ReloadCovers ();
				}
			}
		}
		
		protected int visibleCovers = 15;
		public EventHandler<EventArgs> VisibleCoversChanged;
		public int VisibleCovers {
			get { return visibleCovers; }
			set {
				if (value!=visibleCovers) {
					visibleCovers = value;
					if (VisibleCoversChanged!=null) VisibleCoversChanged(this, EventArgs.Empty);
				}
			}
		}
		public int HalfVisCovers {
			get { return (int) ((visibleCovers-1) * 0.5); }
		}
		protected static uint maxAnimationSpan = 250; //maximal length of a single step in the animations in ms
		public static uint MaxAnimationSpan {
			get { return maxAnimationSpan; }
		}
		
		protected static uint minAnimationSpan = 10; //minimal length of a single step in the animations in ms
		public static uint MinAnimationSpan {
			get { return minAnimationSpan; }
		}
		
		private int targetIndex = 0;			// curent targetted cover index
		public int TargetIndex {
			get { return targetIndex; }
			set {
				if (value < 0) value = 0;
				else if (value >= TotalCovers) value = TotalCovers-1;
				if (!disableMovement && value!=targetIndex) {
					targetIndex = value;
					InvokeTargetIndexChanged();
				}
			}
		}
		
		private bool disableMovement = false;	// wether or not index setting is allowed
		
		public CoverManager () : base ()
		{
			UpdateTimeline(2); //Dummy timeline
			cover_cache = new CoverGroupCache (this);
			behaviour = new FlowBehaviour(this);
		}
		
		public void UpdateBehaviour () 
		{
			if (behaviour!=null && Stage!=null) {
				behaviour.Height = Stage.Height;
				behaviour.Width = Stage.Width;
				behaviour.CoverWidth = 0.5f*Stage.Height;
			}
		}
		
		protected void UpdateTimeline () 
		{
			UpdateTimeline(TotalCovers);
		}
		
		protected void UpdateTimeline (int CoverCount) 
		{
			if (CoverCount > 0) {
		 		if (timeline==null) {
					timeline = new ClutterFlowTimeline(this);
					timeline.TargetMarkerReached += HandleTargetMarkerReached;
				} else
					timeline.SetIndexCount((uint) CoverCount, false, false);
			}
		}

		void HandleTargetMarkerReached(object sender, TargetReachedEventArgs args)
		{
			if (args.Target==TargetIndex) {
				int currentIndex = (int) args.Target;
				CurrentCover = covers[currentIndex];
			}
		}
		
		protected void ReloadCovers () 
		{
			int newTargetIndex = 0;
			if (Timeline!=null) Timeline.Halt ();
			if (covers==null || covers.Count==0) {
				Hyena.Log.Information ("Loading Covers");
				disableMovement = true;
				LoadCovers (null);
				if (covers.Count!=0) currentCover = covers[0];
				disableMovement = false;
			} else if (!disableMovement) {
				disableMovement = true;
				
				Hyena.Log.Information ("Reloading Covers");
				
				//Step 1: setup new and old lists:
				int old_current_index = covers.IndexOf (CurrentCover);
				float current_ipos = (float) old_current_index / (float) (covers.Count-1);
				foreach (CoverGroup cover in covers) {
					cover.Index = -1;
				}
				bool keep_current = false;
				LoadCovers(delegate (CoverGroup cover) {
					if (cover==CurrentCover) keep_current = true;
					cover.Show();
				});
				newTargetIndex = keep_current ? covers.IndexOf(CurrentCover) : (int) Math.Round(current_ipos * (covers.Count-1));
				//List<CoverGroup> new_covers = new List<CoverGroup>(SafeGetRange(covers,TargetIndex - HalfVisCovers, visibleCovers));
				disableMovement = false;
			}
			InvokeCoversChanged();
			TargetIndex = newTargetIndex;
		}
		
		private void LoadCovers(System.Action<CoverGroup> method_call) {
			covers = new List<CoverGroup>();
			UpdateTimeline(model.Count);
			for (int i = 1; i < model.Count; i++) {
				AlbumInfo album = (model as IListModel<AlbumInfo>)[i];
				CoverGroup cover = AddCover(album, i);
				if (method_call!=null) method_call(cover);
			}
		}
		/*private IEnumerable<CoverGroup> SafeGetRange(List<CoverGroup> list, int index, int count) {
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
		}*/
		
		public void ForSomeCovers(ForEachCover method_call, int lBound, int uBound) {
			if (covers!=null && lBound <= uBound && lBound >= 0 && uBound < covers.Count) {
				IEnumerator<CoverGroup> enumerator = covers.GetRange(lBound, uBound-lBound + 1).GetEnumerator();
				while (enumerator.MoveNext())
					method_call(enumerator.Current); 
			}
		}
		
		public void ForEachCover(ForEachCover method_call) {
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
			CoverGroup cover = CoverCache.GetCoverGroupFromAlbum(album);
			if (index >= covers.Count) covers.Add(cover);
			else covers.Insert(index, cover);
			if (cover.Parent!=this)
				Add(cover);
			//cover.Hide();
			cover.Index = (index-1);
			return cover;
		}
		
		#region Event Handlers
        protected void OnModelClearedHandler (object o, EventArgs args)
        {
			ReloadCovers ();
        }
        
        protected void OnModelReloadedHandler (object o, EventArgs args)
        {
			ReloadCovers ();
        }
		#endregion
	}
}
/*
 * 
 * 	public struct AnimationValues {
		
		public float X;
		public float Y;
		public float Z;
		public double RotY;
		public byte A;

		public AnimationValues (float X, float Y, float Z, double RotY, byte A) 
		{
			this.X = X;
			this.Y = Y;
			this.Z = Z;
			this.RotY = RotY;
			this.A = A;
		}
		
	   public override string ToString ()
	   {
	      return String.Format("AnimationValues[{0},{1},{2},{3},{4},{5}]", X, Y, Z, RotY, A);
	   }

	}
	
	public abstract class AnimationManager
	{
		
		protected uint animation_span = 250;							//animations are this long (ms)
		
		public event NewFrameHandler NewFrame;
		protected void InvokeNewFrameEvent(object o, NewFrameArgs args) {
			if (NewFrame!=null) NewFrame(o, args);
		}
		
		protected Timeline timeline;
		public virtual Timeline Timeline {
			get { return timeline; }
			set {
				if (value!=timeline) {
					if (timeline!=null) {
						timeline.Completed -= OnTimelineCompleted;
						timeline.NewFrame -= InvokeNewFrameEvent;
						timeline.Dispose ();
					}
					timeline = value;
					if (timeline!=null) {
						timeline.Completed += OnTimelineCompleted;
						timeline.NewFrame += InvokeNewFrameEvent;
					}
				}
			}
		}
		
		public virtual uint MiddlePosition { 
			get { return (positions - 1) / 2; }
		}
		
		protected AnimationValues[] anm_vals;
		public virtual AnimationValues this[uint index] {
			get { return anm_vals[index]; }
		}
		
		protected uint positions = 1;
		public virtual uint Positions {
			get { return positions; }
		}

		public bool IsAnimating {
			get {
			 	try {
					return timeline!=null ? timeline.IsPlaying : false; 
				}
				catch {
					return false;
				}
			}
		}
		

			
		public AnimationManager(uint positions)
		{		
			this.positions = positions;
			SetupPositions ();
			SetupCache ();
		}
		
		protected virtual void SetupCache ()
		{
			cover_cache = new CoverGroupCache (this);
		}
		
		public virtual void PauseAndClearTimeline () 
		{
			timeline.Pause ();
			Timeline = null;
			InvokeStoppedAnimation (EventArgs.Empty);
		}
		
		protected virtual void SetupPositions () 
		{
			throw (new System.NotImplementedException ("SetupPositions is not implemented, did you forget to override?"));
		}

		#region Animation Methods
		public virtual void AnimateSynchronously (Actor actor, string[] prop, GLib.Value val) 
		{
			if (timeline==null) {
				Animation anmn = actor.Animatev ((ulong) AnimationMode.Linear.value__, animation_span, prop, val);
				Timeline = anmn.Timeline;
			} else {
				actor.AnimateWithTimelinev ((ulong) AnimationMode.Linear.value__,timeline, prop, val);
			}
		}
		
		public virtual void Animate (Actor actor, uint position) 
		{
			AnimateAndReturn (actor, position, timeline);
		}
		public virtual void AnimateLast (Actor actor) 
		{
			Animate (actor, positions-1);
		}
		public virtual void AnimateFirst (Actor actor) 
		{
			Animate (actor, 0);
		}
		protected virtual Animation AnimateAndReturn (Actor actor, uint position, Timeline timeline)
		{
			AnimationValues anm_val = this[position];
			GLib.Value X = new GLib.Value (anm_val.X);
			GLib.Value Y = new GLib.Value (anm_val.Y);
			GLib.Value Z = new GLib.Value (anm_val.Z);
			GLib.Value A = new GLib.Value (anm_val.A);
			GLib.Value R = new GLib.Value (anm_val.RotY);
			
			actor.ShowAll ();
			Animation anmn = actor.Animatev ((ulong) AnimationMode.Linear.value__, animation_span,new string[] { "x" }, X);
			Timeline = anmn.Timeline;
			actor.AnimateWithTimelinev ((ulong) AnimationMode.Linear.value__,Timeline,new string[] { "y" }, Y);
			actor.AnimateWithTimelinev ((ulong) AnimationMode.Linear.value__,Timeline,new string[] { "depth" }, Z);
			actor.AnimateWithTimelinev ((ulong) AnimationMode.Linear.value__,Timeline,new string[] { "opacity" }, A);
			actor.AnimateWithTimelinev ((ulong) AnimationMode.Linear.value__,Timeline,new string[] { "rotation-angle-y" }, R);
			
			return anmn;
		}
		
		public virtual void Apply (Actor actor, uint position) 
		{
			AnimationValues anm_val = this[position];
			actor.SetPosition (anm_val.X, anm_val.Y);
			actor.Depth = anm_val.Z;
			actor.Opacity = anm_val.A;
			actor.SetRotation (RotateAxis.Y, anm_val.RotY, 0,0,0);
			if (actor.Opacity>0) actor.ShowAll ();
			else actor.HideAll ();
		}
		
		public virtual void ApplyLast (Actor actor)
		{
			Apply (actor, positions-1);
		}
		public virtual void ApplyFirst (Actor actor) 
		{
			Apply (actor, 0);
		}
		#endregion
		#region Event Handling
		public event System.EventHandler FinishedAnimation;
		protected virtual void InvokeFinishedAnimation (EventArgs args) 
		{
			if (FinishedAnimation!=null) FinishedAnimation(this, args);
		}
		
		protected virtual void OnTimelineCompleted (object sender, EventArgs args)
		{
			InvokeFinishedAnimation(args);
		}
		
		public event System.EventHandler StoppedAnimation;
		protected virtual void InvokeStoppedAnimation (EventArgs args)
		{
			if (StoppedAnimation!=null) StoppedAnimation(this, args);
		}
		#endregion
	}
	
	public class ClutterFlowAnmMgr : AnimationManager
	{
		private const float rotationAngle = 50;						//covers are tilted by this angle at the sides 		
		private const float center = 0.5f;							//central cover at this position (rel. to stage size)
		private const float margin = 0.8f;							//margin at central covers sides (rel. to cover size)
		private const float spacing = 0.1f;							//spacing between covers at the sides  (rel. to stage width)
		
		private const float z_far = 50;
		private const float z_near = 150;
		
		public void ListenForFinishedAnimation (System.EventHandler method_call) 
		{
			if (method_call!=null) {
				if (timeline==null)
					method_call(this, EventArgs.Empty);
				else
					this.FinishedAnimation += method_call;
			}
		}
		
		public void ListenForFinishedFadeAnimations (System.EventHandler method_call) 
		{ 
			if (method_call!=null) {
				if (fade_timeline==null)
					method_call(this, EventArgs.Empty);
				else
					this.FinishedFadeAnimations += method_call;
			}
		}
		
		protected Timeline fade_timeline;
		public virtual void SetFadeTimeline (Timeline fade_timeline) 
		{
			if (this.fade_timeline!=null) this.fade_timeline.Completed -= OnFadeTimelineCompleted;
			this.fade_timeline = fade_timeline;
			if (this.fade_timeline!=null) {
				this.fade_timeline.Completed += OnFadeTimelineCompleted;
				fade_timeline.Start();
			}
		}
		protected virtual void OnFadeTimelineCompleted (object sender, EventArgs args)
		{
			if (FinishedFadeAnimations!= null) FinishedFadeAnimations(this, args);
		}
		public event EventHandler FinishedFadeAnimations;
		
		protected AnimationValues[] scaled_anm_vals;
		public override AnimationValues this[uint index] {
			get { return scaled_anm_vals[index]; }
		}
		
		private float scale_width = 900;
		public float ScaleWidth {
			get { return scale_width; }
		}
		private float scale_height = 300;
		public float ScaleHeight {
			get { return scale_height; }
		}	
		private float cover_size = 100;
		public float CoverSize {
			get { return cover_size; }
		}
		public void SetScale (float scaleWidth, float scaleHeight, float coverSize) 
		{
			scale_height = scaleHeight;
			scale_width = scaleWidth;
			cover_size = coverSize;
			UpdateScale();
		}
		protected void UpdateScale () 
		{
			for (int i=0; i < positions; i++) {
				scaled_anm_vals[i] = new AnimationValues (
					anm_vals[i].X*scale_width + Math.Sign(i - MiddlePosition)*margin*cover_size,
					anm_vals[i].Y*scale_height,
					anm_vals[i].Z,
					anm_vals[i].RotY,
					anm_vals[i].A
				);
			}
		}
		
		public ClutterFlowAnmMgr (uint positions) : base (positions)	{ }
		public ClutterFlowAnmMgr (uint positions, float scaleWidth, float scaleHeight, float coverSize) : base (positions)
		{
			SetScale (scaleWidth, scaleHeight, coverSize);
		}
		
		protected override void SetupPositions ()
		{		
			//initialize arrays:
			anm_vals = new AnimationValues[positions];
			scaled_anm_vals= new AnimationValues[positions];
			
			for (int i=0; i < positions; i++) {
				if (i>=0 && i < MiddlePosition) {
					float progress = (float) i / (float) (MiddlePosition-1);
					anm_vals[i] = new AnimationValues (
						center - spacing*(1-progress),
						center,
						z_far-(1-progress),
						rotationAngle,
						(byte) (progress==0 ? 0 : 255)
					);
				} else if (i == MiddlePosition) {
					anm_vals[i] = new AnimationValues (
						center,
						center,
						z_near,
						0,
						(byte) 255
					);
				} else {
					float progress = (float) (i - (MiddlePosition+1)) / (float) (MiddlePosition-1);
					anm_vals[i] = new AnimationValues (
						center + spacing*progress,
						center,
						z_far-progress,
						-rotationAngle,
						(byte) (progress==1 ? 0 : 255)
					);					
				}
			}
			UpdateScale ();
		}
		
		//TODO transform these animations in behaviour classes
		
		public void CreateFadeInAnimation (CoverGroup cover, uint position) {
			Apply(cover, position);
			if (fade_timeline==null) {
				Animation anmn = cover.Animatev((ulong) AnimationMode.Linear.value__, animation_span*2,new string[] { "fixed::opacity" }, new GLib.Value((byte) 0));
				SetFadeTimeline(anmn.Timeline);
			}
			cover.AnimateWithTimelinev((ulong) AnimationMode.Linear.value__,fade_timeline,new string[] { "opacity" }, new GLib.Value((byte) 255));			
		}
		
		public void CreateFadeOutAnimation(CoverGroup cover, ScrollDirection move_direction) {
			Animation anmn;
			switch(move_direction) {
			case ScrollDirection.Left:
				anmn = AnimateAndReturn(cover, 0, fade_timeline);
				break;
			case ScrollDirection.Right:
				anmn = AnimateAndReturn(cover, positions-1, fade_timeline);
				break;
			default:
				if (fade_timeline==null)
					anmn = cover.Animatev((ulong) AnimationMode.Linear.value__, animation_span*2,new string[] { "opacity" }, new GLib.Value((byte) 0));
				else
					anmn = cover.AnimateWithTimelinev((ulong) AnimationMode.Linear.value__, fade_timeline,new string[] { "opacity" }, new GLib.Value((byte) 0));
				break;
			}
			anmn.Completed += HandleFadeOutCompleted;
			if (fade_timeline==null) SetFadeTimeline(anmn.Timeline);
		}
		
		private bool lock_clickcloning = false;
		public void CreateClickedCloneAnimation(CoverGroup cover) {
			if (lock_clickcloning) return;
			
			if (cover.Parent!=null) {				
				lock_clickcloning = true;
				Clone clone = new Clone(cover);
				Apply(clone, MiddlePosition);
				
				((Group) cover.Parent).Add(clone);
				clone.ShowAll();
				Animation anmn = clone.Animatev((ulong) AnimationMode.Linear.value__, animation_span*2,new string[] { "depth" }, new GLib.Value(z_near+1));
				anmn.Completed += HandleClickedCloneCompleted;
				clone.AnimateWithTimelinev((ulong) AnimationMode.Linear.value__,anmn.Timeline,new string[] { "opacity" }, new GLib.Value((byte) 0));
				clone.AnimateWithTimelinev((ulong) AnimationMode.Linear.value__,anmn.Timeline,new string[] { "scale-x" }, new GLib.Value(2d));
				clone.AnimateWithTimelinev((ulong) AnimationMode.Linear.value__,anmn.Timeline,new string[] { "scale-y" }, new GLib.Value(2d));
				clone.AnimateWithTimelinev((ulong) AnimationMode.Linear.value__,anmn.Timeline,new string[] { "fixed::anchor-x" }, new GLib.Value(clone.Width/2));
				clone.AnimateWithTimelinev((ulong) AnimationMode.Linear.value__,anmn.Timeline,new string[] { "fixed::anchor-y" }, new GLib.Value(clone.Height/4));
			}
		}
		
		protected void HandleFadeOutCompleted (object sender, EventArgs e)
		{
			if (sender is Animation && (sender as Animation).Object is Actor) {
				Actor actor = (Actor) (sender as Animation).Object;
				actor.Hide();
				actor.Opacity = 0;
			}
		}
		
		protected void HandleClickedCloneCompleted(object sender, EventArgs e)
		{
			lock (this) {
				if (sender is Animation && (sender as Animation).Object is Actor) {
					Actor actor = (Actor) (sender as Animation).Object;
					actor.Destroy();
					actor.Dispose();
				}
			}
			lock_clickcloning = false;
		}
	}
*/