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
		CustomView  _view;
		//TrackListModel _nullModel;
		//double			  pbsize=-1.0;
		List<CueSheet> _sheets=new List<CueSheet>();
		CueSheet	   _sheet=null;
        private CueSheetsPrefs preferences;


				
        public CueSheetsSource () : base (AddinManager.CurrentLocalizer.GetString ("CueSheets"),
                                          AddinManager.CurrentLocalizer.GetString ("CueSheets"),
		                                  sort_order,
										  "hod-cuesheets-2013-01-06")
        {
			Console.WriteLine ("CueSheetsSouce init");
			_sheet=new CueSheet();
			_view=new CustomView(this);
            Properties.Set<ISourceContents> ("Nereid.SourceContents", _view);
			Properties.SetString ("Icon.Name", "cueplay");
            Hyena.Log.Information ("CueSheets source has been instantiated.");
        }
		
		
        public override string PreferencesPageId {
            get {
				preferences=new CueSheetsPrefs(this);
                return preferences.PageId;
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
			Console.WriteLine ("First called");
            return true;
        }

        public bool Next (bool restart, bool changeImmediately)
        {
			//Console.WriteLine ("next  called");
			return _view.next ();
        }

        public bool Previous (bool restart)
        {
			//Console.WriteLine ("previous:"+restart);
			return _view.previous ();
        }
		#endregion
		
 		#region ITrackModelSource implementation
        public void Reload () {
			Console.WriteLine ("reloading");
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
		
		public CS_AlbumModel getAlbumModel() {
			if (_model==null) { 
				Console.WriteLine ("AlbumModel init");
				_model=new CS_AlbumModel(this); 
			}
			return _model;
		}
		
		public TrackListModel getTrackModel() {
			return this.TrackModel;
		}
		
		public CS_GenreModel getGenreModel() {
			if (_genreModel==null) { 
				Console.WriteLine ("GenreModel init");
				_genreModel=new CS_GenreModel(this); 
			}
			return _genreModel;
		}
		
		public CS_ArtistModel getArtistModel() {
			if (_artistModel==null) { 
				Console.WriteLine ("ArtistModel init");
				_artistModel=new CS_ArtistModel(this); 
			}
			return _artistModel;
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
		
		public string getCueSheetDir() {
			string dir=Banshee.Configuration.ConfigurationClient.Get<string>("cuesheets_dir","");
			//string dir=Properties.Get<string>("cuesheets_dir","");
			if (dir=="") {
				dir=System.Environment.GetEnvironmentVariable ("HOME");
			}
			Console.WriteLine ("getcuesheetdir="+dir);
			return dir;
		}
		
		public void setCueSheetDir(string dir) {
			//Console.WriteLine ("setcuesheetdir:"+dir);
			//Properties.SetString ("cuesheets_dir",dir);
			Banshee.Configuration.ConfigurationClient.Set<string>("cuesheets_dir",dir);
			_view.fill ();
			//_view.reLoad ();
		}
		
		private class CueSheetsConfigDialog : Gtk.Dialog {
			public CueSheetsConfigDialog(CueSheetsSource src) {

				string dir=src.getCueSheetDir();
				Gtk.Label lbl=new Gtk.Label("CueSheet Music Directory:");
				Gtk.FileChooserButton btn=new Gtk.FileChooserButton("CueSheet Music Directory:",Gtk.FileChooserAction.SelectFolder);
				btn.SelectFilename (dir);
				Gtk.HBox box=new Gtk.HBox();
				box.Add (lbl);
				box.Add (btn);
				box.ShowAll ();
				
				/*double d=src.getPbSize ();
				Gtk.HScale hs=new Gtk.HScale(1.0,5.0,0.25);
				hs.Value=d;
				Gtk.Label lbl1=new Gtk.Label("Size of music icons:");
				Gtk.HBox box1=new Gtk.HBox();
				box1.Add (lbl1);
				box1.Add (hs);
				box1.ShowAll ();*/
				
				this.VBox.Add (box);
				//this.VBox.Add (box1);
				this.AddButton (Gtk.Stock.Ok,1);
				this.AddButton (Gtk.Stock.Cancel,2);
				this.Title="CueSheets Preferences";
				int result=this.Run ();
				if (result==1) {
					dir=btn.Filename;
					src.setCueSheetDir(dir);
					//src.setPbSize (hs.Value);
				}
				this.Destroy ();
			}
		}
		
        private class CustomView : ISourceContents
        {
			Gtk.ListStore     		store;
			Gtk.VBox		  		box;
			string			  		type="directory";
			Gtk.ScrolledWindow 		ascroll,tscroll,aascroll,gscroll;
			int               		index=-1;
			private CueSheetsSource MySource=null;
			AlbumListView 			aview;
			Gtk.TreeView			view;
			ArtistListView 			aaview;
			GenreListView   		gview;
			Gtk.HPaned				hb;
			Gtk.HPaned				hb1;
			Gtk.VPaned				vp;
			
			uint 					_position=0;
			bool					_set_position=false;
			private String  		basedir=null;
			private CueSheet 		_selected=null;
			
			public string basename(string f) {
				string bn=Regex.Replace (f,"^([^/]*[/])+","");
				return bn;
			}
			
			public string cuename(string f) {
				string cn=Regex.Replace(basename (f),"[.]cue$","");
				return cn;
			}
			
			public List<CueSheet> getSheets() {
				return MySource.getSheets ();
			}
			
			
			private void fill(string cwd) {
				//Console.WriteLine ("fill:"+cwd);
				string [] dirs=Directory.GetDirectories(cwd, "*");
				string [] sheets=Directory.GetFiles (cwd,"*.cue");
				foreach (string dir in dirs) {
					if (dir.Substring (0,1)!=".") {
						fill (dir);
					}
				}
				foreach (string sheet in sheets) {
					if (sheet.Substring (0,1)!=".") {
						CueSheet cs=new CueSheet(sheet,cwd,basedir);
						getSheets().Add (cs);
					}
				}
			}
			
			public void fill() {
				getSheets().Clear ();
				basedir=MySource.getCueSheetDir();
				fill (basedir);
				Console.WriteLine ("Filled");
				try {
					Console.WriteLine ("Reload albums");
					MySource.getAlbumModel ().Reload ();
					Console.WriteLine ("Reload artists");
					MySource.getArtistModel ().Reload ();
					Console.WriteLine ("Reload genres");
					MySource.getGenreModel ().Reload ();
					Console.WriteLine ("Reload tracks");
					MySource.getTrackModel ().Reload ();
					Console.WriteLine ("Reloaded all");
					Console.WriteLine (MySource.getAlbumModel ().Count);
					Console.WriteLine (MySource.getArtistModel ().Count);
					Console.WriteLine (MySource.getGenreModel ().Count);
				} catch(System.Exception e) {
					Console.WriteLine (e.ToString());
				}
				Console.WriteLine ("Reloaded");
			}
			
			public void seekSong(int i) {
				Console.WriteLine ("SeekSong called "+i);
				try {
					CueSheet sheet=MySource.getSheet ();
					if (sheet.Count==0) {
						if (_selected!=null) {
							loadCueSheet (_selected);
						}
					}
					CueSheetEntry e=sheet.entry (i);
					double offset=e.offset ();
					ServiceManager.PlayerEngine.SetCurrentTrack(e);
					_position=(uint) (offset*1000.0);
					_set_position=true;
					mscount=chgcount-(1000/timeout);
				} catch (SystemException ex) {
					Console.WriteLine (ex);
				}
			}
			
			public void reLoad() {
				index=0;
				try {
					CueSheet sheet=MySource.getSheet ();
					ServiceManager.PlayerEngine.SetAccurateSeek(true);
					CueSheetEntry e=sheet.entry(index);
				//Console.WriteLine (e.ToString ());
					ServiceManager.PlayerEngine.Open (e);
					ServiceManager.PlayerEngine.Play ();
					if (ServiceManager.PlaybackController.Source!=MySource) {
						ServiceManager.PlaybackController.Source=MySource; 
					} 
					if (ServiceManager.PlaybackController.NextSource!=MySource) {
						ServiceManager.PlaybackController.NextSource=MySource; 
					}
					ServiceManager.PlaybackController.SetSeekMode (true);
				} catch (SystemException ex) {
					Console.WriteLine (ex);
				}
				mscount=chgcount-1;
			}
			
			// Every N ms
			private int timeout=100;
			private int mscount=0;
			private int chgcount=3000/100; // every 3 seconds
			
			public bool PositionDisplay() {
				CueSheet sheet=MySource.getSheet ();
				if (ServiceManager.PlaybackController.Source==MySource) {
				if (sheet!=null) {
					mscount+=1;
					if (mscount>chgcount) { mscount=0; }
					
					// Position if necessary
					if (_set_position) {
						_set_position=false;
						ServiceManager.PlayerEngine.Position=_position;
					}
					
					// Track number
					uint pos=ServiceManager.PlayerEngine.Position;
					double p=((double) pos)/1000.0;
					int i=sheet.searchIndex(p);
					//Console.WriteLine ("Position="+p+", i="+i+", index="+index);
					if (i!=index && i>=0) {
						// Handle repeat track
						if (ServiceManager.PlaybackController.RepeatMode==PlaybackRepeatMode.RepeatSingle) {
							seekSong (index);
						} else {
							// Every 2 seconds
							if (mscount==0) {
								index=i;
								CueSheetEntry e=sheet.entry(index);
								ServiceManager.PlayerEngine.SetCurrentTrack (e);
							}
						}
					}
					
					if (type=="cuesheet" && mscount==0) {
						int [] idx=new int[1];
						idx[0]=index;
						
						Gtk.TreePath path=new Gtk.TreePath(idx);
						Gtk.TreeViewColumn c=new Gtk.TreeViewColumn();
						Gtk.TreePath pp;
						view.GetCursor (out pp,out c);
						if (pp==null || pp.Indices[0]!=index) {
							view.SetCursor (path,null,false);
						}
					}
					
				}
				}
				return true;
			}
			
			public Boolean next() {
				CueSheet sheet=MySource.getSheet ();
				if (sheet==null) { return false; }
				if (index<sheet.nEntries ()-1) {
					index+=1;
				} else {
					index=0;
				}
				seekSong(index);
				return true;
			}
			
			public Boolean previous() {
				CueSheet sheet=MySource.getSheet ();
				if (sheet==null) { return false; }
				if (index>0) {
					index-=1;
				} else {
					index=sheet.nEntries ()-1;
				}
				seekSong(index);
				return true;
			}
			
			public void loadCueSheet(CueSheet s) { //,Gtk.ListStore store) {
				CueSheet sheet=MySource.getSheet ();
				type="cuesheet";
				sheet.Clear ();
				sheet.load (s);
				store.Clear ();
				int i=0;
				for(i=0;i<sheet.nEntries ();i++) {
					store.AppendValues (i+1,sheet.entry (i).title ());
				}
				reLoad ();
			}
			
			public void loadCueSheet(int i) {
				loadCueSheet (MySource.getSheets ()[i]);
			}
			
			internal class MyAlbumListView : AlbumListView {
				private CustomView _view;
				
				public MyAlbumListView(CustomView view) : base() {
					_view=view;
				}
				
				protected override bool OnPopupMenu () {
					Gtk.Menu mnu=new Gtk.Menu();
					Gtk.ImageMenuItem play=new Gtk.ImageMenuItem(Gtk.Stock.MediaPlay,null);
					play.Activated+=delegate(object sender,EventArgs a) {
						_view.PlayAlbum((CS_AlbumInfo) this.Model[Selection.FirstIndex]);
					};
					mnu.Append (play);
					mnu.ShowAll ();
					mnu.Popup();
					return false;
				}
			}


			public CustomView(CueSheetsSource ms) {
				MySource=ms;
				
				basedir=MySource.getCueSheetDir (); 
					
				store = new Gtk.ListStore(typeof(int),typeof(string));
				view  = new Gtk.TreeView();
				view.AppendColumn ("Nr.", new Gtk.CellRendererText (), "text", 0);
				view.AppendColumn ("Track", new Gtk.CellRendererText (), "text", 1);	
				view.CursorChanged += new EventHandler(EvtCursorChanged);
				view.RowActivated += new Gtk.RowActivatedHandler(EvtTrackRowActivated);
				view.Model = store;
				
				Console.WriteLine ("New albumlist");
				aview=new MyAlbumListView(this);
				aaview=new ArtistListView();
				gview=new GenreListView();
				Console.WriteLine ("init models");
				aview.SetModel (MySource.getAlbumModel ());
				aaview.SetModel (MySource.getArtistModel ());
				gview.SetModel (MySource.getGenreModel ());
				MySource.getGenreModel();
				Console.WriteLine ("model albumlist");
				Console.WriteLine ("albumlist initialized");
				
				aview.RowActivated+=new Hyena.Data.Gui.RowActivatedHandler<AlbumInfo>(EvtRowActivated);
				aview.Selection.Changed += HandleAviewSelectionChanged;
				gview.RowActivated+=new Hyena.Data.Gui.RowActivatedHandler<GenreInfo>(EvtGenreActivated);
				aaview.RowActivated+=new Hyena.Data.Gui.RowActivatedHandler<ArtistInfo>(EvtArtistActivated);
				
				Gtk.Toolbar bar=new Gtk.Toolbar();
				
				fill ();
				
				ascroll=new Gtk.ScrolledWindow();
				ascroll.Add (aview);
				aascroll=new Gtk.ScrolledWindow();
				aascroll.Add (aaview);
				tscroll=new Gtk.ScrolledWindow();
				tscroll.Add (view);
				gscroll=new Gtk.ScrolledWindow();
				gscroll.Add (gview);
				
				hb=new Gtk.HPaned();
				hb.Add(gscroll);
				hb.Add (aascroll);
				hb1=new Gtk.HPaned();
				hb1.Add (hb);
				hb1.Add (ascroll);
				
				vp=new Gtk.VPaned();
				vp.Add (hb1);
				vp.Add (tscroll);

				{
					int hb_p,hb1_p,vp_p;
					MySource.getPositions (out hb_p,out hb1_p,out vp_p);
					hb.Position=hb_p;
					hb1.Position=hb1_p;
					vp.Position=vp_p;
				}

				box   = new Gtk.VBox();
				box.PackStart (bar,false,true,0);
				box.PackStart (vp);
				box.ShowAll();
				
				GLib.Timeout.Add ((uint) 1000,(GLib.TimeoutHandler) GardDividers);
				GLib.Timeout.Add ((uint) timeout,(GLib.TimeoutHandler) PositionDisplay);
			}

			void HandleAviewSelectionChanged (object sender, EventArgs e) {
				int index=((Selection) sender).FirstIndex;
				CS_AlbumInfo a=(CS_AlbumInfo) MySource.getAlbumModel ()[index];
				_selected=a.getSheet ();
				//Console.WriteLine (_selected);
			}
			
			int hb_prev=-1;
			int hb1_prev=-1;
			int vp_prev=-1;
			
			public bool GardDividers() {
				if (hb_prev==-1) {
					hb_prev=hb.Position;
					hb1_prev=hb1.Position;
					vp_prev=vp.Position;
				}
				bool changed=false;
				if (hb_prev!=hb.Position) {
					hb_prev=hb.Position;
					changed=true;
				}
				if (hb1_prev!=hb1.Position) {
					hb1_prev=hb1.Position;
					changed=true;
				}
				if (vp_prev!=vp.Position) {
					vp_prev=vp.Position;
					changed=true;
				}
				if (changed) {
					MySource.setPositions(hb_prev,hb1_prev,vp_prev);
				}
				return true;
			}
			
			
			public void EvtCursorChanged(object sender,EventArgs a) {
				mscount=0; // Reset cursor change timer
				Console.WriteLine ("sender:"+sender+", "+a);
			}
			
			public void PlayAlbum(CS_AlbumInfo a) {
				loadCueSheet (a.getSheet ());
			}

			public void EvtRowActivated(object sender,RowActivatedArgs<AlbumInfo> args) {
				//Console.WriteLine ("I'm here! "+sender+", "+args);
				CS_AlbumInfo a=(CS_AlbumInfo) args.RowValue;
				//Console.WriteLine ("sheet: "+a.getSheet ().ToString ());
				PlayAlbum (a);
			}

			public void EvtGenreActivated(object sender,RowActivatedArgs<GenreInfo> args) {
				//Console.WriteLine ("I'm here! "+sender+", "+args);
				GenreInfo g=args.RowValue;
				if (MySource.getGenreModel ().isNullGenre (g)) { g=null; }
				MySource.getAlbumModel ().filterGenre(g);
				MySource.getArtistModel ().filterGenre(g);
			}

			public void EvtArtistActivated(object sender,RowActivatedArgs<ArtistInfo> args) {
				Console.WriteLine ("I'm here! "+sender+", "+args);
				ArtistInfo a=args.RowValue;
				if (MySource.getArtistModel ().isNullArtist (a)) { a=null; }
				MySource.getAlbumModel ().filterArtist(a);
			}

			public void EvtTrackRowActivated(object sender,Gtk.RowActivatedArgs args) {
				Gtk.TreeSelection selection = (sender as Gtk.TreeView).Selection;
				Gtk.TreeModel model;
				Gtk.TreeIter iter;
				// THE ITER WILL POINT TO THE SELECTED ROW
				//Console.WriteLine ("source="+ServiceManager.PlaybackController.Source);
				//Console.WriteLine ("this="+this);
				if (selection.GetSelected(out model, out iter)) {
					if (ServiceManager.PlaybackController.Source != MySource) {
						reLoad ();
					} 
					int track=(int) model.GetValue (iter,0);
					int i=track-1;
					seekSong (i);
					index=i;
				}
			}
				

            public bool SetSource (ISource source) { return true; }
            public void ResetSource () { }
            public Gtk.Widget Widget { get { return box; } }
            public ISource Source { get { return null; } }
        }

    }
}
