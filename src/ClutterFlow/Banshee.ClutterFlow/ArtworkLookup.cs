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
using Hyena;
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
		
        protected bool threaded = false;
        public bool Threaded {
            get { return threaded; }
        }

	    protected readonly object stopLock = new object ();	// Lock covering stopping and stopped
	    protected bool stopping = false;					// Whether or not the worker thread has been asked to stop
	    protected bool stopped = true;						// Whether or not the worker thread has stopped
	
		/// <value>
		/// Returns whether the worker thread has been asked to stop.
	    /// This continues to return true even after the thread has stopped.
		/// </value>
	    public bool Stopping {
	        get { lock (stopLock) { return stopping; } }
            protected set { lock (stopLock) { stopping = value; } }
	    }
	
		//// <value>
	    // Returns whether the worker thread has stopped.
		/// </value>
	    public bool Stopped {
	        get { return t!=null ? (t.ThreadState == ThreadState.Stopped) : true; }
	    }
		#endregion

        Thread t;
		public ArtworkLookup (CoverManager coverManager)
		{
			//Log.Debug ("ArtworkLookup ctor ()");
		 	CoverManager = coverManager;
			artwork_manager = ServiceManager.Get<ArtworkManager> ();
			artwork_manager.AddCachedSize (CoverManager.TextureSize);

            threaded = ClutterFlowSchemas.ThreadedArtwork.Get ();
            //Start ();
		}

		#region Queueing and index hinting	
		public void Enqueue (ClutterFlowAlbum cover)
		{
            if (!cover.Enqueued) {
                cover.Enqueued = true;
                if (threaded) {
                    LookupQueue.Enqueue (cover);
                    Start ();
                } else
                    LoadUnthreaded (cover);
            }
		}

		private int new_focus = -1;
		private object focusLock = new object ();
		protected void HandleTargetIndexChanged(object sender, EventArgs e)
		{
			//Log.Debug ("ArtworkLookup HandleTargetIndexChanged locking focusLock");
			lock (focusLock) {
				//Log.Debug ("ArtworkLookup HandleTargetIndexChanged locked focusLock");
				new_focus = coverManager.TargetIndex;
			}
		}
		#endregion
		
		#region  Start/Stop Handling
        protected bool disposed = false;
		public virtual void Dispose ()
		{
            if (disposed)
                return;
            disposed = true;
            Log.Debug ("ArtworkLookup Dispose ()");
			Stop ();
            LookupQueue.Dispose ();
		}
	
	    // Tells the worker thread to stop, typically after completing its
	    // current work item. (The thread is *not* guaranteed to have stopped
	    // by the time this method returns.)
	    public void Stop ()
	    {
            Log.Debug ("ArtworkLookup Stop ()");
            Stopping = true;
            if (SyncRoot!=null) lock (SyncRoot) {
                Monitor.Pulse (SyncRoot);
            }
	        if (t!=null) t.Join ();
	    }

        public void Start ()
        {
            if (t==null) {
                t = new Thread (new ThreadStart (Run));
                t.Priority = ThreadPriority.BelowNormal;
                stopping = false;
                t.Start ();
            } else if (t.ThreadState == ThreadState.Unstarted) {
                stopping = false;
                t.Start ();
            }
        }
		#endregion
		
	    // Main work loop of the class.
	    private void Run ()
	    {
            if (!disposed) try {
                Log.Debug ("ArtworkLookup Run ()");
	            while (!Stopping) {
                    //Log.Debug ("ArtworkLookup Run locking focusLock");
                    lock (focusLock) {
                        //Log.Debug ("ArtworkLookup Run locked focusLock");
                        if (new_focus>-1)
                            LookupQueue.Focus = new_focus;
                        new_focus = -1;
                    }

					while (!Stopping && LookupQueue!=null && LookupQueue.Count==0) {
						lock (SyncRoot) {
                            //Log.Debug ("ArtworkLookup Run - waiting for pulse");
							bool ret = Monitor.Wait (SyncRoot, 5000);
                            if (!ret) Stopping = true;
                            //Log.Debug ("ArtworkLookup Run - pulsed");
						}
					}
                    if (Stopping) return;
                    t.IsBackground = false;
					ClutterFlowAlbum cover = LookupQueue.Dequeue ();
					float size = cover.CoverManager.TextureSize;
					string cache_id = cover.PbId;
					Gdk.Pixbuf pb = artwork_manager.LookupScalePixbuf (cache_id, (int) size);
					if (pb!=null) {
                        pb = ClutterFlowActor.MakeReflection(pb);
                        Gtk.Application.Invoke (delegate {
                            GtkInvoke (cover, pb);
                        });
					}
                    t.IsBackground = true;
	            }
	        } finally {
	           Log.Debug ("ArtworkLookup stopped");
               threaded = ClutterFlowSchemas.ThreadedArtwork.Get ();
               t = null;
	        }
	    }

        private void LoadUnthreaded (ClutterFlowAlbum cover)
        {
            float size = cover.CoverManager.TextureSize;
            string cache_id = cover.PbId;
            Gdk.Pixbuf pb = artwork_manager.LookupScalePixbuf (cache_id, (int) size);
            if (pb!=null) {
                pb = ClutterFlowActor.MakeReflection(pb);
                GtkInvoke (cover, pb);
            }
        }

        private void GtkInvoke (ClutterFlowAlbum cover, Gdk.Pixbuf pb)
        {
            cover.Enqueued = false;
            cover.SwappedToDefault = false;
            GtkUtil.TextureSetFromPixbuf (cover.Cover, pb);
            pb.Dispose ();
        }
	}
}
