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
    
    public class BansheeActorLoader<TGen> : ActorLoader<string, TGen> where TGen : ICacheableItem, new()  {

        private int count; //previous model count
        
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

        public BansheeActorLoader (CoverManager coverManager) : base (coverManager) { }
        public override void Dispose ()
        {
            Model = null;
            base.Dispose ();
        }
        
        #region Event Handlers        
        protected void OnModelClearedHandler (object o, EventArgs args)
        {
            //Hyena.Log.Information ("OnModelClearedHandler called");
            RefreshCoverManager ();
        }
        
        protected void OnModelReloadedHandler (object o, EventArgs args)
        {
            //Hyena.Log.Information ("OnModelReloadedHandler called");
            if (count!=model.Count) {
                count=model.Count;
                RefreshCoverManager ();
            }
        }
        #endregion
    }
    
    public class AlbumLoader : BansheeActorLoader<AlbumInfo>
    {

        #region Fields    
        public AlbumInfo CurrentAlbum {
            get {
                if (coverManager.CurrentCover!=null && coverManager.CurrentCover is ClutterFlowAlbum)
                    return (coverManager.CurrentCover as ClutterFlowAlbum).Album;
                else
                    return null;
            }
        }
        public int CurrentIndex {
            get {
                if (coverManager.CurrentCover!=null && coverManager.CurrentCover is ClutterFlowAlbum)
                    return (coverManager.CurrentCover as ClutterFlowAlbum).Index;
                else
                    return -1;
            }
        }
        #endregion
        
        public AlbumLoader (CoverManager coverManager) : base (coverManager) 
        {
        }
        
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
                Cache.Add (key, actor);
            }
            actor.Index = list.Count;
            list.Add(actor);
            return actor;
        }
    }
}
