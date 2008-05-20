// ColorEditor.cs
//
//  Copyright (C) 2007-2008 Chris Howie
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
using System.Reflection;
using Gtk;

namespace OpenVP.GtkGui.MemberEditors {
	public class ColorEditor : MemberEditor {
		private ColorButton mColorButton;
		
		private HScale mAlphaScale;
		
		public ColorEditor(object @object, PropertyInfo info) : base(@object, info) {
			HBox box = new HBox();
			box.Spacing = 6;
			box.Show();
			
			this.mColorButton = new ColorButton();
			this.mColorButton.Show();
			box.PackStart(this.mColorButton, false, true, 0);
			
			this.mAlphaScale = new HScale(0, 1, 0.0001);
			this.mAlphaScale.Show();
			box.PackStart(this.mAlphaScale, true, true, 0);
			
			this.Add(box);
			
			this.Revert();
			
			this.mColorButton.ColorSet += this.OnDirtyAction;
			this.mAlphaScale.ValueChanged += this.OnDirtyAction;
		}
		
		private void OnDirtyAction(object o, EventArgs e) {
			this.FireMadeDirty();
		}
		
		public override void Revert() {
			Color color = (Color) this.PropertyInfo.GetValue(this.Object, null);
			
			this.mColorButton.Color = Util.ConvertColor(color);
			this.mAlphaScale.Value = color.Alpha;
			
			this.FireMadeClean();
		}
		
		public override void Apply() {
			Color color = Util.ConvertColor(this.mColorButton.Color);
			
			color.Alpha = (float) this.mAlphaScale.Value;
			
			this.PropertyInfo.SetValue(this.Object, color, null);
			
			this.FireApplied();
			this.FireMadeClean();
		}
	}
}
