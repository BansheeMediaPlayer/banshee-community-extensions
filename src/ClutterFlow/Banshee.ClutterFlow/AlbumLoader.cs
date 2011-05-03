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
using Banshee.Collection.Database;
using Hyena.Data;

using ClutterFlow;

namespace Banshee.ClutterFlow
{

	public interface ISortComparer<T> : IComparer<T>
	{
		string GetSortLabel (T obj);
	}
	
	public abstract class AlbumComparer : ISortComparer<AlbumInfo>
	{
		protected virtual string GetTitle (AlbumInfo obj)
		{
			return (obj.TitleSort ?? obj.Title ?? obj.DisplayTitle ?? "");
		}
		
		protected virtual string GetArtist (AlbumInfo obj)
		{
			return (obj.ArtistNameSort ?? obj.ArtistName ?? obj.DisplayArtistName ?? "");
		}
		
		public abstract int Compare (AlbumInfo x, AlbumInfo y);
		public abstract string GetSortLabel (AlbumInfo obj);
	}
	
	public class AlbumAlbumComparer : AlbumComparer
	{
		
		public override string GetSortLabel (AlbumInfo obj)
		{
			if (obj!=null) {
				return obj.TitleSort ?? obj.Title ?? "?";
			} else
				return "?";
		}
		
		public override int Compare (AlbumInfo x, AlbumInfo y)
		{
			string tx = GetTitle(x) + GetArtist(x);
			string ty = GetTitle(y) + GetArtist(y);
			return tx.CompareTo(ty);
		}
	}

	public class AlbumArtistComparer : AlbumComparer
	{
		public override string GetSortLabel (AlbumInfo obj)
		{
			if (obj!=null) {
				return obj.ArtistNameSort ?? obj.ArtistName ?? "?";
			} else
				return "?";
		}			
		
		public override int Compare (AlbumInfo x, AlbumInfo y)
		{
			string tx = GetArtist(x) + GetTitle(x);
			string ty = GetArtist(y) + GetTitle(y);
			return tx.CompareTo(ty);
		}
	}
	
	public enum SortOptions { Artist = 0 , Album = 1 }
	
    public abstract class BansheeActorLoader<TGen> : ActorLoader<string, TGen> where TGen : ICacheableItem, new()
	{
		
		#region Fields
		#pragma warning disable 0067
		public event EventHandler SortingChanged;
		#pragma warning restore 0067
		protected void InvokeSortingChanged ()
		{
			ClutterFlowSchemas.SortBy.Set (Enum.GetName(typeof(SortOptions), SortBy));
			if (SortingChanged!=null) SortingChanged (this, EventArgs.Empty);
		}

		protected SortOptions sort_by = (SortOptions) Enum.Parse(typeof(SortOptions), ClutterFlowSchemas.SortBy.Get ());
		public virtual SortOptions SortBy {
			get { return sort_by; }
			set {
				if (value!=sort_by) {
					sort_by = value;
					RefreshCoverManager ();
					InvokeSortingChanged ();
				}
			}
		}
		
		protected abstract ISortComparer<TGen> Comparer { get; }
		
        private int count; //previous model count
		
		protected List<int> index_map; //maps list indeces to model indeces

        private FilterListModel<TGen> model;
        public virtual FilterListModel<TGen> Model {
            get { return model; }
            set {
                if (value!=model) {
                    if (model != null) {
                        model.Cleared -= OnModelClearedHandler;
                        model.Reloaded -= OnModelReloadedHandler;
                    }

                    model = value;

                    if (model != null) {
                        model.Cleared += OnModelClearedHandler;
                        model.Reloaded += OnModelReloadedHandler;
                        count = model.Count;
                    }
                    RefreshCoverManager ();
                }
            }
        }
		#endregion
		
        public BansheeActorLoader (CoverManager coverManager) : base (coverManager) { }
        public override void Dispose ()
        {
            Model = null;
            base.Dispose ();
        }
		
        public override List<ClutterFlowBaseActor> GetActors (System.Action<ClutterFlowBaseActor> method_call)
        {
            SortedList<TGen, ClutterFlowBaseActor> list =
				new SortedList<TGen, ClutterFlowBaseActor>(Comparer);
            if (Model!=null) {
				for (int i = 1; i < Model.Count; i++)
                	AddActorToList(Model[i], list);
				index_map = new List<int>(list.Values.Count);
				for (int i = 0; i < list.Values.Count; i++) {
					ClutterFlowBaseActor actor = list.Values[i];
					index_map.Add(actor.Index);
	                actor.Index = i;
					if (method_call!=null) method_call(actor);
				}
			}
            return new List<ClutterFlowBaseActor>(list.Values);
        }
		
		public virtual int ConvertIndexToModelIndex (int index)
		{
			return (index_map!=null && index_map.Count > index) ? index_map[index] : 0;
		}

        #region Event Handlers
        protected void OnModelClearedHandler (object o, EventArgs args)
        {
            RefreshCoverManager ();
        }

        protected void OnModelReloadedHandler (object o, EventArgs args)
        {
            if (count!=model.Count) {
                count=model.Count;
                RefreshCoverManager ();
            }
        }
        #endregion
    }

    public class AlbumLoader : BansheeActorLoader<AlbumInfo>
    {

		#region Events
		public event ActorEventHandler<ClutterFlowAlbum> ActorActivated;
		protected void InvokeActorActivated (ClutterFlowAlbum actor) {
			if (ActorActivated!=null) ActorActivated (actor, EventArgs.Empty);
		}
		#endregion
		
        #region Fields
		protected static ISortComparer<AlbumInfo> sort_by_name = new AlbumAlbumComparer ();
		protected static ISortComparer<AlbumInfo> sort_by_arst = new AlbumArtistComparer ();
		
		protected override ISortComparer<AlbumInfo> Comparer {
			get {
				switch (SortBy) {
				case SortOptions.Album:
					return sort_by_name;
				case SortOptions.Artist:
					return sort_by_arst;
				default:
					return sort_by_name;
				}
			}
		}
		
		public ClutterFlowAlbum CurrentActor {
			get { return (ClutterFlowAlbum) CoverManager.CurrentCover; }
		}
		
        public AlbumInfo CurrentAlbum {
            get {
                if (CoverManager.CurrentCover!=null && CoverManager.CurrentCover is ClutterFlowAlbum)
                    return (CoverManager.CurrentCover as ClutterFlowAlbum).Album;
                else
                    return null;
            }
        }
        public int CurrentIndex {
            get {
                if (CoverManager.CurrentCover!=null && CoverManager.CurrentCover is ClutterFlowAlbum)
                    return (CoverManager.CurrentCover as ClutterFlowAlbum).Index;
                else
                    return -1;
            }
        }
        #endregion

        public AlbumLoader (CoverManager coverManager) : base (coverManager)
        {
        }

        public virtual void ScrollTo (AlbumInfo generator)
        {
            CoverManager.Timeline.Timeout = 500; //give 'm some time to load the song etc.
            ScrollTo (ClutterFlowAlbum.CreateCacheKey (generator));
        }
		
        protected override ClutterFlowBaseActor AddActorToList (AlbumInfo generator, SortedList<AlbumInfo, ClutterFlowBaseActor> list)
        {
            if (generator == null)
                generator = new AlbumInfo (AlbumInfo.UnknownAlbumTitle);
            string key = ClutterFlowAlbum.CreateCacheKey(generator);
            ClutterFlowBaseActor actor = Cache.ContainsKey (key) ? Cache[key] : null;
            if (actor==null) {
                actor = new ClutterFlowAlbum (generator, CoverManager);
				actor.Hide ();
                Cache.Add (key, actor);
            }
			actor.SortLabel = Comparer.GetSortLabel(generator);
            list[generator] = actor;
			actor.Index = list.Count;
            return actor;
        }
		
		public override void HandleActorActivated (ClutterFlowBaseActor actor, EventArgs args)
		{
			if (actor is ClutterFlowAlbum)
				InvokeActorActivated (actor as ClutterFlowAlbum);
		}
    }
}
