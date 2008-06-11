// RadioSource.cs created with MonoDevelop
// User: worldmaker at 5:48 PM 6/10/2008
//
// This source provides just the basics to browse Magnatune genres

using Banshee.Sources;
using Banshee.Sources.Gui;
using Gtk;
using System;

namespace Magnatune
{
	public class RadioSource : Banshee.Sources.Source
	{
		protected override string TypeUniqueId {
			get {  return "Magnatune"; }
		}

		public RadioSource() : base("Magnatune", "Magnatune", 200)
		{ 
			Properties.Set<ISourceContents>("Nereid.SourceContents", new RadioSourceContents());
			Properties.Set<bool>("Nereid.SourceContents.HeaderVisible", false);
		}
	}
}
