//
// CoverGroup.cs
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
// A covergroup is a group containing the cover texture and it's reflection
// It does not contain any real animation code, only code to apply animations
// Animations are provided by sublcasses of the AnimationManager class.
//


using System;
using System.Collections.Generic;

using Gdk;
using Cairo;
using Clutter;

using Banshee.Gui;
using Banshee.Collection;
using Banshee.Collection.Gui;
using Banshee.ServiceStack;

namespace Banshee.ClutterFlow
{
	public class CoverGroup : Clutter.Group, IDisposable
	{
		
		#region static Members
		private static Gdk.Pixbuf default_cover;
		public static Gdk.Pixbuf DefaultCover {
			get { return default_cover; }
		}
		
		private static bool is_setup = false;
		public static bool IsSetup {
			get { return is_setup; }
		}
		
		private static ArtworkManager artwork_manager;
		public static ArtworkManager GetArtworkManager {
			get { return artwork_manager; }
		}
		#endregion
		
		private CairoTexture cover = null;
		
		private CoverManager coverManager;
		public CoverManager CoverManager {
			get { return coverManager; }
		}
		
		#region Position Handling
		protected int index = -1; //-1 = not visible
		public int Index {
			get { return index; }
			set { if (value!=index) index = value; }
		}		
		#endregion
		
		private AlbumInfo album;
		public AlbumInfo Album {
			get { return album; }
			set { 
				album = value;
				ReloadCover ();
			}
		}
		public string ArtworkId {
			get { return album!=null ? album.ArtworkId : null; }
		}
		
	#region Initialization	
		public CoverGroup(AlbumInfo album, CoverManager coverManager) : base()
		{
			this.album = album;
			this.coverManager = coverManager;
					
			LoadCover(ArtworkId, coverManager.Behaviour.CoverWidth);
			
			this.SetPosition(0,0);
		}

	#endregion
		
		public CoverGroup CreateClickClone() {
			coverManager.Behaviour.CreateClickedCloneAnimation(this);			
			return this;
		}
		
		public double AlphaFunction(double progress) {
			if (index < 0)
				//this.Hide(); TODO?
				return 0;
			else {
				double val = (CoverManager.HalfVisCovers - (CoverManager.TotalCovers-1) * progress + index) / (CoverManager.VisibleCovers-1);
				if (val<0) { val=0; }
				if (val>1) { val=1; }
				return val;
			}
		}

	#region Texture Setup
		public void ReloadCover () 
		{
			LoadCover (ArtworkId, cover.Width);
		}
		
		public void LoadCover (string artwork_id, float ideal_dim) 
		{
			ClutterHelper.RemoveAllFromGroup (this);
			//Cover:
			Gdk.Pixbuf pb = Lookup (artwork_id, (int) ideal_dim);
			while (cover==null) {
				cover = new Clutter.CairoTexture ((uint) ideal_dim, (uint) ideal_dim*2);
				cover.SetSize (ideal_dim, ideal_dim*2);
				cover.Opacity = 255;				
				Cairo.Context context = cover.Create();
				Gdk.CairoHelper.SetSourcePixbuf(context, pb, 0, 0); 
				context.Paint();
				
				double alpha = 0.5;
        		double step = 1 / ideal_dim;
      
        		context.Translate (0, 2 * ideal_dim);
        		context.Scale (1, -1);
        
				for (int i = 0; i < ideal_dim; i++) {
					context.Rectangle (0, ideal_dim-i, ideal_dim, 1);
					context.Clip ();
					Gdk.CairoHelper.SetSourcePixbuf(context, pb, 0, 0);
					alpha = alpha - step;
					context.PaintWithAlpha (alpha);
					context.ResetClip ();
				}			
				((IDisposable) context.Target).Dispose();
				((IDisposable) context).Dispose();
			}
			
			this.Add (cover);
			this.SetAnchorPoint (this.Width/2, this.Height/4);
			this.Opacity = 0;
		}
		
/*		protected void ResetShader () 
		{
			shader = new Shader ();
			shader.FragmentSource = @"
varying vec2 texture_coordinate; varying sampler2D my_color_texture;
uniform float alpha_r;
void main()
{
	vec2 tc =  vec2(texture_coordinate.x, 1.0 - texture_coordinate.y);
	vec4 color = texture2D(my_color_texture, tc).rgba;
	float alpha = ((1-texture_coordinate.y)*(1-texture_coordinate.y)*0.5*alpha_r);
	gl_FragColor = vec4(color.r * alpha, color.g * alpha, color.b * alpha, color.a);
}
";
			shader.Compile ();
			reflection.SetShader (shader);
			float alpha = ((float) base.Opacity)/255f;
			reflection.SetShaderParamFloat ("alpha_r", alpha);
		}*/
	#endregion
		
		new public void Dispose () 
		{
			ClutterHelper.RemoveAllFromGroup (this);
			ClutterHelper.DestroyActor (this);
		}
		
		#region static Methods
		static CoverGroup() {
			if (!is_setup) {
				artwork_manager = ServiceManager.Get<ArtworkManager> ();
				default_cover = IconThemeUtils.LoadIcon (100, "media-optical", "browser-album-cover");
			}
				
			is_setup = true;
		}
		
		public static Gdk.Pixbuf Lookup(string artworkId, int size) {
			Gdk.Pixbuf pb = artwork_manager == null ? null 
                : artwork_manager.LookupScalePixbuf(artworkId, size);
			return pb ?? default_cover;
		}
		#endregion

	}
}
