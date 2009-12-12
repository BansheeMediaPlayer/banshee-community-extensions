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
using Banshee.Configuration;
using Banshee.Preferences;

namespace Banshee.ClutterFlow
{

	public static class ClutterFlowSchemas {
        internal static readonly SchemaEntry<bool> DisplayLabel = new SchemaEntry<bool>(
            "clutterflow", "display_album_label",
            true,
            "",
            "Wether or not the album label needs to be shown above the artwork"
        );

        internal static readonly SchemaEntry<int> TextureSize = new SchemaEntry<int>(
            "clutterflow", "texture_size",
            128, 32, 512,
            "Texture size in pixels",
            "The in-memory size of the cover textures in pixels"
        );

        internal static readonly SchemaEntry<int> MinCoverSize = new SchemaEntry<int>(
            "clutterflow", "min_cover_size",
            64, 64, 128,
            "Minimal cover size in pixels",
            "The on-stage minimal cover size in pixels"
        );

        internal static readonly SchemaEntry<int> MaxCoverSize = new SchemaEntry<int>(
            "clutterflow", "max_cover_size",
            256, 128, 512,
            "Maximal cover size in pixels",
            "The on-stage minimal cover size in pixels"
        );
	}
	
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

			//IconTheme.Default.AppendSearchPath();
			
			Properties.SetString ("Icon.Name", "clutterflow-icon");
            Properties.Set<ISourceContents> ("Nereid.SourceContents", clutter_flow_interface);
            Properties.Set<bool> ("Nereid.SourceContents.HeaderVisible", true);
			
			ServiceManager.SourceManager.ActiveSourceChanged += HandleActiveSourceChanged;
			//ServiceManager.PlaybackController.SourceChanged += OnPlaybackSourceChanged; todo

			//This places ClutterFlow inside the Music Library, ideally it should plug itself into the SourceContents as an action...
			SetParentSource(ServiceManager.SourceManager.MusicLibrary);
			Parent.AddChildSource(this);
			
			InstallPreferences();

			
			Reload();
        }
		
        public void Dispose ()
        {		
            if (clutter_flow_interface != null) {
                clutter_flow_interface.Destroy ();
                clutter_flow_interface.Dispose ();
                clutter_flow_interface = null;
            }
			UninstallPreferences ();
        }

		#region FullScreen Overrides
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
		#endregion
		#region Preferences
		//protected string[] sort_fields = new string[] { "TitleLowered" };
		
		private SourcePage pref_page;
        private Section general;
		private Section dimensions;
		
		protected void InstallPreferences () 
		{
			pref_page = new Banshee.Preferences.SourcePage (PreferencesPageId, Name, "clutterflow-icon", 400);
			
            general = PreferencesPage.Add (new Section ("general", 
                Catalog.GetString ("General"), 1));

            general.Add (new SchemaPreference<bool> (ClutterFlowSchemas.DisplayLabel, 
                Catalog.GetString ("Display album _label"), ClutterFlowSchemas.DisplayLabel.LongDescription, UpdateLabelVisibility));

            /*general.Add (new SchemaPreference<bool> (ClutterFlowSchemas.DisplayLabel, 
                Catalog.GetString ("Display album _label"), ClutterFlowSchemas.DisplayLabel.LongDescription, null)); TODO click behaviour*/
			
			dimensions = PreferencesPage.Add (new Section ("dimensions", 
                Catalog.GetString ("Dimensions"), 2));

            dimensions.Add (new SchemaPreference<int> (ClutterFlowSchemas.MinCoverSize, 
                Catalog.GetString ("Minimal cover size"), ClutterFlowSchemas.MinCoverSize.ShortDescription, UpdateMinCoverSize));
			dimensions.Add (new SchemaPreference<int> (ClutterFlowSchemas.MaxCoverSize, 
                Catalog.GetString ("Maximal cover size"), ClutterFlowSchemas.MaxCoverSize.ShortDescription, UpdateMaxCoverSize));
			dimensions.Add (new SchemaPreference<int> (ClutterFlowSchemas.TextureSize, 
                Catalog.GetString ("Texture size"), ClutterFlowSchemas.TextureSize.ShortDescription, UpdateTextureSize));

			LoadPreferences ();
		}

		private void LoadPreferences ()
		{
			UpdateLabelVisibility ();
			UpdateMinCoverSize ();
			UpdateMaxCoverSize ();
			UpdateTextureSize ();
		}
		
		private void UpdateLabelVisibility ()
		{
			if (clutter_flow_interface != null)
				clutter_flow_interface.FilterView.CaptionIsVisble = 
					ClutterFlowSchemas.DisplayLabel.Get ();
		}

		private void UpdateMinCoverSize ()
		{
			if (clutter_flow_interface != null)
				clutter_flow_interface.FilterView.CoverManager.MinCoverWidth = 
					ClutterFlowSchemas.MinCoverSize.Get ();
		}

		private void UpdateMaxCoverSize ()
		{
			if (clutter_flow_interface != null)
				clutter_flow_interface.FilterView.CoverManager.MaxCoverWidth = 
					ClutterFlowSchemas.MaxCoverSize.Get ();
		}

		private void UpdateTextureSize ()
		{
			if (clutter_flow_interface != null)
				clutter_flow_interface.FilterView.CoverManager.TextureSize = 
					ClutterFlowSchemas.TextureSize.Get ();
		}
		
        private void UninstallPreferences ()
        {
            pref_page.Dispose ();
            pref_page = null;
            general = null;
			dimensions = null;
        }
		#endregion
		#region Event Handling		
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
			ServiceManager.PlaybackController.Source = this; //TODO make this an option to set NextSource instead!
			if (!ServiceManager.PlayerEngine.IsPlaying()) 
				ServiceManager.PlayerEngine.Play();
			else
				ServiceManager.PlaybackController.Next();			
			OnUpdated ();
        }
		#endregion
		
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

        public void Reload () // to implement sorting: create a DatabaseAlbumListModel subclass
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
