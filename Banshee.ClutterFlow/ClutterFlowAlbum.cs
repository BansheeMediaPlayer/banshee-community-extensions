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

using Cairo;
using Clutter;

using Banshee.Gui;
using Banshee.Collection;

using ClutterFlow;

namespace Banshee.ClutterFlow
{	
	
	public class ClutterFlowAlbum : ClutterFlowActor,  IEquatable<ClutterFlowAlbum>
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
		public ClutterFlowAlbum (AlbumInfo album, CoverManager coverManager, ArtworkLookup lookup) : this (album, coverManager)
		{
			ClutterFlowAlbum.lookup = lookup;
		}
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

		public bool Equals (ClutterFlowAlbum other)
		{
			return other.CacheKey==this.CacheKey;
		}
	}
}
