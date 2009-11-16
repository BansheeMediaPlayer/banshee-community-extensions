
using System;
using Clutter;

namespace Banshee.ClutterFlow
{
	
	public class NewFrameEventArgs : EventArgs 
	{
		public double Progress;
		public NewFrameEventArgs (double progress) : base ()
		{ 
			Progress = progress;
		}
	}
	
	public class TargetReachedEventArgs : EventArgs 
	{
		public uint Target;
		public TargetReachedEventArgs (uint target) : base ()
		{ 
			Target = target;
		}
	}
	
	public class ThrottledTimeline : IDisposable
	{
		
		#region fields
		public event EventHandler<NewFrameEventArgs> NewFrame;
		protected void InvokeNewFrameEvent () 
		{
			if (NewFrame!=null) NewFrame (this, new NewFrameEventArgs (progress));
		}
		
		public event EventHandler<TargetReachedEventArgs> TargetMarkerReached;
		protected void InvokeTargetReached() {
			if (TargetMarkerReached!=null) TargetMarkerReached(this, new TargetReachedEventArgs(Target));
		}
		
		protected uint funcId;
		
		protected TimelineDirection direction = TimelineDirection.Forward;
		
		protected uint target = 0;
		public virtual uint Target {
			get { return target; }
			set {
				if (value >= indexCount) value = indexCount-1;
				if (value < 0) value = 0;
				if (value > AbsoluteProgress) {
					target = value;
					direction = TimelineDirection.Forward;
				} else if (value < AbsoluteProgress) {
					target = value;
					direction = TimelineDirection.Backward;
				}
				delta = (int) Math.Abs(AbsoluteProgress - Target);
			}
		}
		
		public double RelativeTarget {
			get { return target/ (double) (indexCount-1); }
		}
		
		protected double progress = 0;
		public virtual double Progress {
			get { return progress; }
			set { 
				if (value > 1) value = 1;
				if (value < 0) value = 0;
				progress = value;
				delta = (int) Math.Abs(AbsoluteProgress - Target);
			}
		}
		
		public double AbsoluteProgress {
			get { return (double) (progress*(indexCount-1)); }
		}
		
		int delta = 0;
		public int Delta {
			get { return delta;	}
		}
		
		protected double frequency = 0.004;	//indeces per millisecond
		public virtual double Frequency {
			get { return frequency; }
			set { 
				if (value < 0) value = 0;
				frequency = value;
			}
		}
		protected DateTime lastTime = DateTime.Now;		//last iteration timestamp
		
		protected uint indexCount = 0;
		public uint IndexCount {
			get { return indexCount; }
		}
		
		protected bool isPlaying = false;
		public bool IsPlaying {
			get { return isPlaying; }
			set { isPlaying = value; }
		}
		
		#endregion
		
		public ThrottledTimeline ()
		{
			funcId = Clutter.Threads.AddRepaintFunc(RepaintFunc);
		}
		
		public ThrottledTimeline (uint indexCount, double frequency) : this()
		{
			SetIndexCount(indexCount);
			Frequency = frequency;
		}
		
		protected bool RepaintFunc ()
		{
			DateTime now = DateTime.Now;
			if (IsPlaying) {
				double timeDelta = (now - lastTime).Milliseconds;
				if (direction==TimelineDirection.Forward) {
					progress +=	timeDelta * Frequency / (double) (indexCount-1);
					if (target<=AbsoluteProgress) {
						isPlaying = false;
						progress = RelativeTarget;
						InvokeTargetReached();
					}
				} else {
					progress -= timeDelta * Frequency / (double) (indexCount-1);
					if (target>=AbsoluteProgress) {
						isPlaying = false;
						progress = RelativeTarget;
						InvokeTargetReached();
					}
				}
			}
			lastTime = now;
			InvokeNewFrameEvent ();
			return true; //keep on calling this function
		}
		
		public void Start ()
		{
			lastTime = DateTime.Now;
			IsPlaying = true;
		}
		
		public void Halt ()
		{
			IsPlaying = false;
		}
		
		public void AdvanceToTarget (uint target)
		{
			Target = target;
			if (!IsPlaying) IsPlaying = true;
		}
		
		public void SetIndexCount (uint newCount) 
		{
			SetIndexCount(newCount, true, true);
		}
		public virtual void SetIndexCount (uint newCount, bool scaleProgress, bool scaleTarget)
		{
			if (!scaleProgress && newCount > 0) Progress = (double) AbsoluteProgress / newCount;
			if (scaleTarget && IndexCount > 0)
				Target = (uint) (RelativeTarget * newCount);
			else 
				Target = target;
			indexCount = newCount;
		}
		
		public void Dispose ()
		{
			Clutter.Threads.RemoveRepaintFunc(funcId);
		}
	}
	
	public class ClutterFlowTimeline : ThrottledTimeline
	{
		#region Fields
		protected int lastMarker = 0;
		protected int lastDelta = 0;
		
		protected double[] frequencies = new double[5];
		public override double Frequency {
			get { return frequencies[SpeedClass]; }
		}
		
		public int SpeedClass {
			get {
				if (Delta <= 2)
					return 0;
				else if (Delta > 2 && Delta <= 4)
					return 1;
				else if (Delta > 4 && Delta <= 8)
					return 2;
				else if (Delta > 8 && Delta <= 16)
					return 3;
			 	else
					return 4;
			}
		}
				
		protected CoverManager coverManager;
		public CoverManager CoverManager {
			get { return coverManager; }
			set {
				if (value!=coverManager) {
					if (coverManager!=null) {
						coverManager.CoversChanged -= HandleCoversChanged;
						coverManager.TargetIndexChanged -= HandleTargetIndexChanged;
					}
					coverManager = value;
					if (coverManager!=null)
						coverManager.CoversChanged += HandleCoversChanged;
					coverManager.TargetIndexChanged += HandleTargetIndexChanged;
				}
			}
		}


		#endregion
		
		public ClutterFlowTimeline (CoverManager coverManager) : base((uint) coverManager.TotalCovers, 1 / (double) CoverManager.MaxAnimationSpan)
		{
			this.CoverManager = coverManager;
			SetupFrequencies();
		}
		
		protected void SetupFrequencies ()
		{
			for (int i=0; i < frequencies.Length; i++)
				frequencies[i] = 1 / (double) (CoverManager.MaxAnimationSpan - (CoverManager.MaxAnimationSpan - CoverManager.MinAnimationSpan) * ((double) i / (double) (frequencies.Length-1)));
		}
		
		#region Event Handlers
		protected void HandleCoversChanged(object sender, EventArgs e)
		{
			SetIndexCount ((uint) coverManager.TotalCovers, false, false);
		}
		
		protected void HandleTargetIndexChanged(object sender, EventArgs e)
		{
			AdvanceToTarget((uint) coverManager.TargetIndex);
		}
		
		/*protected void CheckSpeed ()
		{
			uint delta = (uint) Math.Abs(AbsoluteProgress - Target);
			if (delta <= 2)
				frequency = 1 / (double) CoverManager.MaxAnimationSpan;
			else if (delta > 2 && delta <= 4)
				frequency = 1 / (double) (CoverManager.MinAnimationSpan + (CoverManager.MaxAnimationSpan - CoverManager.MinAnimationSpan) * 0.75);
			else if (delta > 4 && delta <= 8)
				frequency = 1 / (double) (CoverManager.MinAnimationSpan + (CoverManager.MaxAnimationSpan - CoverManager.MinAnimationSpan) * 0.50);
			else if (delta > 8 && delta <= 16)
				frequency = 1 / (double) (CoverManager.MinAnimationSpan + (CoverManager.MaxAnimationSpan - CoverManager.MinAnimationSpan) * 0.25);
			else if (delta > 16)
				frequency = 1 / (double) CoverManager.MinAnimationSpan;
		}*/
		#endregion
		
	}
}
