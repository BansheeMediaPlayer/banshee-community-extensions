// 
// ActorLoader.cs
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
using Clutter;

namespace ClutterFlow
{

	public interface IActorLoader {
		CoverManager CoverManager { get; set; }
		List<ClutterFlowActor> GetActors (System.Action<ClutterFlowActor> method_call);
	}
	
	public class ActorLoader<TKey, TGen> : IActorLoader {
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
			CoverManager.ReloadCovers ();
			/*if (coverManager.Enabled) coverManager.ReloadCovers ();
			else coverManager.NeedsReloading = true;*/
		}
		
		public virtual List<ClutterFlowActor> GetActors (System.Action<ClutterFlowActor> method_call)
		{
			throw new System.NotImplementedException();
		}
		
		protected virtual ClutterFlowActor AddActorToList(TGen generator, List<ClutterFlowActor> list) {
			throw new System.NotImplementedException();
		}

		public virtual void ScrollTo (TKey key)
		{
			ClutterFlowActor actor = Cache.ContainsKey (key) ? Cache[key] : null;
			if (actor!=null && coverManager.covers.Contains (actor))
				coverManager.TargetIndex = coverManager.covers.IndexOf (actor); //replace covers with somethinf faster?
		}
	}
}
