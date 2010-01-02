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

	public class ArtworkLookup : IDisposable {

		//PLANNED: add stopped started and iter events that should get invoked on the main loop!
		
		#region Fields
		private ArtworkManager artwork_manager;

		protected CoverManager coverManager;
		public CoverManager CoverManager {
			get { return this.coverManager; }
			set {
				if (coverManager!=null) {
				}
				coverManager = value;
				if (coverManager!=null) {
				}
			}
		}

		protected readonly object listLock = new object ();
		private Queue<ClutterFlowAlbum> LookupQueue = new Queue<ClutterFlowAlbum> ();
		
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
		 	this.CoverManager = coverManager;
			artwork_manager = ServiceManager.Get<ArtworkManager> ();
		}

		#region Queueing and index hinting	
		public void Enqueue (ClutterFlowAlbum cover)
		{
				lock (listLock) {
					if (!LookupQueue.Contains(cover))
						LookupQueue.Enqueue (cover);
					Start ();
					Monitor.Pulse (listLock);
				}
		}
		#endregion
		
		#region  Start/Stop Handling
		public void Dispose ()
		{
			Stop();
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
				Monitor.Pulse (LookupQueue);
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
					lock (listLock) {
						
						while (LookupQueue.Count==0) {
							if (Stopping) return;
							Monitor.Wait (listLock);
						}
						//TODO implement a sliding index thingy in tandem with the covermanager behaviour.
						/* For this to be possible, we should make this an instance so it can register for
						 * events on the Behaviour and Timeline classes of the coverManager.
						 * So, this will become an instancable class. Albumloader creates one instance and passes it
						 * to the ClutterFlowAlbum so it can queue itself.
						 * */
						Clutter.Threads.Enter ();
							ClutterFlowAlbum cover = LookupQueue.Dequeue ();
							float size = cover.CoverManager.TextureSize;
							string cache_id = cover.PbId;
							Gdk.Threads.Enter ();
								Gdk.Pixbuf newPb = artwork_manager.LookupScalePixbuf (cache_id, (int) size);
								if (newPb!=null) {
									Gtk.Application.Invoke (delegate {
										cover.SwappedToDefault = false;
										GtkUtil.TextureSetFromPixbuf (cover.Cover, ClutterFlowActor.MakeReflection(newPb));
									});
								}
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
		protected static ArtworkLookup lookup;
		public static ArtworkLookup Lookup {
			get { return lookup; }
			set { lookup = value; }
		}

		protected AlbumInfo album;
		public virtual AlbumInfo Album {
			get { return album; }
			set {
				if (album!=value) {
					album = value;
					Lookup.Enqueue (this);
				}
			}
		}
	
		public virtual string PbId {
			get { return album.ArtworkId; }
		}

		public override string CacheKey {
			get { return album!=null ? CreateCacheKey(album) : ""; }
			set { 
				throw new System.NotImplementedException ("CacheKey cannot be set directly in a ClutterFlowAlbum," +
				                                          "derived from the Album property."); //TODO should use reflection here
			}
		}
		public static string CreateCacheKey(AlbumInfo album) {
			return album.ArtistName + "\n" + album.Title;
		}

		public override string Label {
			get { return album!=null ? album.ArtistName + "\n" + album.Title : ""; }
			set {
				throw new System.NotImplementedException ("Label cannot be set directly in a ClutterFlowAlbum, derived from the Album property.");
			}
		}

		public override string SortLabel {
			get { return (album!=null && album.Title!=null) ? album.Title : "?" ; }
			set {
				throw new System.NotImplementedException ("SortLabel cannot be set directly in a ClutterFlowAlbum, derived from the Album property.");
			}
		}
		#endregion

		#region Initialization	
		public ClutterFlowAlbum (AlbumInfo album, CoverManager coverManager) : base (coverManager, null)
		{
			this.album = album;
		}
		protected override void SetupStatics ()
		{
			if (lookup==null) lookup = new ArtworkLookup (CoverManager);
			base.SetupStatics ();
		}
		#endregion
		
		#region Texture Handling
		protected override Gdk.Pixbuf GetDefaultPb ()
		{
			return IconThemeUtils.LoadIcon (coverManager.TextureSize, "media-optical", "browser-album-cover");
		}
		
		protected override void SetupActors ()
		{
			base.SetupActors ();
			Lookup.Enqueue(this);
		}			

		protected override void HandleTextureSizeChanged (object sender, System.EventArgs e)
		{
			Lookup.Enqueue(this);
		}
		#endregion
	}
}
