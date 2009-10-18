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

using Banshee.Collection;

namespace Banshee.ClutterFlow
{
	public class CoverGroup : Clutter.Group, IDisposable
	{
		
		private CairoTexture cover = null;
		private Clone reflection = null;
		private Shader shader;
		
		private AnimationManager anim_mgr = null;
		public AnimationManager AnimationManager {
			get { return anim_mgr; }
			set { if (value!=anim_mgr) SetupAnimationManager (value); }
		}
		
		private uint position;
		public uint Position {
			set {
				if (value!=position) {
					position = value;
					if (position == anim_mgr.MiddlePosition) CoverGroupHelper.InvokeNewCurrentCover (this);
					anim_mgr.Animate (this, position);
				} else {
					ReapplyPosition ();
				}
			}
			get {
				return position;
			}
		}
		public bool IsLeft {
			get { return Position < anim_mgr.MiddlePosition; }
		}
		public bool IsRight {
			get { return Position > anim_mgr.MiddlePosition; }
		}
		
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
		
		public void ReapplyPosition ()
		{
			ApplyPosition (Position);
		}
		public void ApplyPosition (uint position)
		{
			if (this.position!=position && position == anim_mgr.MiddlePosition) CoverGroupHelper.InvokeNewCurrentCover (this);
			this.position = position;
			anim_mgr.Apply (this, position);
		}
		
		public void CheckVisibility () {
			if (position==0 || position==anim_mgr.Positions) {
				this.Hide();
			} else {
				this.ShowAll();
			}
		}
		
	#region Initialization	
		public CoverGroup(AlbumInfo album, float ideal_dim, AnimationManager anim_mgr) : base()
		{
			this.album = album;
			base.Painted += HandlePainted;
			SetupAnimationManager(anim_mgr);

			CoverGroupHelper.Setup((int) ideal_dim);
			LoadCover(ArtworkId, ideal_dim);
		}
		
		protected void SetupAnimationManager(AnimationManager new_mgr) {
			if (anim_mgr!=null) {
				anim_mgr.FinishedAnimation -= HandleFinishedAnimation;
				anim_mgr.StoppedAnimation -= HandleStoppedAnimation;
			}
			anim_mgr = new_mgr;
			anim_mgr.FinishedAnimation += HandleFinishedAnimation;
			anim_mgr.StoppedAnimation += HandleStoppedAnimation;
		}
	#endregion
		
	#region Event Handling
		
		protected void HandlePainted(object sender, EventArgs e)
		{
			UpdateOpacity();
		}

		protected void HandleFinishedAnimation(object sender, EventArgs e)
		{
			CheckVisibility();
		}
		
		protected void HandleStoppedAnimation(object sender, EventArgs e)
		{
			CheckVisibility();
		}
		
	#endregion

	#region Texture Setup
		public void ReloadCover () 
		{
			LoadCover (ArtworkId, cover.Width);
		}
		
		public void LoadCover (string artwork_id, float ideal_dim) 
		{
			ClutterHelper.RemoveAllFromGroup (this);
			//Cover:
			Gdk.Pixbuf pb = CoverGroupHelper.Lookup (artwork_id, (int) ideal_dim);
			while (cover==null) {
				cover = new Clutter.CairoTexture ((uint) ideal_dim, (uint) ideal_dim);
				cover.SetSize (ideal_dim, ideal_dim);
				cover.Opacity = 255;				
				Cairo.Context context = cover.Create();
				Gdk.CairoHelper.SetSourcePixbuf(context, pb, 0, 0); 
				context.Paint();
				((IDisposable) context.Target).Dispose();
				((IDisposable) context).Dispose();
			}
			
			//Reflection:
			reflection = new Clutter.Clone (cover);
			reflection.SetSize (ideal_dim ,ideal_dim);
			reflection.SetPosition (0, cover.Height);
			reflection.Opacity = 190;
			ResetShader ();
			
			this.Add (cover);
			this.Add (reflection);
			
			this.SetAnchorPoint (this.Width/2, this.Height/4);
			this.SetOpacity (0);
		}
		
		public void SetOpacity (byte value)
		{
			base.Opacity = value;
			UpdateOpacity ();
		}

		protected void UpdateOpacity () 
		{
			reflection.SetShaderParamFloat ("alpha_r", ((float) base.Opacity)/255f);
		}
		
		protected void ResetShader () 
		{
			shader = new Shader ();
			shader.FragmentSource = @"
varying vec2 texture_coordinate; varying sampler2D my_color_texture;
uniform float alpha_r;
void main()
{
	texture_coordinate.y = 1.0 - texture_coordinate.y;
	vec4 color = texture2D(my_color_texture, texture_coordinate).rgba;
	float alpha = (texture_coordinate.y*texture_coordinate.y*0.5*alpha_r);
	gl_FragColor = vec4(color.r * alpha, color.g * alpha, color.b * alpha, color.a);
}
";
			shader.Compile ();
			reflection.SetShader (shader);
			float alpha = ((float) base.Opacity)/255f;
			reflection.SetShaderParamFloat ("alpha_r", alpha);
		}
	#endregion
		
		new public void Dispose () 
		{
			ClutterHelper.RemoveAllFromGroup (this);
			ClutterHelper.DestroyActor (this);
		}

	}
}
