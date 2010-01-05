//
// ClutterFlowAlbum.cs
//
// Author:
//   Mathijs Dumon <mathijsken@hotmail.com>
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

//
// The ArtworkLookup class is a static property of the ClutterFlowAlbum
// instantiated if null or passed with it's constructor. It is an 
// asynchronous worker thread processing the artwork lookups.
//


using System;
using System.Threading;
using System.Collections.Generic;

using Gdk;
using Clutter;

using Banshee.ServiceStack;
using Banshee.Collection.Gui;
using Hyena.Collections;

using ClutterFlow;

namespace Banshee.ClutterFlow
{

	public class FloatingQueue<T> where T : class, IIndexable
	{
		IndexedQueue<T> queue = new IndexedQueue<T> ();

		private int offset = 0;	// the offset from the focus
		private int sign = 1;		// positive or negative offset
		private int focus = 0;
		public int Focus {
			get { return focus; }
			set {
				if (focus!=value) {
					focus = value;
					ResetFloaters ();
				}
			}
		}

		public int Count {
			get { return queue.Count; }
		}
		
		public FloatingQueue ()
		{
			queue.Changed += HandleChanged;
		}

		#region Methods

		protected virtual void ResetFloaters ()
		{
			offset = 0;
			sign = 1;
		}
		
		public virtual void Enqueue (T item)
		{
			queue.Add(item);
		}

		public virtual T Dequeue ()
		{
			if (queue.Count==0) {
				Console.WriteLine("Inside zero count dequeue");
				return null;
			} else if (queue.Count==1 && queue.TryKey(-1)!=null) {
				Console.WriteLine("Inside offscreen dequeue");
				return queue.PopFrom(-1);
			} else {
				Console.WriteLine("Inside normal Dequeue Focus == " + focus + " offset == " + offset);
				int index = focus + offset * sign;
				T curr = queue.TryKey(index);
				while (curr==null || offset == 10000) {
					//Console.WriteLine("						WHILE WHILE WHILE");
					sign = -sign;
					if (sign < 0)
						offset++;				
					index = focus + offset * sign;
					if (sign < 0) //we do not want offscreens to get loaded yet
						index = Math.Max(1, index);	
					curr = queue.TryKey(index); //re-assign
				}
				return queue.PopFrom(index);
			}
		}


		void HandleChanged(object sender, EventArgs e)
		{
			ResetFloaters ();
		}
		#endregion
	}
	
	public class ArtworkLookup : IDisposable {

		//PLANNED: add stopped started and iter events that should get invoked on the main loop!
		
		#region Fields
		private ArtworkManager artwork_manager;

		protected CoverManager coverManager;
		public CoverManager CoverManager {
			get { return coverManager; }
			set {
				if (coverManager!=null) {
					coverManager.TargetIndexChanged -= HandleTargetIndexChanged;
				}
				coverManager = value;
				if (coverManager!=null) {
					coverManager.TargetIndexChanged += HandleTargetIndexChanged;
				}
			}
		}

		public object LockRoot = new object ();
		private FloatingQueue<ClutterFlowAlbum> LookupQueue = new FloatingQueue<ClutterFlowAlbum> ();
		
		
	    protected readonly object stopLock = new object ();	// Lock covering stopping and stopped
	    protected bool stopping = false;					// Whether or not the worker thread has been asked to stop
	    protected bool stopped = true;						// Whether or not the worker thread has stopped
	    
	    // Returns whether the worker thread has been asked to stop.
	    // This continues to return true even after the thread has stopped.
	    public bool Stopping {
	        get { lock (stopLock) { return stopping; } }
	    }
	    
	    // Returns whether the worker thread has stopped.
	    public bool Stopped {
	        get { lock (stopLock) { return stopped; } }
	    }
		#endregion
		
		public ArtworkLookup (CoverManager coverManager) 
		{
		 	CoverManager = coverManager;
			artwork_manager = ServiceManager.Get<ArtworkManager> ();
		}
		~ArtworkLookup ()
		{
			Stop ();
		}

		#region Queueing and index hinting	
		public void Enqueue (ClutterFlowAlbum cover)
		{
			Hyena.Log.Information ("Enqueue tries to get a lock");
			lock (LockRoot) {
				Hyena.Log.Information ("Enqueue has locked");
				LookupQueue.Enqueue (cover);
				Start ();
				Monitor.Pulse (LockRoot);
			}
		}

		private int new_focus = -1;
		private object focusLock = new object ();
		protected void HandleTargetIndexChanged(object sender, EventArgs e)
		{
			lock (focusLock) {
				new_focus = coverManager.TargetIndex;
			}
		}
		#endregion
		
		#region  Start/Stop Handling
		public void Dispose ()
		{
			Stop ();
		}
		
		public void Start () 
		{
			if (Stopped)  {
				stopped = false;
				Thread t = new Thread (new ThreadStart (Run));
				t.Priority = ThreadPriority.BelowNormal;
				t.Start ();
				Hyena.Log.Information("ArtworkLookup Job has started");
			}
		}
	
	    // Tells the worker thread to stop, typically after completing its 
	    // current work item. (The thread is *not* guaranteed to have stopped
	    // by the time this method returns.)
	    public void Stop ()
	    {
	        lock (stopLock) {
				Monitor.Pulse (LockRoot);
				stopping = true; 
			}
	    }
	
	    // Called by the worker thread to indicate when it has stopped.
	    protected void SetStopped ()
	    {
	        lock (stopLock) { stopped = true; }
			Hyena.Log.Information("ArtworkLookup Job has finished");
	    }
		#endregion
		
	    // Main work loop of the class.
	    public void Run ()
	    {
	        try {
	            while (!Stopping) {
					lock (LockRoot) {
						lock (focusLock) {
							if (new_focus>-1)
								LookupQueue.Focus = new_focus;
							new_focus = -1;
						}
						//Console.WriteLine ("LookupQueue.Count==" + LookupQueue.Count);
						while (LookupQueue.Count==0) {
							if (Stopping) return;
							Monitor.Wait (LockRoot);
						}
						//TODO implement a sliding index thingy in tandem with the covermanager behaviour.
						/* For this to be possible, we should register for
						 * events on the Behaviour and Timeline classes of the coverManager.
						 * */
						Clutter.Threads.Enter ();
							ClutterFlowAlbum cover = LookupQueue.Dequeue ();
							float size = cover.CoverManager.TextureSize;
							string cache_id = cover.PbId;
							Gdk.Threads.Enter ();
								Gdk.Pixbuf newPb = artwork_manager.LookupScalePixbuf (cache_id, (int) size);
								if (newPb!=null) {
									//it would be faster if we could just lock Gdk, but for some reason we end up with a dead lock,
									//probably related with banshee code? Should ask on the banshee-list
									Gtk.Application.Invoke (delegate {
										cover.SwappedToDefault = false;
										GtkUtil.TextureSetFromPixbuf (cover.Cover, ClutterFlowActor.MakeReflection(newPb));
									});
								}
							Gdk.Threads.Leave ();
						Clutter.Threads.Leave ();
					}
					System.Threading.Thread.Sleep (50); //give the other threads time to do some work
	            }
	        } finally {
	            SetStopped ();
	        }
	    }
	}
}
