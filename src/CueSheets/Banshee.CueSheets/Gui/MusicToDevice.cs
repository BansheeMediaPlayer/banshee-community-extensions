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
		
		private void CreateGui() {
			Gtk.Button btn=new Gtk.Button("Split!");
			Gtk.ProgressBar bar=new Gtk.ProgressBar();
			Gtk.ProgressBar nr=new Gtk.ProgressBar();
			Gtk.Button ok=(Gtk.Button) base.AddButton ("OK",1);
			btn.Clicked+=delegate(object sender,EventArgs args) {
				ok.Sensitive=false;				
				_splt.SplitWithPaths ();
				_splt.SplitToDir ("/tmp");
				GLib.Timeout.Add(10,delegate () {
					bar.Fraction=_splt.ProgressOfCurrentTrack;
					int n=_splt.ProgressNTracks;
					int i=_splt.ProgressCurrentTrack;
					double d=((double) i)/((double) n);
					nr.Fraction=d;
					if (_splt.SplitFinished) { ok.Sensitive=true; }
					return !_splt.SplitFinished;
				});
			};
			base.VBox.Add (nr);
			base.VBox.Add (bar);
			base.VBox.Add (btn);
			base.VBox.ShowAll ();
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

