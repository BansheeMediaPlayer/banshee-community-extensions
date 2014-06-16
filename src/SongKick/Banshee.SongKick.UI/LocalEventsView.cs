//
// LocalEventsView.cs
//
// Author:
//   dmitrii <>
//
// Copyright (c) 2014 dmitrii
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

using Gtk;

using Banshee.Sources;
using Banshee.Sources.Gui;

using Banshee.SongKick.Recommendations;
using Banshee.SongKick.Search;
using Banshee.SongKick.UI;

namespace Banshee.SongKick.UI 
{
    public class LocalEventsView : SearchBox<Event>, ISourceContents
    {
        private LocalEventsSource source;
        private Results<Event> events;

        public LocalEventsView (LocalEventsSource source, Results<Event> events) 
            : base (new EventsByLocationSearch())
        {
            this.source = source;
            this.events = events;

            UpdateEvents ();
        }

        public void UpdateEvents(Results<Event> events)
        {
            this.events = events;
            UpdateEvents ();
        }

        private void UpdateEvents()
        {
            foreach (var e in events) {
                event_model.Add(e);
            }

            SetModel (event_model);
            event_search_view.OnUpdated ();
        }

        #region ISourceContents

        public bool SetSource (ISource source)
        {
            this.source = source as LocalEventsSource;
            return this.source != null;
        }

        public void ResetSource ()
        {
        }

        public ISource Source { get { return source; } }

        public Widget Widget { get { return this; } }

        #endregion

        protected override void OnRowActivated (object o, Hyena.Data.Gui.RowActivatedArgs<Event> args)
        {
            var musicEvent = args.RowValue;
            System.Diagnostics.Process.Start (musicEvent.Uri);
        }
    }
}

