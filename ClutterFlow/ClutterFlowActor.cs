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

	public class ClutterFlowActor : Clutter.Group
	{	
		#region Static Fields
		private static Gdk.Pixbuf default_pb;
		public static Gdk.Pixbuf DefaultPb {
			get { return default_pb; }
			protected set { default_pb = value; }
		}	
		#endregion
		
		#region Fields
		protected Texture cover = null;
		public Texture Cover {
			get { return cover; }
		}
		protected CairoTexture shade = null;
		public CairoTexture Shade {
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


		#endregion
		
		#region Initialization	
		public ClutterFlowActor (CoverManager coverManager) : this ()
		{
			this.CoverManager = coverManager;
			SetupActors ();
		}
		protected ClutterFlowActor () : base ()	{ } //to be overriden by subclasses
		protected virtual void SetupActors ()
		{
			float actor_dim = coverManager.Behaviour.CoverWidth;
			float txtre_dim = coverManager.TextureSize;
			
			//Cover:
			cover = new Texture ();
			cover.SetSize (actor_dim, actor_dim*2);
			cover.Opacity = 255;
			GtkUtil.TextureSetFromPixbuf (cover, MakeReflection(default_pb));

			//Shade:
			shade = new CairoTexture ((uint) txtre_dim, (uint) txtre_dim*2);
			shade.SetSize (actor_dim, actor_dim*2);
			shade.Opacity = 0;
			SetTextureToShade (shade, txtre_dim);
			
			Add (cover);
			Add (shade);
			
			SetAnchorPoint (this.Width*0.5f, this.Height*0.25f);
			SetPosition (0,0);
			Opacity = 0;
			
			ShowAll();
		}
		#endregion

		#region Texture Setup
		
		public void SetShade (byte opacity, bool left) {
			shade.Opacity = opacity;
			if (left)
				shade.SetRotation (RotateAxis.Y, 0, shade.Width*0.5f, shade.Height*0.25f, 0);
			else
				shade.SetRotation (RotateAxis.Y, 180, shade.Width*0.5f, shade.Height*0.25f, 0);
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
		#endregion
		
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
		
		protected virtual void HandleTextureSizeChanged(object sender, EventArgs e)
		{
			SetupActors();
		}
		

		
		/*new public void Dispose () 
		{
			ClutterHelper.RemoveAllFromGroup (this);
			//ClutterHelper.DestroyActor (this);
		}*/
		
	}
}
