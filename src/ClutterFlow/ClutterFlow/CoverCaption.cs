//
// CoverCaption.cs
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


    public class CoverCaption : Caption
    {
        #region Fields
        protected string defaultValue = "Unkown Artist\nUnkown Album";
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

        public CoverCaption (CoverManager coverManager, string font_name, Color color) : base (coverManager, font_name, color)
        {
            CoverManager.NewCurrentCover += HandleNewCurrentCover;
            CoverManager.TargetIndexChanged += HandleTargetIndexChanged;
            CoverManager.CoversChanged += HandleCoversChanged;
        }

        public override void Dispose ()
        {
            CoverManager.NewCurrentCover -= HandleNewCurrentCover;
            CoverManager.TargetIndexChanged -= HandleTargetIndexChanged;
            CoverManager.CoversChanged -= HandleCoversChanged;

            base.Dispose ();
        }

        #region Methods

        public override void Update ()
        {
            SetTextFromCover (CoverManager.CurrentCover);
            base.Update ();
        }

        public override void UpdatePosition ()
        {
            if (Stage!=null) {
                SetAnchorPoint (Width*0.5f, Height*0.5f);
                SetPosition(CoverManager.Behaviour.CenterX, Math.Max(CoverManager.Behaviour.CenterY - CoverManager.Behaviour.CoverWidth, Height*0.6f));
            }
        }

        protected void SetTextFromCover(ClutterFlowBaseActor cover)
        {
            //Console.WriteLine ("SetTextFromCover called");
            if (cover!=null && cover.Label!="")
                Value = cover.Label;
            else
                Value = DefaultValue;
        }
        #endregion

        #region Event Handling
        protected virtual void HandleNewCurrentCover (ClutterFlowBaseActor cover, EventArgs e)
        {
            if (Opacity>0) FadeOut ();
            Update ();
            if (IsVisible) FadeIn ();
        }

        protected virtual void HandleTargetIndexChanged (object sender, EventArgs e)
        {
            if (IsVisible) FadeOut ();
        }

        protected virtual void HandleCoversChanged(object sender, EventArgs e)
        {
            Update ();
            if (IsVisible) FadeIn ();
        }
        #endregion
    }
}
