//
// ClutterFlowActor.cs
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

using GLib;

namespace ClutterFlow
{
    /// <summary>
    /// A ClutterFlowActor is a group containing the actor texture and it's reflection
    /// It does not contain any animation code, as this is provided by the FlowBehaviour class.
    /// </summary>
    public class ClutterFlowActor : ClutterFlowBaseActor
    {
        #region Fields
        private static bool is_setup = false;
        public static bool IsSetup {
            get { return is_setup; }
            protected set { is_setup = value; }
        }

       /* private bool swapped = false;
        private bool delayed_cover_swap = false;*/
        private bool delayed_shade_swap = false;
        /*public bool SwappedToDefault {
            get { return swapped; }
            set {
                if (value!=swapped) {
                    swapped = value;
                    if (this.Stage == null)
                        delayed_cover_swap = true;
                    else
                        SetCoverSwap ();
                }
            }
        }*/

        protected bool CanUseShader {
            get {
                return Clutter.Feature.Available (Clutter.FeatureFlags.ShadersGlsl);
            }
        }

        /*private void SetCoverSwap () {
            if (swapped) {
                cover.CoglTexture = textureHolder.DefaultTexture;
            } else {
                cover.CoglTexture = Cogl.Texture.NewWithSize((uint) coverManager.TextureSize, (uint) coverManager.TextureSize,
                                                             Cogl.TextureFlags.NoSlicing, Cogl.PixelFormat.Argb8888);
            }
            delayed_cover_swap = false;
        }*/

        private void SetShadeSwap () {
            if (!has_shader) {
                shade.CoglTexture = CoverManager.TextureHolder.ShadeTexture;
                delayed_shade_swap = false;
            }
        }

        protected Clutter.CairoTexture cover = null;
        public Clutter.CairoTexture Cover {
            get { return cover; }
        }
        protected Clutter.Texture shade = null;
        public Clutter.Texture Shade {
            get { return shade; }
        }

        protected bool shifted_outwards;
        #endregion

        #region Initialization
        public ClutterFlowActor (CoverManager cover_manager) : base (cover_manager)
        {
            this.ParentSet += HandleParentSet;
            this.LeaveEvent += HandleLeaveEvent;
            this.ButtonPressEvent += HandleButtonPressEvent;
            this.ButtonReleaseEvent += HandleButtonReleaseEvent;

            SetupActors ();
        }

        public override void Dispose ()
        {
            this.ParentSet -= HandleParentSet;
            this.LeaveEvent -= HandleLeaveEvent;
            this.ButtonPressEvent -= HandleButtonPressEvent;
            this.ButtonReleaseEvent -= HandleButtonReleaseEvent;
        }

        protected virtual void SetupActors ()
        {
            SetAnchorPoint (0, 0);

            TryShading ();

            SetupCover ();
            SetupShade ();

            SetAnchorPoint (this.Width*0.5f, this.Height*0.25f);
            SetPosition (0,0);

            ShowAll();
        }

        protected Clutter.Shader shader;
        protected bool has_shader = false;
        protected virtual void TryShading ()
        {
            /*if (CanUseShader) {
                shader = new Clutter.Shader ();
                shader.VertexSource = @"
                    attribute vec4 gl_Color;
                    varying vec4 gl_FrontColor;
                    varying vec4 gl_BackColor;
                    uniform float            alpha;
                    uniform float            angle;
                    uniform float            z;

                    void main()
                    {
                        gl_TexCoord[0] = gl_MultiTexCoord0;

                        float shadow = 1;
                        if ((gl_TexCoord[0].s == 1 && angle > 0) || (gl_TexCoord[0].s == 0 && angle < 0)) {
                            shadow = clamp(pow(cos(angle), 2.0) * pow(1 + abs(z - 0.5)*1.25, 2.0), 0.0, 1.0);
                        }

                        gl_Position = ftransform();
                        gl_BackColor = vec4(0, 0, 0, 1);
                        gl_FrontColor = vec4(gl_Color.rgb * shadow, alpha);
                    }";
                shader.Compile ();
                SetShader (shader);
                AddNotification ("opacity", OnOpacityChanged);
                AddNotification ("rotation-angle-y", OnAngleChanged);
                AddNotification ("anchor-x", OnAnchorChanged);
                OnOpacityChanged (this, new GLib.NotifyArgs());
                OnAngleChanged (this, new GLib.NotifyArgs());
                OnAnchorChanged (this, new GLib.NotifyArgs());
                has_shader = true;
            }*/
        }

        protected virtual void SetupCover ()
        {
            if (cover == null) {
                cover = new Clutter.CairoTexture((uint) CoverManager.TextureSize, (uint) CoverManager.TextureSize * 2);
                Add (cover);
                cover.Show ();
                cover.Realize ();
            }
            cover.SetSize (CoverManager.Behaviour.CoverWidth, CoverManager.Behaviour.CoverWidth * 2);
            cover.SetPosition (0, 0);
            cover.Opacity = 255;

            //SwappedToDefault = true;
        }

        protected virtual void OnOpacityChanged (object sender, NotifyArgs args)
        {
            SetShaderParamFloat ("alpha", (float) this.Opacity / 255f);
        }
        protected virtual void OnAngleChanged (object sender, NotifyArgs args)
        {
            SetShaderParamFloat ("angle", (float) ((double) GetProperty("rotation-angle-y") * Math.PI / 180));
        }

        protected virtual void OnAnchorChanged (object sender, NotifyArgs args)
        {
            SetShaderParamFloat ("z", (float) GetProperty("anchor-x") / (float) (Width));
        }

        protected virtual void SetupShade ()
        {
            if (!has_shader) {
                if (shade==null) {
                    shade = new Clutter.Texture();
                    Add (shade);
                    shade.Show ();
                    shade.Realize ();
                    if (Stage!=null)
                        SetShadeSwap ();
                    else
                        delayed_shade_swap = true;
                }
                shade.SetSize (CoverManager.Behaviour.CoverWidth, CoverManager.Behaviour.CoverWidth * 2);
                shade.SetPosition (0, 0);
                shade.Opacity = 255;

                if (cover != null) {
                    Shade.Raise (cover);
                }
            }
        }
        #endregion

        #region Behaviour Functions
        public void SetShade (byte opacity, bool left)
        {
            if (!has_shader) {
                shade.Opacity = opacity;
                if (left)
                    shade.SetRotation (RotateAxis.Y, 0, shade.Width*0.5f, shade.Height*0.25f, 0);
                else
                    shade.SetRotation (RotateAxis.Y, 180, shade.Width*0.5f, shade.Height*0.25f, 0);
            }
        }

        public ClutterFlowActor CreateClickClone ()
        {
            if (CoverManager.CurrentCover!=this)
                CoverManager.NewCurrentCover += HandleNewCurrentCover;
            else
                CoverManager.Behaviour.CreateClickedCloneAnimation (this);
            return this;
        }

        private void HandleNewCurrentCover (ClutterFlowBaseActor Actor, EventArgs args)
        {
            if (CoverManager.CurrentCover==this) {
                CoverManager.NewCurrentCover -= HandleNewCurrentCover;
                CoverManager.Behaviour.CreateClickedCloneAnimation (this, CoverManager.MaxAnimationSpan);
            }
        }

        protected virtual void SlideIn ()
        {
            if (!shifted_outwards)
                return;
            shifted_outwards = false;
            Animation anm = Animatev ((ulong) Clutter.AnimationMode.EaseOutBack.value__, CoverManager.MaxAnimationSpan,
                      new string[] { "anchor-x" }, new GLib.Value ((float) Width*0.5f));
            if (!has_shader)
                shade.AnimateWithTimelinev ((ulong) Clutter.AnimationMode.EaseOutSine.value__, anm.Timeline,
                          new string[] { "anchor-x" }, new GLib.Value (0.0f));
        }

        protected virtual void SlideOut ()
        {
            if (shifted_outwards)
                return;
            shifted_outwards = true;
            float x, y, z;
            double angle = GetRotation(RotateAxis.Y, out x, out y, out z);
            float new_anchor_x = (float) (Width * (0.5f + 1.6f*Math.Tan (angle)));
            Animation anm = Animatev ((ulong) Clutter.AnimationMode.EaseOutBack.value__, CoverManager.MaxAnimationSpan,
                      new string[] { "anchor-x" }, new GLib.Value ((float) new_anchor_x));
            if (!has_shader)
                shade.AnimateWithTimelinev ((ulong) Clutter.AnimationMode.EaseOutSine.value__, anm.Timeline,
                          new string[] { "anchor-x" }, new GLib.Value ((float) -new_anchor_x*0.5f));
        }
        #endregion

        #region Event Handling
        void HandleParentSet(object o, ParentSetArgs args)
        {
            if (this.Stage != null) {
                if (delayed_shade_swap) SetShadeSwap ();
                //if (delayed_cover_swap) SetCoverSwap ();
            }
        }

        protected virtual void HandleLeaveEvent (object o, LeaveEventArgs args)
        {
            SlideIn ();
            args.RetVal = true;
        }

        protected virtual void HandleButtonReleaseEvent (object o, ButtonReleaseEventArgs args)
        {
            if (args.Event.Button == 3) {
                SlideIn ();
            } else {
                if (Index>=0 && Opacity > 0) {
                    if (CoverManager.CurrentCover==this || args.Event.ClickCount==3) {
                        CreateClickClone ();
                        CoverManager.InvokeActorActivated (this);
                    } else
                        GLib.Timeout.Add ((uint) (CoverManager.DoubleClickTime*0.75), new GLib.TimeoutHandler (
                            delegate { CoverManager.TargetIndex = Index; return false; }));
                }
            }
            args.RetVal = true;
        }

        protected virtual void HandleButtonPressEvent (object o, ButtonPressEventArgs args)
        {
            if (args.Event.Button == 3) {
                float x, y;
                Clutter.EventHelper.GetCoords (args.Event, out x, out y);
                TransformStagePoint (x, y, out x, out y);
                if (y < Height*0.5f)
                    SlideOut ();
                args.RetVal = true;
            } else {

            }
        }
        #endregion
    }
}
