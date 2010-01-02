
using System;
using Clutter;

namespace ClutterFlow
{
	
	
	public class TrackCaption : Caption
	{
		#region Fields
		protected string defaultValue = "";
		public override string DefaultValue {
			get { return defaultValue; }
			set {
				if (value!=defaultValue) {
					if (Value==defaultValue) Value = value;
					defaultValue = value;
				}
			}
		}

		public override CoverManager CoverManager {
			get { return coverManager; }
			set { coverManager = value;	}
		}
		#endregion
		
		public TrackCaption (CoverManager coverManager, string font_name, Color color) : base (coverManager, font_name, color)
		{
		}

		#region Methods

		public override void FadeIn ()
		{
			EventHandler hFadeIn = delegate (object sender, EventArgs e) {
				this.Value = new_caption;
				this.UpdatePosition ();
				this.Animatev ((ulong) AnimationMode.Linear.value__, (uint) (CoverManager.MaxAnimationSpan*0.5f), new string[] { "opacity" }, new GLib.Value ((byte) 255));
				aFade = null;
			};
			if (aFade!=null && aFade.Timeline.IsPlaying)
				aFade.Completed +=  hFadeIn;
			else
				hFadeIn (this, EventArgs.Empty);
		}

		
		public override void UpdatePosition ()
		{
			if (Stage!=null) {
				SetAnchorPoint (Width*0.5f, Height*0.5f);
				SetPosition(coverManager.Behaviour.CenterX, Math.Max(coverManager.Behaviour.CenterY - coverManager.Behaviour.CoverWidth + Height*3, Height*3.6f));
			}
		}

		private string new_caption;
		public void SetValueWithAnim (string caption) {
			new_caption = caption;
			if (Opacity>0) FadeOut ();
			FadeIn ();
		}
		#endregion
	}
}

