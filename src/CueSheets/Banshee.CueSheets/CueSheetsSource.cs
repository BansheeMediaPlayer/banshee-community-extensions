//
// CueSheetsSource.cs
//
// Authors:
//   Cool Extension Author <hans@oesterholt.net>
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
		TrackListModel _nullModel;
		double			  pbsize=-1.0;

				
        public CueSheetsSource () : base (AddinManager.CurrentLocalizer.GetString ("CueSheets"),
                                               AddinManager.CurrentLocalizer.GetString ("CueSheets"),
		                                       sort_order,
		                                       "hod-cuesheets-2013-01-06")
        {
			_view=new CustomView(this);
			_nullModel=new MemoryTrackListModel();
            Properties.Set<ISourceContents> ("Nereid.SourceContents", _view);
			Properties.SetString ("Icon.Name", "cueplay");

            Hyena.Log.Information ("CueSheets source has been instantiated.");
        }

        // A count of 0 will be hidden in the source TreeView
        public override int Count {
            get { 
				CueSheet s=_view.getSheet ();
				if (s==null) { return 0; }
				else { return s.nEntries (); }
			}
        }
		
		public CueSheet getSheet() {
			return _view.getSheet();
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
				CueSheet s=_view.getSheet ();
				if (s==null) {
					return _nullModel; 
				} else {
					return s;
				}
			} 
		}

        public bool HasDependencies { get { return false; } }

        public bool CanAddTracks { get { return false; } }

        public bool CanRemoveTracks { get { return false; } }

        public bool CanDeleteTracks { get { return false; } }

        public bool ConfirmRemoveTracks { get { return false; } }

        public bool CanRepeat { get { return true; } }

        public bool CanShuffle { get { return true; } }

        public bool ShowBrowser { get { return false; } }

        public bool Indexable { get { return true; } }

        public void RemoveTracks (Hyena.Collections.Selection selection) {
        }

        public void DeleteTracks (Hyena.Collections.Selection selection) {
        }
        #endregion
		
		public void OnConfigure (object o, EventArgs ea) {
			new CueSheetsConfigDialog (this);
            return;
        }
		
		public double getPbSize() {
			if (pbsize<0.0) {
				pbsize=Banshee.Configuration.ConfigurationClient.Get<double>("cuesheets_zoom",1.0);
			}
			return pbsize;
		}
		
		public void setPbSize(double s) {
			pbsize=s;
			Banshee.Configuration.ConfigurationClient.Set<double>("cuesheets_zoom",s);
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
				
				double d=src.getPbSize ();
				Gtk.HScale hs=new Gtk.HScale(1.0,3.0,0.25);
				hs.Value=d;
				Gtk.Label lbl1=new Gtk.Label("Size of music icons:");
				Gtk.HBox box1=new Gtk.HBox();
				box1.Add (lbl1);
				box1.Add (hs);
				box1.ShowAll ();
				
				this.VBox.Add (box);
				this.VBox.Add (box1);
				this.AddButton (Gtk.Stock.Ok,1);
				this.AddButton (Gtk.Stock.Cancel,2);
				this.Title="CueSheets Preferences";
				int result=this.Run ();
				if (result==1) {
					dir=btn.Filename;
					src.setCueSheetDir(dir);
					src.setPbSize (hs.Value);
				}
				this.Destroy ();
			}
		}
		
        private class CustomView : ISourceContents
        {
			//Gtk.Button 		up;
			Gtk.ListStore     store;
			Gtk.TreeView      view;
			//Gtk.Image		  img;
			Gtk.VBox		  box;
			string			  type="directory";
			CueSheet		  sheet;
			//Gtk.Table		  table;
			Gtk.ScrolledWindow scroll;
			int               index=-1;
			private CueSheetsSource MySource=null;
			
			uint _position=0;
			bool _set_position=false;
			
			const int IMG_NOIMAGE=0;
			const int IMG_NOIMAGE_DIR=1;
			const int IMG_DIRECTORY=2;
			
			Gdk.Pixbuf _img_dir=null;
			Gdk.Pixbuf _img_playlist=null;
			Gdk.Pixbuf _pb_size=null;
			

			private Gdk.Pixbuf GetDefaultPixBuf(int type) {
				Gdk.Pixbuf img=_img_playlist;
				if (type==1000) { img=_pb_size; }
				if (type==IMG_DIRECTORY) {
					img=_img_dir;
				}
				return img;
			}

			private int getPixbufWidth() {
				return (int) (_pb_size.Width*MySource.getPbSize());
			}

			private int getPixbufHeight() {
				return (int) (_pb_size.Height*MySource.getPbSize());
			}
			
			public CueSheet getSheet() {
				return sheet;
			}

			private String  basedir=null;
			private String  cwdir=null;
			
			public string basename(string f) {
				string bn=Regex.Replace (f,"^([^/]*[/])+","");
				return bn;
			}
			
			public string cuename(string f) {
				string cn=Regex.Replace(basename (f),"[.]cue$","");
				return cn;
			}
			
			public void fill(Gtk.ListStore store) {
				type="directory";
				store.Clear ();
				if (basedir==null) { 
					basedir=MySource.getCueSheetDir (); 
				}
				if (cwdir==null) { cwdir=basedir; }
				string [] dirs=Directory.GetDirectories(cwdir, "*");
				string [] sheets=Directory.GetFiles (cwdir,"*.cue");
				int i=0;
				foreach (string file in sheets) {
					i=i+1;
					string name=cuename(file);
					CueSheet s=new CueSheet(file,cwdir);
					Gdk.Pixbuf pb;
					if (s.imageFileName ()!="") {
						string imgf=cwdir+"/"+s.imageFileName ();
						pb=new Gdk.Pixbuf(imgf,getPixbufWidth(),getPixbufHeight());
					} else {
						pb=GetDefaultPixBuf(IMG_NOIMAGE_DIR);
					}					
					store.AppendValues(i,pb,name);
				}
				foreach (string dir in dirs) {
					string name=basename (dir);
					if (name.Substring (0,1)!=".") {
						i=i+1;
						Gdk.Pixbuf pb=GetDefaultPixBuf(IMG_DIRECTORY);
						store.AppendValues(i,pb,name);
					}
				}
			}
			
			public void seekSong(int i) {
				CueSheetEntry e=sheet.entry (i);
				double offset=e.offset ();
				//Console.WriteLine ("Offset="+offset+", current="+ServiceManager.PlayerEngine.Position+
				//                   ", msecs="+offset*1000.0
				//                   );
				//ServiceManager.PlayerEngine.SetNextTrack (e);
				//ServiceManager.PlayerEngine.Open(e);
				ServiceManager.PlayerEngine.SetCurrentTrack(e);
				_position=(uint) (offset*1000.0);
				_set_position=true;
				mscount=chgcount-(1000/timeout);
				//ServiceManager.PlayerEngine.PlayAt((uint) (offset*1000.0));
				//ServiceManager.PlayerEngine.Position=_position;
				
			}
			
			public void reLoad() {
				index=0;
				ServiceManager.PlayerEngine.SetAccurateSeek(true);
				CueSheetEntry e=sheet.entry(index);
				ServiceManager.PlayerEngine.Open (e);
				ServiceManager.PlayerEngine.Play ();
				//ServiceManager.PlayerEngine.PlayAt ((uint) (e.offset ()*1000.0));
				if (ServiceManager.PlaybackController.Source!=MySource) {
					ServiceManager.PlaybackController.Source=MySource; 
				} 
				if (ServiceManager.PlaybackController.NextSource!=MySource) {
					ServiceManager.PlaybackController.NextSource=MySource; 
				}
				ServiceManager.PlaybackController.SetSeekMode (true);
				mscount=chgcount-1;
			}
			
			// Every N ms
			private int timeout=100;
			private int mscount=0;
			private int chgcount=3000/100; // every 3 seconds
			
			public bool PositionDisplay() {
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
				if (sheet==null) { return false; }
				if (index>0) {
					index-=1;
				} else {
					index=sheet.nEntries ()-1;
				}
				seekSong(index);
				return true;
			}
			
			public void loadCueSheet(string file,Gtk.ListStore store) {
				type="cuesheet";
				//Console.WriteLine ("loadCueSheet:"+file);
				store.Clear ();
				sheet=new CueSheet(file,cwdir);
				Gdk.Pixbuf pb;
				/*if (sheet.imageFileName ()!="") {
					string imgf=cwdir+"/"+sheet.imageFileName ();
					pb=new Gdk.Pixbuf(imgf,getPixbufWidth(),getPixbufHeight());
				} else {*/
					pb=GetDefaultPixBuf(IMG_NOIMAGE);
				/*}*/
				int i=0;
				for(i=0;i<sheet.nEntries ();i++) {
					store.AppendValues (i+1,pb,sheet.entry (i).title ());
				}
				//Console.WriteLine (type+", image="+sheet.imageFileName ());
				
				/*if (File.Exists (imgf)) {
					Gdk.Rectangle a=img.Allocation;
					Gdk.Pixbuf pb=new Gdk.Pixbuf(imgf,a.Width,a.Height);
					img.Pixbuf=pb;
				}*/
				reLoad ();
			}

			public CustomView(CueSheetsSource ms) {
				MySource=ms;
				{
					Gtk.Label w=new Gtk.Label();
					_pb_size=w.RenderIcon (Gtk.Stock.Index.ToString (),Gtk.IconSize.Button,null);
					_img_dir=w.RenderIcon (Gtk.Stock.Directory.ToString (),Gtk.IconSize.Button,null);
					_img_playlist=w.RenderIcon(Gtk.Stock.Index.ToString (),Gtk.IconSize.Button,null);
				}
					
				
				store = new Gtk.ListStore(typeof(int),typeof(Gdk.Pixbuf),typeof(string));
				view  = new Gtk.TreeView();
				Gtk.CellRendererPixbuf cpb=new Gtk.CellRendererPixbuf();
				view.AppendColumn ("Nr.", new Gtk.CellRendererText (), "text", 0);
				view.AppendColumn ("Icon.",cpb,"pixbuf",1);
				view.AppendColumn ("Track/Album", new Gtk.CellRendererText (), "text", 2);	
				view.CursorChanged += new EventHandler(EvtCursorChanged);
				view.RowActivated += new Gtk.RowActivatedHandler(EvtRowActivated);
				view.Model = store;
				scroll=new Gtk.ScrolledWindow();
				scroll.Add (view);
				
				Gtk.Toolbar bar=new Gtk.Toolbar();
				
				Gtk.Image icn_up=new Gtk.Image(Gtk.Stock.GoUp,Gtk.IconSize.Button);
				Gtk.Button up=new Gtk.Button(icn_up);
				up.Clicked+=new EventHandler(handleUp);
				bar.Add (up);
				
				Gtk.Image icn_set=new Gtk.Image(Gtk.Stock.Preferences,Gtk.IconSize.Button);
				Gtk.Button prefs=new Gtk.Button(icn_set);
				prefs.Clicked+=new EventHandler(handleSet);
				bar.Add (prefs);
				
				Gtk.Image icn_about=new Gtk.Image(Gtk.Stock.About,Gtk.IconSize.Button);
				Gtk.Button about=new Gtk.Button(icn_about);
				about.Clicked+=new EventHandler(handleAbout);
				bar.Add (about);

				fill(store);
				box   = new Gtk.VBox();
				box.PackStart (bar,false,true,0);
				box.PackStart(scroll);
				box.ShowAll();
				
				GLib.Timeout.Add ((uint) timeout,(GLib.TimeoutHandler) PositionDisplay);
			}
			
			public void handleUp(object sender,EventArgs a) {
				if (cwdir==basedir) {
					// do nothing
				} else {
					if (type!="cuesheet") {
						cwdir=Regex.Replace (cwdir,"[/][^/]*$","");
					}
					fill (store);
				}
			}
			
			public void handleSet(object sender,EventArgs a) {
				MySource.OnConfigure (sender,a);
				string d=MySource.getCueSheetDir (); 
				if (basedir!=d) {
					basedir=d;
					cwdir=d;
					fill (store);
				}
			}
			
			public void handleAbout(object sender,EventArgs a) {
				Gtk.AboutDialog ab=new Gtk.AboutDialog();
				ab.Title="About the CueSheets extension";
				ab.Authors=new string[] {"Hans Oesterholt"};
				ab.Authors[0]="Hans Oesterholt";
				ab.Version="0.0.1";
				ab.Comments="CueSheets is an extension that allows you to play music from cuesheets in banshee";
				ab.Website="http://oesterholt.net?env=data&page=banshee-cuesheets";
				ab.Run ();
				ab.Destroy ();
			}

			public void EvtCursorChanged(object sender,EventArgs a) {
				mscount=0; // Reset cursor change timer
			}

			public void EvtRowActivated(object sender, Gtk.RowActivatedArgs a) {
				Gtk.TreeSelection selection = (sender as Gtk.TreeView).Selection;
				Gtk.TreeModel model;
				Gtk.TreeIter iter;
				// THE ITER WILL POINT TO THE SELECTED ROW
				Console.WriteLine ("source="+ServiceManager.PlaybackController.Source);
				Console.WriteLine ("this="+this);
				if (selection.GetSelected(out model, out iter)) {
					if (this.type=="cuesheet") {
						if (ServiceManager.PlaybackController.Source != MySource) {
							reLoad ();
							//this.type="";
							//EvtRowActivated (sender,a);
						} 
						int track=(int) model.GetValue (iter,0);
						int i=track-1;
						seekSong (i);
						index=i;
					} else {
						string name=model.GetValue (iter,2).ToString ();
						string file=cwdir+"/"+name;
						string cuef=file+".cue";
						Console.WriteLine ("File="+file);
						Console.WriteLine ("CueF="+cuef);
						if (Directory.Exists (file)) {
							Console.WriteLine ("Directory");
							cwdir=file;
							fill (store);
						} else {
							if (File.Exists (cuef)) {
								Console.WriteLine ("CueSheet");
								loadCueSheet(cuef,store);
							}
						}
					}
				}
			}


            public bool SetSource (ISource source) { return true; }
            public void ResetSource () { }
            public Gtk.Widget Widget { get { return box; } }
            public ISource Source { get { return null; } }
        }

    }
}
