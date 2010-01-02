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

        internal static readonly SchemaEntry<bool> InstantPlayback = new SchemaEntry<bool>(
            "clutterflow", "instant_playback",
            true,
            Catalog.GetString ("Immediately play album when clicked"),
            Catalog.GetString ("The album can start playing immediately or after the currently playing song has finished")
        );
		
        internal static readonly SchemaEntry<bool> DisplayLabel = new SchemaEntry<bool>(
            "clutterflow", "display_album_label",
            true,
            Catalog.GetString ("Display album _label"),
            Catalog.GetString ("Wether or not the album label needs to be shown above the artwork")
        );

        internal static readonly SchemaEntry<bool> DisplayTitle = new SchemaEntry<bool>(
            "clutterflow", "display_track_title",
            true,
            Catalog.GetString ("Display track _title"),
            Catalog.GetString ("Wether or not the album title needs to be shown above the artwork in fullscreen mode")
        );

        internal static readonly SchemaEntry<int> TextureSize = new SchemaEntry<int>(
            "clutterflow", "texture_size",
            128, 32, 512,
            Catalog.GetString ("Texture size in pixels"),
            Catalog.GetString ("The in-memory size of the cover textures in pixels")
        );

        internal static readonly SchemaEntry<int> MinCoverSize = new SchemaEntry<int>(
            "clutterflow", "min_cover_size",
            64, 64, 128,
            Catalog.GetString ("Minimal cover size in pixels"),
            Catalog.GetString ("The on-stage minimal cover size in pixels")
        );

        internal static readonly SchemaEntry<int> MaxCoverSize = new SchemaEntry<int>(
            "clutterflow", "max_cover_size",
            256, 128, 512,
            Catalog.GetString ("Maximal cover size in pixels"),
            Catalog.GetString ("The on-stage minimal cover size in pixels")
        );

        internal static readonly SchemaEntry<int> VisibleCovers = new SchemaEntry<int>(
            "clutterflow", "visbible_covers",
            7, 1, 15,
            Catalog.GetString ("Number of visible covers at the side"),
            Catalog.GetString ("The number of covers that need to be displayed on the stage (at one side)")
        );
	}
	
    public class ClutterFlowSource : Source, IDisposable, ITrackModelSource
    {
	
        private ClutterFlowInterface clutter_flow_interface;
		private TrackInfo previous_track;
		private MemoryTrackListModel track_model = new MemoryTrackListModel();
		private DatabaseAlbumListModel album_model = null;
        
		public ClutterFlowSource () : base("ClutterFlow", "ClutterFlow", 0)
        {
			TypeUniqueId = "ClutterFlow";
		
            clutter_flow_interface = new ClutterFlowInterface ();
			clutter_flow_interface.FilterView.UpdatedAlbum += HandleUpdatedAlbum;
			clutter_flow_interface.FilterView.PMButton.Toggled += HandlePMButtonToggled;
			clutter_flow_interface.SetSource(this);
			
			Properties.SetString ("Icon.Name", "clutterflow-icon");
            Properties.Set<ISourceContents> ("Nereid.SourceContents", clutter_flow_interface);
            Properties.Set<bool> ("Nereid.SourceContents.HeaderVisible", true);
			
			ServiceManager.SourceManager.ActiveSourceChanged += HandleActiveSourceChanged;
            ServiceManager.PlaybackController.TrackStarted += OnPlaybackControllerTrackStarted;
			ServiceManager.PlaybackController.SourceChanged += OnPlaybackSourceChanged;

			//This places ClutterFlow inside the Music Library, ideally it should plug itself into the SourceContents as an action...
			SetParentSource(ServiceManager.SourceManager.MusicLibrary);
			Parent.AddChildSource(this);
			
			InstallPreferences ();
			CreateAlbumListModel ();

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
			
            general.Add (new SchemaPreference<bool> (ClutterFlowSchemas.InstantPlayback, 
                ClutterFlowSchemas.InstantPlayback.ShortDescription, ClutterFlowSchemas.InstantPlayback.LongDescription, UpdateLabelVisibility));
            general.Add (new SchemaPreference<bool> (ClutterFlowSchemas.DisplayLabel, 
                ClutterFlowSchemas.DisplayLabel.ShortDescription, ClutterFlowSchemas.DisplayLabel.LongDescription, UpdateLabelVisibility));
			general.Add (new SchemaPreference<bool> (ClutterFlowSchemas.DisplayTitle, 
                ClutterFlowSchemas.DisplayTitle.ShortDescription, ClutterFlowSchemas.DisplayTitle.LongDescription, UpdateTitleVisibility));
			general.Add (new SchemaPreference<int> (ClutterFlowSchemas.VisibleCovers, 
                ClutterFlowSchemas.VisibleCovers.ShortDescription, ClutterFlowSchemas.VisibleCovers.LongDescription, UpdateVisibleCovers));
			
			dimensions = PreferencesPage.Add (new Section ("dimensions", 
                Catalog.GetString ("Dimensions"), 2));

            dimensions.Add (new SchemaPreference<int> (ClutterFlowSchemas.MinCoverSize, 
                ClutterFlowSchemas.MinCoverSize.ShortDescription, ClutterFlowSchemas.MinCoverSize.LongDescription, UpdateMinCoverSize));
			dimensions.Add (new SchemaPreference<int> (ClutterFlowSchemas.MaxCoverSize, 
                ClutterFlowSchemas.MaxCoverSize.ShortDescription, ClutterFlowSchemas.MaxCoverSize.LongDescription, UpdateMaxCoverSize));
			dimensions.Add (new SchemaPreference<int> (ClutterFlowSchemas.TextureSize, 
                ClutterFlowSchemas.TextureSize.ShortDescription, ClutterFlowSchemas.TextureSize.LongDescription, UpdateTextureSize));

			LoadPreferences ();
		}

		private void LoadPreferences ()
		{
			UpdateLabelVisibility ();
			UpdateTitleVisibility ();
			UpdateVisibleCovers ();
			UpdateMinCoverSize ();
			UpdateMaxCoverSize ();
			UpdateTextureSize ();
		}
		
		private void UpdateLabelVisibility ()
		{
			if (clutter_flow_interface != null)
				clutter_flow_interface.FilterView.LabelCoverIsVisible = 
					ClutterFlowSchemas.DisplayLabel.Get ();
		}
		
		private void UpdateTitleVisibility ()
		{
			if (clutter_flow_interface != null)
				clutter_flow_interface.FilterView.LabelTrackIsVisible = 
					ClutterFlowSchemas.DisplayTitle.Get ();
		}
		
		private void UpdateVisibleCovers ()
		{
			if (clutter_flow_interface!=null)
				clutter_flow_interface.FilterView.CoverManager.VisibleCovers =
					((ClutterFlowSchemas.VisibleCovers.Get () + 1) * 2 + 1);
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
		#region State Fields
		private bool IsActiveSource {
			get { return ServiceManager.SourceManager.ActiveSource == this; }
		}
		private bool IsPlaybackSource {
			get { return ServiceManager.PlaybackController.Source == this; }
		}
		
		private bool InPlaybackMode {
			get {
				return (IsActiveSource && IsPlaybackSource);
			}
		}
		
		private bool InPartyMode {
			get {
				return (IsActiveSource && 
				        ServiceManager.PlaybackController.Source == Parent);
			}
		}
		#endregion
		#region Event Handling
		private void HandleActiveSourceChanged (SourceEventArgs args)
		{
			if (args.Source==this)
				clutter_flow_interface.FilterView.Enabled = true;
			else
				clutter_flow_interface.FilterView.Enabled = false;

			UpdatePartyMode ();
		}
        private void OnPlaybackSourceChanged (object o, EventArgs args)
        {
			UpdatePartyMode ();
        }

        private void OnPlaybackControllerTrackStarted (object o, EventArgs args)
        {			
            CheckForSwitch ();
        }

        private void HandleUpdatedAlbum(object sender, EventArgs e)
        {
			Reload ();
			ServiceManager.SourceManager.SetActiveSource (this);
			UpdatePlayback ();
			UpdatePartyMode ();
        }

		private void HandlePMButtonToggled(object sender, EventArgs e)
		{
			//Check if this is caused by UpdatePartyMode or by user intervention:
			if (!InPartyMode && clutter_flow_interface.FilterView.PMButton.IsActive) {
				ServiceManager.PlaybackController.Source = (ITrackModelSource) Parent;
				if (!IsActiveSource) ServiceManager.SourceManager.SetActiveSource (this);
				UpdatePlayback (false);
			} else if (InPartyMode && !clutter_flow_interface.FilterView.PMButton.IsActive) {
				if (!IsActiveSource) ServiceManager.SourceManager.SetActiveSource (this);
				UpdatePlayback ();
			}
		}
		
		/// <summary>
		/// Checks if we are in PartyMode & if a new song started playing
		/// Called from OnPlaybackControllerTrackStarted
		/// </summary>
        private void CheckForSwitch ()
        {
			if (clutter_flow_interface!=null) {
	            TrackInfo current_track = ServiceManager.PlaybackController.CurrentTrack;
	            if (current_track != null && previous_track != current_track) {
					if (IsActiveSource)
						clutter_flow_interface.FilterView.LabelTrack.SetValueWithAnim (current_track.TrackNumber + " - " + current_track.TrackTitle);
					if (InPartyMode) {
						clutter_flow_interface.FilterView.Enabled = true;
						DatabaseAlbumInfo album = DatabaseAlbumInfo.FindOrCreate (
								DatabaseArtistInfo.FindOrCreate (current_track.AlbumArtist, current_track.AlbumArtistSort),
								current_track.AlbumTitle, current_track.AlbumTitleSort, current_track.IsCompilation);
						clutter_flow_interface.FilterView.AlbumLoader.ScrollTo(album);
					}
	            }
				previous_track = current_track;
			}
        }

		private void UpdatePartyMode ()
		{
			if (InPartyMode) {
				clutter_flow_interface.FilterView.AlbumLoader.NoReloading = true;
				clutter_flow_interface.FilterView.PMButton.IsActive = true;
			} else {
				clutter_flow_interface.FilterView.AlbumLoader.NoReloading = false;
				clutter_flow_interface.FilterView.PMButton.IsActive = false;
			}
		}
		
		private void UpdatePlayback ()
		{
			UpdatePlayback (true);
		}
		private void UpdatePlayback (bool switch_source)
		{
			if (!ClutterFlowSchemas.InstantPlayback.Get ()) {
				if (switch_source) ServiceManager.PlaybackController.NextSource = this;
				if (!ServiceManager.PlayerEngine.IsPlaying()) 
					ServiceManager.PlayerEngine.Play();
			} else {
				if (switch_source) ServiceManager.PlaybackController.Source = this;
				if (!ServiceManager.PlayerEngine.IsPlaying()) 
					ServiceManager.PlayerEngine.Play();
				else
					ServiceManager.PlaybackController.Next();
			}
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

        public DatabaseAlbumListModel AlbumModel {
            get {
				if (album_model==null) CreateAlbumListModel ();
				return album_model; 
			}
        }

        public void Reload () // to implement sorting: create a DatabaseAlbumListModel subclass
        {
			AlbumInfo album = clutter_flow_interface.FilterView.ActiveAlbum;
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

		public void CreateAlbumListModel ()
		{
			if (album_model==null) {
				DatabaseSource src = ServiceManager.SourceManager.MusicLibrary;
				album_model = new DatabaseAlbumListModel (src, src.DatabaseTrackModel, ServiceManager.DbConnection, src.UniqueId);
				src.AppendFilter (album_model);
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
