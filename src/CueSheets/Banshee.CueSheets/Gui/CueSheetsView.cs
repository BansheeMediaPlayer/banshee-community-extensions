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
    public class CueSheetsView : ISourceContents
    {
		private Gtk.ListStore     		store;
		private Gtk.VBox		  		box;
		private Gtk.ScrolledWindow 		ascroll,tscroll,aascroll,gscroll,ccscroll;
		private int             		index=-1;
		private CueSheetsSource 		MySource=null;
		private CS_AlbumListView 		aview;
		private Gtk.TreeView			view;
		private CS_ArtistListView 		aaview;
		private CS_ComposerListView		ccview;
		private CS_GenreListView   		gview;
		private Gtk.HPaned				hb;
		private Gtk.HPaned				hb1;
		private Gtk.VPaned				vp;
		private Gtk.Toolbar				bar;
		private Gtk.Label				filling;
		private Gtk.TreeViewColumn 		c_track,c_piece,c_artist,c_composer;
		
		private uint 					_position=0;
		private bool					_set_position=false;
		private bool					_positioning=false;
		private String  				basedir=null;
		private CueSheet 				_selected=null;
		
		public string cuename(string f) {
			string cn=Regex.Replace(Tools.basename(f),"[.]cue$","");
			return cn;
		}
		
		public List<CueSheet> getSheets() {
			return MySource.getSheets ();
		}
					
		private bool _fill_ready=true;
		private Stack<string> _fill_dirs=new Stack<string>();
		private Stack<string> _fill_cues=new Stack<string>();
		private int  _fill_count=0;
		private int  _fill_dir_count=0;
		private bool _fill_canceled=false;			
		
		private void fill(string cwd) {
			Hyena.Log.Information ("Scanning directory "+cwd);
			string [] dirs=Directory.GetDirectories(cwd, "*");
			string [] sheets=Directory.GetFiles (cwd,"*.cue");
			foreach (string dir in dirs) {
				_fill_dirs.Push (dir);
			}
			foreach (string sheet in sheets) {
				_fill_cues.Push (sheet);
			} 
			
			GLib.Timeout.Add (10,delegate() {
				if (_fill_canceled) {
					return false;
				}
				
				int i;
				while(i<50 && _fill_cues.Count>0) {
					string sheet=_fill_cues.Pop ();
					string bn=Tools.basename (sheet);
					if (bn!="") {
						if (bn.Substring (0,1)!=".") {
							CueSheet cs=new CueSheet(sheet,cwd,basedir);
							getSheets().Add (cs);
						}
					}
					i+=1;
					_fill_count+=1;
				}
				
				filling.Text="scanning "+basedir+"..."+_fill_count+" files, "+_fill_dir_count+" directories";
				if (_fill_cues.Count==0) {
					if (_fill_dirs.Count>0) {
						string dir=_fill_dirs.Pop ();
						_fill_dir_count+=1;
						fill (dir);
					} else {
						_fill_ready=true;
					}
					return false;
				} else {
					return true;
				}
			});
		}
		
		public void fill() {
			Gtk.Button cancel=new Gtk.Button("Cancel scan");
			Gtk.VSeparator sep=new Gtk.VSeparator();
			cancel.Clicked+=delegate(object sender,EventArgs args) {
				_fill_canceled=true;
			};
			bar.Add (sep);
			sep.Show ();
			bar.Add (cancel);
			cancel.Show ();
			bar.Show ();
			getSheets().Clear ();
			basedir=MySource.getCueSheetDir();
			Hyena.Log.Information ("Base directory="+basedir);
			if (basedir!=null) {
				_fill_ready=false;
				_fill_count=0;
				_fill_dir_count=0;
				_fill_canceled=false;
				_fill_cues.Clear ();
				_fill_dirs.Clear ();
				fill (basedir);
				GLib.Timeout.Add (500,delegate() {
					if (_fill_ready || _fill_canceled) {
						try {
							Hyena.Log.Information("Reload albums");
							MySource.getAlbumModel ().Reload ();
							Hyena.Log.Information(MySource.getAlbumModel ().Count.ToString ());
							Hyena.Log.Information("Reload artists");
							MySource.getArtistModel ().Reload ();
							Hyena.Log.Information(MySource.getArtistModel ().Count.ToString ());
							Hyena.Log.Information("Reload composers");
							MySource.getComposerModel ().Reload ();
							Hyena.Log.Information(MySource.getComposerModel ().Count.ToString ());
							Hyena.Log.Information("Reload genres");
							MySource.getGenreModel ().Reload ();
							Hyena.Log.Information(MySource.getGenreModel ().Count.ToString ());
							Hyena.Log.Information("Reload tracks");
							MySource.getTrackModel ().Reload ();
							Hyena.Log.Information("Reloaded all");
						} catch(System.Exception e) {
							Hyena.Log.Information (e.ToString());
						}
						Hyena.Log.Information("Reloaded");
						filling.Text="";
						bar.Remove (sep);
						bar.Remove (cancel);
						bar.Hide ();
						return false;
					} else {
						return true;
					}
				});
			} 
		}
		
		public void seekSong(int i) {
			Hyena.Log.Information("SeekSong called "+i);
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
				Hyena.Log.Information(ex.ToString ());
			}
		}
		
		public void reLoad() {
			index=0;
			try {
				CueSheet sheet=MySource.getSheet ();
				ServiceManager.PlayerEngine.SetAccurateSeek(true);
				CueSheetEntry e=sheet.entry(index);
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
				Hyena.Log.Information(ex.ToString ());
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
					_positioning=true;
					ServiceManager.PlayerEngine.Position=_position;
				}
					
				// Do nothing while seeking
				uint pos=ServiceManager.PlayerEngine.Position;
				double p=((double) pos)/1000.0;
				if (_positioning && pos<=_position) {
						//Hyena.Log.Information ("seek="+_position+", pos="+pos);
						// do nothing
				} else {
					_positioning=false;
					// Track number
					int i=sheet.searchIndex(p);
					if (i!=index && i>=0) {
						// Handle repeat track
						if (ServiceManager.PlaybackController.RepeatMode==PlaybackRepeatMode.RepeatSingle) {
							seekSong (index);
						} 
						// Every 2 seconds
						if (mscount==0) {
							index=i;
							CueSheetEntry e=sheet.entry(index);
							ServiceManager.PlayerEngine.SetCurrentTrack (e);
						}
					}
					
					if (mscount==0 && index>=0) {
						int [] idx=new int[1];
						idx[0]=index;
						
						Gtk.TreePath path=new Gtk.TreePath(idx);
						//Hyena.Log.Information ("Setting cursor: "+index+", path=");
						Gtk.TreeViewColumn c=new Gtk.TreeViewColumn();
						Gtk.TreePath pp;
						view.GetCursor (out pp,out c);
						if (pp==null || (pp.Indices[0]!=index && pp.Indices[0]>=0)) {
							view.SetCursor (path,null,false);
						}
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
		
		public void setColumnSizes(CueSheet s) {
			c_track.FixedWidth=MySource.getColumnWidth("track",s.id ());
			c_piece.FixedWidth=MySource.getColumnWidth("piece",s.id ());
			c_artist.FixedWidth=MySource.getColumnWidth("artist",s.id ());
			c_composer.FixedWidth=MySource.getColumnWidth("composer",s.id ());
		}
		
		public void loadCueSheet(CueSheet s) { //,Gtk.ListStore store) {
			setColumnSizes (s);
			CueSheet sheet=MySource.getSheet ();
			//type="cuesheet";
			sheet.Clear ();
			sheet.load (s);
			store.Clear ();
			int i=0;
			for(i=0;i<sheet.nEntries ();i++) {
				CueSheetEntry e=sheet.entry (i);
				double l=e.length ();
				int t=(int) (l*100.0);
				int m=t/(60*100);
				int secs=(t/100)%60;
				string ln=String.Format ("{0:00}:{0:00}",m,secs);
				store.AppendValues (i+1,e.title (),e.getPiece (),e.performer (),e.getComposer(),ln);
			}
			reLoad ();
		}
		
		public void loadCueSheet(int i) {
			loadCueSheet (MySource.getSheets ()[i]);
		}
		
		public void EditSheet(CueSheet s) {
			Hyena.Log.Information (s.cueFile ());
			CueSheetEditor edt=new CueSheetEditor(s);
			edt.Do ();
			MySource.getAlbumModel ().Reload ();
			MySource.getArtistModel ().Reload ();
		}
		
		public void MusicFileToDevice(CueSheet s) {
			MusicToDevice mtd=new MusicToDevice(s);
			mtd.Do ();
		}
			
		public void ToggleGrid(string forId) {
			Hyena.Log.Information ("ToggleGrid for id "+forId);
			bool grid=!MySource.getGridLayout (forId);
			Hyena.Log.Information ("Grid = "+grid);
			if (grid) {
				aview.EnableGrid ();
			} else {
				aview.DisableGrid ();
			}
			MySource.setGridLayout (forId,grid);
		}
		
		public void ToggleGrid() {
			ArtistInfo aa=MySource.getAlbumModel().filterArtist();
			GenreInfo  gg=MySource.getAlbumModel().filterGenre();
			string fa="";
			bool in_tracks=false;
			MySource.getAlbumModel ().filterAlbumOrTracks(out fa,out in_tracks);
			string a="@@allartist@@";
			if (aa!=null) { a=aa.Name; }
			string g="@@allgenre@@";
			if (gg!=null) { g=gg.Genre; }
			string f="@@searchfilter@@";
			if (fa!=null) { f=fa; }
			string id=a+"-"+g+"-"+f;
			ToggleGrid(id);
		}
		
		public void SetGrid() {
			ArtistInfo aa=MySource.getAlbumModel().filterArtist();
			GenreInfo  gg=MySource.getAlbumModel().filterGenre();
			string fa="";
			bool in_tracks=false;
			MySource.getAlbumModel ().filterAlbumOrTracks(out fa,out in_tracks);
			string a="@@allartist@@";
			if (aa!=null) { a=aa.Name; }
			string g="@@allgenre@@";
			if (gg!=null) { g=gg.Genre; }
			string f="@@searchfilter@@";
			if (fa!=null) { f=fa; }
			string id=a+"-"+g+"-"+f;
			Hyena.Log.Information ("SetGrid for id "+id);
			bool grid=MySource.getGridLayout (id);
			Hyena.Log.Information ("Grid = "+grid);
			if (grid) { aview.EnableGrid (); }
			else { aview.DisableGrid (); }
		}
		
		public CueSheetsView(CueSheetsSource ms) {
			MySource=ms;
			
			basedir=MySource.getCueSheetDir (); 
			
			store = new Gtk.ListStore(typeof(int),typeof(string),typeof(string),typeof(string),typeof(string),typeof(string));
			view  = new Gtk.TreeView();
			
			Gtk.CellRendererText cr_txt=new Gtk.CellRendererText();
			cr_txt.Scale=0.8;
			cr_txt.Ellipsize=Pango.EllipsizeMode.End;
			
			Gtk.CellRendererText cr_other=new Gtk.CellRendererText();
			cr_other.Scale=0.8;
			
			{ 
				CueSheet s=MySource.getSheet ();
			
				view.AppendColumn ("Nr.", cr_other, "text", 0);
				c_track=new Gtk.TreeViewColumn("Track",cr_txt,"text",1);
				c_track.Sizing=Gtk.TreeViewColumnSizing.Fixed;
				c_track.FixedWidth=MySource.getColumnWidth("track",s.id());
				c_track.Resizable=true;
				c_track.AddNotification ("width",delegate(object o, GLib.NotifyArgs args) {
					MySource.setColumnWidth ("track",s.id(),c_track.Width);	
				});
				view.AppendColumn (c_track);
				
				c_piece=new Gtk.TreeViewColumn("Piece",cr_txt,"text",2);
				c_piece.FixedWidth=MySource.getColumnWidth("piece",s.id ());
				c_piece.Sizing=Gtk.TreeViewColumnSizing.Fixed;
				c_piece.Resizable=true;
				c_piece.AddNotification ("width",delegate(object o, GLib.NotifyArgs args) {
					MySource.setColumnWidth ("piece",s.id (),c_piece.Width);
				});
				view.AppendColumn (c_piece);
				
				c_artist=new Gtk.TreeViewColumn("Artist",cr_txt,"text",3);
				c_artist.Sizing=Gtk.TreeViewColumnSizing.Fixed;
				c_artist.FixedWidth=MySource.getColumnWidth("artist",s.id ());
				c_artist.AddNotification("width",delegate(object o, GLib.NotifyArgs args) {
					MySource.setColumnWidth ("artist",s.id (),c_artist.Width);	
				});
				c_artist.Resizable=true;
				view.AppendColumn (c_artist);
				
				c_composer=new Gtk.TreeViewColumn("Composer",cr_txt,"text",4);
				c_composer.Sizing=Gtk.TreeViewColumnSizing.Fixed;
				c_composer.FixedWidth=MySource.getColumnWidth("composer",s.id ());
				c_composer.AddNotification("width",delegate(object o, GLib.NotifyArgs args) {
					MySource.setColumnWidth ("composer",s.id(),c_composer.Width);	
				});
				c_composer.Resizable=true;
				view.AppendColumn (c_composer);
				
				view.AppendColumn ("length", cr_other, "text", 5);	
			}
				
			view.CursorChanged += new EventHandler(EvtCursorChanged);
			view.RowActivated += new Gtk.RowActivatedHandler(EvtTrackRowActivated);
			view.Model = store;
			
			Hyena.Log.Information("New albumlist");
			aview=new CS_AlbumListView(this);
			aaview=new CS_ArtistListView();
			ccview=new CS_ComposerListView();
			gview=new CS_GenreListView();
			Hyena.Log.Information("init models");
			aview.SetModel (MySource.getAlbumModel ());
			aaview.SetModel (MySource.getArtistModel ());
			gview.SetModel (MySource.getGenreModel ());
			ccview.SetModel (MySource.getComposerModel());
			
			MySource.getGenreModel();
			Hyena.Log.Information("model albumlist");
			Hyena.Log.Information("albumlist initialized");
			
			aview.RowActivated+=new Hyena.Data.Gui.RowActivatedHandler<AlbumInfo>(EvtRowActivated);
			aview.Selection.Changed += HandleAviewSelectionChanged;
			gview.RowActivated+=new Hyena.Data.Gui.RowActivatedHandler<GenreInfo>(EvtGenreActivated);
			aaview.RowActivated+=new Hyena.Data.Gui.RowActivatedHandler<ArtistInfo>(EvtArtistActivated);
			ccview.RowActivated+=new Hyena.Data.Gui.RowActivatedHandler<CS_ComposerInfo>(EvtComposerActivated);
			
			bar=new Gtk.Toolbar();
			if (basedir==null) {
				Hyena.Log.Information("basedir="+basedir);
				Gtk.Label lbl=new Gtk.Label();
				lbl.Markup="<b>You need to configure the CueSheets music directory first, using the right mouse button on the extension</b>";
				bar.Add (lbl);
			}
			filling=new Gtk.Label();
			bar.Add (filling);
			
			ascroll=new Gtk.ScrolledWindow();
			ascroll.Add (aview);
			aascroll=new Gtk.ScrolledWindow();
			aascroll.Add (aaview);
			tscroll=new Gtk.ScrolledWindow();
			tscroll.Add (view);
			gscroll=new Gtk.ScrolledWindow();
			gscroll.Add (gview);
			ccscroll=new Gtk.ScrolledWindow();
			ccscroll.Add(ccview);
			
			bool view_artist=true;
			Gtk.VBox vac=new Gtk.VBox();
			Gtk.Button vab=new Gtk.Button("Artists");
			vab.Clicked+=delegate(object sender,EventArgs args) {
				if (view_artist) {
					view_artist=false;
					vab.Label="Composers";
					vac.Remove (aascroll);
					vac.PackEnd (ccscroll);
					ccscroll.ShowAll ();
				} else {
					view_artist=true;
					vab.Label="Artists";
					vac.Remove (ccscroll);
					vac.PackEnd (aascroll);
					aascroll.ShowAll ();
				}
			};
			vac.PackStart (vab,false,false,0);
			vac.PackEnd (aascroll);
			
			hb=new Gtk.HPaned();
			hb.Add(gscroll);
			hb.Add (vac);
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
			
			fill ();
		}

		void HandleAviewSelectionChanged (object sender, EventArgs e) {
			int index=((Selection) sender).FirstIndex;
			CS_AlbumInfo a=(CS_AlbumInfo) MySource.getAlbumModel ()[index];
			_selected=a.getSheet ();
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
			mscount=0; 
		}
		
		public void PlayAlbum(CS_AlbumInfo a) {
			loadCueSheet (a.getSheet ());
		}
		
		public void OpenContainingFolder(CS_AlbumInfo a) {
			CueSheet s=a.getSheet ();
		 	string path = System.IO.Path.GetDirectoryName (s.cueFile ());
            if (Banshee.IO.Directory.Exists (path)) {
               System.Diagnostics.Process.Start (path);
            }
		}
		
		public void EvtSearchAlbumOrTracks(object sender,string searchString,bool also_in_tracks) {
			MySource.getAlbumModel ().filterAlbumOrTracks(searchString,also_in_tracks);
			SetGrid ();
		}
		
		public void EvtRowActivated(object sender,RowActivatedArgs<AlbumInfo> args) {
			CS_AlbumInfo a=(CS_AlbumInfo) args.RowValue;
			PlayAlbum (a);
		}

		public void EvtGenreActivated(object sender,RowActivatedArgs<GenreInfo> args) {
			GenreInfo g=args.RowValue;
			if (MySource.getGenreModel ().isNullGenre (g)) { g=null; }
			MySource.getAlbumModel ().filterGenre(g);
			MySource.getArtistModel ().filterGenre(g);
			MySource.getComposerModel ().filterGenre (g);
			SetGrid ();
		}

		public void EvtArtistActivated(object sender,RowActivatedArgs<ArtistInfo> args) {
			//Hyena.Log.Information("I'm here! "+sender+", "+args);
			ArtistInfo a=args.RowValue;
			if (MySource.getArtistModel ().isNullArtist (a)) { a=null; }
			MySource.getAlbumModel ().filterArtist(a);
			MySource.getComposerModel ().filterArtist (a);
			SetGrid ();
		}

		public void EvtComposerActivated(object sender,RowActivatedArgs<CS_ComposerInfo> args) {
			//Hyena.Log.Information("I'm here! "+sender+", "+args);
			CS_ComposerInfo a=(CS_ComposerInfo) args.RowValue;
			if (MySource.getComposerModel ().isNullComposer (a)) { a=null; }
			MySource.getAlbumModel ().filterComposer(a);
			MySource.getArtistModel ().filterComposer (a);
			SetGrid ();
		}
		
		
		public void EvtTrackRowActivated(object sender,Gtk.RowActivatedArgs args) {
			Hyena.Log.Information ("Row activated, seeking");
			Gtk.TreeSelection selection = (sender as Gtk.TreeView).Selection;
			Gtk.TreeModel model;
			Gtk.TreeIter iter;
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

