// UserScript.cs
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
using System.Runtime.Serialization;

namespace OpenVP.Scripting {
	[Serializable]
	public delegate void ScriptCall();
	
	[Serializable]
	public abstract class UserScript : IDeserializationCallback {
		private string mScript = null;
		
		public string Script {
			get {
				return this.mScript;
			}
			set {
				this.mScript = value;
				this.mDirty = true;
				this.mCall = null;
				
				if (this.MadeDirty != null)
					this.MadeDirty(this, EventArgs.Empty);
			}
		}
		
		[NonSerialized]
		private bool mDirty = true;
		
		[NonSerialized]
		private ScriptCall mCall = null;
		
		[field: NonSerialized]
		public event EventHandler MadeDirty;
		
		public ScriptCall Call {
			get {
				try {
					this.Compile();
				} catch (ScriptCompileException) {
					// Ignore compile errors here and silently return null.
				}
				
				return this.mCall;
			}
		}
		
		[NonSerialized]
		private ScriptCall mCallWrapper;
		
		public ScriptCall CallWrapper {
			get {
				return this.mCallWrapper;
			}
		}
		
		protected UserScript() {
			this.mCallWrapper = this.Run;
		}
		
		public void Run() {
			if (this.Call != null)
				this.Call();
		}
		
		public void Run(object sender, EventArgs args) {
			this.Run();
		}
		
		protected abstract ScriptCall CompileScript();
		
		public void Compile() {
			if (this.mDirty)
				this.Recompile();
		}
		
		public void Recompile() {
			this.mCall = this.CompileScript();
			this.mDirty = false;
		}
		
		void IDeserializationCallback.OnDeserialization(object sender) {
			this.OnDeserialization(sender);
		}
		
		protected virtual void OnDeserialization(object sender) {
			this.mDirty = true;
			this.mCall = null;
			
			this.mCallWrapper = this.Run;
		}
	}
}
