// Util.cs
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
using System.Reflection;

namespace OpenVP.GtkGui {
	public static class Util {
		public static T GetAttribute<T>(MemberInfo info,
		                                bool inherit) where T : Attribute {
			object[] attrs = info.GetCustomAttributes(typeof(T), inherit);
			
			if (attrs.Length == 0)
				return null;
			
			return (T) attrs[0];
		}
		
		public static Gdk.Color ConvertColor(OpenVP.Color color) {
			Gdk.Color conv = new Gdk.Color();
			
			conv.Red = (ushort) (color.Red * ushort.MaxValue);
			conv.Green = (ushort) (color.Green * ushort.MaxValue);
			conv.Blue = (ushort) (color.Blue * ushort.MaxValue);
			
			return conv;
		}
		
		public static OpenVP.Color ConvertColor(Gdk.Color color) {
			return new OpenVP.Color((float) color.Red / ushort.MaxValue,
			                        (float) color.Green / ushort.MaxValue,
			                        (float) color.Blue / ushort.MaxValue);
		}
	}
}
