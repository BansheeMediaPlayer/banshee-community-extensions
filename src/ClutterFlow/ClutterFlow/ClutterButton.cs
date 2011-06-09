//
// ClutterButton.cs
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

namespace ClutterFlow.Buttons
{

    public abstract class ClutterButtonState : Group {

        #region Fields
        protected bool bubble = false;
        public virtual bool BubbleEvents {
            get { return bubble; }
            set { bubble = value; }
        }

        protected int state = 0;
        protected abstract int MaxState { get; }
        //// <value>
        /// State represents the toggle buttons state, the bits represent:
        ///     1: mouse_over
        ///     2: mouse_down
        /// Overriding classes might have more bits,, check MaxBits
        /// </value>
        public virtual int State {
            get { return state; }
            set {
                value &= MaxState; //block any other bits
                if (value!=state) {
                    state = value;
                    Update();
                }
            }
        }
        #endregion

        #region Methods
        protected virtual void Initialise () {
            IsReactive = true;
            ButtonPressEvent += HandleButtonPressEvent;
            ButtonReleaseEvent += HandleButtonReleaseEvent;
            EnterEvent += HandleEnterEvent;
            LeaveEvent += HandleLeaveEvent;
        }

        public abstract void Update();
        #endregion

        #region Event Handling
        protected virtual void HandleEnterEvent(object o, EnterEventArgs args)
        {
            State |= 1;
            args.RetVal = !BubbleEvents;
        }

        protected virtual void HandleButtonPressEvent(object o, ButtonPressEventArgs args)
        {
            State |= 2;
            args.RetVal = !BubbleEvents;
        }

        protected virtual void HandleButtonReleaseEvent(object o, ButtonReleaseEventArgs args)
        {
            State &= ~2;
            args.RetVal = !BubbleEvents;
        }

        protected virtual void HandleLeaveEvent(object o, LeaveEventArgs args)
        {
            State &= ~1;
            args.RetVal = !BubbleEvents;
        }
        #endregion
    }

    public class ClutterButton : ClutterButtonState
    {

        #region Fields
        protected override int MaxState {
            get { return 3; }
        }

        protected CairoTexture[] textures;
        protected virtual int GetTextureIndex(int with_state) {
            return ((with_state == 3) ? 2 : with_state);
        }
        public virtual CairoTexture StateTexture {
            get { return textures[GetTextureIndex(State)]; }
        }
        #endregion

        #region Initialization
        protected ClutterButton (uint width, uint height, int state, bool init) : base ()
        {
            this.State = state;
            this.SetSize (width, height);

            if (init) Initialise ();
        }

        public ClutterButton (uint width, uint height, int state) : this (width, height, state, true)
        {
        }

        protected override void Initialise () {
            CreateTextures ();
            base.Initialise ();
        }

        protected virtual void CreateTextures () {
            if (textures == null || textures.Length == 0) {
                InitTextures ();
            }
            for (int i=0; i < textures.Length; i++) {
                if (textures[i] != null) {
                    GC.SuppressFinalize (textures[i]);
                    if (textures[i].Parent != null) {
                        ((Container) textures[i].Parent).Remove(textures[i]);
                    }
                }
                textures[i] = new Clutter.CairoTexture((uint) Width,(uint) Height);
                Add (textures[i]);
                CreateTexture (textures[i], (byte) i);
            }
        }

        protected virtual void InitTextures () {
            textures = new CairoTexture[3];
        }
        #endregion

        #region Rendering
        protected virtual void CreateTexture (CairoTexture texture, int with_state) {
            throw new System.NotImplementedException ();
        }
        #endregion

        public override void Update() {
            HideAll ();
            StateTexture.Show ();
            Show ();
        }

    }
}
