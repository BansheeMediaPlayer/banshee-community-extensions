//
// ClutterFlowActor.cs
//
// Author:
//   Mathijs Dumon <mathijsken@hotmail.com>
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

//
// A ClutterFlowActor is a group containing the actor texture and it's reflection
// It does not contain any real animation code, only code to apply animations
// Animation is provided by the FlowBehaviour class.
//


using System;
using System.Collections;
using System.Collections.Generic;

using Gdk;
using Cairo;
using Clutter;

using GLib;

namespace ClutterFlow
{

	public delegate Gdk.Pixbuf NeedPixbuf();
	
	public class TextureHolder {
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
				if (default_pb==null && GetDefaultPb!=null) default_pb = GetDefaultPb();
				return default_pb; 
			}
		}
		
		protected Texture defltTexture;
		public Texture DefaultTexture {
			get {
				if (defltTexture==null) SetupDefaultTexture ();
				return defltTexture;
			}
		}

		protected CairoTexture shadeTexture;
		public CairoTexture ShadeTexture {
			get {
				if (shadeTexture==null) SetupShadeTexture ();
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

		public void ReloadDefaultTextures ()
		{
			default_pb = null; //reset this so it gets reloaded
			SetupDefaultTexture (true);
			SetupShadeTexture (true);
		}
		
		public void SetupDefaultTexture () 
		{
			SetupDefaultTexture (false);
		}
		public void SetupDefaultTexture (bool forceResize) 
		{
			if (defltTexture==null) {
				defltTexture = new Texture ();
				defltTexture.SetSize (coverManager.TextureSize, coverManager.TextureSize);
				coverManager.Add (defltTexture);
				defltTexture.Hide();
			} else if (forceResize)
				defltTexture.SetSize (coverManager.TextureSize, coverManager.TextureSize);
			if (DefaultPb!=null)
				GtkUtil.TextureSetFromPixbuf (defltTexture, ClutterFlowActor.MakeReflection (DefaultPb));
		}


		public void SetupShadeTexture () 
		{
			SetupShadeTexture (false);
		}
		public void SetupShadeTexture (bool forceResize) 
		{
			if (shadeTexture==null) {
				shadeTexture = new CairoTexture ((uint) coverManager.TextureSize, (uint) coverManager.TextureSize);
				coverManager.Add (shadeTexture);
				shadeTexture.Hide();
				ClutterFlowActor.SetTextureToShade (shadeTexture, coverManager.TextureSize);
			} else if (forceResize)
				shadeTexture.SetSize (coverManager.TextureSize, coverManager.TextureSize);
			ClutterFlowActor.SetTextureToShade (shadeTexture, coverManager.TextureSize);
		}
		#endregion
		
		protected void HandleTextureSizeChanged(object sender, EventArgs e)
		{
			ReloadDefaultTextures ();
		}
	}
	
	public class ClutterFlowActor : Clutter.Group
	{	
		#region Fields
		protected static TextureHolder textureHolder;

		private static bool is_setup = false;
		public static bool IsSetup {
			get { return is_setup; }
			protected set { is_setup = value; }
		}
		
		protected IntPtr coverMaterial;
		private bool swapped = false;
	 	public bool SwappedToDefault {
			get { return swapped; }
			set {
				if (value!=swapped) {
					swapped = value;
					if (swapped) {
						coverMaterial = Cogl.Material.Ref(cover.CoglMaterial);
						cover.CoglMaterial = textureHolder.DefaultTexture.CoglMaterial;
					} else {
						cover.CoglMaterial = coverMaterial;
						Cogl.Material.Unref(coverMaterial);
					}
				}
			}
		}
		
		protected Texture cover = null;
		public Texture Cover {
			get { return cover; }
		}
		protected Texture shade = null;
		public Texture Shade {
			get { return shade; }
		}
		
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
		
		protected int index = -1; //-1 = not visible
		public int Index {
			get { return index; }
			set { if (value!=index) { index = value; } }
		}

		private NeedPixbuf getDefaultPb;

		#endregion
		
		#region Initialization	
		public ClutterFlowActor (CoverManager coverManager, NeedPixbuf getDefaultPb) : base ()
		{
			this.CoverManager = coverManager;
			this.getDefaultPb = getDefaultPb;
			SetupStatics ();
			SetupActors ();
		}
		protected virtual void SetupStatics ()
		{
			if (textureHolder==null) 
				textureHolder = new TextureHolder(CoverManager, GetDefaultPb);
			IsSetup = true;
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
				cover = new Texture();
				Add (cover);
				cover.Show ();
			}
			cover.SetSize (coverManager.Behaviour.CoverWidth, coverManager.Behaviour.CoverWidth*2);
			cover.SetPosition (0, 0);
			cover.Opacity = 255;
			SwappedToDefault = true;
		}

		protected virtual void SetupShade ()
		{
			if (shade==null) {
				shade = new Texture();
				Add (shade);
				shade.Show ();
				shade.CoglMaterial = textureHolder.ShadeTexture.CoglMaterial;
			}
			shade.SetSize (coverManager.Behaviour.CoverWidth, coverManager.Behaviour.CoverWidth*2);
			shade.SetPosition (0, 0);
			shade.Opacity = 255;

			if (cover!=null) Raise (cover);
		}
		#endregion

		#region Texture Handling
		protected virtual Gdk.Pixbuf GetDefaultPb ()
		{
			return (getDefaultPb!=null) ? getDefaultPb() : null;
		}

		public static void SetTextureToShade (CairoTexture text, float txtre_dim)
		{
			text.Clear ();
			Cairo.Context context = text.Create ();
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
		}

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
	}
}
