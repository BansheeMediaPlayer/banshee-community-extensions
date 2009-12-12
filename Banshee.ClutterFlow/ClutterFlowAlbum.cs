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
// A ClutterFlowAlbum is a subclass of the ClutterFlowActor class, containing
// banshee related Album art fetching code.
//


using System;
using System.Threading;
using System.Collections.Generic;

using Gdk;
using Cairo;
using Clutter;

using Banshee.Gui;
using Banshee.Collection;
using Banshee.Collection.Gui;
using Banshee.ServiceStack;

using ClutterFlow;

namespace Banshee.ClutterFlow
{

	public static class ArtworkLookup {

		#region static Fields
		private static ArtworkManager artwork_manager;

		private static bool is_setup = false;
		public static bool IsSetup {
			get { return is_setup; }
			protected set { is_setup = value; }
		}

		//Threaded pixel actor lookup:
		public static readonly object listLock = new Object ();
		private static Queue<ClutterFlowAlbum> LookupQueue = new Queue<ClutterFlowAlbum> ();
		public static void Enqueue (ClutterFlowAlbum cover)
		{
				lock (listLock) {
					//Hyena.Log.Information("Enqueued a ClutterFlowAlbum");
					LookupQueue.Enqueue (cover);
					Start ();
					Monitor.Pulse (listLock);
				}
		}
		#endregion

		static ArtworkLookup () 
		{
			if (!is_setup) {
				artwork_manager = ServiceManager.Get<ArtworkManager> ();
				Start ();
			}
			is_setup = true;
		}

		public static void Start () 
		{
			if (Stopped)  {
				stopped = false;
				Thread t = new Thread (new ThreadStart (Run));
				t.Priority = ThreadPriority.BelowNormal;
				t.Start ();
				Hyena.Log.Information("ArtworkLookup Job has started");
			}
		}
		
	    static readonly object stopLock = new object ();  // Lock covering stopping and stopped
	    static bool stopping = false;  // Whether or not the worker thread has been asked to stop
	    static bool stopped = true; // Whether or not the worker thread has stopped
	    
	    // Returns whether the worker thread has been asked to stop.
	    // This continues to return true even after the thread has stopped.
	    public static  bool Stopping {
	        get { lock (stopLock) { return stopping; } }
	    }
	    
	    // Returns whether the worker thread has stopped.
	    public static bool Stopped {
	        get { lock (stopLock) { return stopped; } }
	    }
	
	    // Tells the worker thread to stop, typically after completing its 
	    // current work item. (The thread is *not* guaranteed to have stopped
	    // by the time this method returns.)
	    public static void Stop ()
	    {
	        lock (stopLock) {
				Monitor.Pulse (LookupQueue);
				stopping = true; 
			}
	    }
	
	    // Called by the worker thread to indicate when it has stopped.
	    static void SetStopped ()
	    {
	        lock (stopLock) { stopped = true; }
			Hyena.Log.Information("ArtworkLookup Job has finished");
	    }
		
	    // Main work loop of the class.
	    public static void Run ()
	    {
	        try {
	            while (!Stopping) {
	                // Insert work here. Make sure it doesn't tight loop!
	                // (If work is arriving periodically, use a queue and Monitor.Wait,
	                // changing the Stop method to pulse the monitor as well as setting
	                // stopping.)
	
	                // Note that you may also wish to break out *within* the loop
	                // if work items can take a very long time but have points at which
	                // it makes sense to check whether or not you've been asked to stop.
	                // Do this with just:
	                // if (Stopping)
	                // {
	                //     return;
	                // }
	                // The finally block will make sure that the stopped flag is set.
						
					
					lock (listLock) {
						
						while (LookupQueue.Count==0) {
							if (Stopping) return;
							Monitor.Wait (listLock);
						}
						//TODO implement a sliding index thingy in tandem with the covermanager behaviour.
						/*
						 * this will become an instancable class, on a static field in ClutterFlowAlbum
						 * */
						Clutter.Threads.Enter ();
							ClutterFlowAlbum cover = LookupQueue.Dequeue ();
							float size = cover.CoverManager.TextureSize;
							string cache_id = cover.PbId;
							Gdk.Threads.Enter ();
								Gdk.Pixbuf newPb = artwork_manager.LookupScalePixbuf (cache_id, (int) size) ?? ClutterFlowActor.DefaultPb;
								if (newPb!=null) Gtk.Application.Invoke (delegate {
									GtkUtil.TextureSetFromPixbuf (cover.Cover, ClutterFlowActor.MakeReflection(newPb));
								});
							Gdk.Threads.Leave ();
						Clutter.Threads.Leave ();
					}
					System.Threading.Thread.Sleep (50); //give the other threas time to do some work
	            }
	        } finally {
	            SetStopped ();
	        }
	    }
	}
	
	
	public class ClutterFlowAlbum : ClutterFlowActor
	{
		#region Fields
		private static bool is_setup = false;
		public static bool IsSetup {
			get { return is_setup; }
			protected set { is_setup = value; }
		}

		protected AlbumInfo key;
		public virtual AlbumInfo Key {
			get { return key; }
			set {
				if (key!=value) {
					key = value;
					ArtworkLookup.Enqueue (this);
				}
			}
		}

	
		public virtual string PbId {
			get { return key.ArtworkId; }
		}

		public override string Label {
			get { return Key!=null ? Key.ArtistName + "\n" + Key.Title : ""; }
			set {
				throw new System.NotImplementedException ("Label cannot be set directly in a ClutterFlowAlbum, derived from the Key property.");
			}
		}

		public override string SortLabel {
			get { return (Key!=null && Key.Title!=null) ? Key.Title : "?" ; }
			set {
				throw new System.NotImplementedException ("SortLabel cannot be set directly in a ClutterFlowAlbum, derived from the Key property.");
			}
		}

		#endregion

		#region Initialization	
		public ClutterFlowAlbum (AlbumInfo album, CoverManager coverManager) : base ()
		{
			this.CoverManager = coverManager;
			this.key = album;
			if (DefaultPb==null) DefaultPb = IconThemeUtils.LoadIcon (coverManager.TextureSize, "media-optical", "browser-album-cover");
			SetupActors ();
		}

		protected override void SetupActors ()
		{
			base.SetupActors ();
			ArtworkLookup.Enqueue(this);
		}			

		protected override void HandleTextureSizeChanged (object sender, System.EventArgs e)
		{
			DefaultPb = IconThemeUtils.LoadIcon (coverManager.TextureSize, "media-optical", "browser-album-cover");
			ArtworkLookup.Enqueue(this);
		}
		#endregion
	}
}
