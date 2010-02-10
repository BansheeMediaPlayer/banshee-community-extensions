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

    /// <summary>
    /// A ClutterFlowActor is a group containing the actor texture and it's reflection
    /// It does not contain any animation code, as this is provided by the FlowBehaviour class.
    /// </summary>
	public class ClutterFlowActor : Clutter.Group, IIndexable
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

	 	void HandleParentSet(object o, ParentSetArgs args)
	 	{
	 		if (this.Stage != null) {
				if (delayed_shade_swap) SetShadeSwap ();				
				if (delayed_cover_swap) SetCoverSwap ();
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
		
		protected CoverManager coverManager;
		public CoverManager CoverManager {
			get { return coverManager; }
			set {
				if (value!=coverManager) {
					if (coverManager!=null) {
						coverManager.TextureSizeChanged -= HandleTextureSizeChanged;
                        System.GC.SuppressFinalize (this);
						coverManager.Remove (this);
					}
					coverManager = value;
					if (coverManager!=null) {
						coverManager.TextureSizeChanged += HandleTextureSizeChanged;
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
		
		protected double lastAlpha = 0;
		public double LastAlpha {
			get { return lastAlpha; }
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

		private NeedPixbuf getDefaultPb;

		#endregion
		
		#region Initialization	
		public ClutterFlowActor (CoverManager coverManager, NeedPixbuf getDefaultPb) : base ()
		{
			this.ParentSet += HandleParentSet;
			this.CoverManager = coverManager;
			this.getDefaultPb = getDefaultPb;

			this.ButtonPressEvent += HandleButtonPressEvent;
			this.ButtonReleaseEvent += HandleButtonReleaseEvent;
			
			IsSetup = SetupStatics ();
			SetupActors ();
		}
        ~ ClutterFlowActor ()
        {
            Dispose ();
        }
        protected bool disposed = false;
        public override void Dispose ()
        {
            if (disposed)
                return;
            disposed = true;
            //Console.WriteLine("ClutterFlowActor.Dispose ()");
            
            CoverManager = null;
            this.ParentSet -= HandleParentSet;
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

		/*public static void SetSurfaceToShade (Cairo.ImageSurface surf, float txtre_dim)
		{
			Cairo.Context context = new Cairo.Context (surf);
			Gradient gr = new LinearGradient (0, 0, txtre_dim, 0);
			gr.AddColorStop (0.25, new Cairo.Color (0.0, 0.0, 0.0, 0.0));
			gr.AddColorStop (1.0, new Cairo.Color (0.0, 0.0, 0.0, 0.6));
			context.Pattern = gr;
			context.Rectangle(new Cairo.Rectangle (0, 0, txtre_dim, txtre_dim));
			context.Fill ();
			MakeReflection (context, txtre_dim);

			((IDisposable) gr).Dispose ();
			((IDisposable) context.Target).Dispose ();
			((IDisposable) context).Dispose ();
		}
		
		protected static void MakeReflection (Context context, float txtre_dim) {
			context.Save ();
				
			double alpha = 0.65;
    		double step = 1 / txtre_dim;
  
    		context.Translate (0, 2 * txtre_dim);
    		context.Scale (1, -1);
    
			for (int i = 0; i < txtre_dim; i++) {
				context.Rectangle (0, txtre_dim-i, txtre_dim, 1);
				context.Clip ();
				context.SetSource (context.Target);
				alpha = alpha - step;
				context.PaintWithAlpha (alpha);
				context.ResetClip ();
			}

			context.Restore ();
		}*/

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

		public double AlphaFunction (double progress)
		{
			if (index < 0)
				lastAlpha = 0;
			else {
				double val = (CoverManager.HalfVisCovers - (CoverManager.TotalCovers-1) * progress + index) / (CoverManager.VisibleCovers-1);
				if (val<0) { val=0; }
				if (val>1) { val=1; }
				lastAlpha = val;
			}
			return lastAlpha;
		}
		#endregion

		#region Event Handling
		private void HandleButtonReleaseEvent (object o, ButtonReleaseEventArgs args)
		{
			if (Index>=0 && Opacity > 0) {
                //Console.WriteLine ("HandleButtonReleaseEvent in ClutterFlowActor with index " + Index + " current cover is " + ((CoverManager.CurrentCover!=null) ? CoverManager.CurrentCover.Index.ToString():"null") + " and lc_index is " + index);
				if (CoverManager.CurrentCover==this) {
					CoverManager.InvokeCoverActivated (this);
				} else
					CoverManager.TargetIndex = Index;
			}
            args.RetVal = true;
		}

		private void HandleButtonPressEvent (object o, ButtonPressEventArgs args)
		{
			//should register time for double clicks
		}
		#endregion
	}
}
