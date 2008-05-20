// EnumEditor.cs
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
using System.Reflection;
using OpenVP.Metadata;
using Gtk;

namespace OpenVP.GtkGui.MemberEditors {
	public class EnumEditor : MemberEditor {
		private ComboBox mCombo;
		
		private ListStore mStore = new ListStore(typeof(EnumValue));
		
		public EnumEditor(object @object, PropertyInfo info) : base(@object, info) {
			Type type = info.PropertyType;
			
			foreach (FieldInfo fi in type.GetFields()) {
				if (!fi.IsStatic || fi.FieldType != type)
					continue;
				
				string name = fi.Name;
				
				DisplayNameAttribute dispname = Util.GetAttribute<DisplayNameAttribute>(fi, false);
				if (dispname != null)
					name = dispname.DisplayName;
				
				object value = fi.GetValue(null);
				
				this.mStore.AppendValues(new EnumValue(value, name));
			}
			
			this.mCombo = new ComboBox(this.mStore);
			CellRendererText renderer = new CellRendererText();
			this.mCombo.PackStart(renderer, true);
			this.mCombo.SetCellDataFunc(renderer, ComboFunc);
			
			this.mCombo.Show();
			this.Add(this.mCombo);
			
			this.mCombo.Changed += this.OnComboChanged;
			
			this.Revert();
		}
		
		private void OnComboChanged(object o, EventArgs e) {
			this.FireMadeDirty();
		}
		
		private static void ComboFunc(CellLayout layout, CellRenderer r, TreeModel m, TreeIter i) {
			((CellRendererText) r).Text = ((EnumValue) m.GetValue(i, 0)).Name;
		}
		
		public override void Revert() {
			object value = this.PropertyInfo.GetValue(this.Object, null);
			
			TreeIter i;
			TreeIter target = TreeIter.Zero;
			bool found = false;
			
			if (this.mStore.GetIterFirst(out i)) {
				do {
					EnumValue v = (EnumValue) this.mStore.GetValue(i, 0);
					if (v.Value.Equals(value)) {
						target = i;
						found = true;
						break;
					}
				} while (this.mStore.IterNext(ref i));
			}
			
			if (found)
				this.mCombo.SetActiveIter(target);
			else
				this.mCombo.Active = -1;
			
			this.FireMadeClean();
		}
		
		public override void Apply() {
			TreeIter i;
			if (this.mCombo.GetActiveIter(out i)) {
				EnumValue v = (EnumValue) this.mStore.GetValue(i, 0);
				
				this.PropertyInfo.SetValue(this.Object, v.Value, null);
			}
			
			this.FireMadeClean();
		}
		
		private class EnumValue {
			public readonly object Value;
			public readonly string Name;
			
			public EnumValue(object value, string name) {
				this.Value = value;
				this.Name = name;
			}
		}
	}
}
