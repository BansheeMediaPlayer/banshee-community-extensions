// Configuration.cs created with MonoDevelop
// User: worldmaker at 11:08 PM 6/11/2008

using System;

namespace Magnatune
{
	public partial class Configuration : Gtk.Dialog
	{
		public static readonly string[] membershipTypes = {"", "streaming", "download"};	
		
		public Configuration(string type, string user, string pass)
		{
			this.Build();
			username.Text = user;
			password.Text = pass;
			switch (type)
			{
				case "streaming":
					membershipType.Active = 1;
					break;
				case "download":
					membershipType.Active = 2;
					break;
				default:
					membershipType.Active = 0;
					break;
			}
		}

		protected virtual void OnButtonOkPressed (object sender, System.EventArgs e)
		{
			RadioSource.MembershipTypeSchema.Set(membershipTypes[membershipType.Active]);
			RadioSource.UsernameSchema.Set(username.Text);
			RadioSource.PasswordSchema.Set(password.Text);
		}

		protected virtual void OnButtonCancelPressed (object sender, System.EventArgs e)
		{
			this.Hide();
		}
	}
}
