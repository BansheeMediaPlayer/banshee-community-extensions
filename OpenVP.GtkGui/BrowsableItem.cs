// BrowsableItem.cs
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
using System.Reflection;

using OpenVP.Metadata;

namespace OpenVP.GtkGui {
	public class BrowsableItem<T> where T : MemberInfo {
		public readonly T Item;
		
		public readonly string DisplayName;
		
		public readonly string Category;
		
		public readonly string Description;
		
		public readonly string Follows;
		
		public readonly string Author;
		
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
			
			FollowsAttribute follows = Util.GetAttribute<FollowsAttribute>(item, false);
			
			if (follows != null)
				this.Follows = follows.Follows;
			else
				this.Follows = null;
			
			AuthorAttribute author = Util.GetAttribute<AuthorAttribute>(item, false);
			
			if (author != null)
				this.Author = author.Author;
			else
				this.Author = null;
		}
		
		public static BrowsableItem<T> Create(T item) {
			BrowsableAttribute browsable = Util.GetAttribute<BrowsableAttribute>(item, false);
			
			if (browsable != null && !browsable.Browsable)
				return null;
			
			return new BrowsableItem<T>(item);
		}
		
		public static void Sort(BrowsableItem<T>[] array) {
			Array.Sort(array, Sorter);
			ApplyFollows(array);
		}
		
		public static void Sort(List<BrowsableItem<T>> array) {
			array.Sort(Sorter);
			ApplyFollows(array);
		}
		
		private static void ApplyFollows(IList<BrowsableItem<T>> array) {
			if (array.Count == 0)
				return;
			
			while (array[0].Follows != null) {
				BrowsableItem<T> item = array[0];
				
				bool found = false;
				
				for (int i = 1; i < array.Count; i++) {
					// Shift up.
					array[i - 1] = array[i];
					
					// This is where this one goes.
					if (array[i].Item.Name == item.Follows) {
						array[i] = item;
						found = true;
						break;
					}
				}
				
				// We didn't find it, stick this one on the end.
				if (!found)
					array[array.Count - 1] = item;
			}
		}
		
		private static int Sorter(BrowsableItem<T> a, BrowsableItem<T> b) {
			// Push items with FollowsAttribute to the top for quicker
			// post-sort arrangement.
			if (a.Follows != null && b.Follows == null)
				return -1;
			
			if (a.Follows == null && b.Follows != null)
				return 1;
			
			int order;
			
			order = a.Category.CompareTo(b.Category);
			if (order != 0)
				return order;
			
			return a.DisplayName.CompareTo(b.DisplayName);
		}
	}
}
