// FollowsAttribute.cs
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

namespace OpenVP.Metadata {
	/// <summary>
	/// Specifies that the member or class this attribute is applied to should
	/// be displayed after a specific class or member.
	/// </summary>
	/// <remarks>
	/// This can be used to break the default alphabetic sort and ensure that
	/// some series of classes or members are always displayed in a certain
	/// order.
	/// </remarks>
	[AttributeUsage(AttributeTargets.All)]
	public class FollowsAttribute : Attribute {
		private string mFollows;
		
		/// <value>
		/// The name of the class or member that this one should follow.
		/// </value>
		/// <remarks>
		/// This refers to the actual class or member name, not its display
		/// name.
		/// </remarks>
		public string Follows {
			get {
				return this.mFollows;
			}
		}
		
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="follows">
		/// The name of the class or member that this one should follow.
		/// </param>
		public FollowsAttribute(string follows) {
			if (follows == null)
				throw new ArgumentNullException();
			
			this.mFollows = follows;
		}
	}
}
