// TimedPresetBase.cs
//
//  Copyright (C) 2008 Chris Howie
//
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 3 of the License, or (at your option) any later version.
//
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
// Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA 
//
//

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;

namespace OpenVP {
	/// <summary>
	/// Base class for presets that change at predetermined times.
	/// </summary>
	/// <remarks>
	/// This class constructs an internal timeline based on which methods in the
	/// concrete subclass are tagged <see cref="EventAttribute"/> and
	/// <see cref="SceneAttribute"/>. Methods so tagged will be inserted into
	/// the timeline based on the parameter passed to the attribute, which is
	/// the time in fractional seconds.
	/// 
	/// <para>A method tagged as a scene will be executed every iteration when
	/// the playing song's position becomes greater than or equal to the time
	/// associated with the scene, and until the playing song's position becomes
	/// greater than or equal to the next scene in the timeline.  A method can
	/// be tagged as a scene multiple times if it should be executed for more
	/// than one span of time.  If two methods are tagged as scenes with the
	/// same time, which one gets executed is undefined.  If two methods are
	/// defined very close together and the granularity used to measure song
	/// position is too low, the first scene will be skipped.</para>
	/// 
	/// <para>A method tagged as an event will be executed once, when the
	/// playing song's time becomes greater than or equal to the time associated
	/// with the event.  A method may be tagged as an event multiple times, in
	/// which case it will be executed at all of the times specified.  Event
	/// methods are always executed prior to the scene for the current
	/// frame.</para>
	/// </remarks>
	[Serializable]
	public abstract class TimedPresetBase : IRenderer, IBeatDetector, IDeserializationCallback {
		[NonSerialized]
		private List<TimedPresetEvent> mTimeline;
		
		/// <summary>
		/// The internal timeline.
		/// </summary>
		/// <value>
		/// The internal timeline.
		/// </value>
		/// <remarks>
		/// This property allows direct tweaking of the internal timeline.  If
		/// this is done, all responsibility for the maintenance of the correct
		/// structures and sorting is taken on by the subclass.
		/// </remarks>
		protected IList<TimedPresetEvent> Timeline {
			get {
				return this.mTimeline;
			}
		}
		
		[NonSerialized]
		private TimedPresetCallback mCurrentScene = null;
		
		[NonSerialized]
		private int mCurrentPosition = -1;
		
		[NonSerialized]
		private bool mIsBeat = false;
		
		/// <summary>
		/// True if a beat occurs on the current song position.
		/// </summary>
		/// <value>
		/// True if a beat occurs on the current song position.
		/// </value>
		/// <remarks>
		/// The value of this property may be set by a subclass.
		/// </remarks>
		public bool IsBeat {
			get {
				return this.mIsBeat;
			}
			protected set {
				this.mIsBeat = value;
			}
		}
		
		private bool mResetBeat = true;
		
		/// <summary>
		/// If true, the <see cref="IsBeat"/> property will be reset to false
		/// every render iteration before timeline events are processed.
		/// </summary>
		/// <value>
		/// If true, the <see cref="IsBeat"/> property will be reset to false
		/// every render iteration before timeline events are processed.
		/// </value>
		/// <remarks>
		/// This property is true by default.
		/// </remarks>
		protected bool ResetBeat {
			get {
				return this.mResetBeat;
			}
			set {
				this.mResetBeat = value;
			}
		}
		
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <remarks>
		/// This constructor sets up the internal timeline using reflected data
		/// of the subclass.
		/// </remarks>
		protected TimedPresetBase() {
			this.Construct();
		}
		
		void IDeserializationCallback.OnDeserialization(object sender) {
			this.Construct();
		}
		
		private void Construct() {
			this.mTimeline = new List<TimedPresetEvent>(this.GetTimedEvents());
			this.mTimeline.Sort();
		}
		
		/// <summary>
		/// Add a single preset event to the timeline.
		/// </summary>
		/// <param name="ev">
		/// A <see cref="TimedPresetEvent"/>.
		/// </param>
		/// <remarks>
		/// When adding multiple events, <see cref="TimedPresetBase.AddEvents"/>
		/// will be more efficient.
		/// </remarks>
		protected void AddEvent(TimedPresetEvent ev) {
			this.mTimeline.Add(ev);
			
			// TODO: Make this more efficient by inserting at the right place.
			this.mTimeline.Sort();
		}
		
		/// <summary>
		/// Add multiple events to the timeline.
		/// </summary>
		/// <param name="evs">
		/// A source of events.
		/// </param>
		protected void AddEvents(IEnumerable<TimedPresetEvent> evs) {
			this.mTimeline.AddRange(evs);
			this.mTimeline.Sort();
		}
		
		private IEnumerable<TimedPresetEvent> GetTimedEvents() {
			foreach (MethodInfo i in this.GetType().GetMethods(BindingFlags.Instance |
			                                                   BindingFlags.Public |
			                                                   BindingFlags.NonPublic)) {
				object[] attrs = i.GetCustomAttributes(typeof(TimedAttribute),
				                                       true);
				
				if (attrs.Length == 0)
					continue;
				
				ParameterInfo[] ps = i.GetParameters();
				
				if (ps.Length != 1 || ps[0].ParameterType != typeof(IController))
					throw new InvalidOperationException("Method tagged as a timeline event does not have the correct signature: " +
					                                    i.DeclaringType.FullName + ":" + i.Name);
				
				foreach (TimedAttribute timed in i.GetCustomAttributes(typeof(TimedAttribute), true)) {
					TimedPresetCallback callback = (TimedPresetCallback)
						Delegate.CreateDelegate(typeof(TimedPresetCallback), this, i);
					
					yield return new TimedPresetEvent(timed.Time, callback, timed.EventType);
				}
			}
		}
		
		private void SyncCurrent(IController controller) {
			if (this.mTimeline.Count <= this.mCurrentPosition) {
				// If the list isn't empty then something bad happened.
				if (this.mTimeline.Count != 0)
					throw new InvalidOperationException("Logic error -- this should not happen.");
				
				return;
			}
			
			double position = controller.PlayerData.SongPosition;
			
			if (this.mCurrentPosition >= 0 && this.mTimeline[this.mCurrentPosition].Time > position) {
				this.mCurrentPosition = -1;
				this.mCurrentScene = null;
			}
			
			while (this.mCurrentPosition + 1 < this.mTimeline.Count &&
			       this.mTimeline[this.mCurrentPosition + 1].Time <= position) {
				this.mCurrentPosition++;
				
				TimedPresetEvent ev = this.mTimeline[this.mCurrentPosition];
				
				switch (ev.Type) {
				case TimedPresetEventType.Scene:
					this.mCurrentScene = ev.Callback;
					break;
					
				case TimedPresetEventType.Event:
					ev.Callback(controller);
					break;
				}
			}
		}
		
		/// <summary>
		/// Renders one frame.
		/// </summary>
		/// <param name="controller">
		/// The controller.
		/// </param>
		public void Render(IController controller) {
			if (this.mResetBeat)
				this.mIsBeat = false;
			
			this.OnRender(controller);
			
			this.SyncCurrent(controller);
			
			if (this.mCurrentScene != null)
				this.mCurrentScene(controller);
		}
		
		/// <summary>
		/// Provides a mechanism for subclasses to execute code every frame.
		/// </summary>
		/// <param name="controller">
		/// The controller.
		/// </param>
		/// <remarks>
		/// This method is called just after the <see cref="IsBeat"/> property
		/// is reset (or would be reset if <see cref="ResetBeat"/> is false) and
		/// before any timeline events are processed.  This allows the subclass
		/// to execute common code every frame without duplicating it through
		/// all scene methods.
		/// 
		/// <para>This implementation does nothing.</para>
		/// </remarks>
		protected virtual void OnRender(IController controller) {
		}
		
		void IBeatDetector.Update(IController controller) {
		}
		
		/// <summary>
		/// Signature of methods to be used as event and scene callbacks.
		/// </summary>
		public delegate void TimedPresetCallback(IController controller);
		
		/// <summary>
		/// Timed event type.
		/// </summary>
		public enum TimedPresetEventType {
			/// <summary>
			/// A scene event.
			/// </summary>
			Scene,
			
			/// <summary>
			/// A one-shot event.
			/// </summary>
			Event
		}
		
		/// <summary>
		/// Represents one event in the timeline.
		/// </summary>
		public class TimedPresetEvent : IComparable<TimedPresetEvent> {
			private double mTime;
			
			/// <summary>
			/// The time that this event should be executed.
			/// </summary>
			public double Time {
				get {
					return this.mTime;
				}
			}
			
			private TimedPresetCallback mCallback;
			
			/// <summary>
			/// The callback to execute for this event.
			/// </summary>
			public TimedPresetCallback Callback {
				get {
					return this.mCallback;
				}
			}
			
			private TimedPresetEventType mType;
			
			/// <summary>
			/// The type of this event.
			/// </summary>
			public TimedPresetEventType Type {
				get {
					return this.mType;
				}
			}
			
			/// <summary>
			/// Constructor.
			/// </summary>
			/// <param name="time">
			/// The time in fractional seconds that this event is to be inserted
			/// into the timeline.
			/// </param>
			/// <param name="callback">
			/// The callback associated with this event.
			/// </param>
			/// <param name="type">
			/// The type of this event.
			/// </param>
			public TimedPresetEvent(double time, TimedPresetCallback callback,
			                        TimedPresetEventType type) {
				if (time < 0)
					throw new ArgumentOutOfRangeException("time < 0");
				
				if (callback == null)
					throw new ArgumentNullException("callback");
				
				this.mTime = time;
				this.mCallback = callback;
				this.mType = type;
			}
			
			/// <summary>
			/// Compares this event to another based on time.
			/// </summary>
			/// <param name="ev">
			/// The event to compare to this one.
			/// </param>
			/// <returns>
			/// A negative value if the time on this event is less than that on
			/// the parameter, a positive value if it is greater than, or zero
			/// if they are equal.
			/// </returns>
			public int CompareTo(TimedPresetEvent ev) {
				if (ev == null)
					throw new ArgumentNullException("ev");
				
				if (ev.Time > this.Time)
					return -1;
				
				if (ev.Time < this.Time)
					return 1;
				
				return 0;
			}
		}
	}
	
	/// <summary>
	/// Base class for attributes that represent events on the timeline of a
	/// <see cref="TimedPresetBase"/>.
	/// </summary>
	[AttributeUsage(AttributeTargets.Method, AllowMultiple=true)]
	public abstract class TimedAttribute : Attribute {
		private double mTime;
		
		/// <value>
		/// The time of the event in fractional seconds.
		/// </value>
		public double Time {
			get {
				return this.mTime;
			}
		}
		
		/// <value>
		/// The event type.
		/// </value>
		public abstract TimedPresetBase.TimedPresetEventType EventType { get; }
		
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="time">
		/// Time in fractional seconds of this event.  Must not be less than
		/// zero.
		/// </param>
		public TimedAttribute(double time) {
			if (time < 0)
				throw new ArgumentOutOfRangeException("time < 0");
			
			this.mTime = time;
		}
	}
	
	/// <summary>
	/// Represents a one-shot event in the timeline of a
	/// <see cref="TimedPresetBase"/>.
	/// </summary>
	public class EventAttribute : TimedAttribute {
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="time">
		/// Time in fractional seconds of this event.  Must not be less than
		/// zero.
		/// </param>
		public EventAttribute(double time) : base(time) {
		}
		
		/// <value>
		/// The event type.
		/// </value>
		public override TimedPresetBase.TimedPresetEventType EventType {
			get {
				return TimedPresetBase.TimedPresetEventType.Event;
			}
		}
	}
	
	/// <summary>
	/// Represents a scene event in the timeline of a
	/// <see cref="TimedPresetBase"/>.
	/// </summary>
	public class SceneAttribute : TimedAttribute {
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="time">
		/// Time in fractional seconds of this event.  Must not be less than
		/// zero.
		/// </param>
		public SceneAttribute(double time) : base(time) {
		}
		/// <value>
		/// The event type.
		/// </value>
		
		public override TimedPresetBase.TimedPresetEventType EventType {
			get {
				return TimedPresetBase.TimedPresetEventType.Scene;
			}
		}
	}
}
