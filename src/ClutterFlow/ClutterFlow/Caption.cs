//
// Caption.cs
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
using System.Runtime.InteropServices;
using Clutter;

namespace ClutterFlow.Captions
{


    public abstract class Caption : Clutter.Text
    {

        #region Fields
        public abstract string DefaultValue { get; set; }

        private CoverManager coverManager;
        public CoverManager CoverManager {
            get { return coverManager; }
        }

        protected Animation aFade = null;
        #endregion

        public Caption (CoverManager coverManager, string font_name, Color color) : base (clutter_text_new ())
        {
            this.coverManager = coverManager;
            Editable = false;
            Selectable = false;
            Activatable = false;
            CursorVisible = false;
            LineAlignment = Pango.Alignment.Center;
            FontName = font_name;
            SetColor (color);
            Value = DefaultValue;

            UpdatePosition ();
        }

        #region Methods
        [DllImport("libclutter-glx-1.0.so.0")]
        static extern IntPtr clutter_text_new ();

        public virtual void FadeOut ()
        {
            EventHandler hFadeOut = delegate (object sender, EventArgs e) {
                aFade = this.Animatev ((ulong) AnimationMode.Linear.value__, (uint) (CoverManager.MaxAnimationSpan*0.5f), new string[] { "opacity" }, new GLib.Value ((byte) 0));
            };
            if (aFade!=null && aFade.Timeline!=null && aFade.Timeline.IsPlaying)
                aFade.Completed +=  hFadeOut;
            else
                hFadeOut (this, EventArgs.Empty);
        }

        public virtual void FadeIn ()
        {
            EventHandler hFadeIn = delegate (object sender, EventArgs e) {
                aFade = this.Animatev ((ulong) AnimationMode.Linear.value__, (uint) (CoverManager.MaxAnimationSpan*0.5f), new string[] { "opacity" }, new GLib.Value ((byte) 255));
            };
            if (aFade!=null && aFade.Timeline!=null && aFade.Timeline.IsPlaying)
                aFade.Completed +=  hFadeIn;
            else
                hFadeIn (this, EventArgs.Empty);
        }

        public virtual void Update ()
        {
            UpdatePosition ();
        }

        public abstract void UpdatePosition ();
        #endregion
    }
}
