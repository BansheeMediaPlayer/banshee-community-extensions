//
// ClutterSlider.cs
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
using Cairo;
using Gdk;
using ClutterFlow.Buttons;

namespace ClutterFlow.Slider
{

    public class ClutterSlider : Group
    {
        #region Events
        public event EventHandler<System.EventArgs> SliderHasMoved;
        void HandleSliderHasMoved(object sender, EventArgs e)
        {
            if (SliderHasMoved!=null) SliderHasMoved(this, System.EventArgs.Empty);
        }
        public event EventHandler<System.EventArgs> SliderHasChanged;
        private void HandleSliderHasChanged(object sender, EventArgs e)
        {
            SetPostionFromIndexSilently(HandlePostionFromIndex);
            if (SliderHasChanged!=null) SliderHasChanged(this, System.EventArgs.Empty);
        }
        #endregion

        #region Fields
        protected CairoTexture outline;
        protected ClutterArrowButton arrow_left;
        protected ClutterArrowButton arrow_right;
        protected ClutterSliderHandle handle;

        //Count is 1-based
        protected int count = 1;
        public int Count {
            get {
                return count;
            }
            set {
                if (count!=value) {
                    int index = HandlePostionFromIndex;
                    count = value;
                    HandlePostionFromIndex = index;
                }
            }
        }

        public string Label {
            get { return handle.Button.Label; }
            set { handle.Button.Label = value; }
        }

        public uint SliderWidth {
            get { return (uint) (Width - Height*1.5f); }
        }
        #endregion

        #region Initialisation
        public ClutterSlider (float width, float height) : base()
        {
            //this.IsReactive = true;
            this.SetSize(width, height);

            handle = new ClutterSliderHandle((float) arrow_width*0.5f + line_width*2, 0, (float) SliderWidth, (float) Height, 0);
            handle.SliderHasChanged += HandleSliderHasChanged;
            handle.SliderHasMoved += HandleSliderHasMoved;
            handle.BubbleEvents = true;
            Add(handle);

            outline = new CairoTexture (SliderWidth, (uint) Height);
            Add(outline);
            outline.SetAnchorPoint (outline.Width*0.5f, outline.Height*0.5f);
            outline.SetPosition (Width*0.5f, Height*0.5f);

            arrow_left = new ClutterArrowButton((uint) arrow_width,(uint) arrow_height, 0, 0x03);
            arrow_left.ButtonPressEvent += HandleLeftArrowButtonPressEvent;
            Add (arrow_left);
            arrow_left.SetPosition (0,0);

            arrow_right = new ClutterArrowButton ((uint) arrow_width,(uint) arrow_height, 0, 0x01);
            arrow_right.ButtonPressEvent += HandleRightArrowButtonPressEvent;
            Add (arrow_right);
            arrow_right.SetPosition ((float) (Width-arrow_width),0);

            Update ();
            ShowAll ();
        }
        #endregion

        #region Index & Bounds methods
        public void UpdateBounds(int count, int index)
        {
            this.count = count;
            HandlePostionFromIndex = index;
        }

        public int HandlePostionFromIndex
        {
            get {
                return (int) Math.Round(handle.Value * (float)(count-1));
            }
            set {
                int retval = value;
                if (retval >= count) retval = count-1;
                if (retval < 0) retval = 0;
                handle.Value = (float) retval / (float) (count-1);
            }
        }

        protected void SetPostionFromIndexSilently (int value)
        {
            if (value >= count) value = count-1;
            if (value < 0) value = 0;
            handle.SetValueSilently ((float) value / (float) (count-1));
        }
        #endregion

        #region Events
        protected void HandleLeftArrowButtonPressEvent(object o, ButtonPressEventArgs args)
        {
            if (args.Event.ClickCount==1 || args.Event.ClickCount%2!=1)
                HandlePostionFromIndex -= 1;
            args.RetVal = true;
        }

        protected void HandleRightArrowButtonPressEvent(object o, ButtonPressEventArgs args)
        {
            if (args.Event.ClickCount==1 || args.Event.ClickCount%2!=1)
                HandlePostionFromIndex += 1;
            args.RetVal = true;
        }
        #endregion

        #region Rendering
        private double arrow_height {
            get { return Height; }
        }
        private double arrow_width {
            get { return arrow_height*1.25; }
        }
        private int line_width {
            get { return 1; }
        }

        public virtual void Update() {
            outline.Clear();
            Cairo.Context context = outline.Create ();

            context.LineWidth = line_width;
            context.MoveTo(outline.Height*0.5, 0.5);
            context.Arc(outline.Width-outline.Height*0.5,outline.Height*0.5,(outline.Height-line_width)*0.5,1.5*Math.PI,0.5*Math.PI);
            context.Arc(outline.Height*0.5,outline.Height*0.5,(outline.Height-line_width)*0.5,0.5*Math.PI,1.5*Math.PI);
            context.ClosePath();
            context.SetSourceRGBA(1.0,1.0,1.0,0.2);
            context.FillPreserve();
            context.SetSourceRGB(1.0,1.0,1.0);
            context.Stroke();

            ((IDisposable) context.Target).Dispose();
            ((IDisposable) context).Dispose();

            handle.Update();
            handle.RaiseTop();
            arrow_left.Update();
            arrow_right.Update();
        }
        #endregion
    }
}