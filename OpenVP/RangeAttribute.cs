// RangeAttribute.cs
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

namespace OpenVP {
	/// <summary>
	/// Specifies the range of acceptable values for numeric members.
	/// </summary>
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
	public class RangeAttribute : Attribute {
		private double mMinimum;
		
		/// <value>
		/// The minimum accepted value, inclusive.
		/// </value>
		public double Minimum {
			get {
				return this.mMinimum;
			}
		}
		
		private double mMaximum;
		
		/// <value>
		/// The maximum accepted value, inclusive.
		/// </value>
		public double Maximum {
			get {
				return this.mMaximum;
			}
		}
		
		/// <summary>
		/// Specifies the range of acceptable values for numeric members.
		/// </summary>
		/// <param name="min">
		/// The minimum accepted value, inclusive.
		/// </param>
		/// <param name="max">
		/// The maximum accepted value, inclusive.
		/// </param>
		public RangeAttribute(double min, double max) {
			this.mMinimum = min;
			this.mMaximum = max;
		}
	}
}
