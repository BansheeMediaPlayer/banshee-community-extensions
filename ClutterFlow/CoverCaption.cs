
using System;
using Clutter;

namespace ClutterFlow
{
	
	
	public class CoverCaption : Caption
	{
		#region Fields
		protected string defaultValue = "Unkown Artist\nUnkown Album";
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
		#endregion
		
		public CoverCaption (CoverManager coverManager, string font_name, Color color) : base (coverManager, font_name, color)
		{
		}

		#region Methods

		public override void Update ()
		{
			SetTextFromCover (coverManager.CurrentCover);
			base.Update ();
		}
		
		public override void UpdatePosition ()
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
		protected virtual void HandleNewCurrentCover (ClutterFlowActor cover, EventArgs e)
		{
			if (Opacity>0) FadeOut ();
			Update ();
			if (IsVisible) FadeIn ();
		}

		protected virtual void HandleTargetIndexChanged (object sender, EventArgs e)
		{
			if (IsVisible) FadeOut ();
		}

		protected virtual void HandleCoversChanged(object sender, EventArgs e)
		{
			Update ();
			if (IsVisible) FadeIn ();
		}
		#endregion
	}
}
