// project created on 12/12/2007 at 12:29 AM
using System;
using Gtk;

namespace OpenVP.GtkGui {
	public class MainClass {
		public static void Main(string[] args) {
			Application.Init();
			MainWindow win = new MainWindow();
			win.Show();
			Application.Run();
		}
	}
}