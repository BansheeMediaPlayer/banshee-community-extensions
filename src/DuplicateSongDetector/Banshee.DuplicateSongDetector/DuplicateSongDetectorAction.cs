//
// DuplicateSongDetectorAction.cs
//
// Authors:
//   Kevin Anthony <Kevin@NoSideRacing.com>
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

using System;

using Mono.Addins;
using Mono.Unix;

using Banshee.Base;
using Banshee.Gui;
using Banshee.ServiceStack;

using Hyena;
using Hyena.Data.Sqlite;

namespace Banshee.DuplicateSongDetector
{
    public class DuplicateSongDetectorAction : BansheeActionGroup
    {
        private DuplicateSongDetectorSource source;

        public DuplicateSongDetectorAction () : base (AddinManager.CurrentLocalizer.GetString ("Detect Duplicate Songs"))
        {
            Add (new Gtk.ActionEntry ("DuplicateSongAction", null,
            AddinManager.CurrentLocalizer.GetString ("Detect Duplicate Songs"), null, null, onStartDetecting));
            AddUiFromFile ("GlobalUI.xml");
        }

        public void onStartDetecting (object o, EventArgs args)
        {
            if (source == null) {
                source = new DuplicateSongDetectorSource ();
                ServiceManager.SourceManager.MusicLibrary.AddChildSource (source);
            }
            ServiceManager.SourceManager.SetActiveSource (source);
            SongDuplicateView.ReloadWindow ();

            source.Parent.ChildSourceRemoved += delegate(Sources.SourceEventArgs source_args) {
                if (source_args.Source.Equals(source)){
                    source=null;
                }
            };
        }
    }
}

