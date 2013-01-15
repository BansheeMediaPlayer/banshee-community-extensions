using System;
using System.Text.RegularExpressions;

namespace Banshee.CueSheets
{
	public class CueSheetEditor : Gtk.Dialog
	{
		private CueSheet _sheet;
		
		private Gtk.Image 				_image;
		private Gtk.FileChooserButton 	_imagefile;
		private Gtk.Entry 				_performer;
		private Gtk.Entry 				_title;
		private Gtk.Entry				_composer;
		private Gtk.Entry				_subtitle;
		private Gtk.Entry				_year;
		private Gtk.TreeView			_tracks;
		private Gtk.ListStore			_store;
		private Gtk.Button				_reload;
		private Gtk.Button				_add_track;
		private Gtk.Button				_del_track;
		private Gtk.Button				_save;
		
		public CueSheetEditor (CueSheet s) {
			//_sheet=new CueSheet(s.cueFile());
			_sheet=s;
		}
		
		public void Do() {
			CreateGui();
			Reload();
			base.Run ();
			this.Destroy ();
		}
		
		public void Reload() {
			_title.Text=_sheet.title ();
			_performer.Text=_sheet.performer ();
			_composer.Text=_sheet.composer ();
			_year.Text=_sheet.year ();
			_subtitle.Text=_sheet.subtitle ();
			
			try {
				_imagefile.SelectFilename (_sheet.imageFullFileName());
				Gdk.Pixbuf pb=new Gdk.Pixbuf(_sheet.imageFullFileName(),100,100);
				_image.Pixbuf=pb;
			} catch(System.Exception e) {
				Hyena.Log.Information (e.ToString ());
			}
			_store.Clear ();
			int i,N;
			for(i=0,N=_sheet.nEntries ();i<N;i++) {
				double b=_sheet.entry (i).offset ();
				int m,s,hs,t;
				t=(int) (b*100.0);
				hs=t%100;
				m=t/(100*60);
				s=(t/100)%60;
				String offset=String.Format ("{0:00}",m)+":"+
							  String.Format ("{0:00}",s)+"."+
							  String.Format ("{0:00}",hs);
				_store.AppendValues (i+1,_sheet.entry (i).title (),_sheet.entry (i).performer (),
				                     _sheet.entry (i).getComposer (),_sheet.entry (i).getPiece (),offset);
			}
		}
		
		private void setCell(int column,string nt,Gtk.TreePath path) {
			Gtk.TreeIter iter;
			_store.GetIter(out iter,path);
			_store.SetValue (iter,column,nt);
			Hyena.Log.Information ("Cuesheet editing - data="+nt+", path="+path.Indices[0]);
		}
		
		public void CreateGui() {

			Gtk.Image icn_reload=new Gtk.Image(Gtk.Stock.Refresh,Gtk.IconSize.Button);
			_reload=new Gtk.Button(icn_reload);
			_reload.Clicked+=OnReload;
			
			_performer=new Gtk.Entry(200);
			_title=new Gtk.Entry(200);
			_title.WidthChars=60;
			_performer.WidthChars=60;
			_subtitle=new Gtk.Entry(300);
			_subtitle.WidthChars=60;
			_composer=new Gtk.Entry(200);
			_composer.WidthChars=60;
			_year=new Gtk.Entry(20);
			_year.WidthChars=20;
			
			_image=new Gtk.Image();
			_image.SetSizeRequest (100,100);
			_imagefile=new Gtk.FileChooserButton("Choose image file",Gtk.FileChooserAction.Open);
			_imagefile.FileSet+=new EventHandler(EvtImageSet);
			
			Gtk.Image icn_add_track=new Gtk.Image(Gtk.Stock.Add,Gtk.IconSize.Button);
			_add_track=new Gtk.Button(icn_add_track);
			_add_track.Clicked+=OnAddTrack;
			
			Gtk.Image icn_del_track=new Gtk.Image(Gtk.Stock.Delete,Gtk.IconSize.Button);
			_del_track=new Gtk.Button(icn_del_track);
			_del_track.Clicked+=OnDelTrack;
			
			Gtk.Image icn_save=new Gtk.Image(Gtk.Stock.Save,Gtk.IconSize.Button);
			_save=new Gtk.Button(icn_save);
			_save.Clicked+=OnSave;
			
			_store=new Gtk.ListStore(typeof(int),typeof(string),typeof(string),typeof(string),typeof(string),typeof(string));
			_tracks=new Gtk.TreeView();
			{
				Gtk.CellRendererText cr0=new Gtk.CellRendererText();
				cr0.Scale=0.8;
				_tracks.AppendColumn ("Nr.", cr0, "text", 0);
				
				Gtk.CellRendererText cr_title=new Gtk.CellRendererText();
				cr_title.Scale=0.8;
				cr_title.Editable=true;
				cr_title.Edited+=new Gtk.EditedHandler(delegate(object sender,Gtk.EditedArgs args) {
					setCell(1,args.NewText,new Gtk.TreePath(args.Path));
				});
				_tracks.AppendColumn ("Title", cr_title, "text", 1);	

				Gtk.CellRendererText cr_artist=new Gtk.CellRendererText();
				cr_artist.Editable=true;
				cr_artist.Scale=0.8;
				cr_artist.Edited+=new Gtk.EditedHandler(delegate(object sender,Gtk.EditedArgs args) {
					setCell(2,args.NewText,new Gtk.TreePath(args.Path));
				});
				_tracks.AppendColumn ("Artist", cr_artist, "text", 2);	

				Gtk.CellRendererText cr_composer=new Gtk.CellRendererText();
				cr_composer.Editable=true;
				cr_composer.Scale=0.8;
				cr_composer.Edited+=new Gtk.EditedHandler(delegate(object sender,Gtk.EditedArgs args) {
					setCell(3,args.NewText,new Gtk.TreePath(args.Path));
				});
				_tracks.AppendColumn ("Composer", cr_composer, "text", 3);	
				
				Gtk.CellRendererText cr_piece=new Gtk.CellRendererText();
				cr_piece.Editable=true;
				cr_piece.Scale=0.8;
				cr_piece.Edited+=new Gtk.EditedHandler(delegate(object sender,Gtk.EditedArgs args) {
					setCell(4,args.NewText,new Gtk.TreePath(args.Path));
				});
				_tracks.AppendColumn ("Piece", cr_piece, "text", 4);	
				
				_tracks.AppendColumn ("Offset", cr0, "text", 5);	
			}
			
			//_tracks.CursorChanged += new EventHandler(EvtCursorChanged);
			//_tracks.RowActivated += new Gtk.RowActivatedHandler(EvtTrackRowActivated);
			_tracks.Model = _store;
			
			Gtk.Table tbl=new Gtk.Table(2,5,false);
			tbl.Attach (new Gtk.Label("Album:"),0,1,0,1);
			tbl.Attach (_title,1,2,0,1);
			tbl.Attach (new Gtk.Label("Artist:"),0,1,1,2);
			tbl.Attach (_performer,1,2,1,2);
			tbl.Attach (new Gtk.Label("Composer:"),0,1,2,3);
			tbl.Attach (_composer,1,2,2,3);
			tbl.Attach (new Gtk.Label("Subtitle:"),0,1,3,4);
			tbl.Attach (_subtitle,1,2,3,4);
			tbl.Attach (new Gtk.Label("year:"),0,1,4,5);
			tbl.Attach (_year,1,2,4,5);
			
			Gtk.Frame frm=new Gtk.Frame();
			frm.Add (tbl);
			
			Gtk.HBox hb2=new Gtk.HBox();
			hb2.PackEnd (_reload,false,false,1);
			hb2.PackEnd (_del_track,false,false,1);
			hb2.PackEnd (_add_track,false,false,1);
			hb2.PackEnd (_save,false,false,1);
			
			Gtk.HBox hb=new Gtk.HBox();
			Gtk.VBox vb1=new Gtk.VBox();
			vb1.PackStart (frm,false,false,0);
			vb1.PackStart (hb2,true,true,0);
			hb.PackStart (vb1,false,false,0);
			
			Gtk.Frame frm2=new Gtk.Frame();
			frm2.Add (_image);
			hb.PackEnd (frm2,false,false,2);
			
			Gtk.ScrolledWindow scroll=new Gtk.ScrolledWindow();
			scroll.Add (_tracks);
			scroll.SetSizeRequest (800,300);
			
			base.VBox.PackStart(hb,false,false,4);
			base.VBox.PackStart (_imagefile,false,false,4);
			base.VBox.PackStart(scroll,true,true,0);
			base.VBox.ShowAll ();
			
			base.AddButton ("Close",0);
		}
		
		public void OnReload(object sender,EventArgs args) {
			Reload ();
		}
		
		public void OnAddTrack(object sender,EventArgs args) {
		}
		
		public void OnDelTrack(object sender,EventArgs args) {
		}
		
		public void EvtImageSet(object sender,EventArgs args) {
			try {
				string imgf=_imagefile.Filename;
				Gdk.Pixbuf pb=new Gdk.Pixbuf(imgf,100,100);
				_image.Pixbuf=pb;
			} catch(System.Exception e) {
				Hyena.Log.Information (e.ToString ());
			}
		}
		
		public void OnSave(object sender,EventArgs args) {
			string nPerformer=_performer.Text.Trim ();
			string nTitle=_title.Text.Trim ();
			string nComposer=_composer.Text.Trim();
			string nYear=_year.Text.Trim();
			string nSubtitle=_subtitle.Text.Trim();
			
			_sheet.SetPerformer(nPerformer);
			_sheet.SetTitle(nTitle);
			_sheet.SetComposer(nComposer);
			_sheet.SetYear(nYear);
			_sheet.SetSubtitle(nSubtitle);
			_sheet.SetImagePath(_imagefile.Filename);
			
			_sheet.ClearTracks();
			
			Gtk.TreeIter iter;
			if (_store.GetIterFirst(out iter)) {
				do {
					string title=(string) _store.GetValue (iter,1);
					string perf=(string) _store.GetValue (iter,2);
					string composer=(string) _store.GetValue (iter,3);
					if (composer.Trim ()=="") { composer=nComposer; }
					string piece=(string) _store.GetValue (iter,4);
					piece=piece.Trim ();
					string offset=(string) _store.GetValue (iter,5);
					string []parts=Regex.Split(offset,"[.:]");
					double e_offset;
					int min=Convert.ToInt32(parts[0]);
					int secs=Convert.ToInt32(parts[1]);
					int hsecs=Convert.ToInt32(parts[2]);
					e_offset=min*60+secs+(hsecs/100.0);
					if (perf.Trim ()=="") { perf=nPerformer; }
					CueSheetEntry e=_sheet.AddTrack(title,perf,e_offset);
					e.setComposer (composer);
					e.setPiece (piece);
				} while(_store.IterNext (ref iter));
			}
			_sheet.Save();
		}
	}
}

