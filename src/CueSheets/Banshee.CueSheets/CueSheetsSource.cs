//
// CueSheetsSource.cs
//
// Authors:
//   Hans Oesterholt <hans@oesterholt.net>
//
// Copyright (C) 2013 Hans Oesterholt
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

using Mono.Addins;

using Banshee.Base;
using Banshee.Sources;
using Banshee.Sources.Gui;

// Other namespaces you might want:
using Banshee.ServiceStack;
using Banshee.Preferences;
using Banshee.MediaEngine;
using Banshee.PlaybackController;
using System.IO;

using Banshee.CueSheets;
using System.Text.RegularExpressions;
using Banshee.Collection;
using Banshee.Library;
using Banshee.Collection.Gui;
using Hyena.Data;
using Hyena.Collections;
using System.Collections.Generic;
using Banshee.Collection.Database;
using Hyena.Data.Gui;
using Banshee.Gui;
using Banshee.I18n;
using Banshee.Configuration;

namespace Banshee.CueSheets
{

		

    // We are inheriting from Source, the top-level, most generic type of Source.
    // Other types include (inheritance indicated by indentation):
    //      DatabaseSource - generic, DB-backed Track source; used by PlaylistSource
    //        PrimarySource - 'owns' tracks, used by DaapSource, DapSource
    //          LibrarySource - used by Music, Video, Podcasts, and Audiobooks
    public class CueSheetsSource : Source, IBasicPlaybackController, ITrackModelSource
    {
        // In the sources TreeView, sets the order value for this source, small on top
        const int sort_order = 50;
		
		private CueSheetsView			_view;
		private Gtk.MenuItem 			_menuItem;
		private Gtk.SeparatorMenuItem 	_sep;
		private Gtk.CheckButton			_track_search;
		
		private List<CueSheet> 			_sheets=new List<CueSheet>();
		private CueSheet	   			_sheet=null;
        private CueSheetsPrefs 			_preferences;
		private Actions					_actions;
		
		private CS_TrackInfoDb			_track_info_db;
			
        public CueSheetsSource () : base (AddinManager.CurrentLocalizer.GetString ("CueSheets"),
                                          AddinManager.CurrentLocalizer.GetString ("CueSheets"),
		                                  sort_order,
										  "hod-cuesheets-2013-01-06")
        {
			Hyena.Log.Information ("CueSheetsSouce init");
			
			_track_info_db=new CS_TrackInfoDb(ServiceManager.DbConnection);

			_sheet=new CueSheet();
			
			_view=new CueSheetsView(this);
            
			Properties.Set<ISourceContents> ("Nereid.SourceContents", _view);
			Properties.SetString ("Icon.Name", "cueplay");
            Hyena.Log.Information ("CueSheets source has been instantiated.");
			
			Properties.Set<string> ("SearchEntryDescription", Catalog.GetString ("Search albums and tracks"));
			
			try {
				Properties.SetString("GtkActionPath","/CueSheetsPopup");
				_actions = new Actions (this);
				Hyena.Log.Information(_actions.ToString());
			} catch (System.Exception ex) {
				Hyena.Log.Information(ex.ToString ());
			}
			
            InterfaceActionService action_service = ServiceManager.Get<InterfaceActionService> ();
			try {
				_track_search=new Gtk.CheckButton("Search Tracks");
				_track_search.Clicked+=delegate(object sender,EventArgs args) {
					this.DoFilter();
				};
				Gtk.Toolbar header_toolbar = (Gtk.Toolbar) action_service.UIManager.GetWidget ("/HeaderToolbar");
				int i,N,k;
				for(i=0,k=-1,N=header_toolbar.NItems;i<N;i++) {
					Gtk.Widget w=header_toolbar.GetNthItem(i).Child;
					if (w!=null) {
						if (w.GetType()==typeof(Banshee.Gui.Widgets.ConnectedVolumeButton)) {
							k=i;
						}
					}
				}
				if (k>=0) {
					Hyena.Log.Information("Toolitem itm");
					Gtk.ToolItem itm=new Gtk.ToolItem();
					Hyena.Log.Information ("Add cbk");
					itm.Add (_track_search);
					Hyena.Log.Information ("Insert cbk");
					header_toolbar.Insert (itm,k);
					itm.Show ();
				}
			} catch (System.Exception ex) {
				Hyena.Log.Error (ex.ToString ());
			}
			Gtk.Menu viewMenu = (action_service.UIManager.GetWidget ("/MainMenu/ViewMenu") as Gtk.MenuItem).Submenu as Gtk.Menu;
            _menuItem = new Gtk.MenuItem (Catalog.GetString ("_Change Album View"));
            _menuItem.Activated += delegate {
				_view.ToggleGrid();
            };
            viewMenu.Insert (_menuItem, 2);
			_sep=new Gtk.SeparatorMenuItem();
			viewMenu.Insert (_sep,3);
        }
		
		public override void Activate ()
		{
            _menuItem.Show ();
			_sep.Show ();
			_track_search.Show ();
		}
		
	    public override void Deactivate()  {
            _menuItem.Hide ();
			_sep.Hide ();
			_track_search.Hide();
		}
		
		
        public override string PreferencesPageId {
            get {
				if (_preferences==null) { _preferences=new CueSheetsPrefs(this); }
				_preferences.createGui();
                return _preferences.PageId;
            }
        }

        // A count of 0 will be hidden in the source TreeView
        public override int Count {
            get { 
				CueSheet s=getSheet ();
				if (s==null) { return 0; }
				else { return s.nEntries (); }
			}
        }
		
		public CueSheet getSheet() {
			return _sheet;
		}
		
		public List<CueSheet> getSheets() {
			return _sheets;
		}
		
        #region IBasicPlaybackController implementation
        public bool First ()
        {
			Hyena.Log.Information("First called");
            return true;
        }

        public bool Next (bool restart, bool changeImmediately)
        {
			//Hyena.Log.Information ("next  called");
			return _view.next ();
        }

        public bool Previous (bool restart)
        {
			//Hyena.Log.Information ("previous:"+restart);
			return _view.previous ();
        }
		#endregion
		
		private string _query="";
		
		public override bool CanSearch { get { return true; } }
		
		public override string FilterQuery {
			get { return _query; }
			set { 
				_query=value;
				DoFilter ();
			}
		}
		
		public void DoFilter() {
			_view.EvtSearchAlbumOrTracks (this,_query,_track_search.Active);
		}
		
 		#region ITrackModelSource implementation
        public void Reload () {
			Hyena.Log.Information("reloading");
			_view.reLoad();
        }

        public TrackListModel TrackModel { 
			get { 
				return _sheet;				
			} 
		}

        public bool HasDependencies { get { return false; } }

        public bool CanAddTracks { get { return false; } }

        public bool CanRemoveTracks { get { return false; } }

        public bool CanDeleteTracks { get { return false; } }

        public bool ConfirmRemoveTracks { get { return false; } }

        public bool CanRepeat { get { return true; } }

        public bool CanShuffle { get { return true; } }

        public bool ShowBrowser { get { return true; } }

        public bool Indexable { get { return true; } }

        public void RemoveTracks (Hyena.Collections.Selection selection) {
        }

        public void DeleteTracks (Hyena.Collections.Selection selection) {
        }
        #endregion

		private CS_AlbumModel _model=null;
		private CS_ArtistModel _artistModel=null;
		private CS_GenreModel  _genreModel=null;
		private CS_ComposerModel _composerModel=null;
		
		public CS_AlbumModel getAlbumModel() {
			if (_model==null) { 
				Hyena.Log.Information("AlbumModel init");
				_model=new CS_AlbumModel(this); 
			}
			return _model;
		}
		
		public TrackListModel getTrackModel() {
			return this.TrackModel;
		}
		
		public CS_GenreModel getGenreModel() {
			if (_genreModel==null) { 
				Hyena.Log.Information("GenreModel init");
				_genreModel=new CS_GenreModel(this); 
			}
			return _genreModel;
		}
		
		public CS_ArtistModel getArtistModel() {
			if (_artistModel==null) { 
				Hyena.Log.Information ("ArtistModel init");
				_artistModel=new CS_ArtistModel(this); 
			}
			return _artistModel;
		}
		
		public CS_ComposerModel getComposerModel() {
			if (_composerModel==null) { 
				Hyena.Log.Information ("ComposerModel init");
				_composerModel=new CS_ComposerModel(this); 
			}
			return _composerModel;
		}
		
		public void setPositions(int hb,int hb1,int vp) {
			Banshee.Configuration.ConfigurationClient.Set<int>("cuesheets_hb",hb);
			Banshee.Configuration.ConfigurationClient.Set<int>("cuesheets_hb1",hb1);
			Banshee.Configuration.ConfigurationClient.Set<int>("cuesheets_vp",vp);
		}
		
		public void getPositions(out int hb,out int hb1, out int vp) {
			hb=Banshee.Configuration.ConfigurationClient.Get<int>("cuesheets_hb",100);
			hb1=Banshee.Configuration.ConfigurationClient.Get<int>("cuesheets_hb1",200);
			vp=Banshee.Configuration.ConfigurationClient.Get<int>("cuesheets_vp",200);
		}
		
		public bool getGridLayout(string id) {
			bool grid=true;
			_track_info_db.Get ("grid-"+id,out grid,true);
			return grid;
		}
		
		public void setGridLayout(string id,bool g) {
			_track_info_db.Set ("grid-"+id,g);
		}
		
		public void setColumnWidth(string type,string albumid,int w) {
			_track_info_db.Set ("col-"+type+"-"+albumid,w);
		}
		
		public int getColumnWidth(string type,string albumid) {
			int w=150;
			_track_info_db.Get ("col-"+type+"-"+albumid,out w,150);
			return w;
		}
		
		public string getCueSheetDir() {
			string dir=Banshee.Configuration.ConfigurationClient.Get<string>("cuesheets_dir",null);
			Hyena.Log.Information ("cuesheets dir="+dir);
			return dir;
		}
		
		public void setCueSheetDir(string dir) {
			Hyena.Log.Information ("Setting cuesheets dir to "+dir);
			Banshee.Configuration.ConfigurationClient.Set<string>("cuesheets_dir",dir);
			_view.fill ();
		}
		
		public void RefreshCueSheets() {
			Hyena.Log.Information("refreshing");
			_view.fill ();
		}
		
    }
}
