using System;

namespace Banshee.CueSheets
{
	public class MusicToDevice : Gtk.Dialog
	{
		private CueSheet _sheet;
		private Mp3Split _splt;
		
		public MusicToDevice (CueSheet s) {
			_sheet=s;
		}
		
		private Gtk.HSeparator hsep() {
			Gtk.HSeparator h=new Gtk.HSeparator();
			h.Show ();
			return h;
		}
		
		private void CreateGui() {
			Gtk.FileChooserButton fc=new Gtk.FileChooserButton("Choose directory to put your splitted files",Gtk.FileChooserAction.SelectFolder);
			string fn=Banshee.Configuration.ConfigurationClient.Get<string>("cuesheets_todevice","");
			if (fn!="") { fc.SelectFilename(fn); }
			fc.FileSet+=new EventHandler(delegate(Object sender,EventArgs args) {
				fn=fc.Filename;
				Banshee.Configuration.ConfigurationClient.Set<string>("cuesheets_todevice",fn);
			});
			Gtk.Button btn=new Gtk.Button("Split CueSheet Audio File");
			Gtk.Button csplit=new Gtk.Button("Cancel");
			csplit.Clicked+=delegate(object sender,EventArgs args) {
				_splt.CancelSplit();
			};
			Gtk.ProgressBar bar=new Gtk.ProgressBar();
			Gtk.ProgressBar nr=new Gtk.ProgressBar();
			Gtk.Button ok=(Gtk.Button) base.AddButton ("OK",1);
			Gtk.Label result=new Gtk.Label("-");
			
			btn.Clicked+=delegate(object sender,EventArgs args) {
				btn.Hide ();
				csplit.Show ();
				result.Markup="";
				
				ok.Sensitive=false;				
				fc.Sensitive=false;
				btn.Sensitive=false;
				
				_splt.SplitWithPaths ();
				
				bool convert_to_latin1=true; 
				_splt.SplitToDir (fn,convert_to_latin1);
				
				GLib.Timeout.Add(50,delegate () {
					bar.Fraction=_splt.ProgressOfCurrentTrack;
					int n=_splt.ProgressNTracks;
					int i=_splt.ProgressCurrentTrack;
					double d=((double) i)/((double) n);
					nr.Fraction=d;
					if (_splt.SplitFinished) { 
						ok.Sensitive=true; 
						btn.Sensitive=true;
						fc.Sensitive=true;
						csplit.Hide ();
						btn.Show ();
						if (_splt.Cancelled) {
							result.Markup="<b>Split Cancelled</b>";
						} else {
							result.Markup="<b>Finished</b>";
						}
					}
					return !_splt.SplitFinished;
				});
				
			};
			fc.Show ();
			nr.Show ();
			bar.Show ();
			btn.Show ();
			result.Show ();
			base.VBox.Add (fc);
			base.VBox.Add (hsep());
			base.VBox.Add (nr);
			base.VBox.Add (bar);
			base.VBox.Add (hsep ());
			base.VBox.Add (result);
			base.VBox.Add (hsep ());
			base.VBox.Add (btn);
			base.VBox.Add (csplit);
			
			base.VBox.Show();
		}
		
		public void Do() {
			_splt=new Mp3Split(_sheet);
			Hyena.Log.Information ("splt="+_splt.ToString ()+" sheet="+_sheet);
			CreateGui ();
			base.Run ();
			base.Hide ();
			base.Destroy ();
		}
	}
}

