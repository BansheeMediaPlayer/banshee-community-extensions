// StringEditor.cs
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
	public class StringEditor : MemberEditor {
		private Entry mEntry;
		
		public StringEditor(object @object, PropertyInfo info) : base(@object, info) {
			this.mEntry = new Entry();
			
			this.mEntry.Show();
			this.Add(this.mEntry);
			
			this.mEntry.Changed += this.OnEntryChanged;
			this.mEntry.Activated += this.OnEntryActivated;
			
			this.Revert();
		}
		
		private void OnEntryChanged(object o, EventArgs e) {
			this.FireMadeDirty();
		}
		
		private void OnEntryActivated(object o, EventArgs e) {
			this.Apply();
		}
		
		public override void Revert() {
			string val = (string) this.PropertyInfo.GetValue(this.Object, null);
			
			if (val == null)
				val = "";
			
			this.mEntry.Text = val;
			
			this.FireMadeClean();
		}
		
		public override void Apply() {
			this.PropertyInfo.SetValue(this.Object, this.mEntry.Text, null);
			
			this.FireApplied();
			this.FireMadeClean();
		}
	}
}
