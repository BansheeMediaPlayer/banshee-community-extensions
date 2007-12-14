// EffectSelectorDialog.cs
//
//  Copyright (C) 2007 Chris Howie
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
//
//

using System;
using System.Collections.Generic;
using Gtk;

namespace OpenVP.GtkGui {
	using EffectEntry = BrowsableItem<Type>;
	
	public partial class EffectSelectorDialog : Gtk.Dialog {
		private TreeStore mEffectStore = new TreeStore(typeof(object));
		
		public Type SelectedEffect {
			get {
				TreeIter i;
				
				if (!this.EffectList.Selection.GetSelected(out i))
					return null;
				
				return ((EffectEntry) this.mEffectStore.GetValue(i, 0)).Item;
			}
		}
		
		public EffectSelectorDialog() {
			this.Build();
			
			this.PopulateStore();
			
			this.EffectList.AppendColumn("Name", new CellRendererText(),
			                             new TreeCellDataFunc(NameFunc));
			this.EffectList.Model = this.mEffectStore;
			this.EffectList.Selection.Changed += this.OnSelectionChanged;
			
			this.EffectList.ExpandAll();
		}
		
		private void OnSelectionChanged(object o, EventArgs e) {
			TreeIter i;
			bool valid;
			
			if (!this.EffectList.Selection.GetSelected(out i)) {
				valid = false;
			} else {
				valid = this.mEffectStore.GetValue(i, 0) is EffectEntry;
			}
			
			this.buttonOk.Sensitive = valid;
		}
		
		private void PopulateStore() {
			List<EffectEntry> effects = new List<EffectEntry>();
			
			foreach (Type i in Registry.EffectTypes) {
				EffectEntry entry = EffectEntry.Create(i);
				
				if (entry != null)
					effects.Add(entry);
			}
			
			effects.Sort(EffectEntry.Sorter);
			
			string category = null;
			
			TreeIter node = TreeIter.Zero;
			
			foreach (EffectEntry i in effects) {
				if (i.Category != category) {
					category = i.Category;
					
					node = this.mEffectStore.AppendValues(new CategoryDivider(category));
				}
				
				this.mEffectStore.AppendValues(node, i);
			}
		}
		
		private static void NameFunc(TreeViewColumn col, CellRenderer r,
		                             TreeModel m, TreeIter i) {
			object val = m.GetValue(i, 0);
			CellRendererText text = (CellRendererText) r;
			
			if (val is CategoryDivider)
				text.Markup = "<b>" + ((CategoryDivider) val).Category + "</b>";
			else if (val is EffectEntry)
				text.Text = ((EffectEntry) val).DisplayName;
		}
		
		protected virtual void OnEffectListRowActivated(object o, Gtk.RowActivatedArgs args) {
			if (this.buttonOk.Sensitive)
				this.buttonOk.Click();
		}
		
		// strings don't store well in a TreeStore...
		private class CategoryDivider {
			public readonly string Category;
			
			public CategoryDivider(string category) {
				this.Category = category;
			}
		}
	}
}
