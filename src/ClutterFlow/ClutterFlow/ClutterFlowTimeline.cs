//
// ClutterFlowTimeline.cs
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
using Clutter;

namespace ClutterFlow
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

        #region Fields
        public event EventHandler<NewFrameEventArgs> NewFrame;
        protected void InvokeNewFrameEvent ()
        {
            if (NewFrame!=null) NewFrame (this, new NewFrameEventArgs (progress));
        }

        public event EventHandler<TargetReachedEventArgs> TargetMarkerReached;
        protected void InvokeTargetReached() {
            if (TargetMarkerReached!=null) TargetMarkerReached(this, new TargetReachedEventArgs(Target));
        }

        public TimelineDirection Direction {
            get { return Target>AbsoluteProgress ? TimelineDirection.Forward : TimelineDirection.Backward; }
        }

        protected uint target = 0;
        public virtual uint Target {
            get { return target; }
            set {
                //Console.WriteLine ("IndexCount is " + IndexCount + " value is " + value);
                if (value >= IndexCount) value = IndexCount-1;
                if (value < 0) value = 0;
                target = value;
                delta = (int) Math.Abs(AbsoluteProgress - Target);
                if (target == AbsoluteProgress) {
                    InvokeNewFrameEvent ();
                    InvokeTargetReached ();
                }
            }
        }

        public double RelativeTarget {
            get { return (IndexCount > 0 ? (double) Target / (double) (IndexCount-1) : 0.0); }
        }

        protected double progress = 0;
        public virtual double Progress {
            get { return progress; }
            set {
                if (value > 1) value = 1;
                if (value < 0) value = 0;
                if (double.IsInfinity(value) || double.IsNaN(value)) value = 0;
                progress = value;
                delta = (int) Math.Abs(AbsoluteProgress - Target);
                if (delta==0) InvokeTargetReached ();
            }
        }

        //// <value>
        /// This is the progress expressed as an index number. Returned as a double to prevent
        /// rounding errors in the code.
        /// </value>
        public double AbsoluteProgress {
            get { return (IndexCount > 0 ? (progress*(double)(IndexCount-1)) : 0); }
        }

        protected int delta = 0;
        public int Delta {
            get { return delta; }
        }

        protected int timeout = -1;
        public virtual int Timeout {
            get { return timeout; }
            set {
                last_time = DateTime.Now;
                timeout = value;
            }
        }

        protected static double time_threshold = 1000;          // threshold to assure visible animations
        protected static double target_fps = 30;                // target fps, TODO needs a setting
        protected static double target_tmd = 1000 / target_fps; // target timestep in ms;

        private readonly double frequency;
        protected virtual double Frequency {
            get { return frequency; }
        }
        protected DateTime last_time = DateTime.Now;   // last iteration timestamp

        protected uint index_count = 0;
        //// <value>
        /// The number of indeces currently set on this Timeline. This should be equal
        /// to the CoverManager.TotalCovers value.
        /// </value>
        public virtual uint IndexCount {
            set {
                if (value!= index_count) {
                    index_count = value;
                    InvokeNewFrameEvent ();
                }
            }
            get { return index_count; }
        }

        protected bool is_paused = false;
        public bool IsPaused {
            get { return is_paused; }
            set { is_paused = value; }
        }

        public bool CanPlay {
            get { return (Target != AbsoluteProgress); }
        }

        private bool run_frame_source = false;
        protected bool RunFrameSource {
            get { return run_frame_source; }
            set {
                if (run_frame_source!=value) {
                    run_frame_source = value;
                    if (value)
                        Clutter.Threads.AddFrameSourceFull (250, (uint) target_fps, RepaintFunc);
                }
            }
        }

        protected uint func_id;
        protected bool stop_timeout = false;
        #endregion

        public ThrottledTimeline ()
        {
            RunFrameSource = true;
            func_id = Clutter.Threads.AddRepaintFunc (RepaintFunc);
        }
        public virtual void Dispose ()
        {
            Clutter.Threads.RemoveRepaintFunc (func_id);
            RunFrameSource = false;
        }

        public ThrottledTimeline (uint index_count, double frequency) : this()
        {
            this.index_count = index_count;
            this.frequency = frequency;
        }

        public void JumpToIndex (uint value)
        {
            Progress = (double) value / (IndexCount-1);
        }

        public void JumpToTarget ()
        {
            JumpToIndex (Target);
        }

        protected virtual bool RepaintFunc ()
        {
            DateTime now = DateTime.Now;
            double time_delta = (now - last_time).Milliseconds;
            if (timeout != -1) {
                if (timeout <= time_delta) {
                    timeout = -1;
                    last_time = now;
                }
                return true;
            }
            if (time_delta > time_threshold) time_delta = time_threshold;
            if (time_delta >= target_tmd) { //if smaller we are at a higher fps than targetted
                //Console.Write ("RepaintFunc with IndexCount = " + IndexCount + "\t Target = " + Target + "\t AbsoluteProgress = " + AbsoluteProgress + " CanPlay == " + CanPlay + " IsPaused == " + IsPaused);
                if (!IsPaused && CanPlay) {
                    //Console.Write (" Moving ");
                    if (Target>AbsoluteProgress) {
                        //Console.Write (" Forward - Target is " + Target + " AbsoluteProgress is " + AbsoluteProgress + "\n");
                        Progress +=    time_delta * Frequency / (double) (IndexCount-1);
                        if (Target<=AbsoluteProgress)
                            Progress = RelativeTarget;
                    } else if (Target<AbsoluteProgress) {
                        //Console.Write (" Backward - Target is " + Target + " AbsoluteProgress is " + AbsoluteProgress + "\n");
                        Progress -= time_delta * Frequency / (double) (IndexCount-1);
                        //Console.Write (" - Afterwards progress is " + Progress + "\n");
                        if (Target>=AbsoluteProgress)
                            Progress = RelativeTarget;
                    }
                }
                last_time = now;
                InvokeNewFrameEvent ();
            }
            return RunFrameSource; //keep on calling this function
        }

        public void Play ()
        {
            if (IsPaused) {
                last_time = DateTime.Now;
                IsPaused = false;
            }
        }

        public void Pause ()
        {
            IsPaused = true;
        }
    }

    public class ClutterFlowTimeline : ThrottledTimeline
    {
        #region Fields
        protected int last_delta = 0;
        protected override double Frequency {
            get {
                double retval = (double) Math.Max((Delta - (Delta - last_delta)*0.25 ),1) / (double) CoverManager.MaxAnimationSpan;
                last_delta = Delta;
                return retval;
            }
        }

        private CoverManager coverManager;
        public CoverManager CoverManager {
            get { return coverManager; }
        }

        public override uint Target {
            get {
                return (uint) (CoverManager!=null ? CoverManager.TargetIndex : 0);
            }
            set {
                throw new System.NotImplementedException();
            }
        }

        public override uint IndexCount {
            get {
                return (uint) (CoverManager!=null ? CoverManager.TotalCovers : 0);
            }
            set {
                throw new System.NotImplementedException();
            }
        }
        #endregion

        public ClutterFlowTimeline (CoverManager coverManager) : base((uint) coverManager.TotalCovers, 1 / (double) CoverManager.MaxAnimationSpan)
        {
            this.coverManager = coverManager;
        }
    }
}
