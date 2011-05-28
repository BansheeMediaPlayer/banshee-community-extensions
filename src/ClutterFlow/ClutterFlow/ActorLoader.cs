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

    public interface IActorLoader : IDisposable {
        CoverManager CoverManager { get; }
        List<ClutterFlowBaseActor> GetActors (System.Action<ClutterFlowBaseActor> method_call);
    }

    public abstract class ActorLoader<TKey, TGen> : IActorLoader {
        #region Fields
        protected Dictionary<TKey, ClutterFlowBaseActor> cached_covers = new Dictionary<TKey, ClutterFlowBaseActor> ();
        public virtual Dictionary<TKey, ClutterFlowBaseActor> Cache {
            get { return cached_covers; }
        }

        private CoverManager coverManager;
        public virtual CoverManager CoverManager {
            get { return coverManager; }
        }
        #endregion

        public ActorLoader (CoverManager coverManager)
        {
            this.coverManager = coverManager;
            CoverManager.ActorActivated += HandleActorActivated;
            CoverManager.ActorLoader = this;
        }
        protected bool disposed = false;
        public virtual void Dispose ()
        {
            if (disposed)
                return;
            disposed = true;

            CoverManager.ActorActivated -= HandleActorActivated;

            foreach (ClutterFlowBaseActor actor in cached_covers.Values)
                actor.Dispose ();
            cached_covers.Clear ();
        }

        protected virtual void RefreshCoverManager ()
        {
            CoverManager.ReloadCovers ();
        }

        public virtual List<ClutterFlowBaseActor> GetActors (System.Action<ClutterFlowBaseActor> method_call)
        {
            throw new System.NotImplementedException();
        }

        protected virtual ClutterFlowBaseActor AddActorToList(TGen generator, SortedList<TGen, ClutterFlowBaseActor> list) {
            throw new System.NotImplementedException();
        }

        public virtual void ScrollTo (TKey key)
        {
            ClutterFlowBaseActor actor = Cache.ContainsKey (key) ? Cache[key] : null;
            if (actor != null && coverManager.covers.Contains (actor)) {
                coverManager.TargetIndex = actor.Index;
            }
        }

        public abstract void HandleActorActivated (ClutterFlowBaseActor actor, EventArgs args);
    }
}
