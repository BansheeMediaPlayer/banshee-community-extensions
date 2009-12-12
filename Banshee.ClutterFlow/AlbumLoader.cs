
using System;
using System.Collections.Generic;

using Banshee.Collection;
using Hyena.Data;

using ClutterFlow;

namespace Banshee.ClutterFlow
{

	public class BansheeActorLoader<T> : ActorLoader<T>  {
		private IListModel<T> model;
		public virtual IListModel<T> Model {
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
        protected void OnModelClearedHandler (object o, EventArgs args)
        {
			RefreshCoverManager ();
        }
        
        protected void OnModelReloadedHandler (object o, EventArgs args)
        {
			RefreshCoverManager ();
        }
		#endregion
	}
	
	public class AlbumLoader : BansheeActorLoader<AlbumInfo>
	{

		public AlbumInfo CurrentAlbum {
			get {
				if (coverManager.CurrentCover!=null && coverManager.CurrentCover is ClutterFlowAlbum)
					return (coverManager.CurrentCover as ClutterFlowAlbum).Key;
				else
					return null;
			}
		}
		
		public AlbumLoader (CoverManager coverManager) : base (coverManager) { }
		
		public override List<ClutterFlowActor> GetActors (System.Action<ClutterFlowActor> method_call)
		{
			List<ClutterFlowActor> list = new List<ClutterFlowActor>();
			for (int i = 1; i < Model.Count; i++) {
				ClutterFlowActor actor = AddActorFromKeyToList(Model[i], list);
				if (method_call!=null) method_call(actor);
			}
			return list;
		}
	
		protected override ClutterFlowActor AddActorFromKeyToList (AlbumInfo album, List<ClutterFlowActor> list)
		{
			ClutterFlowActor actor = Cache.ContainsKey (album) ? Cache[album] : null;
			if (actor==null) {
				actor = new ClutterFlowAlbum (album, coverManager);
				coverManager.Add ((Clutter.Actor) actor);
				Cache.Add (album, actor);
			}
			actor.Index = list.Count; //placing this before the add saves us one subtraction
			list.Add(actor);
			return actor;
		}
	}
}
