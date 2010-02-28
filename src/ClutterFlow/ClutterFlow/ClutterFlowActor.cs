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
//using Cairo;
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
    
	public delegate Gdk.Pixbuf NeedPixbuf();
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
		
		protected Gdk.Pixbuf default_pb;
		public Gdk.Pixbuf DefaultPb {
			get {
				if (default_pb==null && GetDefaultPb!=null) default_pb = ClutterFlowActor.MakeReflection(GetDefaultPb());
				return default_pb; 
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
		
		public NeedPixbuf GetDefaultPb;
		#endregion
        
		#region Initialisation
		public TextureHolder (CoverManager coverManager, NeedPixbuf getDefaultPb)
		{
			this.CoverManager = coverManager;
			this.GetDefaultPb = getDefaultPb;
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
            GetDefaultPb = null;
            default_pb.Dispose ();
            Cogl.Handle.Unref (defltTexture);
            Cogl.Handle.Unref (shadeTexture);
            defltTexture = IntPtr.Zero;
            shadeTexture = IntPtr.Zero;
        }


		public void ReloadDefaultTextures ()
		{
			default_pb = null; //reset this so it gets reloaded
			SetupDefaultTexture ();
			SetupShadeTexture ();
		}

		public void SetupDefaultTexture () 
		{
			if (defltTexture==IntPtr.Zero) {
				if (DefaultPb!=null) {
					Cogl.PixelFormat fm;
					if (DefaultPb.HasAlpha)
						fm = PixelFormat.Rgba8888;
					else
						fm = PixelFormat.Rgb888;
					unsafe {
						defltTexture = ClutterHelper.cogl_texture_new_from_data((uint) DefaultPb.Width, (uint) DefaultPb.Height, Cogl.TextureFlags.None, 
						                                         fm, Cogl.PixelFormat.Any, (uint) DefaultPb.Rowstride, DefaultPb.Pixels);
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
                        coverManager.Realize ();
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
        
        protected double last_alpha = 0;
        public double LastAlpha {
            get { return last_alpha; }
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

        public int CompareTo (IIndexable obj) {
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
        public static Gdk.Pixbuf MakeReflection (Pixbuf pb) {
            Gdk.Pixbuf finalPb = new Gdk.Pixbuf(Colorspace.Rgb, true, pb.BitsPerSample, pb.Width, pb.Height*2);
            if (pb.BitsPerSample != 8)
                throw new System.Exception ("Invalid bits per sample");

            unsafe {

                bool alpha = pb.HasAlpha;
                int src_rowstride = pb.Rowstride;
                int src_width = pb.Width;
                int src_height = pb.Height;
                byte * src_byte = (byte *) pb.Pixels;
                byte * src_base = src_byte;
                
                int dst_rowstride = finalPb.Rowstride;
                int dst_width = finalPb.Width;
                int dst_height = finalPb.Height;
                byte * dst_byte = (byte *) finalPb.Pixels;
                byte * dst_base = dst_byte;
    
                byte * refl_byte = dst_base + (dst_height-1) * dst_rowstride + (dst_width-1) * 4  + 3;

                for (int j = 0; j < src_height; j++) {
                    src_byte = ((byte *) src_base) + j * src_rowstride;
                    dst_byte = ((byte *) dst_base) + j * dst_rowstride;
                    refl_byte = ((byte *) dst_base) + (dst_height-1-j) * dst_rowstride;
                    for (int i = 0; i < src_width; i++) {
                        byte r = *(src_byte++);
                        byte g = *(src_byte++);
                        byte b = *(src_byte++);
                        byte a = 0xff;
                        if (alpha)
                            a = *(src_byte++);
                        
                        *dst_byte++ = r;
                        *dst_byte++ = g;
                        *dst_byte++ = b;
                        *dst_byte++ = a;
                        *refl_byte++ = r;
                        *refl_byte++ = g;
                        *refl_byte++ = b;
                        *refl_byte++ = (byte) ((float) a * (float) (Math.Max(0, j - 0.3*src_height) / src_height));
                    }
                }
            }
            return finalPb;
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
    
        private bool swapped = false;
        private bool delayed_cover_swap = false;
        private bool delayed_shade_swap = false;
        public bool SwappedToDefault {
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
        }

        private void SetCoverSwap () {
            if (swapped) {
                cover.CoglTexture = textureHolder.DefaultTexture;
            } else {
                cover.CoglTexture = Cogl.Texture.NewWithSize((uint) coverManager.TextureSize, (uint) coverManager.TextureSize,
                                                             Cogl.TextureFlags.NoSlicing, Cogl.PixelFormat.Argb8888);
            }
            delayed_cover_swap = false;
        }

        private void SetShadeSwap () {
            shade.CoglTexture = textureHolder.ShadeTexture;
            delayed_shade_swap = false;
        }
        
        protected Clutter.Texture cover = null;
        public Clutter.Texture Cover {
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

		private NeedPixbuf getDefaultPb;
		
		protected bool shifted_outwards;
		#endregion
		
		#region Initialization	
		public ClutterFlowActor (CoverManager cover_manager, NeedPixbuf getDefaultPb) : base (cover_manager)
		{
			this.getDefaultPb = getDefaultPb;

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
            getDefaultPb = null;
            
            DisposeStatics ();
        }
		protected virtual bool SetupStatics ()
		{
			if (textureHolder==null) 
				textureHolder = new TextureHolder(CoverManager, GetDefaultPb);
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
			
			SetupCover ();
			SetupShade ();

			SetAnchorPoint (this.Width*0.5f, this.Height*0.25f);
			SetPosition (0,0);
			
			ShowAll();
		}
		protected virtual void SetupCover ()
		{
			if (cover==null) {
				cover = new Clutter.Texture();
				Add (cover);
				cover.Show ();
				cover.Realize ();
			}
			cover.SetSize (coverManager.Behaviour.CoverWidth, coverManager.Behaviour.CoverWidth*2);
			cover.SetPosition (0, 0);
			cover.Opacity = 255;
			
			SwappedToDefault = true;
		}

		protected virtual void SetupShade ()
		{
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
		#endregion

		#region Texture Handling
		protected virtual Gdk.Pixbuf GetDefaultPb ()
		{
			return (getDefaultPb!=null) ? getDefaultPb() : null;
		}

		protected virtual void HandleTextureSizeChanged(object sender, EventArgs e)
		{
			SetupActors();
		}
		#endregion

		#region Behaviour Functions
		public void SetShade (byte opacity, bool left) {
			shade.Opacity = opacity;
			if (left)
				shade.SetRotation (RotateAxis.Y, 0, shade.Width*0.5f, shade.Height*0.25f, 0);
			else
				shade.SetRotation (RotateAxis.Y, 180, shade.Width*0.5f, shade.Height*0.25f, 0);
		}
		
		public ClutterFlowActor CreateClickClone () {
			coverManager.Behaviour.CreateClickedCloneAnimation (this);			
			return this;
		}

		/*public double AlphaFunction (double progress)
		{
			if (index < 0)
				last_alpha = 0;
			else {
				double val = (CoverManager.HalfVisCovers - (CoverManager.TotalCovers-1) * progress + index) / (CoverManager.VisibleCovers-1);
				if (val<0) val=0;
				if (val>1) val=1;
				last_alpha = val;
			}
			return last_alpha;
		}*/
		
		protected virtual void SlideIn ()
		{
			//FIXME: we should not shift the shade along!!
			if (!shifted_outwards)
				return;
			shifted_outwards = false;
			Animatev ((ulong) Clutter.AnimationMode.EaseOutBack.value__, CoverManager.MaxAnimationSpan, new string[] { "anchor-x" }, new GLib.Value ((float) Width*0.5f));
		}
		
		protected virtual void SlideOut ()
		{
			//FIXME: we should not shift the shade along!!
			if (shifted_outwards)
				return;
			shifted_outwards = true;
			float x, y, z;
			double angle = GetRotation(RotateAxis.Y, out x, out y, out z);
			float new_anchor_x = (float) (Width * (0.5f + 1.6f*Math.Tan (angle)));
			Animatev ((ulong) Clutter.AnimationMode.EaseOutBack.value__, CoverManager.MaxAnimationSpan, new string[] { "anchor-x" }, new GLib.Value ((float) new_anchor_x));
		}
		#endregion

        /* TODO
         * use Cogl.General.AreFeaturesAvailable(FeatureFlags.ShadersGlsl) to check
         * if we can use vertex shaders for the shadow effect:
         *  gl_FrontColor = gl_Color;
         * if not available use the current implementation
         */
        
		#region Event Handling
        void HandleParentSet(object o, ParentSetArgs args)
        {
            if (this.Stage != null) {
                if (delayed_shade_swap) SetShadeSwap ();                
                if (delayed_cover_swap) SetCoverSwap ();
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
					if (CoverManager.CurrentCover==this) {
	                    CreateClickClone ();
						CoverManager.InvokeCoverActivated (this);
					} else
						CoverManager.TargetIndex = Index;
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
