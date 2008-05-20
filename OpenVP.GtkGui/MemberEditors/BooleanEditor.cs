// BooleanEditor.cs
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
	public class BooleanEditor : MemberEditor {
		private CheckButton mCheck;
		
		public BooleanEditor(object @object, PropertyInfo info) : base(@object, info) {
			this.mCheck = new CheckButton();
			this.mCheck.Show();
			this.Add(this.mCheck);
			
			this.Revert();
			
			this.mCheck.Toggled += this.OnCheckToggled;
		}
		
		private void OnCheckToggled(object o, EventArgs e) {
			this.FireMadeDirty();
		}
		
		public override void Apply() {
			this.PropertyInfo.SetValue(this.Object, this.mCheck.Active, null);
			
			this.FireApplied();
			this.FireMadeClean();
		}
		
		public override void Revert() {
			this.mCheck.Active = (bool) this.PropertyInfo.GetValue(this.Object, null);
			
			this.FireMadeClean();
		}
	}
}
