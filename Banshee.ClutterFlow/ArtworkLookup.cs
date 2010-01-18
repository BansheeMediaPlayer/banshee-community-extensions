// 
// ArtworkLookup.cs
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

    /// <summary>
    /// The ArtworkLookup class is an asynchronous worker thread processing artwork lookups.
    /// </summary>
	public class ArtworkLookup : IDisposable {
	
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

		public object SyncRoot {
			get { return LookupQueue.SyncRoot; }
		}
		private FloatingQueue<ClutterFlowAlbum> LookupQueue = new FloatingQueue<ClutterFlowAlbum> ();
		
		
	    protected readonly object stopLock = new object ();	// Lock covering stopping and stopped
	    protected bool stopping = false;					// Whether or not the worker thread has been asked to stop
	    protected bool stopped = true;						// Whether or not the worker thread has stopped
	    
		/// <value>
		/// Returns whether the worker thread has been asked to stop.
	    /// This continues to return true even after the thread has stopped.
		/// </value>
	    public bool Stopping {
	        get { lock (stopLock) { return stopping; } }
	    }
	    
		//// <value>
	    // Returns whether the worker thread has stopped.
		/// </value>
	    public bool Stopped {
	        get { lock (stopLock) { return stopped; } }
	    }
		#endregion
		
		public ArtworkLookup (CoverManager coverManager) 
		{
			Hyena.Log.Information ("ArtworkLookup created!!");
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
			LookupQueue.Enqueue (cover);
			Start ();
		}

		private int new_focus = -1;
		private object focusLock = new object ();
		protected void HandleTargetIndexChanged(object sender, EventArgs e)
		{
			//Hyena.Log.Information ("ArtworkLookup HandleTargetIndexChanged locking focusLock");
			lock (focusLock) {
				//Hyena.Log.Information ("ArtworkLookup HandleTargetIndexChanged locked focusLock");
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
			}
		}
	
	    // Tells the worker thread to stop, typically after completing its 
	    // current work item. (The thread is *not* guaranteed to have stopped
	    // by the time this method returns.)
	    public void Stop ()
	    {
	        lock (stopLock) {
				Monitor.Pulse (SyncRoot);
				stopping = true; 
			}
	    }
	
	    // Called by the worker thread to indicate when it has stopped.
	    protected void SetStopped ()
	    {
	        lock (stopLock) { stopped = true; }
	    }
		#endregion
		
	    // Main work loop of the class.
	    public void Run ()
	    {
	        try {
	            while (!Stopping) {
					ClutterFlowAlbum cover; //no API calling here!
					
					//Hyena.Log.Information ("ArtworkLookup Run locking focusLock");
					lock (focusLock) {
						//Hyena.Log.Information ("ArtworkLookup Run locked focusLock");
						if (new_focus>-1)
							LookupQueue.Focus = new_focus;
						new_focus = -1;
					}
					while (LookupQueue.Count==0) {
						if (Stopping) return;
						lock (SyncRoot) {
							Monitor.Wait (SyncRoot);
						}
					}
					cover = LookupQueue.Dequeue ();
						
					//Hyena.Log.Information ("ArtworkLookup Run locking Clutter");
					Clutter.Threads.Enter ();
						//Hyena.Log.Information ("ArtworkLookup Run locked Clutter");
						
						float size = cover.CoverManager.TextureSize;
						string cache_id = cover.PbId;
						//Gdk.Threads.Enter ();
							Gdk.Pixbuf newPb = artwork_manager.LookupScalePixbuf (cache_id, (int) size);
							if (newPb!=null) {
								//it would be faster if we could just lock Gdk, but for some reason we end up with a dead lock,
								//probably related with banshee code? Should ask on the banshee-list
								//Hyena.Log.Information ("ArtworkLookup invokes Gtk calls");
								Gtk.Application.Invoke (delegate {
									cover.SwappedToDefault = false;
									GtkUtil.TextureSetFromPixbuf (cover.Cover, ClutterFlowActor.MakeReflection(newPb));
								});
								
							}
						//Gdk.Threads.Leave ();
					Clutter.Threads.Leave ();
					//Hyena.Log.Information ("ArtworkLookup Run released Clutter");
					System.Threading.Thread.Sleep (50); //give the other threads time to do some work
	            }
	        } finally {
	            SetStopped ();
	        }
	    }
	}
}
