// 
// ClutterFlowSlider.cs
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


using ClutterFlow;
using System;

namespace ClutterFlow.Slider
{
	
	
	public class ClutterFlowSlider : ClutterSlider
	{
		
		protected CoverManager coverManager;
		public CoverManager CoverManager {
			get { return CoverManager; }
			set {
				if (value!=coverManager) {
					if (coverManager!=null) {
						coverManager.CoversChanged -= HandleCoversChanged;
						coverManager.TargetIndexChanged -= HandleTargetIndexChanged;
					}
					coverManager = value;
					if (coverManager!=null) {
						coverManager.CoversChanged += HandleCoversChanged;
						coverManager.TargetIndexChanged += HandleTargetIndexChanged;
					}
				}
			}
		}
	
		public ClutterFlowSlider (float width, float height, CoverManager coverManager) : base (width, height)
		{
			this.CoverManager = coverManager;
			this.SliderHasChanged += HandleSliderHasChanged;
			this.SliderHasMoved += HandleSliderHasMoved;
		}

		public override void Update ()
		{
			if (Stage!=null) {
				SetAnchorPoint(Width*0.5f, Height*0.5f);
				SetPosition(coverManager.Behaviour.CenterX, Math.Min(coverManager.Behaviour.CenterY + coverManager.Behaviour.CoverWidth * 1.25f, Stage.Height - Height));
			}
			base.Update ();
		}
		
        protected void HandleCoversChanged (object sender, EventArgs e)
        {
        	UpdateBounds (coverManager.TotalCovers, coverManager.TargetIndex);
        }
		
		bool ignoreTargetIndexOnce = false;
		protected void HandleTargetIndexChanged(object sender, EventArgs e)
		{
			if (coverManager.TargetActor!=null)
				this.handle.Button.Label = coverManager.TargetActor.SortLabel.ToUpper ().Substring (0,1);
			else
				this.handle.Button.Label = "?";
			if (!ignoreTargetIndexOnce)
				HandlePostionFromIndex = coverManager.TargetIndex;
			ignoreTargetIndexOnce = false;
		}
		
		protected void HandleSliderHasMoved(object sender, EventArgs e)
		{
			ignoreTargetIndexOnce = true;
			coverManager.TargetIndex = HandlePostionFromIndex;
		}
		
		protected void HandleSliderHasChanged(object sender, EventArgs e)
		{
			coverManager.TargetIndex = HandlePostionFromIndex;
		}
	}
}
