// EffectEditor.cs
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
using System.ComponentModel;
using System.Reflection;
using Gtk;
using OpenVP;

namespace OpenVP.GtkGui {
	using EffectMember = BrowsableItem<PropertyInfo>;
	
	public partial class EffectEditor : Gtk.Bin {
		private Effect mEffect;
		
		private List<EffectMember> mMembers = new List<EffectMember>();
		
		public EffectEditor(Effect effect) {
			Stetic.BinContainer.Attach(this);
			
			this.mEffect = effect;
			
			this.BuildMemberList();
			this.BuildInterface();
		}
		
		private void BuildMemberList() {
			Type type = this.mEffect.GetType();
			
			foreach (PropertyInfo i in type.GetProperties(BindingFlags.Instance |
			                                              BindingFlags.Public)) {
				EffectMember member = EffectMember.Create(i);
				
				if (member != null)
					this.mMembers.Add(member);
			}
			
			this.mMembers.Sort(EffectMember.Sorter);
		}
		
		private void BuildInterface() {
			string category = null;
			
			uint count = 0;
			
			foreach (EffectMember i in this.mMembers) {
				if (i.Category != category) {
					category = i.Category;
					count++;
				}
				
				count++;
			}
			
			category = null;
			
			Table table = new Table(count, 2, false);
			
			uint row = 0;
			
			foreach (EffectMember i in this.mMembers) {
				Label l;
				
				if (i.Category != category) {
					category = i.Category;
					
					l = new Label();
					l.Markup = "<b>" + category + "</b>";
					l.Show();
					
					table.Attach(l, 0, 2, row, ++row,
					             AttachOptions.Expand | AttachOptions.Fill,
					             AttachOptions.Shrink, 5, 5);
				}
				
				l = new Label(i.DisplayName);
				l.Show();
				table.Attach(l, 0, 1, row, row + 1, AttachOptions.Fill,
				             AttachOptions.Shrink, 5, 5);
				
				Widget w = this.GetEditor(i);
				w.Show();
				table.Attach(w, 1, 2, row, ++row,
				             AttachOptions.Fill | AttachOptions.Expand,
				             AttachOptions.Shrink, 5, 5);
			}
			
			this.Add(table);
			table.Show();
		}
		
		private Widget GetEditor(EffectMember member) {
			return new Entry();
		}
	}
}
