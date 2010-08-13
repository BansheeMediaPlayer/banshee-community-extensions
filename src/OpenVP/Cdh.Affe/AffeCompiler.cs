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
using System.IO;
using System.Reflection;
using System.Reflection.Emit;

using Cdh.Affe.Tree;

namespace Cdh.Affe {
	public class LoopState {
		private Label mBeginLabel;
		
		public Label BeginLabel {
			get {
				return this.mBeginLabel;
			}
		}
		
		private Label mEndLabel;
		
		public Label EndLabel {
			get {
				return this.mEndLabel;
			}
		}
		
		public LoopState(Label begin, Label end) {
			this.mBeginLabel = begin;
			this.mEndLabel = end;
		}
	}
	
	public class AffeCompiler {
		private SymbolTable mTable = new SymbolTable();
		
		public SymbolTable SymbolTable {
			get {
				return this.mTable;
			}
		}
		
		private Type mHostType;
		
		public Type HostType {
			get {
				return this.mHostType;
			}
		}
		
		private FieldInfo mState;
		
		public AffeCompiler(Type host) {
			if (host == null)
				throw new ArgumentNullException("host");
			
			this.mHostType = host;
			
			this.FindFieldSymbols();
			this.FindMethodSymbols();
		}
		
		private void FindFieldSymbols() {
			foreach (FieldInfo f in this.mHostType.GetFields(BindingFlags.Instance |
															 BindingFlags.Static |
															 BindingFlags.Public |
															 BindingFlags.FlattenHierarchy)) {
				object[] attrs = f.GetCustomAttributes(typeof(AffeBoundAttribute), true);
				
				if (attrs.Length == 0)
					continue;
				
				if (f.FieldType == typeof(ScriptState)) {
					this.mState = f;
					continue;
				}
				
				AffeBoundAttribute attr = (AffeBoundAttribute) attrs[0];
				
				string name = (attr.Name == null) ? f.Name : attr.Name;
				
				this.mTable.AddSymbol(new FieldSymbol(name, f));
			}
		}
		
		private void FindMethodSymbols() {
			foreach (MethodInfo m in this.mHostType.GetMethods(BindingFlags.Instance |
															   BindingFlags.Static |
															   BindingFlags.Public |
															   BindingFlags.FlattenHierarchy)) {
				object[] bound = m.GetCustomAttributes(typeof(AffeBoundAttribute), true);
				object[] trans = m.GetCustomAttributes(typeof(AffeTransformAttribute), true);
				
				if (trans.Length != 0) {
					ParameterInfo[] ps = m.GetParameters();
					if (ps.Length != 2 ||
					    ps[0].ParameterType != typeof(AffeCompilerState) ||
					    ps[1].ParameterType != typeof(Expression[]))
						continue;
					
					string name = m.Name;
					if (bound.Length > 0) {
						AffeBoundAttribute battr = (AffeBoundAttribute) bound[0];
						if (battr.Name != null)
							name = battr.Name;
					}
					
					AffeTransformAttribute tattr = (AffeTransformAttribute) trans[0];
					
					this.mTable.AddSymbol(new TransformSymbol(name, m,
					                                          tattr.ResultType));
				} else {
					if (bound.Length == 0)
						continue;
					
					AffeBoundAttribute attr = (AffeBoundAttribute) bound[0];
					
					string name = (attr.Name == null) ? m.Name : attr.Name;
					
					this.mTable.AddSymbol(new MethodSymbol(name, m));
				}
			}
		}
		
		public DynamicMethod Compile(Block root) {
			return new AffeCompilerState(this.mHostType, this.mTable.Copy(),
			                             this.mState).Compile(root);
		}
		
		private Block Parse(AffeParser p) {
			try {
				return p.Parse();
			} catch (Exception e) {
				throw new AffeException("Exception at input position "
				                        + p.Lexer.TokenStartLocation,
				                        e, p.Lexer.TokenStartLocation);
			}
		}
		
		public DynamicMethod Compile(string source) {
			return this.Compile(this.Parse(new AffeParser(new Lexer(source))));
		}
		
		public DynamicMethod Compile(TextReader source) {
			return this.Compile(this.Parse(new AffeParser(new Lexer(source))));
		}
	}
	
	public class AffeException : Exception {
		private Node mRelatedNode;
		
		public Node RelatedNode {
			get {
				return this.mRelatedNode;
			}
		}
		
		private int mSourceLocation;
		
		public int SourceLocation {
			get {
				return this.mSourceLocation;
			}
		}
		
		public AffeException(string message, Exception inner, int location)
			: base(message, inner) {
			this.mRelatedNode = null;
			this.mSourceLocation = location;
		}
		
		public AffeException(string message, Node node) : base(message) {
			if (node == null)
				throw new ArgumentNullException("node");
			
			this.mRelatedNode = node;
			this.mSourceLocation = node.SourceLocation;
		}
		
		public AffeException(string message, Exception inner, Node node)
			: base(message, inner) {
			if (node == null)
				throw new ArgumentNullException("node");
			
			this.mRelatedNode = node;
			this.mSourceLocation = node.SourceLocation;
		}
		
		public override string ToString() {
			if (this.mRelatedNode == null)
				return base.ToString();
			
			return "Exception at input position " +
				this.mRelatedNode.SourceLocation + ": " + base.ToString();
		}
	}
	
	public class AffeCompilerState {
		private SymbolTable mGlobal;
		
		public SymbolTable GlobalSymbolTable {
			get {
				return this.mGlobal;
			}
		}
		
		private SymbolTableStack mScope = new SymbolTableStack();
		
		internal SymbolTableStack Scope {
			get {
				return this.mScope;
			}
		}
		
		private ILGenerator il = null;
		
		public ILGenerator ILGenerator {
			get {
				return this.il;
			}
		}
		
		private Type mHostType;
		
		public Type HostType {
			get {
				return this.mHostType;
			}
		}
		
		private List<LoopState> mLoopStack = new List<LoopState>();
		
		private Label mReturnLabel;
		
		public Label ReturnLabel {
			get {
				return this.mReturnLabel;
			}
		}
		
		private FieldInfo mState = null;
		
		private LocalBuilder mStateLocal = null;
		
		private static readonly MethodInfo mStateGet = typeof(ScriptState).GetMethod("GetValue");
		
		private static readonly MethodInfo mStateSet = typeof(ScriptState).GetMethod("SetValue");
		
		private List<VariableSymbol> mAutoVars = new List<VariableSymbol>();
		
		private List<LocalBuilder> mLocalBag = new List<LocalBuilder>();
		
		private static readonly MethodInfo mGetDefault = typeof(ScriptState).GetMethod("GetDefault");
		
		internal AffeCompilerState(Type host, SymbolTable table,
		                           FieldInfo state) {
			this.mHostType = host;
			this.mGlobal = table;
			this.mState = state;
			
			this.mScope.PushTable(table);
		}
		
		public LocalBuilder CheckOutLocal(Type type) {
			return this.CheckOutLocal(type, false);
		}
		
		public LocalBuilder CheckOutLocal(Type type, bool temporary) {
			LocalBuilder l;
			
			for (int i = 0; i < this.mLocalBag.Count; i++) {
				l = this.mLocalBag[i];
				
				if (l.LocalType == type) {
					if (!temporary)
						this.mLocalBag.RemoveAt(i);
					return l;
				}
			}
			
			l = this.il.DeclareLocal(type);
			if (temporary)
				this.mLocalBag.Add(l);
			
			return l;
		}
		
		public void CheckInLocal(LocalBuilder local) {
			this.mLocalBag.Add(local);
		}
		
		public void PushSymbolTable(SymbolTable s) {
			this.mScope.PushTable(s);
		}
		
		public void PopSymbolTable() {
			this.mScope.PopTable();
		}
		
		public LoopState GetLoopState(int outerdepth) {
			if (outerdepth >= this.mLoopStack.Count)
				throw new ArgumentOutOfRangeException("outerdepth");
			
			return this.mLoopStack[this.mLoopStack.Count - 1 - outerdepth];
		}
		
		public void PushLoopState(LoopState state) {
			this.mLoopStack.Add(state);
		}
		
		public void PopLoopState() {
			if (this.mLoopStack.Count == 0)
				throw new InvalidOperationException("Loop stack is empty.");
			
			this.mLoopStack.RemoveAt(this.mLoopStack.Count - 1);
		}
		
		internal DynamicMethod Compile(Block root) {
			DynamicMethod dm = new DynamicMethod("affeMethod", typeof(void),
			                                     new Type[] { this.mHostType },
			                                     this.mHostType);
			
			this.il = dm.GetILGenerator();
			
			root = (Block) root.Analyze(this);
			
			this.mReturnLabel = this.il.DefineLabel();
			
			if (this.mState != null) {
				this.mStateLocal = this.il.DeclareLocal(typeof(ScriptState));
				this.il.Emit(OpCodes.Ldarg_0);
				this.il.Emit(OpCodes.Ldfld, this.mState);
				this.il.Emit(OpCodes.Stloc, this.mStateLocal);
			}
			
			foreach (VariableSymbol vs in this.mAutoVars) {
				if (this.mStateLocal != null) {
					this.il.Emit(OpCodes.Ldloc, this.mStateLocal);
					this.il.Emit(OpCodes.Ldstr, vs.Name);
					this.il.Emit(OpCodes.Call,
					             mStateGet.MakeGenericMethod(new Type[] { vs.Type }));
				} else {
					this.il.Emit(OpCodes.Call, mGetDefault.MakeGenericMethod(new Type[] { vs.Type }));
				}
				
				this.il.Emit(OpCodes.Stloc, vs.Local);
			}
			
			if (this.mState != null)
				this.il.BeginExceptionBlock();
			
			root.Emit(this);
			
			if (this.mState != null) {
				this.il.BeginFinallyBlock();
				
				foreach (VariableSymbol vs in this.mAutoVars) {
					this.il.Emit(OpCodes.Ldloc, this.mStateLocal);
					this.il.Emit(OpCodes.Ldstr, vs.Name);
					this.il.Emit(OpCodes.Ldloc, vs.Local);
					
					if (vs.Local.LocalType.IsValueType)
						this.il.Emit(OpCodes.Box, vs.Local.LocalType);
					
					this.il.Emit(OpCodes.Call, mStateSet);
				}
				
				this.il.EndExceptionBlock();
			}
			
			this.il.MarkLabel(this.mReturnLabel);
			
			this.il.Emit(OpCodes.Ret);
			
			return dm;
		}
		
		public static Type FindCompatibleType(Type left, Type right) {
			if (left == right)
				return left;
			
			if (!left.IsValueType || !right.IsValueType) {
				if (left.IsAssignableFrom(right))
					return left;
				
				if (right.IsAssignableFrom(left))
					return right;
				
				// If we can't assign either direction (e.g. System.IO.Stream
				// and System.Int32), use object.
				return typeof(object);
			}
			
			if (IsI4(left) && IsI4(right))
				return typeof(int);
			
			if (left == typeof(double) || right == typeof(double))
				return typeof(double);
			
			return typeof(float);
		}
		
		internal Type CheckForTypeExpression(Expression e) {
			if (e is IdentifierExpression) {
				TypeSymbol ts = this.mScope.GetSymbol(((IdentifierExpression) e)
				                                      .Identifier.Name)
					as TypeSymbol;
				
				if (ts != null && ts.Type != typeof(void)) {
					// Setting the expression type means we don't have to
					// check for a TypeSymbol later, since the expression
					// is not actualy emitted for static calls.
					e.Type = ts.Type;
					return ts.Type;
				}
			}
			
			return null;
		}
		
		public static bool IsNumeric(Type type) {
			return type.IsPrimitive && type != typeof(IntPtr);
		}
		
		public static bool IsI4(Type type) {
			return ((type == typeof(bool)) ||
			        (type == typeof(byte)) ||
			        (type == typeof(sbyte)) ||
			        (type == typeof(short)) ||
			        (type == typeof(ushort)) ||
			        (type == typeof(int)) ||
			        (type == typeof(uint)) ||
			        (type == typeof(char)));
		}
		
		public Expression CastTo(Expression e, Type type) {
			if (e.Type == type)
				return e;
			
			if (type == typeof(void))
				throw new AffeException("Cannot cast to void.", e);
			
			if (e.Type == typeof(void))
				throw new AffeException("Cannot cast from void.", e);
			
			if (e is FloatExpression && type == typeof(string))
				return new StringExpression(((FloatExpression) e).Float.ToString());
			
			// More checking during the compile run.
			return (Expression) new CastExpression(e, type).AnalyzeShallow(this);
		}
		
		public void MakePersistent(VariableSymbol s) {
			if (!this.mAutoVars.Contains(s))
				this.mAutoVars.Add(s);
		}
		
		internal Symbol DefineLocal(Identifier i) {
			Symbol s = this.mScope.GetSymbol(i.Name);
			
			if (s != null)
				return s;
			
			VariableSymbol v = new VariableSymbol(i.Name, typeof(float));
			v.Local = this.CheckOutLocal(typeof(float));
			this.mGlobal.AddSymbol(v);
			this.mAutoVars.Add(v);
			
			return v;
		}
	}
	
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Method)]
	public class AffeBoundAttribute : Attribute {
		private string mName;
		
		public string Name {
			get {
				return this.mName;
			}
		}
		
		public AffeBoundAttribute() {
			this.mName = null;
		}
		
		public AffeBoundAttribute(string name) {
			if (name == null) {
				this.mName = null;
				return;
			}
			
			if (name.Length == 0)
				throw new ArgumentException("name.Length == 0");
			
			if (!Lexer.IsLetter(name[0]))
				throw new ArgumentException("Invalid identifier");
			
			for (int i = 1; i < name.Length; i++) {
				char c = name[i];
				if (!Lexer.IsLetter(c) && !Lexer.IsDigit(c))
					throw new ArgumentException("Invalid identifier");
			}
			
			this.mName = name;
		}
	}
	
	[AttributeUsage(AttributeTargets.Method)]
	public class AffeTransformAttribute : Attribute {
		private Type mResultType;
		
		public Type ResultType {
			get {
				return this.mResultType;
			}
		}
		
		public AffeTransformAttribute(Type resulttype) {
			if (resulttype == null)
				throw new ArgumentNullException("resulttype");
			
			this.mResultType = resulttype;
		}
	}
}
