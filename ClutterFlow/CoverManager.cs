
using System;
using System.Collections.Generic;
//using Banshee.Collection;
//using Hyena.Data;

using Clutter;
using GLib;

namespace ClutterFlow
{
	
	public delegate void CoverEventHandler(ClutterFlowActor actor, EventArgs e);
	
	public class CoverManager : Clutter.Group {
			
		public event CoverEventHandler NewCurrentCover;
		protected void InvokeNewCurrentCover(ClutterFlowActor cover) {
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
		
		internal List<ClutterFlowActor> covers;		// list with cover actors
		private ClutterFlowActor currentCover = null; // currently centered cover
		public ClutterFlowActor CurrentCover {
			get { return currentCover; }
			set {
				if (value!=currentCover) {
					currentCover = value;
					InvokeNewCurrentCover (currentCover);
				}
			}
		}

		protected IActorLoader actorLoader;	//Loads the actors (detaches C;utterFlow from Banshee related dependencies)
		internal IActorLoader ActorLoader {
			get { return actorLoader; }
			set { actorLoader = value; }
		}
		
		public int TotalCovers	{				// number of covers or zero if null
			get { return (covers != null) ? covers.Count : 0; }
		}
		
		protected FlowBehaviour behaviour;
		public FlowBehaviour Behaviour {
			get { return behaviour; }
		}
		
		protected int maxCoverWidth = 256;
		public int MaxCoverWidth {
			get { return maxCoverWidth; }
			set { 
				if (maxCoverWidth!=value) {
					maxCoverWidth = value;
					UpdateBehaviour(); //FIXME use a flag instead. Multiple update calls may cause slowdowns?
				}
			}
		}
		protected int minCoverWidth = 64;
		public int MinCoverWidth {
			get { return minCoverWidth; }
			set { 
				if (minCoverWidth!=value) {
					minCoverWidth = value;
					UpdateBehaviour(); //FIXME use a flag instead. Multiple update calls may cause slowdowns?
				}
			}
		}

		public event EventHandler TextureSizeChanged;

		protected int textureSize = 128;
		public int TextureSize {
			get { return textureSize; }
			set {
				if (textureSize!=value) {
					textureSize = value;
					if (TextureSizeChanged!=null) TextureSizeChanged(this, EventArgs.Empty); //FIXME use a flag instead. Multiple update calls may cause slowdowns?
				}
			}
		}
		
		protected int visibleCovers = 17;
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
				if (value!=targetIndex) {
					targetIndex = value;
					currentCover = null; //to prevent clicks to load the old centered cover!
					InvokeTargetIndexChanged();
				}
			}
		}

		public ClutterFlowActor TargetActor {
			get {
				if (covers.Count > targetIndex)
					return covers[targetIndex];
				else
					return null;
			}
		}

		protected bool needsReloading = true;
		public bool NeedsReloading {
			get { return needsReloading; }
			internal set { needsReloading = value; }
		}
		
		protected bool enabled = false;
		public bool Enabled {
			get { return enabled; }
			set {
				enabled = value;
				behaviour.HoldUpdates = !value;
				if (enabled && needsReloading) {
					ReloadCovers();
					needsReloading = false;
				}
			}
		}
		
		public CoverManager () : base ()
		{
			UpdateTimeline(2); //Dummy timeline
			behaviour = new FlowBehaviour (this);
		}
		
		public void UpdateBehaviour () 
		{
			if (behaviour!=null && Stage!=null) {
				behaviour.Height = Stage.Height;
				behaviour.Width = Stage.Width;
				behaviour.CoverWidth = Math.Max(Math.Min(Stage.Height*0.5f, maxCoverWidth), minCoverWidth);;
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
			//Hyena.Log.Information("HandleTargetMarkerReached --> covers.Count == " + covers.Count);
			if (args.Target==TargetIndex) {
				if (TotalCovers==0)
					CurrentCover = null;
				else
					CurrentCover = covers[(int) args.Target];
			}
		}
		
		internal void ReloadCovers () 
		{
			if (Timeline!=null) Timeline.Halt ();
			//Hyena.Log.Information ("Updating Covers");
			if (covers!=null && covers.Count!=0) {
				behaviour.HoldUpdates = true;		
				
				int old_current_index = CurrentCover!=null ? covers.IndexOf (CurrentCover) : 0;
				List<ClutterFlowActor> old_covers = new List<ClutterFlowActor>();
				old_covers.AddRange(SafeGetRange(covers, old_current_index - HalfVisCovers, visibleCovers));
				foreach (ClutterFlowActor cover in covers) {
					cover.Index = -1;
					if (!old_covers.Contains(cover)) cover.Hide();
				}
				
				bool keepCurrent = false;
				covers = actorLoader.GetActors (delegate (ClutterFlowActor actor) {
						if (currentCover==actor) keepCurrent = true;
				});
				UpdateTimeline(covers.Count);

				//recalculate timeline progression and the target index
				int newTargetIndex = 0;
				Timeline.Progress = 0;
				if (covers.Count > 1) {
					Timeline.Progress = (float) newTargetIndex/ (float) (covers.Count-1);
					if (keepCurrent)
						newTargetIndex = currentCover.Index;
					else
						newTargetIndex = (int) Math.Round(Timeline.Progress * (covers.Count-1));		
				}
				
				List<ClutterFlowActor> new_covers = new List<ClutterFlowActor>(SafeGetRange(covers, newTargetIndex - HalfVisCovers, visibleCovers));
				behaviour.FadeCoversInAndOut (old_covers, new_covers, Timeline.Progress, delegate (object o, EventArgs e) {
					behaviour.HoldUpdates = false;
					TargetIndex = newTargetIndex;
					InvokeCoversChanged();
				});
				
			} else {
				covers = actorLoader.GetActors (null);
				currentCover = covers[0];
   				InvokeCoversChanged();
   				TargetIndex = 0;
			}
		}
		

		private IEnumerable<ClutterFlowActor> SafeGetRange(List<ClutterFlowActor> list, int index, int count) {
			for (int i = index; i < index + count; i++) {
				ClutterFlowActor cover;
				try {
					cover = list[i];
				} catch {
					cover = null;
				}
				yield return cover;
			}
			yield break;
		}
		
		public void ForSomeCovers(System.Action<ClutterFlowActor> method_call, int lBound, int uBound) {
			if (covers!=null && lBound <= uBound && lBound >= 0 && uBound < covers.Count) {
				IEnumerator<ClutterFlowActor> enumerator = covers.GetRange(lBound, uBound-lBound + 1).GetEnumerator();
				while (enumerator.MoveNext())
					method_call(enumerator.Current); 
			}
		}
		
		public void ForEachCover(System.Action<ClutterFlowActor> method_call) {
			if (covers!=null) {
				IEnumerator<ClutterFlowActor> enumerator = covers.GetEnumerator();
				while (enumerator.MoveNext())
					method_call(enumerator.Current);
			}
		}
	}
}