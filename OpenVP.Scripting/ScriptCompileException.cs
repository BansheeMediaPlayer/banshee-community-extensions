// ScriptCompileException.cs
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

namespace OpenVP.Scripting {
	public class ScriptCompileException : Exception {
		private int mPosition;
		
		public int Position {
			get {
				return this.mPosition;
			}
		}
		
		public ScriptCompileException(string message, int position) : base(message) {
			this.mPosition = position;
		}
		
		public ScriptCompileException(string message, int position,
		                              Exception inner) : base(message, inner) {
			this.mPosition = position;
		}
	}
}
