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
using System.Reflection;
using System.Reflection.Emit;

namespace Cdh.Affe {
	public abstract class Symbol {
		private string mName;
		
		public string Name {
			get {
				return this.mName;
			}
		}
		
		protected Symbol(string name) {
			if (name == null)
				throw new ArgumentNullException("name");
			
			this.mName = name;
		}
	}
	
	public sealed class VariableSymbol : Symbol {
		private Type mType;
		
		public Type Type {
			get {
				return this.mType;
			}
		}
		
		private LocalBuilder mLocal = null;
		
		public LocalBuilder Local {
			get {
				return this.mLocal;
			}
			set {
				this.mLocal = value;
			}
		}
		
		public VariableSymbol(string name, Type type) : base(name) {
			this.mType = type;
		}
	}
	
	public sealed class FieldSymbol : Symbol {
		private FieldInfo mField;
		
		public FieldInfo Field {
			get {
				return this.mField;
			}
		}
		
		public FieldSymbol(string name, FieldInfo field) : base(name) {
			this.mField = field;
		}
	}
	
	public sealed class MethodSymbol : Symbol {
		private MethodInfo mMethod;
		
		public MethodInfo Method {
			get {
				return this.mMethod;
			}
		}
		
		public MethodSymbol(string name, MethodInfo method) : base(name) {
			if (method == null)
				throw new ArgumentNullException("method");
			
			this.mMethod = method;
		}
	}
	
	public sealed class TransformSymbol : Symbol {
		private MethodInfo mMethod;
		
		public MethodInfo Method {
			get {
				return this.mMethod;
			}
		}
		
		private Type mResultType;
		
		public Type ResultType {
			get {
				return this.mResultType;
			}
		}
		
		public TransformSymbol(string name, MethodInfo method, Type resulttype)
			: base(name) {
			if (method == null)
				throw new ArgumentNullException("method");
			
			if (resulttype == null)
				throw new ArgumentNullException("resulttype");
			
			this.mMethod = method;
			this.mResultType = resulttype;
		}
	}
	
	public sealed class TypeSymbol : Symbol {
		private Type mType;
		
		public Type Type {
			get {
				return this.mType;
			}
		}
		
		public TypeSymbol(string name, Type type) : base(name) {
			if (type == null)
				throw new ArgumentNullException("type");
			
			this.mType = type;
		}
	}
}
