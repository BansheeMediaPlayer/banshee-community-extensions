//
// CoverGroupCache.cs
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
// The CoverGroupCache provides us with easy caching based on the covers albums
// CacheEntryId. It is only loosely bound to the CoverGroup, but is required by
// the AnimationManagers.
//

using System;
using System.Collections.Generic;
using Banshee.Collection;

namespace Banshee.ClutterFlow
{
	public class CoverGroupCache {
		
		private Dictionary<object, CoverGroup> cached_covers = new Dictionary<object, CoverGroup> ();
		
		private CoverManager coverManager;
		
		private bool is_ready = false;
		public bool IsReady {
			get { return is_ready; }
		}
			
		public CoverGroupCache (CoverManager coverManager) 
		{
			this.coverManager = coverManager;
			is_ready = true;
		}
		
		public CoverGroup GetCoverGroupFromAlbum (AlbumInfo album) 
		{
			if (!is_ready) return null;
			if (!cached_covers.ContainsKey(album.CacheEntryId)) {
				CoverGroup new_cover = new CoverGroup (album, coverManager);
				cached_covers.Add (album.CacheEntryId, new_cover);
			}
			return cached_covers[album.CacheEntryId];
		}
		
		public void HideAllCachedCoversNotInside (List<CoverGroup> covers) 
		{
			for (int i=0; i < cached_covers.Count; i++) {
				if (!covers.Contains (cached_covers[i]))
					cached_covers[i].Hide ();
			}
		}
		
	}
}
