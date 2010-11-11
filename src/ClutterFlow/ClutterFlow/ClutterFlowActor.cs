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
using System.Collections;
using System.Collections.Generic;

using Gdk;
using Clutter;
using Cogl;

using GLib;

namespace ClutterFlow
{

    public interface IIndexable : IComparable<IIndexable>
    {
        int Index { get; }
        event IndexChangedEventHandler IndexChanged;
    }

	public delegate Cairo.ImageSurface NeedSurface();
    public delegate void IndexChangedEventHandler(IIndexable item, int old_index, int new_index);
	
	public class TextureHolder : IDisposable {
		#region Fields
		protected CoverManager coverManager;
		public CoverManager CoverManager {
			get { return coverManager; }
			set {
				if (value!=coverManager) {
					if (coverManager!=null) {
						coverManager.TextureSizeChanged -= HandleTextureSizeChanged;
					}
					coverManager = value;
					if (coverManager!=null) {
						coverManager.TextureSizeChanged += HandleTextureSizeChanged;
					}
				}
			}
		}
		
		protected Cairo.ImageSurface default_surface;
		public Cairo.ImageSurface DefaultSurface {
			get {
				if (default_surface==null && GetDefaultSurface!=null) default_surface = ClutterFlowActor.MakeReflection(GetDefaultSurface());
				return default_surface;
			}
		}
		
		protected IntPtr defltTexture;
		public IntPtr DefaultTexture {
			get {
				if (defltTexture==IntPtr.Zero) SetupDefaultTexture ();
				return defltTexture;
			}
		}

		protected IntPtr shadeTexture;
		public IntPtr ShadeTexture {
			get {
				if (shadeTexture==IntPtr.Zero) SetupShadeTexture ();
				return shadeTexture;
			}
		}
		
		public NeedSurface GetDefaultSurface;
		#endregion

		#region Initialisation
		public TextureHolder (CoverManager coverManager, NeedSurface getDefaultSurface)
		{
			this.CoverManager = coverManager;
			this.GetDefaultSurface = getDefaultSurface;
			ReloadDefaultTextures ();
		}
        ~ TextureHolder () {
            Dispose ();
        }
        protected bool disposed = false;
        public virtual void Dispose ()
        {
            if (disposed)
                return;
            disposed = true;
            CoverManager = null;
            GetDefaultSurface = null;
            ((IDisposable) default_surface).Dispose ();
            Cogl.Handle.Unref (defltTexture);
            Cogl.Handle.Unref (shadeTexture);
            defltTexture = IntPtr.Zero;
            shadeTexture = IntPtr.Zero;
        }


		public void ReloadDefaultTextures ()
		{
			if (default_surface != null)
				((IDisposable) default_surface).Dispose ();
			default_surface = null; //reset this so it gets reloaded
			
			SetupDefaultTexture ();
			SetupShadeTexture ();
		}

		public void SetupDefaultTexture ()
		{
			if (defltTexture==IntPtr.Zero) {
				if (DefaultSurface!=null) {
					Cogl.PixelFormat fm;
					if (DefaultSurface.Format == Cairo.Format.ARGB32)
						fm = PixelFormat.Argb8888Pre;
					else //if (DefaultSurface.Format == Cairo.Format.RGB24)
						fm = PixelFormat.Rgb888;
					
					unsafe {
						
						defltTexture = ClutterHelper.cogl_texture_new_from_data((uint) DefaultSurface.Width, (uint) DefaultSurface.Height, Cogl.TextureFlags.None,
						                                         fm, Cogl.PixelFormat.Any, (uint) DefaultSurface.Stride, DefaultSurface.DataPtr);
					}
				} else {
					defltTexture = Cogl.Texture.NewWithSize ((uint) coverManager.TextureSize, (uint) coverManager.TextureSize,
					                                         Cogl.TextureFlags.None, Cogl.PixelFormat.Any);
				}
			}
		}

		public void SetupShadeTexture ()
		{
			if (shadeTexture==IntPtr.Zero) {

				Gdk.Pixbuf finalPb = new Gdk.Pixbuf(Colorspace.Rgb, true, 8, coverManager.TextureSize, coverManager.TextureSize*2);
	
				unsafe {				
					int dst_rowstride = finalPb.Rowstride;
					int dst_width = finalPb.Width;
					int shd_width = (int) ((float) dst_width * 0.25f);
					int dst_height = finalPb.Height;
					byte * dst_byte = (byte *) finalPb.Pixels;
					byte * dst_base = dst_byte;
		
					for (int j = 0; j < dst_height; j++) {
						dst_byte = ((byte *) dst_base) + j * dst_rowstride;
						for (int i = 0; i < dst_width; i++) {
							*dst_byte++ = 0x00;
							*dst_byte++ = 0x00;
							*dst_byte++ = 0x00;
							if (i > shd_width)
								*dst_byte++ = (byte) (255 * (float) (i - shd_width) / (float) (dst_width - shd_width));
							else
								*dst_byte++ = 0x00;
						}
					}
				}

				unsafe {
					shadeTexture = ClutterHelper.cogl_texture_new_from_data((uint) finalPb.Width, (uint) finalPb.Height, Cogl.TextureFlags.None,
					                                         PixelFormat.Rgba8888, Cogl.PixelFormat.Any, (uint) finalPb.Rowstride, finalPb.Pixels);
				}
			}
		}
		#endregion
		
		protected void HandleTextureSizeChanged(object sender, EventArgs e)
		{
			ReloadDefaultTextures ();
		}
	}

    public abstract class ClutterFlowBaseActor : Clutter.Group, IIndexable
    {
        #region Fields
        protected CoverManager coverManager;
        public virtual CoverManager CoverManager {
            get { return coverManager; }
            set {
                if (value!=coverManager) {
                    if (coverManager!=null) {
                        System.GC.SuppressFinalize (this);
                        coverManager.Remove (this);
                    }
                    coverManager = value;
                    if (coverManager!=null) {
                        coverManager.Add (this);
                    }
                }
            }
        }

        protected string cache_key = "";
        public virtual string CacheKey {
            get { return cache_key; }
            set { cache_key = value; }
        }

        protected string label = "";
        public virtual string Label {
            get { return label; }
            set { label = value; }
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
                if (value!=index) {
                    int old_index = index;
                    index = value;
                    IsReactive = !(index < 0);
                    if (IndexChanged!=null) IndexChanged (this, old_index, index);
                }
            }
        }

        public virtual int CompareTo (IIndexable obj) {
            if (obj.Index==-1 && this.Index!=-1)
                return 1;
            return obj.Index - this.Index;
        }
        #endregion

        public ClutterFlowBaseActor (CoverManager cover_manager) : base ()
        {
            this.CoverManager = cover_manager;
        }

        ~ ClutterFlowBaseActor ()
        {
            Dispose ();
        }
        protected bool disposed = false;
        public override void Dispose ()
        {
            if (disposed)
                return;
            disposed = true;

            CoverManager = null;
        }

        #region Texture Handling
        public static void MakeReflection (Cairo.ImageSurface source, CairoTexture dest) {
	        int w = source.Width + 4;
	        int h = source.Height * 2 + 4;
			
			dest.SetSurfaceSize ((uint) w, (uint) h);
			
			
			
			Cairo.Context context = dest.Create();
			
			MakeReflection (context, source,  w, h);

			//((IDisposable) context.Target).Dispose();
			((IDisposable) context).Dispose();
        }
		
		
        public static Cairo.ImageSurface MakeReflection (Cairo.ImageSurface source) {
	        int w = source.Width + 4;
	        int h = source.Height * 2 + 4;
			
			Cairo.ImageSurface dest = new Cairo.ImageSurface(Cairo.Format.ARGB32, w, h);
			Cairo.Context context = new Cairo.Context(dest);
	
			MakeReflection (context, source, w, h);

			//((IDisposable) context.Target).Dispose();
			((IDisposable) context).Dispose();
			
			return dest;
        }
		
		private static void MakeReflection (Cairo.Context context, Cairo.ImageSurface source, int w, int h) {

			context.ResetClip ();			
			context.SetSourceSurface(source, 2, 2);
	        context.Paint();
			
	        double alpha = -0.3;
	        double step = 1.0 / (double) source.Height;

			context.Translate(0, h);
			context.Scale (1, -1);
			context.SetSourceSurface(source, 2, 2);
	        for (int i = 0; i < source.Height; i++) {
				context.Rectangle(0, i+2, w, 1);
	            context.Clip();
				alpha += step;
	            context.PaintWithAlpha(Math.Max(Math.Min(alpha, 0.7), 0.0));
	            context.ResetClip();
			}
		}
        #endregion
    }

    /// <summary>
    /// A ClutterFlowActor is a group containing the actor texture and it's reflection
    /// It does not contain any animation code, as this is provided by the FlowBehaviour class.
    /// </summary>
	public class ClutterFlowActor : ClutterFlowBaseActor
	{	
		#region Fields
        protected static TextureHolder textureHolder;

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
	            shade.CoglTexture = textureHolder.ShadeTexture;
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


        public override CoverManager CoverManager {
            get { return base.CoverManager; }
            set {
                if (coverManager!=null)
                    coverManager.TextureSizeChanged -= HandleTextureSizeChanged;
                base.CoverManager = value;
                if (coverManager!=null)
                    coverManager.TextureSizeChanged += HandleTextureSizeChanged;
            }
        }

		private NeedSurface getDefaultSurface;
		
		protected bool shifted_outwards;
		#endregion
		
		#region Initialization	
		public ClutterFlowActor (CoverManager cover_manager, NeedSurface getDefaultSurface) : base (cover_manager)
		{
			this.getDefaultSurface = getDefaultSurface;

            this.ParentSet += HandleParentSet;
			this.LeaveEvent += HandleLeaveEvent;
			this.ButtonPressEvent += HandleButtonPressEvent;
			this.ButtonReleaseEvent += HandleButtonReleaseEvent;
			
			IsSetup = SetupStatics ();
			SetupActors ();
		}

        ~ ClutterFlowActor ()
        {
            Dispose ();
        }
        public sealed override void Dispose ()
        {
            base.Dispose ();

            this.ParentSet -= HandleParentSet;
			this.LeaveEvent -= HandleLeaveEvent;
            this.ButtonPressEvent -= HandleButtonPressEvent;
            this.ButtonReleaseEvent -= HandleButtonReleaseEvent;
            getDefaultSurface = null;

            DisposeStatics ();
        }
		protected virtual bool SetupStatics ()
		{
			if (textureHolder==null)
				textureHolder = new TextureHolder(CoverManager, getDefaultSurface);
			return true;
		}
        protected virtual void DisposeStatics ()
        {
            if (textureHolder!=null)
                textureHolder.Dispose ();
            textureHolder = null;
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
					uniform float			alpha;
					uniform float			angle;
					uniform float			z;
					
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
			if (cover==null) {
				cover = new Clutter.CairoTexture((uint) coverManager.TextureSize, (uint) coverManager.TextureSize*2);				
				Add (cover);
				cover.Show ();
				cover.Realize ();
			}
			cover.SetSize (coverManager.Behaviour.CoverWidth, coverManager.Behaviour.CoverWidth*2);
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
				shade.SetSize (coverManager.Behaviour.CoverWidth, coverManager.Behaviour.CoverWidth*2);
				shade.SetPosition (0, 0);
				shade.Opacity = 255;
	
				if (cover!=null) Shade.Raise (cover);
			}
		}
		#endregion

		#region Texture Handling
		protected virtual Cairo.ImageSurface GetDefaultSurface ()
		{
			return (getDefaultSurface!=null) ? getDefaultSurface() : null;
		}

		protected virtual void HandleTextureSizeChanged(object sender, EventArgs e)
		{
			SetupActors();
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
				coverManager.Behaviour.CreateClickedCloneAnimation (this);
			return this;
		}
		
		private void HandleNewCurrentCover (ClutterFlowBaseActor Actor, EventArgs args)
		{
			if (CoverManager.CurrentCover==this) {
				CoverManager.NewCurrentCover -= HandleNewCurrentCover;
				coverManager.Behaviour.CreateClickedCloneAnimation (this, CoverManager.MaxAnimationSpan);
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
						GLib.Timeout.Add ((uint) (CoverManager.DoubleClickTime*0.75), new GLib.TimeoutHandler (delegate { CoverManager.TargetIndex = Index; return false; }));
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
