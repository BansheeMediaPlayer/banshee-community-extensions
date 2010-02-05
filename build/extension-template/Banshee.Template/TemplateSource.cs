//
// EXTENSION-NAMESource.cs
//
// Authors:
//   Cool Extension Author
//
// Copyright (C) 2010 Cool Extension Author
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
using System.Collections.Generic;
using System.Linq;

using Mono.Unix;

using Hyena;

using Banshee.Base;
using Banshee.Sources;

// Other namespaces you might want:
using Banshee.ServiceStack;
using Banshee.Preferences;
using Banshee.MediaEngine;
using Banshee.PlaybackController;

namespace Banshee.EXTENSION-NAME
{
    // We are inheriting from Source, the top-level, most generic type of Source.
    // Other types include (inheritance indicated by indentation):
    //      DatabaseSource - generic, DB-backed Track source; used by PlaylistSource
    //        PrimarySource - 'owns' tracks, used by DaapSource, DapSource
    //          LibrarySource - used by Music, Video, Podcasts, and Audiobooks
    public class EXTENSION-NAMESource : Source
    {
        // In the sources TreeView, sets the order value for this source, small on top
        const int sort_order = 190;

        // For a playlist, its specific name is given by the user,
        // and its generic_name is "Playlist"
        const string specific_name = Catalog.GetString ("EXTENSION-NAME");
        const string generic_name = specific_name;

        public EXTENSION-NAMESource () : base (generic_name, specific_name, source-order)
        {
            Properties.Set<Widget> ("Nereid.SourceContents",
                new Label ("Excellent, this custom view for the EXTENSION-NAME extension is working!"));
        }

        // A count of 0 will be hidden in the source TreeView
        public override int Count {
            get { return 0; }
        }
    }
}
