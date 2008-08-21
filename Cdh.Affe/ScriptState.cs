// Cdh.Affe: Affe language compiler.
// Copyright (C) 2007  Chris Howie
// 
// This library is free software; you can redistribute it and/or
// Modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// Version 2.1 of the License, or (at your option) any later version.
// 
// This library is distributed in the hope that it will be useful,
// But WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA

using System;
using System.Collections.Generic;

namespace Cdh.Affe {
	public sealed class ScriptState {
		private Dictionary<string, object> mState =
			new Dictionary<string, object>();
		
		public ScriptState() {
		}
		
		public void Clear() {
			this.mState.Clear();
		}
		
		public T GetValue<T>(string key) {
			object v;
			
			if (this.mState.TryGetValue(key, out v)) {
				if (v is T)
					return (T) v;
			}
			
			return default(T);
		}
		
		public void SetValue(string key, object v) {
			this.mState[key] = v;
		}
		
		public static T GetDefault<T>() {
			return default(T);
		}
	}
}
