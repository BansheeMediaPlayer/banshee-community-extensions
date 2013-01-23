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
using Mono.Unix;
using System.Threading;

namespace Banshee.CueSheets
{
    public class CueSheetsView : ISourceContents
    {
		private CS_TrackListModel     	store;
		private Gtk.VBox		  		box;
		private Gtk.ScrolledWindow 		ascroll,tscroll,aascroll,gscroll,ccscroll;
		private int             		index=-1;
		private CueSheetsSource 		MySource=null;
		private CS_AlbumListView 		aview;
		private CS_TrackListView		view;
		private CS_ArtistListView 		aaview;
		private CS_ComposerListView		ccview;
		private CS_GenreListView   		gview;
		private CS_PlayListsView		plsview;
		private CS_PlayListAdmin		plsadmin;
		private Gtk.HPaned				hb;
		private Gtk.HPaned				hb1;
		private Gtk.HPaned				hbpls;
		private Gtk.VPaned				vp;
		private Gtk.Toolbar				bar;
		private Gtk.Label				filling;
		
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
		
		private LibraryImportManager import_manager=null;
		
		//private Stack<DatabaseTrackInfo> stk=new Stack<DatabaseTrackInfo>();
		
		public void ImportSheet(CueSheet s) {
			if (import_manager==null) {
				try {
					import_manager = new LibraryImportManager (false);
					/*import_manager.ImportResult+=delegate(object sender,DatabaseImportResultArgs args) {
						DatabaseTrackInfo trk=args.Track;
						stk.Push (trk);
					};*/
				} catch (Exception ex) {
					Hyena.Log.Error (ex.ToString ());
				}
			}
			
			Hyena.Log.Debug ("Importsheet: Starting transaction");
			int i,N;
			for(i=0,N=s.nEntries ();i<N;i++) {
				try {
					CueSheetEntry e=s.entry (i);
					string file=e.file ();
					string uuid=Regex.Replace(e.id (),"\\s","_");
					string ext=".mp3";
					
					string uid=Guid.NewGuid ().ToString ();
					string u1=uid.Substring (0,1);
					string u2=uid.Substring (0,2);
					string dir=basedir+"/.banshee/"+u1;
					if (!Directory.Exists (dir))  {
						Directory.CreateDirectory(dir);
					}
					dir+="/"+u2;
					if (!Directory.Exists(dir)) {
						Directory.CreateDirectory(dir);
					}
					uuid=dir+"/"+uuid+ext;
					
					UnixFileInfo f=new UnixFileInfo(file);	
					if (File.Exists (uuid)) { File.Delete(uuid); }
					//f.CreateLink (uuid);
					f.CreateSymbolicLink(uuid);
					
					DatabaseTrackInfo trk=import_manager.ImportTrack(uuid);
					//File.Delete (uuid);
					/*if (trk==null) {
						Hyena.Log.Warning ("track = null (file="+e.file ()+")");
						if (stk.Count>0) { trk=stk.Pop (); }
					}*/ 
					
					if (trk==null) {
						Hyena.Log.Error ("track = null (file="+e.file ()+")");
					} else {
						Hyena.Log.Information ("track!=null (file="+e.file ()+")");
						//MySource.DbConnection.BeginTransaction();
						trk.PartOfCue=1;
						trk.CueAudioFile=e.file ();
						trk.AlbumTitle=s.title ();
						//trk.Album=s.title ();
						trk.AlbumArtist=s.performer ();
						trk.Composer=(e.Composer=="") ? s.composer () : e.Composer;
						//trk.ArtworkId=s.getArtId ();
						//trk.Artist=
						trk.ArtistName=(e.performer ()=="") ? s.performer () : e.performer ();
						trk.TrackTitle=e.title ();
						trk.TrackNumber=i+1;
						trk.Genre=s.genre ();
						trk.BeginOffset=e.BeginOffset;
						trk.EndOffset=e.EndOffset;
						//trk.Uri=trk.CueAudioUri;
  						//trk.MediaAttributes = TrackMediaAttributes.ExternalResource;
                    	//trk.PrimarySource = ServiceManager.SourceManager.MusicLibrary;
						
						trk.Save ();
						//MySource.DbConnection.CommitTransaction();
					}
				} catch (Exception ex) {
					Hyena.Log.Error (ex.ToString ());
				}
			}
			import_manager.NotifyAllSources ();
		}
		
		
		
		private void FillLibrary(string cwd) {
			string [] dirs=Directory.GetDirectories(cwd, "*");
			string [] sheets=Directory.GetFiles (cwd,"*.cue");
			string ddir=basedir+"/.banshee";
			if (!Directory.Exists(ddir)) {
				Directory.CreateDirectory(ddir);
			}
			foreach (string sheet in sheets) {
				CueSheet cs=new CueSheet(sheet,cwd,basedir);
				ImportSheet(cs);
				Thread.Sleep (500);
			}
			foreach (string dir in dirs) {
				FillLibrary (dir);
			}
		}
		
		private void FillLibrary() {
			basedir=MySource.getCueSheetDir();
			Hyena.Log.Information ("Base directory="+basedir);
			Thread thrd=new Thread(delegate() {
				FillLibrary (basedir);
			});
			thrd.Start ();
		}
		
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
				
				int i=0;
				while(i<1 && _fill_cues.Count>0) {
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
							Hyena.Log.Information("Reload play lists");
							MySource.getPlayListsModel().Reload();
							Hyena.Log.Information("Reloaded all");
						} catch(System.Exception e) {
							Hyena.Log.Information (e.ToString());
						}
						Hyena.Log.Information("Reloaded");
						filling.Text="";
						bar.Remove (sep);
						bar.Remove (cancel);
						bar.Hide ();
						FillLibrary ();
						return false;
					} else {
						return true;
					}
				});
			} 
		}
		
		private string _song_file=null;
		private string _song_id=null;
		
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
				_song_id=e.id ();
				if (_song_file!=e.file ()) {
					ServiceManager.PlayerEngine.Open (e);
					ServiceManager.PlayerEngine.Play ();
					_song_file=e.file ();
				}
				double offset=e.offset ();
				//ServiceManager.PlayerEngine.SetCurrentTrack(e);
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
				//ServiceManager.PlayerEngine.SetAccurateSeek(true);
				CueSheetEntry e=sheet.entry(index);
				_song_id=e.id ();
				ServiceManager.PlayerEngine.Open (e);
				ServiceManager.PlayerEngine.Play ();
				if (ServiceManager.PlaybackController.Source!=MySource) {
					ServiceManager.PlaybackController.Source=MySource; 
				} 
				if (ServiceManager.PlaybackController.NextSource!=MySource) {
					ServiceManager.PlaybackController.NextSource=MySource; 
				}
				//ServiceManager.PlaybackController.SetSeekMode (true);
			} catch (SystemException ex) {
				Hyena.Log.Information(ex.ToString ());
			}
			mscount=chgcount-1;
		}
		
		// Every N ms
		private int 			timeout=100;
		private int 			mscount=0;
		private int 			chgcount=3000/100; // every 3 seconds
		//private CueSheetEntry 	_playing=null;
		
		public bool PositionDisplay() {
			if (ServiceManager.PlaybackController.Source==MySource) {
				CueSheet sheet=MySource.getSheet ();
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
						int i=sheet.searchIndex(_song_id,p);
						if (i!=index && i>=0) {
							// Handle repeat track
							if (ServiceManager.PlaybackController.RepeatMode==PlaybackRepeatMode.RepeatSingle) {
								seekSong (index);
							} else if (sheet.SheetKind==CueSheet.Kind.PlayList) {
								index=i;
								seekSong (i);
							}
							// Every 2 seconds
							if (mscount==0) {
								Hyena.Log.Information("Found index i="+i+", songid="+_song_id);
								index=i;
								CueSheetEntry e=sheet.entry(index);
								Hyena.Log.Information ("current entry: "+e);
								//ServiceManager.PlayerEngine.SetCurrentTrack (e);
							}
						}
						
						if (mscount==0 && index>=0) {
							Hyena.Log.Information ("mscount="+mscount+", index="+index);
							//view.ScrollTo(index);
							view.Selection.QuietUnselect (view.Selection.FirstIndex);
							view.Selection.Select(index);
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
		
		private int _set_column_sizes=0;
		
		public void setColumnSizes(CueSheet s) {
			int N=view.ColumnController.Count;
			int i;
			for(i=0;i<N;i++) {
				_set_column_sizes+=1;
				CS_Column col=(CS_Column) view.ColumnController[i];
				double w=(i==0) ? 0.05 : 0.16;
				if (s!=null) {
					w=MySource.getColumnWidth (col.id(),s.id ());
				}
				col.Width=w;
			}
		}
		
		public void loadCueSheet(CueSheet s) { //,Gtk.ListStore store) {
			MySource.setSheet(s);
			CueSheet sheet=MySource.getSheet ();
			store.SetSheet(sheet);
			store.Reload ();
			reLoad ();
			Hyena.Log.Information ("Setting column sizes for "+s.id ()); 
			setColumnSizes (s);
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
			CS_MusicToDevice mtd=new CS_MusicToDevice(s);
			mtd.Do ();
		}
		
		public void PlayPlayList (CS_PlayList pls)
		{
			Hyena.Log.Information ("Playing playlist "+pls.PlsName);
			CueSheet s=pls.GetCueSheet ();
			loadCueSheet (s);
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
			CS_GenreInfo  gg=MySource.getAlbumModel().filterGenre();
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
			CS_GenreInfo  gg=MySource.getAlbumModel().filterGenre();
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
			
			store = new CS_TrackListModel();
			view  = new CS_TrackListView(this);
			
			{
				ColumnController colc=view.ColumnController;
				int i,N;
				for(i=0,N=colc.Count;i<N;i++) {
					CS_Column col=(CS_Column) colc[i];
					col.WidthChanged+=delegate(object sender,EventArgs args) {
						Hyena.Log.Information ("set-column-sizes="+_set_column_sizes);
						if (_set_column_sizes<=0) {
							_set_column_sizes=0;
							MySource.setColumnWidth (col.id(),MySource.getSheet ().id (),col.Width);
						} else {
							_set_column_sizes-=1;
						}
					};
				}
			}
			
			view.SetModel(store);
			this.setColumnSizes(null);
			
			Hyena.Log.Information("New albumlist");
			aview=new CS_AlbumListView(this);
			aaview=new CS_ArtistListView();
			ccview=new CS_ComposerListView();
			gview=new CS_GenreListView();
			try {
			plsview=new CS_PlayListsView(this);
			} catch (System.Exception ex) {
				Hyena.Log.Error (ex.ToString ());
			}
			
			Hyena.Log.Information("init models");
			aview.SetModel (MySource.getAlbumModel ());
			aaview.SetModel (MySource.getArtistModel ());
			gview.SetModel (MySource.getGenreModel ());
			ccview.SetModel (MySource.getComposerModel());
			plsview.SetModel(MySource.getPlayListsModel());
			
			plsadmin=new CS_PlayListAdmin(plsview,MySource.getPlayListsModel(),MySource.getPlayListCollection());

			MySource.getGenreModel();
			Hyena.Log.Information("model albumlist");
			Hyena.Log.Information("albumlist initialized");
			
			aview.RowActivated+=new Hyena.Data.Gui.RowActivatedHandler<AlbumInfo>(EvtRowActivated);
			aview.Selection.Changed += HandleAviewSelectionChanged;
			gview.RowActivated+=new Hyena.Data.Gui.RowActivatedHandler<CS_GenreInfo>(EvtGenreActivated);
			aaview.RowActivated+=new Hyena.Data.Gui.RowActivatedHandler<ArtistInfo>(EvtArtistActivated);
			ccview.RowActivated+=new Hyena.Data.Gui.RowActivatedHandler<CS_ComposerInfo>(EvtComposerActivated);
			plsview.RowActivated+=new Hyena.Data.Gui.RowActivatedHandler<CS_PlayList>(EvtPlayListActivated);
			view.RowActivated+=new RowActivatedHandler<CueSheetEntry>(EvtTrackRowActivated); 
			
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

			Gtk.HPaned hppls=new Gtk.HPaned();
			hppls.Add1 (vp);
			hppls.Add2 (plsadmin);
			hbpls=hppls;
			
			{
				int hb_pls,hb_p,hb1_p,vp_p;
				MySource.getPositions (out hb_pls,out hb_p,out hb1_p,out vp_p);
				hppls.Position=hb_pls;
				hb.Position=hb_p;
				hb1.Position=hb1_p;
				vp.Position=vp_p;
			}
			

			box   = new Gtk.VBox();
			box.PackStart (bar,false,true,0);
			box.PackStart (hppls);
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
		int hbpls_prev=-1;
		
		public bool GardDividers() {
			if (hb_prev==-1) {
				hb_prev=hb.Position;
				hb1_prev=hb1.Position;
				vp_prev=vp.Position;
				hbpls_prev=hbpls.Position;
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
			if (hbpls_prev!=hbpls.Position) {
				hbpls_prev=hbpls.Position;
				changed=true;
			}
			if (changed) {
				MySource.setPositions(hbpls_prev,hb_prev,hb1_prev,vp_prev);
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

		public void EvtGenreActivated(object sender,RowActivatedArgs<CS_GenreInfo> args) {
			CS_GenreInfo g=args.RowValue;
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
			CS_ComposerInfo a=(CS_ComposerInfo) args.RowValue;
			if (MySource.getComposerModel ().isNullComposer (a)) { a=null; }
			MySource.getAlbumModel ().filterComposer(a);
			MySource.getArtistModel ().filterComposer (a);
			SetGrid ();
		}
		
		public void EvtPlayListActivated(object sender,RowActivatedArgs<CS_PlayList> args) {
			Hyena.Log.Information("I'm here! "+sender+", "+args);
			
			
			//CS_ComposerInfo a=(CS_ComposerInfo) args.RowValue;
			//if (MySource.getComposerModel ().isNullComposer (a)) { a=null; }
			//MySource.getAlbumModel ().filterComposer(a);
			//MySource.getArtistModel ().filterComposer (a);
			//SetGrid ();
		}
		
		public void EvtTrackRowActivated(object sender,RowActivatedArgs<CueSheetEntry> args) {
			Hyena.Log.Information ("Row activated, seeking");
			seekSong (args.Row);
		}
			
		public CueSheetsSource GetSource() { return MySource; }

        public bool SetSource (ISource source) { return true; }
        public void ResetSource () { }
        public Gtk.Widget Widget { get { return box; } }
        public ISource Source { get { return null; } }
    }
}

