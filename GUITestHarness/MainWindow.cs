using System;
using Gtk;
using Banshee.Lirc;

public partial class MainWindow: Gtk.Window
{	
	public MainWindow (): base (Gtk.WindowType.Toplevel)
	{
		Build ();
	}
	
	protected void OnDeleteEvent (object sender, DeleteEventArgs a)
	{
		Application.Quit ();
		a.RetVal = true;
	}

	protected virtual void OnDialog (object sender, System.EventArgs e)
	{
		ConfigDialog dlg = new ConfigDialog();
		dlg.Run();
		dlg.Destroy();
	}
}