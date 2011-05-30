// 
// ClutterFlowBaseActor.cs
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

using Clutter;

namespace ClutterFlow
{
    public interface IIndexable : IComparable<IIndexable>
    {
        int Index { get; }
        event IndexChangedEventHandler IndexChanged;
    }

    public delegate void IndexChangedEventHandler (IIndexable item, int old_index, int new_index);

    public abstract class ClutterFlowBaseActor : Clutter.Group, IIndexable
    {
        #region Fields
        private CoverManager cover_manager;
        public CoverManager CoverManager {
            get { return cover_manager; }
            set { cover_manager = value ; }
        }

        protected string cache_key = "";
        public virtual string CacheKey {
            get { return cache_key; }
        }

        protected string label = "";
        public virtual string Label {
            get { return label; }
        }

        protected string sort_label = "";
        public virtual string SortLabel {
            get { return sort_label; }
            set { sort_label = value; }
        }

        public virtual event IndexChangedEventHandler IndexChanged;

        protected int index = -1; //-1 = not visible
        public virtual int Index {
            get { return index; }
            set {
                if (value != index) {
                    int old_index = index;
                    index = value;
                    IsReactive = !(index < 0);
                    if (IndexChanged != null) {
                        IndexChanged (this, old_index, index);
                    }
                }
            }
        }

        public virtual int CompareTo (IIndexable obj) {
            if (obj.Index == -1 && this.Index != -1) {
                return 1;
            }
            return obj.Index - this.Index;
        }
        #endregion

        public ClutterFlowBaseActor () : base ()
        { }

        public ClutterFlowBaseActor (CoverManager cover_manager) : base ()
        {
            this.cover_manager = cover_manager;
            this.cover_manager.Add (this);
        }

        private bool disposed = false;
        public override void Dispose ()
        {
            if (disposed) {
                return;
            }
            disposed = true;

            cover_manager.Remove (this);

            base.Dispose ();
        }

        #region Texture Handling
        public static void MakeReflection (Cairo.ImageSurface source, CairoTexture dest)
        {
            int w = source.Width + 4;
            int h = source.Height * 2 + 4;

            dest.SetSurfaceSize ((uint) w, (uint) h);

            Cairo.Context context = dest.Create ();

            MakeReflection (context, source,  w, h);

            //((IDisposable) context.Target).Dispose ();
            ((IDisposable) context).Dispose ();
        }

        public static Cairo.ImageSurface MakeReflection (Cairo.ImageSurface source)
        {
            int w = source.Width + 4;
            int h = source.Height * 2 + 4;

            Cairo.ImageSurface dest = new Cairo.ImageSurface(Cairo.Format.ARGB32, w, h);
            Cairo.Context context = new Cairo.Context(dest);

            MakeReflection (context, source, w, h);

            //((IDisposable) context.Target).Dispose ();
            ((IDisposable) context).Dispose ();

            return dest;
        }

        private static void MakeReflection (Cairo.Context context, Cairo.ImageSurface source, int w, int h)
        {
            context.ResetClip ();
            context.SetSourceSurface (source, 2, 2);
            context.Paint ();

            double alpha = -0.3;
            double step = 1.0 / (double) source.Height;

            context.Translate (0, h);
            context.Scale (1, -1);
            context.SetSourceSurface (source, 2, 2);
            for (int i = 0; i < source.Height; i++) {
                context.Rectangle (0, i+2, w, 1);
                context.Clip ();
                alpha += step;
                context.PaintWithAlpha (Math.Max (Math.Min (alpha, 0.7), 0.0));
                context.ResetClip ();
            }
        }
        #endregion
    }
}
