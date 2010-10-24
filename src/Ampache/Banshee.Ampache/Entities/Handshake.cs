
using System;
using System.Collections.Generic;
namespace Banshee.Ampache
{
	public class Handshake
	{
		public string Server { get; protected set; }
		public string Passphrase { get; protected set; }
		public string User { get; protected set; }
	}
}
