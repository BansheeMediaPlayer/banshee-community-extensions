// Registry.cs
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

namespace OpenVP {
	/// <summary>
	/// Registry of various extendable types.
	/// </summary>
	/// <remarks>
	/// This class tracks which loaded types subclass <see cref="Effect"/> and
	/// <see cref="PlayerData"/> so that UIs can present a list of such types.
	/// </remarks>
	public static class Registry {
		private static List<Type> mEffectTypes = new List<Type>();
		
		private static List<Type> mPlayerDataTypes = new List<Type>();
		
		private static List<Assembly> mAssemblies = new List<Assembly>();
		
		/// <value>
		/// An object enumerating loaded types that subclass
		/// <see cref="Effect"/>, are not abstract, and have a public
		/// parameterless constructor.
		/// </value>
		public static IEnumerable<Type> EffectTypes {
			get {
				return mEffectTypes;
			}
		}
		
		/// <value>
		/// An object enumerating loaded types that subclass
		/// <see cref="PlayerData"/>, are not abstract, and have a public
		/// parameterless constructor.
		/// </value>
		public static IEnumerable<Type> PlayerDataTypes {
			get {
				return mPlayerDataTypes;
			}
		}
		
		/// <summary>
		/// Update the list of types.
		/// </summary>
		/// <remarks>
		/// This method checks all types in the current AppDomain that have not
		/// been previously checked.
		/// </remarks>
		public static void Update() {
			foreach (Assembly i in AppDomain.CurrentDomain.GetAssemblies()) {
				if (mAssemblies.Contains(i))
					continue;
				
				mAssemblies.Add(i);
				
				foreach (Type t in i.GetTypes()) {
					if (t.IsAbstract)
						continue;
					
					if (t.GetConstructor(Type.EmptyTypes) == null)
						continue;
					
					if (t.IsSubclassOf(typeof(Effect)))
						mEffectTypes.Add(t);
					else if (t.IsSubclassOf(typeof(PlayerData)))
						mPlayerDataTypes.Add(t);
				}
			}
		}
	}
}
