// Mirror.cs
//
//  Copyright (C) 2008 Chris Howie
//
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 3 of the License, or (at your option) any later version.
//
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
// Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA 
//
//

using System;

using OpenVP.Metadata;

using Tao.OpenGl;

namespace OpenVP.Core {
	[Serializable, Browsable(true), DisplayName("Mirror"),
	 Category("Transform"), Description("Mirror the buffer."),
	 Author("Chris Howie")]
	public class Mirror : MovementBase {
		public enum HorizontalMirrorType : byte {
			[DisplayName("None")] None,
			[DisplayName("Copy left to right")] LeftToRight,
			[DisplayName("Copy right to left")] RightToLeft
		}
		
		public enum VerticalMirrorType : byte {
			[DisplayName("None")] None,
			[DisplayName("Copy top to bottom")] TopToBottom,
			[DisplayName("Copy bottom to top")] BottomToTop
		}
		
		private HorizontalMirrorType mHorizontalMirror = HorizontalMirrorType.LeftToRight;
		
		[Browsable(true), DisplayName("Horizontal"), Category("Mirror"),
		 DefaultValue(HorizontalMirrorType.LeftToRight),
		 Description("How to mirror about the Y axis.")]
		public HorizontalMirrorType HorizontalMirror {
			get { return this.mHorizontalMirror; }
			set {
				this.mHorizontalMirror = value;
				this.MakeStaticDirty();
			}
		}
		
		private VerticalMirrorType mVerticalMirror = VerticalMirrorType.TopToBottom;
		
		[Browsable(true), DisplayName("Vertical"), Category("Mirror"),
		 DefaultValue(VerticalMirrorType.TopToBottom),
		 Description("How to mirror about the X axis.")]
		public VerticalMirrorType VerticalMirror {
			get { return this.mVerticalMirror; }
			set {
				this.mVerticalMirror = value;
				this.MakeStaticDirty();
			}
		}
		
		public Mirror() {
			this.XResolution = 3;
			this.YResolution = 3;
			this.Static = true;
		}
		
		protected override void PlotVertex(MovementData data) {
			data.Method = MovementMethod.Rectangular;
			
			switch (this.mHorizontalMirror) {
			case HorizontalMirrorType.LeftToRight:
				if (data.X == 1)
					data.X = 0;
				
				break;
				
			case HorizontalMirrorType.RightToLeft:
				if (data.X == 0)
					data.X = 1;
				
				break;
			}
			
			switch (this.mVerticalMirror) {
			case VerticalMirrorType.TopToBottom:
				if (data.Y == 0)
					data.Y = 1;
				
				break;
				
			case VerticalMirrorType.BottomToTop:
				if (data.Y == 1)
					data.Y = 0;
				
				break;
			}
		}
	}
}
