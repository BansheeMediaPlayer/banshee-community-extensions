// BrowsableItem.cs
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
using System.ComponentModel;
using System.Reflection;

namespace OpenVP.GtkGui {
	public class BrowsableItem<T> where T : MemberInfo {
		public readonly T Item;
		
		public readonly string DisplayName;
		
		public readonly string Category;
		
		public readonly string Description;
		
		private BrowsableItem(T item) {
			this.Item = item;
			
			DisplayNameAttribute dn = Util.GetAttribute<DisplayNameAttribute>(item, false);
			
			if (dn != null)
				this.DisplayName = dn.DisplayName;
			else
				this.DisplayName = item.Name;
			
			CategoryAttribute cat = Util.GetAttribute<CategoryAttribute>(item, false);
			
			if (cat != null)
				this.Category = cat.Category;
			else
				this.Category = "(None)";
			
			DescriptionAttribute desc = Util.GetAttribute<DescriptionAttribute>(item, false);
			
			if (desc != null)
				this.Description = desc.Description;
			else
				this.Description = string.Empty;
		}
		
		public static BrowsableItem<T> Create(T item) {
			BrowsableAttribute browsable = Util.GetAttribute<BrowsableAttribute>(item, false);
			
			if (browsable != null && !browsable.Browsable)
				return null;
			
			return new BrowsableItem<T>(item);
		}
		
		public static int Sorter(BrowsableItem<T> a, BrowsableItem<T> b) {
			int order;
			
			order = a.Category.CompareTo(b.Category);
			if (order != 0)
				return order;
			
			return a.DisplayName.CompareTo(b.DisplayName);
		}
	}
}
