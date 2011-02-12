//
// ContactContainerSource.cs
//
// Author:
//   Neil Loknath <neil.loknath@gmail.com>
//
// Copyright (C) 2009 Neil Loknath
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

using Banshee.Configuration;
using Banshee.Sources;
using Banshee.ServiceStack;
using Banshee.Sources.Gui;
using Banshee.Telepathy.Gui;

namespace Banshee.Telepathy.Data
{
    public class SchemaChangedEventArgs : EventArgs
    {
        public SchemaChangedEventArgs (bool allowed)
        {
            this.allowed = allowed;
        }

        private bool allowed;
        public bool Allowed {
            get { return allowed; }
        }
    }

    public class ContactContainerSource : Source, IDisposable
    {
        public static event EventHandler<SchemaChangedEventArgs> DownloadingAllowedChanged;
        public static event EventHandler<SchemaChangedEventArgs> StreamingAllowedChanged;

        public ContactContainerSource (TelepathyService service) : base (AddinManager.CurrentLocalizer.GetString ("Contacts"), "Contacts", 1000, "telepathy-container")
        {
            TelepathyService = service;
            TypeUniqueId = "telepathy-container";

            Properties.SetString ("Icon.Name", "stock_people");
            Properties.Set<ISourceContents> ("Nereid.SourceContents", new ContactSourceContents (this));
            Properties.Set<bool> ("Nereid.SourceContents.HeaderVisible", false);
            Properties.SetString ("GtkActionPath", "/ContactSourceContainerPopup");

            actions = new TelepathyActions (this);
        }

        public override SourceSortType DefaultChildSort {
            get { return SortNameAscending; }
        }

        private TelepathyActions actions;
        public TelepathyActions Actions {
            get { return actions; }
        }

        private TelepathyService service;
        public TelepathyService TelepathyService {
            get { return service; }
            protected set {
                if (value == null) {
                    throw new ArgumentNullException ("service");
                }
                service = value;
            }
        }

        public void Dispose ()
        {
            Dispose (true);
        }

        protected virtual void Dispose (bool disposing)
        {
            if (disposing) {
                actions.Dispose ();
            }
        }

        public void UpdateDownloadingAllowed (bool allowed)
        {
            AllowDownloadsSchema.Set (allowed);
            OnDownloadingAllowedChanged (new SchemaChangedEventArgs (allowed));
        }

        public void UpdateStreamingAllowed (bool allowed)
        {
            AllowStreamingSchema.Set (allowed);
            OnStreamingAllowedChanged (new SchemaChangedEventArgs (allowed));
        }

        public static readonly SchemaEntry <bool> ShareCurrentlyPlayingSchema = new SchemaEntry <bool> (
            "plugins.telepathy-container", "share_currently_playing",
            false,
            "Share Currently Playing",
            "Set Empathy presence message to what you're currently playing"
        );

        public static readonly SchemaEntry <bool> AllowDownloadsSchema = new SchemaEntry <bool> (
            "plugins.telepathy-container", "allow_downloads",
            false,
            "Allow Downloads",
            "Allow downloads when sharing libraries"
        );

        public static readonly SchemaEntry <bool> AllowStreamingSchema = new SchemaEntry <bool> (
            "plugins.telepathy-container", "allow_streaming",
            false,
            "Allow Streaming",
            "Allow streaming when sharing libraries"
        );

        private void OnDownloadingAllowedChanged (SchemaChangedEventArgs args)
        {
            var handler = DownloadingAllowedChanged;
            if (handler != null) {
                handler (this, args);
            }
        }

        private void OnStreamingAllowedChanged (SchemaChangedEventArgs args)
        {
            var handler = StreamingAllowedChanged;
            if (handler != null) {
                handler (this, args);
            }
        }
    }
}
