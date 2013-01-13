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
				_store.AppendValues (i+1,_sheet.entry (i).title (),offset);
			}
		}
		
		public void CreateGui() {

			Gtk.Image icn_reload=new Gtk.Image(Gtk.Stock.Refresh,Gtk.IconSize.Button);
			_reload=new Gtk.Button(icn_reload);
			_reload.Clicked+=OnReload;
			
			_performer=new Gtk.Entry(200);
			_title=new Gtk.Entry(200);
			_title.WidthChars=60;
			_performer.WidthChars=60;
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
			
			_store=new Gtk.ListStore(typeof(int),typeof(string),typeof(string));
			_tracks=new Gtk.TreeView();
			_tracks.AppendColumn ("Nr.", new Gtk.CellRendererText (), "text", 0);
			_tracks.AppendColumn ("Title", new Gtk.CellRendererText (), "text", 1);	
			_tracks.AppendColumn ("Offset", new Gtk.CellRendererText (), "text", 2);	
			//_tracks.CursorChanged += new EventHandler(EvtCursorChanged);
			//_tracks.RowActivated += new Gtk.RowActivatedHandler(EvtTrackRowActivated);
			_tracks.Model = _store;
			
			Gtk.Table tbl=new Gtk.Table(2,2,false);
			tbl.Attach (new Gtk.Label("Album:"),0,1,0,1);
			tbl.Attach (_title,1,2,0,1);
			tbl.Attach (new Gtk.Label("Artist:"),0,1,1,2);
			tbl.Attach (_performer,1,2,1,2);
			
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
			scroll.SetSizeRequest (-1,300);
			
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
			_sheet.SetPerformer(nPerformer);
			_sheet.SetTitle(nTitle);
			_sheet.SetImagePath(_imagefile.Filename);
			_sheet.ClearTracks();
			Gtk.TreeIter iter;
			if (_store.GetIterFirst(out iter)) {
				do {
					string title=(string) _store.GetValue (iter,1);
					string offset=(string) _store.GetValue (iter,2);
					string []parts=Regex.Split(offset,"[.:]");
					double e_offset;
					int min=Convert.ToInt32(parts[0]);
					int secs=Convert.ToInt32(parts[1]);
					int hsecs=Convert.ToInt32(parts[2]);
					e_offset=min*60+secs+(hsecs/100.0);
					_sheet.AddTrack(title,nPerformer,e_offset);
				} while(_store.IterNext (ref iter));
			}
			_sheet.Save();
		}
	}
}

