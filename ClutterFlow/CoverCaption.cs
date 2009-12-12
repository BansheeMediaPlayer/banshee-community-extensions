
using System;
using System.Runtime.InteropServices;
using Clutter;

namespace ClutterFlow
{
	
	
	public class CoverCaption : Clutter.Text
	{
		#region Fields
		protected string defaultValue = "Unkown Artist\nUnkown Album";
		public string DefaultValue {
			get { return defaultValue; }
			set {
				if (value!=defaultValue) {
					if (Value==defaultValue) Value = value;
					defaultValue = value;
				}
			}
		}
		
		protected CoverManager coverManager;
		public CoverManager CoverManager {
			get { return coverManager; }
			set {
				if (value!=coverManager) {
					if (coverManager!=null) {
						coverManager.NewCurrentCover -= HandleNewCurrentCover;
						coverManager.TargetIndexChanged -= HandleTargetIndexChanged;
						coverManager.CoversChanged -= HandleCoversChanged; 
					}
					coverManager = value;
					if (coverManager!=null) {
						coverManager.NewCurrentCover += HandleNewCurrentCover;
						coverManager.TargetIndexChanged += HandleTargetIndexChanged;
						coverManager.CoversChanged += HandleCoversChanged; 
					}
				}
			}
		}



		private Animation aFade = null;
		
		#endregion
		
		public CoverCaption (CoverManager coverManager, string font_name, Color color) : base (clutter_text_new ())
		{
			CoverManager = coverManager;
			Editable = false;
			Selectable = false;
			Activatable = false;
			CursorVisible = false;
			LineAlignment = Pango.Alignment.Center;
			FontName = font_name;
			SetColor (color);
		 	Value = defaultValue;

			UpdatePosition ();
		}

		#region Methods
		[DllImport("libclutter-glx-1.0.so.0")]
		static extern IntPtr clutter_text_new ();
		
		public void FadeOut ()
		{
			aFade = this.Animatev ((ulong) AnimationMode.Linear.value__, (uint) (CoverManager.MaxAnimationSpan*0.5f), new string[] { "opacity" }, new GLib.Value ((byte) 0));
		}

		public void FadeIn() 
		{
			EventHandler hFadeIn = delegate (object sender, EventArgs e) {
				this.Animatev ((ulong) AnimationMode.Linear.value__, (uint) (CoverManager.MaxAnimationSpan*0.5f), new string[] { "opacity" }, new GLib.Value ((byte) 255));
				aFade = null;
			};
			if (aFade!=null && aFade.Timeline.IsPlaying)
				aFade.Completed +=  hFadeIn;
			else
				hFadeIn (this, EventArgs.Empty);
		}

		public void Update ()
		{
			SetTextFromCover (coverManager.CurrentCover);
			UpdatePosition ();
		}
		
		public void UpdatePosition ()
		{
			if (Stage!=null) {
				SetAnchorPoint (Width*0.5f, Height*0.5f);
				SetPosition(coverManager.Behaviour.CenterX, Math.Max(coverManager.Behaviour.CenterY - coverManager.Behaviour.CoverWidth, Height*0.6f));
			}
		}

		protected void SetTextFromCover(ClutterFlowActor cover) 
		{
			if (cover!=null && cover.Label!="")
				Value = cover.Label;
			else
				Value = DefaultValue;
		}
		#endregion

		#region Event Handling
		protected void HandleNewCurrentCover (ClutterFlowActor cover, EventArgs e)
		{
			Update ();
			if (IsVisible) FadeIn ();
		}

		protected void HandleTargetIndexChanged (object sender, EventArgs e)
		{
			if (IsVisible) FadeOut ();
		}

		protected void HandleCoversChanged(object sender, EventArgs e)
		{
			Update ();
			if (IsVisible) FadeIn ();
		}
		#endregion
	}
}
