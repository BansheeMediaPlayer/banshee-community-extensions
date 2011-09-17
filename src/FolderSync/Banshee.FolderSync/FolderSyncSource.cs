// 
// Copyright (c) 2011 Timo Dörr <timo.doerr@latecrew.de>
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Linq;

using Mono.Addins;

using Banshee.Base;
using Banshee.Sources;
using Banshee.Sources.Gui;

// Other namespaces you might want:
using Banshee.ServiceStack;
using Banshee.Preferences;
using Banshee.MediaEngine;
using Banshee.PlaybackController;
using Hyena.Data.Sqlite;
using Banshee.Gui;
using System.Collections.Generic;

using Banshee.IO;
using System.IO;
using Hyena;
using System.Collections;

namespace Banshee.FolderSync
{
	// We are inheriting from Source, the top-level, most generic type of Source.
	// Other types include (inheritance indicated by indentation):
	//      DatabaseSource - generic, DB-backed Track source; used by PlaylistSource
	//        PrimarySource - 'owns' tracks, used by DaapSource, DapSource
	//          LibrarySource - used by Music, Video, Podcasts, and Audiobooks
	public class FolderSyncSource : Source, IUnmapableSource
	{
		// In the sources TreeView, sets the order value for this source, small on top
		const int sort_order = 190;
		public FolderSyncController Controller = new FolderSyncController ();

		public FolderSyncSource () : base (AddinManager.CurrentLocalizer.GetString ("FolderSync"),
                                               AddinManager.CurrentLocalizer.GetString ("FolderSync"),
		                                       sort_order,
		                                       "extension-unique-id")
		{
			Properties.SetStringList ("Icon.Name", "refresh", "gtk-refresh");
			Properties.Set<ISourceContents> ("Nereid.SourceContents", Controller);
			Properties.SetString ("ActiveSourceUIResource", "ActiveUI.xml");
			Properties.SetString ("UnmapSourceActionLabel", AddinManager.CurrentLocalizer.GetString ("Close"));
			Properties.SetString ("UnmapSourceActionIconName", "gtk-close");
			var actions = new BansheeActionGroup ("directory-sync");
			actions.AddImportant (
                new Gtk.ActionEntry ("StopSyncing", Gtk.Stock.Stop, AddinManager.CurrentLocalizer.GetString ("Stop"), null, null,
				(o, a) => {
				if (Controller != null)
					Controller.StopSync ();
			})
            );
			actions.Register ();
			

		}

		// A count of 0 will be hidden in the source TreeView
		public override int Count {
			get { return 0; }
		}
		//This Allows us to close the window when not in use
		public bool CanUnmap {
			get { return true; }
		}

		public bool ConfirmBeforeUnmap {
			get { return false; }
		}

		public bool Unmap ()
		{
			Parent.RemoveChildSource (this);
			Controller.StopSync ();
			Properties.Get<Banshee.Sources.Gui.ISourceContents> ("Nereid.SourceContents").Widget.Destroy ();
			Controller = null;
			return true;
		}
	}
}