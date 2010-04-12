// AffeScript.cs
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
using System.Reflection.Emit;
using System.Runtime.Serialization;
using Cdh.Affe;

namespace OpenVP.Scripting {
	[Serializable]
	public class AffeScript : UserScript {
		[NonSerialized]
		private AffeCompiler mCompiler = null;
		
		public AffeCompiler Compiler {
			get {
				return this.mCompiler;
			}
			set {
				this.mCompiler = value;
			}
		}
		
		private object mTargetObject = null;
		
		public object TargetObject {
			get {
				return this.mTargetObject;
			}
			set {
				this.mTargetObject = value;
			}
		}
		
		public AffeScript() {
		}
		
		public AffeScript(AffeCompiler compiler, object target) {
			this.mCompiler = compiler;
			this.mTargetObject = target;
		}
		
		protected override ScriptCall CompileScript() {
			string script = this.Script;
			if (script == null)
				script = "";
			
			DynamicMethod dm;
			try {
				dm = this.mCompiler.Compile(script);
			} catch (AffeException e) {
				throw new ScriptCompileException(e.RelatedNode == null ?
				                                 "Syntax error" :
				                                 e.Message, e.SourceLocation,
				                                 e);
			}
			
			return (ScriptCall) dm.CreateDelegate(typeof(ScriptCall),
			                                      this.mTargetObject);
		}
		
		protected virtual void SetupCompiler() {
			if (this.mTargetObject != null)
				this.mCompiler = new AffeCompiler(this.mTargetObject.GetType());
			else
				this.mCompiler = null;
		}
		
		protected override void OnDeserialization(object sender) {
			base.OnDeserialization(sender);
			
			this.SetupCompiler();
		}
	}
}
