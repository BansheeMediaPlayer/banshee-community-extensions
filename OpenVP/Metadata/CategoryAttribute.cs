// CategoryAttribute.cs
//
//  Copyright (C) 2008 Chris Howie
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

namespace OpenVP.Metadata {
	/// <summary>
	/// Specifies a category for the target.
	/// </summary>
	public class CategoryAttribute : Attribute {
		private string mCategory;
		
		/// <value>
		/// The category.
		/// </value>
		public string Category {
			get { return this.mCategory; }
		}
		
		/// <summary>
		/// Create a CategoryAttribute.
		/// </summary>
		/// <param name="category">
		/// The category of a target.
		/// </param>
		public CategoryAttribute(string category) {
			this.mCategory = category;
		}
	}
}
