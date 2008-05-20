// EffectSelectorDialog.cs
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
using System.Collections.Generic;
using Gtk;

namespace OpenVP.GtkGui {
	using EffectEntry = BrowsableItem<Type>;
	
	public partial class EffectSelectorDialog : Gtk.Dialog {
		private TreeStore mEffectStore = new TreeStore(typeof(object));
		
		public Type SelectedEffect {
			get {
				EffectEntry effect;
				if (this.GetSelectedEffectEntry(out effect))
					return effect.Item;
				
				return null;
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
		
		private bool GetSelectedEffectEntry(out EffectEntry entry) {
			entry = null;
			
			TreeIter i;
			
			if (!this.EffectList.Selection.GetSelected(out i))
				return false;
			
			entry = this.mEffectStore.GetValue(i, 0) as EffectEntry;
			
			return entry != null;
		}
		
		private void OnSelectionChanged(object o, EventArgs e) {
			EffectEntry effect;
			bool valid = this.GetSelectedEffectEntry(out effect);
			
			if (valid) {
				this.EffectLabel.Text = effect.DisplayName;
				this.DescriptionLabel.Text = effect.Description;
				this.AuthorLabel.Text = effect.Author;
				
				this.EffectInfoTable.Show();
			} else {
				this.EffectInfoTable.Hide();
				
				this.EffectLabel.Markup = "";
				this.DescriptionLabel.Markup = "";
				this.AuthorLabel.Markup = "";
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
			
			EffectEntry.Sort(effects);
			
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
