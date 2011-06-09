//
// ClutterFlowAlbum.cs
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

using Cairo;
using Clutter;

using Banshee.Gui;
using Banshee.Collection;

using ClutterFlow;

namespace Banshee.ClutterFlow
{
    /// <summary>
    /// A ClutterFlowAlbum is a subclass of the ClutterFlowActor class, containing
    /// banshee related Album art fetching code.
    /// </summary>
    public class ClutterFlowAlbum : ClutterFlowActor, IEquatable<ClutterFlowAlbum>
    {
        #region Fields
        private static ArtworkLookup artwork_lookup;

        private AlbumInfo album;
        public AlbumInfo Album {
            get { return album; }
            set {
                if (album != value) {
                    album = value;
                    artwork_lookup.Enqueue (this);
                }
            }
        }

        public virtual string PbId {
            get { return Album != null ? Album.ArtworkId : "NOT FOUND"; }
        }

        public override string CacheKey {
            get { return CreateCacheKey (album); }
        }
        public static string CreateCacheKey (AlbumInfo album)
        {
            return album != null ? album.ArtistName + "\n" + album.Title : "";
        }

        public override string Label {
            get { return album != null ? album.ArtistName + "\n" + album.Title : ""; }
        }

        object sync = new object();
        bool enqueued = false;
        public bool Enqueued {
            get { lock (sync) { return enqueued; } }
            internal set { lock (sync) { enqueued = value; } }
        }
        #endregion

        #region Initialization
        public ClutterFlowAlbum (AlbumInfo album, CoverManager coverManager) : base (coverManager)
        {
            this.album = album;
            if (artwork_lookup == null) {
                artwork_lookup = new ArtworkLookup (CoverManager);
            }
            artwork_lookup.Enqueue(this);
        }

        public override void Dispose ()
        {
            if (artwork_lookup != null) {
                artwork_lookup.Dispose ();
            }
            artwork_lookup = null;
            base.Dispose ();
        }
        #endregion

        public virtual bool Equals (ClutterFlowAlbum other)
        {
            return other.CacheKey == this.CacheKey;
        }
    }
}
