//
// ClutterFlowSource.cs
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
// The ClutterFlowSource is a Banshee Source with ITrackModelSource interface.
// It sets up and binds together the source contents and the tracklist model.
//

using System;
using System.Data;
using System.Collections.Generic;
using Gtk;

using Hyena.Data.Sqlite;

using Banshee.I18n;
using Banshee.Playlist;
using Banshee.Library;
using Banshee.Sources;
using Banshee.ServiceStack;
using Banshee.MediaEngine;
using Banshee.Collection;
using Banshee.Collection.Database;
using Banshee.PlaybackController;
using Banshee.Gui;
using Banshee.Sources.Gui;

namespace Banshee.ClutterFlow
{
    public class ClutterFlowSource : Source, IDisposable, ITrackModelSource
    {
	
        private ClutterFlowInterface clutter_flow_interface;
		private MemoryTrackListModel track_model = new MemoryTrackListModel();
        
		public ClutterFlowSource () : base("ClutterFlow", "ClutterFlow", 0)
        {
			TypeUniqueId = "ClutterFlow";
		
            clutter_flow_interface = new ClutterFlowInterface ();
			clutter_flow_interface.FilterView.UpdatedAlbum += HandleUpdatedAlbum;
			clutter_flow_interface.SetSource(this);
			
			Properties.SetString ("Icon.Name", "clutterflow-icon");
            Properties.Set<ISourceContents> ("Nereid.SourceContents", clutter_flow_interface);
            Properties.Set<bool> ("Nereid.SourceContents.HeaderVisible", true);
			
			ServiceManager.SourceManager.ActiveSourceChanged += HandleActiveSourceChanged;

			//This places ClutterFlow inside the Music Library, ideally it should plug itself into the SourceContents as an action...
			SetParentSource(ServiceManager.SourceManager.MusicLibrary);
			Parent.AddChildSource(this);
			
			/* TODO add context menu's, actions etc. */
			
			Reload();
        }

        public void Dispose ()
        {
            if (clutter_flow_interface != null) {
                clutter_flow_interface.Destroy ();
                clutter_flow_interface.Dispose ();
                clutter_flow_interface = null;
            }
        }
		
        public override void Activate ()
        {
            if (clutter_flow_interface != null) {
                clutter_flow_interface.OverrideFullscreen ();
            }
        }

        public override void Deactivate ()
        {
            if (clutter_flow_interface != null) {
                clutter_flow_interface.RelinquishFullscreen ();
            }
        }

		//ISourceContents old_source_contents = null;
		protected void HandleActiveSourceChanged(SourceEventArgs args)
		{
			if (args.Source==this)
				clutter_flow_interface.FilterView.Enabled = true;
			else
				clutter_flow_interface.FilterView.Enabled = false;
		}
		
        private void HandleUpdatedAlbum(object sender, EventArgs e)
        {
			this.Reload();
			ServiceManager.SourceManager.SetActiveSource(this);
			if (!ServiceManager.PlayerEngine.IsPlaying()) 
				ServiceManager.PlayerEngine.Play();
			else
				ServiceManager.PlaybackController.Next();			
			OnUpdated ();
        }
		
        public override int Count {
            get { return track_model.Count; }
        }
		
#region ITrackModelSource Implementation
		
        public bool HasDependencies {
            get { return false; }
        }
		
        public TrackListModel TrackModel {
            get { return track_model; }
        }

        public AlbumListModel AlbumModel {
            get { return null; }
        }

        public ArtistListModel ArtistModel {
            get { return null; }
        }

        public void Reload ()
        {
			AlbumInfo album = clutter_flow_interface.FilterView.CurrentAlbum;
			if (album!=null) {
				track_model.Clear();
	            string query = String.Format(
	                @"SELECT TrackID FROM CoreTracks 
	                    WHERE PrimarySourceID = {0} AND AlbumId IN
	                        (SELECT CoreAlbums.AlbumId FROM CoreAlbums
	                            WHERE CoreAlbums.Title = '{1}')",
	                ServiceManager.SourceManager.MusicLibrary.DbId,
					album.Title.Replace ("'", "''")
				);
	            IDataReader reader = ServiceManager.DbConnection.Query(query);
	            while(reader.Read()) {
					DatabaseTrackInfo track = DatabaseTrackInfo.Provider.FetchSingle(Convert.ToInt32(reader["TrackID"]));
					track_model.Add(track);
	            }
				track_model.Reload();
			}
        }

        public override string FilterQuery {
            get { return Parent.Properties.Get<string> ("FilterQuery"); }
            set { 
				((Parent as DatabaseSource).TrackModel as DatabaseTrackListModel).UserQuery = value;
				(Parent as DatabaseSource).TrackModel.Reload();
			}
        }
        
        new public TrackFilterType FilterType {
            get { return (TrackFilterType) Parent.Properties.GetInteger ("FilterType"); }
            set { 
				Parent.Properties.SetInteger ("FilterType", (int)value);
				(Parent as DatabaseSource).TrackModel.Reload();
			}
        }
		
		
        public void RemoveSelectedTracks ()
        {
			throw new Exception ("Should not call RemoveSelectedTracks on a ClutterFlowSource");
        }

        public void DeleteSelectedTracks ()
        {
            throw new Exception ("Should not call DeleteSelectedTracks on a ClutterFlowSource");
        }

        public bool CanAddTracks {
            get { return false; }
        }

        public bool CanRemoveTracks {
            get { return true; }
        }

        public bool CanDeleteTracks {
            get { return true; }
        }

        public bool ConfirmRemoveTracks {
            get { return true; }
        }

		public override bool CanSearch {
            get { return true; }
        }
		
        public virtual bool CanRepeat {
            get { return true; }
        }

        public virtual bool CanShuffle {
            get { return true; }
        }

        public bool ShowBrowser {
            get { return false; }
        }
        
        public bool Indexable {
            get { return true; }
        }

#endregion
    }
}
