//
// LocalEventsSource.cs
//
// Author:
//   dimart.sp@gmail.com <Dmitrii Petukhov>
//
// Copyright (c) 2014 Dmitrii Petukhov
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

using Mono.Addins;
using Mono.Unix;

using Banshee.Sources;
using Banshee.Sources.Gui;
using Banshee.ServiceStack;

using Banshee.SongKick.Recommendations;

namespace Banshee.SongKick.UI
{
    public class LocalEventsSource : Source, IUnmapableSource
    {
        const int sort_order = 195;

        public Results<Event> Events { get; private set; }
        public LocalEventsView view;

        public LocalEventsSource (Results<Event> events) : 
                                            base (AddinManager.CurrentLocalizer.GetString ("City Concerts"),
                                                  AddinManager.CurrentLocalizer.GetString ("City Concerts"),
                                                  sort_order,
                                                  "SongKick-city-concerts")
        {
            Events = events;

            Properties.SetStringList ("Icon.Name", "songkick_logo");
            Properties.SetString ("UnmapSourceActionLabel", Catalog.GetString ("Close Item"));
            Properties.SetString ("UnmapSourceActionIconName", "gtk-close");

            view = new LocalEventsView (this, events);
            Properties.Set<ISourceContents> ("Nereid.SourceContents", view);

            Hyena.Log.Information ("SongKick CityConcerts source has been instantiated!");
        }

        #region IUnmapableSource

        public bool CanUnmap { get { return true; } }
        public bool ConfirmBeforeUnmap { get { return false; } }

        public bool Unmap ()
        {
            if (this == ServiceManager.SourceManager.ActiveSource) {
                ServiceManager.SourceManager.SetActiveSource (Parent);
            }

            Parent.RemoveChildSource (this);
            return true;
        }

        #endregion
    }
}

