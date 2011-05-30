//
// ClutterFlowEmptyPlaceholder.cs
//
// Author:
//       Mathijs Dumon <>
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

using ClutterFlow;

namespace ClutterFlow
{
    public class ClutterFlowFixedActor : ClutterFlowBaseActor
    {
        #region Fields
        private uint cover_width;

        private Clutter.CairoTexture texture;
        public Clutter.CairoTexture Texture {
            get {
                if (texture==null) {
                    texture = new Clutter.CairoTexture (cover_width, cover_width * 2);
                    Add (texture);
                    texture.Show ();
                }
                return texture;
            }
        }

        public override string Label {
            get { return "\nNo Matches Found"; }
        }


        public override string CacheKey {
            get { return "Dummy Actor"; }
        }

        public override string SortLabel {
            get { return "?"; }
            set {
                throw new System.NotImplementedException ("SortLabel cannot be set in a ClutterFlowDummyActor."); //TODO should use reflection here
            }
        }

        public override int Index {
            get { return 0; }
            set {
                //throw new System.NotImplementedException ("Index cannot be set in a ClutterFlowDummyActor.");
            }
        }
        #endregion

        public ClutterFlowFixedActor (uint cover_width) : base ()
        {
            this.cover_width = cover_width;
            IsReactive = false;
        }

        public void SetToPb (Gdk.Pixbuf pb)
        {
            SetAnchorPoint (0, 0);

            if (pb!=null) {
                Cairo.Context context = Texture.Create ();

                Gdk.CairoHelper.SetSourcePixbuf(context, pb, 0, 0);
                context.Paint();

                ((IDisposable) context.Target).Dispose ();
                ((IDisposable) context).Dispose ();
            }

            Texture.SetPosition (0, 0);

            SetAnchorPoint (this.Width*0.5f, this.Height*0.25f);

            ShowAll ();
        }
    }
}
