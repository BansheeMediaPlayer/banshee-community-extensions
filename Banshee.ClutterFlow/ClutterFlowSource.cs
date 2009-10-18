//
// NowPlayingSource.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2008 Novell, Inc.
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
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
	
        private ClutterFlowSourceContents clutter_flow_contents;
		private MemoryTrackListModel track_model = new MemoryTrackListModel();
        
        //public ClutterFlowSource () : base("clutterflow", ServiceManager.SourceManager.MusicLibrary)
		public ClutterFlowSource () : base("ClutterFlow", "ClutterFlow", 0)
        {
			TypeUniqueId = "ClutterFlow";
            //Initialize ();
            //AfterInitialized ();	
		
            clutter_flow_contents = new ClutterFlowSourceContents ();
			clutter_flow_contents.FilterView.UpdatedAlbum += HandleUpdatedAlbum;
			clutter_flow_contents.SetSource(this);
			Properties.SetString ("Icon.Name", "clutterflow-icon");
            Properties.Set<ISourceContents> ("Nereid.SourceContents", clutter_flow_contents);
			
            Properties.Set<bool> ("Nereid.SourceContents.HeaderVisible", true);
			
			//ServiceManager.SourceManager.ActiveSourceChanged += HandleActiveSourceChanged;
			
			SetParentSource(ServiceManager.SourceManager.MusicLibrary);
			Parent.AddChildSource(this);
			
			/* TODO add context menu's, actions etc. */
			
			//Create();
			Reload();
        }
		
		//ISourceContents old_source_contents = null;
		/*protected void HandleActiveSourceChanged(SourceEventArgs args)
		{

		}*/
		
        public void Dispose ()
        {
            if (clutter_flow_contents != null) {
                clutter_flow_contents.Destroy ();
                clutter_flow_contents.Dispose ();
                clutter_flow_contents = null;
            }
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
			AlbumInfo album = clutter_flow_contents.FilterView.CurrentAlbum;
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
        }

        public void DeleteSelectedTracks ()
        {
            throw new Exception ("Should not call DeleteSelectedTracks on ClutterFlowSource");
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
