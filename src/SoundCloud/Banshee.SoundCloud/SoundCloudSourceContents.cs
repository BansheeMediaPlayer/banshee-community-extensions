/*
 * SoundCloudSourceContents.cs
 *
 *
 * Copyright 2012 Paul Mackin
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using Mono.Unix;

using Gtk;

using Hyena.Data;
using Hyena.Data.Gui;

using Banshee.Base;
using Banshee.Configuration;

using Banshee.Sources;
using Banshee.Sources.Gui;
using Banshee.ServiceStack;

using Banshee.Collection;
using Banshee.Collection.Database;
using Banshee.Collection.Gui;

namespace Banshee.SoundCloud
{
    public class SoundCloudSourceContents : FilteredListSourceContents, ITrackModelSourceContents
    {
        private TrackListView			track_view;
        private QueryFilterView<string>	artist_view;

        public SoundCloudSourceContents() : base("soundcloud")
        {
        }

        protected override void InitializeViews()
        {
            SetupMainView(track_view = new TrackListView());
            SetupFilterView(artist_view = new QueryFilterView<string>(Catalog.GetString("Artist")));
        }

        protected override void ClearFilterSelections()
        {
            if(artist_view.Model != null) {
                artist_view.Selection.Clear();
            }
        }

        protected override bool ActiveSourceCanHasBrowser {
            get { return true; }
        }

        protected override string ForcePosition {
            get { return "left"; }
        }

        #region Implement ISourceContents

        public override bool SetSource(ISource source)
        {
            DatabaseSource track_source = source as DatabaseSource;
            if(track_source == null) {
                return false;
            }

            base.source = source;

            SetModel(track_view, track_source.TrackModel);

            foreach(IListModel model in track_source.CurrentFilters) {
                IListModel<QueryFilterInfo<string>> artist_model = model as IListModel<QueryFilterInfo<string>>;
                if(artist_model != null) {
                    SetModel(artist_view, artist_model);
                }
            }

            return true;
        }

        public override void ResetSource()
        {
            base.source = null;
            track_view.SetModel(null);
            artist_view.SetModel(null);
        }

        #endregion

        #region ITrackModelSourceContents implementation

        public IListView<TrackInfo> TrackView {
            get { return track_view; }
        }

        #endregion
    }
}
