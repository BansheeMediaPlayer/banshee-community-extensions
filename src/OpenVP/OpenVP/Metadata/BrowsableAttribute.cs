// BrowsableAttribute.cs
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

namespace OpenVP.Metadata {
	/// <summary>
	/// Marks the specified target as browsable in a designer.
	/// </summary>
	/// <remarks>
	/// If this attribute is not present, the browasble status of the class is
	/// not specified.  It would be up to the implementor to decide whether or
	/// not to show targets not so marked.
	/// </remarks>
	public class BrowsableAttribute : Attribute {
		private bool mBrowsable;
		
		/// <value>
		/// Whether or not the target is browsable.
		/// </value>
		public bool Browsable {
			get { return this.mBrowsable; }
		}
		
		/// <summary>
		/// Create a BrowsableAttribute.
		/// </summary>
		/// <param name="browsable">
		/// Whether or not the target is browsable.
		/// </param>
		public BrowsableAttribute(bool browsable) {
			this.mBrowsable = browsable;
		}
	}
}
