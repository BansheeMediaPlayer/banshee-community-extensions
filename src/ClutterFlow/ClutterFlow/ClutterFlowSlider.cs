//
// ClutterFlowSlider.cs
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

using ClutterFlow;
using ClutterFlow.Alphabet;

namespace ClutterFlow.Slider
{


    public class ClutterFlowSlider : Group
    {
        #region Fields
        private CoverManager coverManager;
        public CoverManager CoverManager {
            get { return coverManager; }
        }

        protected ClutterSlider slider;
        protected AlphabetBar alphabet;

        #endregion

        #region Initialisation
        public ClutterFlowSlider (float width, float height, CoverManager coverManager)
        {
            this.IsReactive = true;
            this.coverManager = coverManager;
            CoverManager.CoversChanged += HandleCoversChanged;
            CoverManager.TargetIndexChanged += HandleTargetIndexChanged;
            CoverManager.LetterLookupChanged += HandleLetterLookupChanged;

            this.SetSize (width, height);
            this.EnterEvent += HandleEnterEvent;
            this.LeaveEvent += HandleLeaveEvent;

            InitChildren ();
            Update ();
        }

        public override void Dispose ()
        {
            CoverManager.CoversChanged -= HandleCoversChanged;
            CoverManager.TargetIndexChanged -= HandleTargetIndexChanged;
            CoverManager.LetterLookupChanged -= HandleLetterLookupChanged;

            EnterEvent += HandleEnterEvent;
            LeaveEvent += HandleLeaveEvent;

            slider.Dispose ();
            alphabet.Dispose ();

            base.Dispose ();
        }

        protected virtual void InitChildren ()
        {
            slider = new ClutterSlider (Width, Height*0.6f);
            slider.SetAnchorPoint (0, 0);
            slider.SetPosition (0, 0);
            slider.SliderHasChanged += HandleSliderHasChanged;
            slider.SliderHasMoved += HandleSliderHasMoved;
            Add (slider);
            slider.Show ();

            alphabet = new AlphabetBar ((uint) (Width-Height*1.5), (uint) (Height*0.4f));
            alphabet.SetAnchorPoint (alphabet.Width*0.5f, alphabet.Height);
            alphabet.SetPosition (Width*0.5f, Height);
            alphabet.LetterClicked += HandleAlphabetLetterClicked;
            Add (alphabet);
            alphabet.Opacity = (byte) 0;
            alphabet.Show ();
        }
        #endregion

        public void Update ()
        {
            if (Stage!=null) {
                SetAnchorPoint(Width*0.5f, Height);
                //Console.WriteLine ("ClutterFlowSlider.Update, Width = " + Width + " coverManager.Behaviour.CenterX = " + coverManager.Behaviour.CenterX);
                SetPosition(coverManager.Behaviour.CenterX, Math.Min(coverManager.Behaviour.CenterY + coverManager.Behaviour.CoverWidth * 1.5f, Stage.Height));
            }
            slider.Update ();
        }


        #region Event Handling
        protected void HandleEnterEvent (object o, EnterEventArgs args)
        {
            alphabet.Animatev ((ulong) AnimationMode.EaseOutExpo.value__, CoverManager.MaxAnimationSpan, new string[] { "opacity" }, new GLib.Value((byte) 255));
            args.RetVal = true;
        }

        protected void HandleLeaveEvent (object o, LeaveEventArgs args)
        {
            alphabet.Animatev ((ulong) AnimationMode.EaseOutExpo.value__, CoverManager.MaxAnimationSpan, new string[] { "opacity" }, new GLib.Value((byte) 0));
            args.RetVal = true;
        }

        protected void HandleCoversChanged (object sender, EventArgs e)
        {
            slider.UpdateBounds (coverManager.TotalCovers, coverManager.TargetIndex);
        }

        bool ignoreTargetIndexOnce = false;
        protected void HandleTargetIndexChanged(object sender, EventArgs e)
        {
            if (coverManager.TargetActor!=null) {
                slider.Label = coverManager.TargetActor.SortLabel.ToUpper ().Substring (0,1);
            } else
                slider.Label = "?";
            if (!ignoreTargetIndexOnce)
                slider.HandlePostionFromIndex = coverManager.TargetIndex;
            ignoreTargetIndexOnce = false;
        }

        protected void HandleSliderHasMoved(object sender, EventArgs e)
        {
            ignoreTargetIndexOnce = true;
            coverManager.TargetIndex = slider.HandlePostionFromIndex;
        }

        protected void HandleSliderHasChanged(object sender, EventArgs e)
        {
            coverManager.TargetIndex = slider.HandlePostionFromIndex;
        }

        protected void HandleAlphabetLetterClicked (object sender, AlphabetEventArgs e)
        {
            ignoreTargetIndexOnce = false;
            /*Console.WriteLine ("HandleAlphabetLetterClicked e.Letter = " + e.Letter);
            Console.WriteLine ("CoverManager.LetterLookup is " + (CoverManager.LetterLookup == null ? "null" : "not null"));
            Console.WriteLine ("CoverManager.LetterLookup.ContainsKey(e.Letter) = " + CoverManager.LetterLookup.ContainsKey(e.Letter));
            Console.WriteLine ("CoverManager.LetterLookup[e.Letter] = " + CoverManager.LetterLookup[e.Letter]);*/
            CoverManager.TargetIndex = CoverManager.LetterLookup[e.Letter];
        }

        protected void HandleLetterLookupChanged (object sender, EventArgs e)
        {
            foreach (AlphabetChars key in Enum.GetValues(typeof(AlphabetChars)))
                alphabet[key].Disabled = CoverManager.LetterLookup[key]==-1;
        }
        #endregion
    }
}
