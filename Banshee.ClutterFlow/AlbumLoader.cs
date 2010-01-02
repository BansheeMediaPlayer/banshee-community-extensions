
using System;
using System.Collections.Generic;

using Banshee.Collection;
using Banshee.Collection.Database;
using Hyena.Data;

using ClutterFlow;

namespace Banshee.ClutterFlow
{
	
	public class BansheeActorLoader<TGen> : ActorLoader<string, TGen> where TGen : ICacheableItem  {
		bool no_reloading = false;
		public bool NoReloading {
			get { return no_reloading; }
			set { no_reloading = value;	}
		}
		
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
		            }
		            RefreshCoverManager ();
				}
			}
		}

		public BansheeActorLoader (CoverManager coverManager) : base (coverManager) { }
		
		#region Event Handlers
		protected void OnSourceUpdatedHandler (object o, EventArgs args)
		{
			Console.WriteLine("OnSourceUpdatedHandler");
		}
		
        protected void OnModelClearedHandler (object o, EventArgs args)
        {
			if (!no_reloading) RefreshCoverManager ();
        }
        
        protected void OnModelReloadedHandler (object o, EventArgs args)
        {
			if (!no_reloading) RefreshCoverManager ();
        }
		#endregion
	}
	
	public class AlbumLoader : BansheeActorLoader<AlbumInfo>
	{

		public AlbumInfo CurrentAlbum {
			get {
				if (coverManager.CurrentCover!=null && coverManager.CurrentCover is ClutterFlowAlbum)
					return (coverManager.CurrentCover as ClutterFlowAlbum).Album;
				else
					return null;
			}
		}
		
		public AlbumLoader (CoverManager coverManager) : base (coverManager) { }
		
		public override List<ClutterFlowActor> GetActors (System.Action<ClutterFlowActor> method_call)
		{
			List<ClutterFlowActor> list = new List<ClutterFlowActor>();
			if (Model!=null) for (int i = 1; i < Model.Count; i++) {
				ClutterFlowActor actor = AddActorToList(Model[i], list);
				if (method_call!=null) method_call(actor);
			}
			return list;
		}

		public virtual void ScrollTo (AlbumInfo generator)
		{
			coverManager.Timeline.Timeout = 500; //give 'm some time to load the song etc.
			ScrollTo (ClutterFlowAlbum.CreateCacheKey (generator));
		}
		
		protected override ClutterFlowActor AddActorToList (AlbumInfo generator, List<ClutterFlowActor> list)
		{
			string key = ClutterFlowAlbum.CreateCacheKey(generator);
			ClutterFlowActor actor = Cache.ContainsKey (key) ? Cache[key] : null;
			if (actor==null) {
				actor = new ClutterFlowAlbum (generator, coverManager);
				coverManager.Add ((Clutter.Actor) actor);
				Cache.Add (key, actor);
			}
			actor.Index = list.Count;
			list.Add(actor);
			return actor;
		}
	}
}
