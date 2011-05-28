//
// ClutterToggleButton.cs
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


namespace ClutterFlow.Buttons
{


    public abstract class ClutterToggleButton : ClutterButtonState
    {

        public event EventHandler Toggled;
        protected void InvokeToggled () {
            if (Toggled!=null) Toggled (this, EventArgs.Empty);
        }

        //// <value>
        /// MaxBits represents the toggle buttons maximal bit-states to be set:
        ///     1: mouse_over
        ///     2: mouse_down
        ///     4: toggled
        /// </value>
        protected override int MaxState {
            get { return 7; }
        }

        private bool freeze_state = false;
        public virtual bool IsActive {
            get { return (state & 4) > 0;    }
            set {
                if (!freeze_state) {
                    if (value && (state & 4)==0) {
                        State |= 4;
                        InvokeToggled ();
                    } else if (!value && (state & 4)>0) {
                        State &= ~4;
                        InvokeToggled ();
                    }
                }
            }
        }
        public virtual void SetSilent (bool value)
        {
            if (!freeze_state) {
                if (value && (state & 4)==0) {
                    State |= 4;
                } else if (!value && (state & 4)>0) {
                    State &= ~4;
                }
            }
        }

        public virtual CairoTexture StateTexture {
            get { return (IsActive) ? active_button.StateTexture : passive_button.StateTexture; }
        }

        protected ClutterGenericButton passive_button;
        protected ClutterGenericButton active_button;

        public ClutterToggleButton (uint width, uint height, bool toggled) : this (width, height, toggled ? 0x04 : 0x00)
        {
        }

        public ClutterToggleButton (uint width, uint height, int state) : base ()
        {
            this.SetSize (width, height);
            this.state = state;

            passive_button = new ClutterGenericButton(width, height, state, CreatePassiveTexture);
            passive_button.BubbleEvents = true;
            active_button  = new ClutterGenericButton(width, height, state, CreateActiveTexture);
            active_button.BubbleEvents = true;

            Initialise ();
        }

        protected override void Initialise () {
            Add (passive_button);
            Add (active_button);

            base.Initialise ();

            active_button.Update ();
            passive_button.Update ();
            Update ();
            Show ();
        }

        #region Event Handling
        protected override void HandleButtonReleaseEvent(object o, ButtonReleaseEventArgs args)
        {
            State = (state^4) & ~2;
            freeze_state = true; //freeze the state untill after the event has finished handling
            InvokeToggled ();
            freeze_state = false; //thaw the state
            args.RetVal = !bubble;
        }
        #endregion

        public override void Update () {
            if ((state & 4) > 0) {
                passive_button.Hide ();
                active_button.State = state;
                active_button.Show ();
            } else {
                active_button.Hide ();
                passive_button.State = state;
                passive_button.Show ();
            }
            Show ();
        }

        protected abstract void CreatePassiveTexture (Clutter.CairoTexture texture, int with_state);
        protected abstract void CreateActiveTexture (Clutter.CairoTexture texture, int with_state);
    }
}
