
using System;

namespace Banshee.ClutterFlow
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
		
        protected void HandleCoversChanged (object sender, EventArgs e)
        {
        	UpdateBounds (coverManager.TotalCovers, coverManager.TargetIndex);
        }
		
		bool ignoreTargetIndexOnce = false;
		protected void HandleTargetIndexChanged(object sender, EventArgs e)
		{
			if (!ignoreTargetIndexOnce)
				HandlePostionFromIndex = coverManager.TargetIndex;
			ignoreTargetIndexOnce = false;
		}
		
		void HandleSliderHasMoved(object sender, EventArgs e)
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
