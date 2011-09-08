//
// DuplicateSongDetectorSource.cs
//
// Authors:
//   Kevin Anthony <Kevin@NoSideRacing.Com>
//
// Copyright (C) 2011 Kevin Anthony
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;

using Mono.Addins;

using Banshee.Base;
using Banshee.Sources;
using Banshee.ServiceStack;
using Banshee.Gui;

namespace Banshee.DuplicateSongDetector
{
    public class DuplicateSongDetectorSource : Source, IUnmapableSource
    {
        // In the sources TreeView, sets the order value for this source, small on top
        const int sort_order = 190;

        public DuplicateSongDetectorSource () : base(AddinManager.CurrentLocalizer.GetString ("Duplicate Song Detector"), AddinManager.CurrentLocalizer.GetString ("Duplicate Song Detector"), sort_order, "extension-unique-id")
        {
            Properties.SetStringList ("Icon.Name", "search", "gtk-search");
            Properties.Set<Banshee.Sources.Gui.ISourceContents> ("Nereid.SourceContents", new SongDuplicateView ());
            Properties.SetString ("ActiveSourceUIResource", "ActiveUI.xml");
            Properties.SetString ("UnmapSourceActionLabel", AddinManager.CurrentLocalizer.GetString ("Close"));
            Properties.SetString ("UnmapSourceActionIconName", "gtk-close");

            var actions = new BansheeActionGroup ("duplicate-source");
            actions.AddImportant (
                new Gtk.ActionEntry ("onStartDetecting", Gtk.Stock.Refresh, AddinManager.CurrentLocalizer.GetString ("Refresh"), null, null, (o, a) => {
                    SongDuplicateView.ReloadWindow ();
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
            Properties.Get<Banshee.Sources.Gui.ISourceContents> ("Nereid.SourceContents").Widget.Destroy ();
            return true;
        }
    }
}
