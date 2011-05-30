//
// AlbumLoader.cs
//
// Author:
//       Mathijs Dumon <mathijsken@hotmail.com>
//
// Copyright (c) 2010 Mathijs Dumon
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
using System.Collections.Generic;

using Banshee.Collection;

using ClutterFlow;

namespace Banshee.ClutterFlow
{
    public class AlbumLoader : IActorLoader
    {
        #region Events
        public event EventHandler SortingChanged;

        protected void OnSortingChanged ()
        {
            var handler = SortingChanged;
            if (handler != null) {
                handler (this, EventArgs.Empty);
            }
        }
        #endregion

        #region Fields
        private Dictionary<string, ClutterFlowBaseActor> cached_covers = new Dictionary<string, ClutterFlowBaseActor> ();
        public Dictionary<string, ClutterFlowBaseActor> Cache {
            get { return cached_covers; }
        }

        protected static ISortComparer<AlbumInfo> sort_by_name = new AlbumAlbumComparer ();
        protected static ISortComparer<AlbumInfo> sort_by_artist = new AlbumArtistComparer ();

        protected ISortComparer<AlbumInfo> Comparer {
            get {
                switch (SortBy) {
                case SortOptions.Album:
                    return sort_by_name;
                case SortOptions.Artist:
                    return sort_by_artist;
                default:
                    return sort_by_name;
                }
            }
        }

        private SortOptions sort_by = (SortOptions) Enum.Parse(typeof(SortOptions), ClutterFlowSchemas.SortBy.Get ());
        public SortOptions SortBy {
            get { return sort_by; }
            set {
                if (value != sort_by) {
                    sort_by = value;
                    ClutterFlowSchemas.SortBy.Set (Enum.GetName(typeof(SortOptions), SortBy));
                    OnSortingChanged ();
                }
            }
        }

        //maps list indices to model indices
        private List<int> index_map;

        private FilterListModel<AlbumInfo> model;
        public FilterListModel<AlbumInfo> Model {
            get { return model; }
            set { model = value; }
        }
        #endregion

        public AlbumLoader ()
        { }

        protected bool disposed = false;
        public void Dispose ()
        {
            if (disposed) {
                return;
            }
            disposed = true;

            foreach (ClutterFlowBaseActor actor in cached_covers.Values) {
                actor.Dispose ();
            }
            cached_covers.Clear ();
        }

        private ClutterFlowBaseActor AddActorToList (AlbumInfo generator, SortedList<AlbumInfo, ClutterFlowBaseActor> list, CoverManager cover_manager)
        {
            if (generator == null) {
                generator = new AlbumInfo (AlbumInfo.UnknownAlbumTitle);
            }
            string key = ClutterFlowAlbum.CreateCacheKey(generator);
            ClutterFlowBaseActor actor = null;
            if (!Cache.TryGetValue (key, out actor)) {
                actor = new ClutterFlowAlbum (generator, cover_manager);
                actor.Hide ();
                Cache.Add (key, actor);
            }
            actor.SortLabel = Comparer.GetSortLabel (generator);
            list[generator] = actor;
            actor.Index = list.Count;
            return actor;
        }

        public List<ClutterFlowBaseActor> GetActors (CoverManager cover_manager)
        {
            SortedList<AlbumInfo, ClutterFlowBaseActor> list =
                new SortedList<AlbumInfo, ClutterFlowBaseActor>(Comparer);
            if (Model != null) {
                for (int i = 1; i < Model.Count; i++) {
                    AddActorToList(Model[i], list, cover_manager);
                }
                index_map = new List<int>(list.Values.Count);
                for (int i = 0; i < list.Values.Count; i++) {
                    ClutterFlowBaseActor actor = list.Values[i];
                    index_map.Add(actor.Index);
                    actor.Index = i;
                }
            }
            return new List<ClutterFlowBaseActor>(list.Values);
        }

        public int ConvertIndexToModelIndex (int index)
        {
            return (index_map != null && index_map.Count > index) ? index_map[index] : 0;
        }
    }
}
