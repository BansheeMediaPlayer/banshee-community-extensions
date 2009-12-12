
using System;
using System.Collections.Generic;
using Clutter;

namespace ClutterFlow
{

	public interface IActorLoader {
		CoverManager CoverManager { get; set; }
		List<ClutterFlowActor> GetActors (System.Action<ClutterFlowActor> method_call);
	}
	
	public class ActorLoader<TKey> : IActorLoader {
		#region Fields	
		protected Dictionary<TKey, ClutterFlowActor> cached_covers = new Dictionary<TKey, ClutterFlowActor> ();
		public virtual Dictionary<TKey, ClutterFlowActor> Cache {
			get { return cached_covers; }
		}
		
		protected CoverManager coverManager;
		public virtual CoverManager CoverManager { 
			get { return coverManager; }
			set {
				coverManager = value;
				coverManager.ActorLoader = this;
			}
		}
		#endregion
		
		public ActorLoader (CoverManager coverManager) 
		{
			this.CoverManager = coverManager;
		}

		protected virtual void RefreshCoverManager () 
		{
			if (coverManager.Enabled) coverManager.ReloadCovers ();
			else coverManager.NeedsReloading = true;
		}
		
		public virtual List<ClutterFlowActor> GetActors (System.Action<ClutterFlowActor> method_call)
		{
			throw new System.NotImplementedException();
		}
		
		protected virtual ClutterFlowActor AddActorFromKeyToList(TKey key, List<ClutterFlowActor> list) {
			throw new System.NotImplementedException();
		}
	}
}
