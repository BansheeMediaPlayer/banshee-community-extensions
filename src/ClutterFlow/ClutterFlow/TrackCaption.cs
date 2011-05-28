//
// TrackCaption.cs
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

namespace ClutterFlow.Captions
{
    public class TrackCaption : Caption
    {
        #region Fields
        protected string defaultValue = "";
        public override string DefaultValue {
            get { return defaultValue; }
            set {
                if (value!=defaultValue) {
                    if (Value==defaultValue) Value = value;
                    defaultValue = value;
                }
            }
        }
        #endregion

        public TrackCaption (CoverManager coverManager, string font_name, Color color) : base (coverManager, font_name, color)
        {
        }

        #region Methods

        public override void FadeIn ()
        {
            EventHandler hFadeIn = delegate (object sender, EventArgs e) {
                this.Value = new_caption;
                this.UpdatePosition ();
                this.Animatev ((ulong) AnimationMode.Linear.value__, (uint) (CoverManager.MaxAnimationSpan*0.5f), new string[] { "opacity" }, new GLib.Value ((byte) 255));
                aFade = null;
            };
            if (aFade!=null && aFade.Timeline.IsPlaying) {
                aFade.Completed +=  hFadeIn;
            } else {
                hFadeIn (this, EventArgs.Empty);
            }
        }


        public override void UpdatePosition ()
        {
            if (Stage!=null) {
                SetAnchorPoint (Width*0.5f, Height*0.5f);
                SetPosition(CoverManager.Behaviour.CenterX, Math.Max(CoverManager.Behaviour.CenterY - CoverManager.Behaviour.CoverWidth + Height*3, Height*3.6f));
            }
        }

        private string new_caption;
        public void SetValueWithAnim (string caption) {
            new_caption = caption;
            if (Opacity>0) {
                FadeOut ();
            }
            FadeIn ();
        }
        #endregion
    }
}
