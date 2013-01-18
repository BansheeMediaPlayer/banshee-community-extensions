using System;
using Banshee.Collection.Gui;
using Gtk;

namespace Banshee.CueSheets
{
	public class CS_PlayListAdmin : Gtk.VBox
	{
		private CS_PlayListsModel 		_model;
		private CS_PlayListCollection 	_col;
		private Gtk.ScrolledWindow      plsscroll,plscroll;
		private CS_PlayListModel		_pls_model;
		private CS_PlayListView			_pls_view;
		private CS_PlayList				_pls;
		private Gtk.Entry				_pls_name;
		
		public CS_PlayListAdmin (CS_PlayListsView plsview,CS_PlayListsModel mdl,CS_PlayListCollection cl)
		{
			_col=cl;
			_model=mdl;
			Gtk.HBox hb=new Gtk.HBox();
			
			Gtk.Button _add=new Gtk.Button(Gtk.Stock.Add);
			_add.Clicked+=delegate (object sender,EventArgs args) {
				OnAddPls();
			};
			
			Gtk.Button _remove=new Gtk.Button(Gtk.Stock.Remove);
			_remove.Clicked+=delegate (object sender,EventArgs args) {
				OnRemovePls();
			};
			
			hb.PackStart (_add);
			hb.PackStart (_remove);
			
			plsscroll=new Gtk.ScrolledWindow();
			plsscroll.Add (plsview);
			
			_pls=null;
			_pls_name=new Gtk.Entry();
			_pls_model=new CS_PlayListModel();
			_pls_view=new CS_PlayListView();
			_pls_view.SetModel (_pls_model);
			plscroll=new Gtk.ScrolledWindow();
			plscroll.Add (_pls_view);
			
			_pls_view.DragEnd+=delegate(object sender,DragEndArgs args) {
				Console.WriteLine (args);
			};
			Gtk.VBox plsvbox=new Gtk.VBox();
			plsvbox.PackStart (_pls_name,false,false,2);
			plsvbox.PackEnd (plscroll);
			
			Gtk.VPaned vpn=new Gtk.VPaned();
			vpn.Add1 (plsscroll);
			vpn.Add2 (plsvbox);
			
			base.PackStart (hb,false,false,2);
			base.PackEnd (vpn);
			
			base.ShowAll ();
			
			mdl.SetListener (delegate(CS_PlayList pls) {
				_pls=pls;
				_pls_model.SetPlayList (_pls);
				_pls_name.Text=_pls.PlsName;
			});
		}
		
		public void OnAddPls() {
			Hyena.Log.Information ("add playlist");
			Gtk.Dialog dlg=new Gtk.Dialog();
			dlg.Title="Add Playlist";
			Gtk.Entry pls=new Gtk.Entry();
			pls.WidthChars=40;
			Gtk.Label lbl=new Gtk.Label("Playlist name:");
			Gtk.HBox hb=new Gtk.HBox();
			hb.PackStart (lbl,false,false,1);
			hb.PackEnd (pls);
			dlg.VBox.PackStart (hb);
			dlg.AddButton (Gtk.Stock.Cancel,0);
			dlg.AddButton (Gtk.Stock.Ok,1);
			dlg.VBox.ShowAll ();
			string plsname="";
			while (plsname=="") {
				int response=dlg.Run ();
				if (response==0) {
					dlg.Hide ();
					dlg.Destroy ();
					return;
				} else {
					plsname=pls.Text.Trim ();
				}
			}
			dlg.Hide ();
			dlg.Destroy ();
			_pls=_col.NewPlayList(plsname);
			_model.Reload ();
			_pls_model.SetPlayList (_pls);
		}
		
		public void OnRemovePls() {
			Hyena.Log.Information("remove playlist");
		}
		
		public void OnTrackAdd() {
			
		}
	}
}

