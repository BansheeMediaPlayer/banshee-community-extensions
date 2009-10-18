//
// ClutterFlowListView.cs
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

//
// The ClutterFlowListView is actually the embedded ClutterWidget containing
// all code related to linking the Banshee API with the ClutterFlow 'API'.
//

using System;

using Hyena.Data;
using Hyena.Data.Gui;

using Banshee.Collection;
using Banshee.Collection.Gui;
using Banshee.ServiceStack;
using Banshee.MediaEngine;
using Banshee.Gui;

using Clutter;
using Gtk;

namespace Banshee.ClutterFlow
{
	
    public partial class ClutterFlowListView : Clutter.Embed
    {	
		
		public event EventHandler UpdatedAlbum;
		
		private CoverGroup current_cover = null;
		private IListModel<AlbumInfo> model;

		public AlbumInfo CurrentAlbum {
			get { return current_cover==null ? null : current_cover.Album; }
		}
        
        public virtual IListModel<AlbumInfo> Model {
            get { return model; }
        }
		
        public void SetModel (IListModel<AlbumInfo> model)
        {
            SetModel (model, 0.0);
        }

        public void SetModel (IListModel<AlbumInfo> value, double vpos)
        {
            if (model == value) {
                return;
            }

            if (model != null) {
                model.Cleared -= OnModelClearedHandler;
                model.Reloaded -= OnModelReloadedHandler;
            }
            
            model = value;

            if (model != null) {
                model.Cleared += OnModelClearedHandler;
                model.Reloaded += OnModelReloadedHandler;
				//model.Selection.Changed += HandleSelectionChanged;
                //selection_proxy.Selection = model.Selection;
                //IsEverReorderable = model.CanReorder;
            }
            ReloadCovers (); //had vpos as argument
        }
		
        private void OnModelClearedHandler (object o, EventArgs args)
        {
            OnModelCleared ();
        }
        
        private void OnModelReloadedHandler (object o, EventArgs args)
        {
            OnModelReloaded ();
        }
		
        protected virtual void OnModelCleared ()
        {
            ReloadCovers ();
        }
        
        protected virtual void OnModelReloaded ()
        {
            ReloadCovers ();
        }
    }
}
