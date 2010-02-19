
using System;

namespace Banshee.Lirc
{
	public partial class ConfigDialog : Gtk.Dialog
	{
		protected virtual void OnCancel (object sender, System.EventArgs e)
		{
		}
		
		public ConfigDialog()
		{
			this.Build();
		}

		protected virtual void OnOk (object sender, System.EventArgs e)
		{
		}
	}
}
