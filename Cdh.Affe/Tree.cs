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
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace Cdh.Affe.Tree {
	public enum Operator {
		Add,
		Minus,
		Multiply,
		Divide,
		Mod,
		And,
		Or,
		
		Lt,
		Gt,
		Lte,
		Gte,
		Eq,
		Ne,
		Bor,
		Band,
		
		// Unary
		Neg,
		Not
	}
	
	public abstract class Node {
		private int mSourceLocation;
		
		public int SourceLocation {
			get {
				return this.mSourceLocation;
			}
			set {
				this.mSourceLocation = value;
			}
		}
		
		public virtual Node Analyze(AffeCompilerState state) {
			return this;
		}
		
		public virtual void Emit(AffeCompilerState state) {
		}
	}
	
	public class Block : Statement {
		private List<Statement> mStatements;
		
		public List<Statement> Statements {
			get {
				return this.mStatements;
			}
		}
		
		private SymbolTable mScope = new SymbolTable();
		
		public SymbolTable Scope {
			get {
				return this.mScope;
			}
		}
		
		public Block(IEnumerable<Statement> statements) {
			if (statements == null)
				throw new ArgumentNullException("statements");
			
			this.mStatements = new List<Statement>(statements);
		}
		
		public Block(List<Statement> statements) {
			if (statements == null)
				throw new ArgumentNullException("statements");
			
			this.mStatements = statements;
		}
		
		public override Node Analyze(AffeCompilerState state) {
			state.PushSymbolTable(this.Scope);
			
			for (int i = 0; i < this.mStatements.Count; i++) {
				this.mStatements[i] = (Statement) this.mStatements[i].Analyze(state);
			}
			
			state.PopSymbolTable();
			
			return this;
		}
		
		public override void Emit(AffeCompilerState state) {
			state.PushSymbolTable(this.Scope);
			
			foreach (Statement i in this.mStatements)
				i.Emit(state);
			
			state.PopSymbolTable();
		}
	}
	
	public class Identifier : Node {
		private string mName;
		
		public string Name {
			get {
				return this.mName;
			}
			set {
				if (value == null)
					throw new ArgumentNullException("value");
				
				if (value == "")
					throw new ArgumentException("value == \"\"");
				
				this.mName = value;
			}
		}
		
		public Identifier(string name) {
			this.Name = name;
		}
	}
	
	public abstract class Expression : Node {
		private Type mType = null;
		
		public virtual Type Type {
			get {
				return this.mType;
			}
			set {
				this.mType = value;
			}
		}
		
		public virtual void EmitForInvocation(AffeCompilerState state) {
			this.Emit(state);
			
			if (this.Type.IsValueType) {
				LocalBuilder l = state.CheckOutLocal(this.Type, true);
				
				state.ILGenerator.Emit(OpCodes.Stloc, l);
				state.ILGenerator.Emit(OpCodes.Ldloca, l);
			}
		}
	}
	
	public class TernaryConditionalExpression : Expression {
		private Expression mConditional;
		
		public Expression Conditional {
			get {
				return this.mConditional;
			}
			set {
				if (value == null)
					throw new ArgumentNullException("value");
				
				this.mConditional = value;
			}
		}
		
		private Expression mIfTrue;
		
		public Expression IfTrue {
			get {
				return this.mIfTrue;
			}
			set {
				if (value == null)
					throw new ArgumentNullException("value");
				
				this.mIfTrue = value;
			}
		}
		
		private Expression mIfFalse;
		
		public Expression IfFalse {
			get {
				return this.mIfFalse;
			}
			set {
				if (value == null)
					throw new ArgumentNullException("value");
				
				this.mIfFalse = value;
			}
		}
		
		public TernaryConditionalExpression(Expression conditional, Expression iftrue,
		                                    Expression iffalse) {
			this.Conditional = conditional;
			this.IfTrue = iftrue;
			this.IfFalse = iffalse;
		}
		
		public override Node Analyze(AffeCompilerState state) {
			this.Conditional = (Expression) this.Conditional.Analyze(state);
			this.Conditional = state.CastTo(this.Conditional, typeof(bool));
			
			this.IfTrue = (Expression) this.IfTrue.Analyze(state);
			this.IfFalse = (Expression) this.IfFalse.Analyze(state);
			
			Type t = AffeCompilerState.FindCompatibleType(this.IfTrue.Type, this.IfFalse.Type);
			this.IfTrue = state.CastTo(this.IfTrue, t);
			this.IfFalse = state.CastTo(this.IfFalse, t);
			
			this.Type = t;
			
			return this;
		}
		
		public override void Emit(AffeCompilerState state) {
			ILGenerator il = state.ILGenerator;
			Label istrue = il.DefineLabel();
			Label isfalse = il.DefineLabel();
			
			this.Conditional.Emit(state);
			il.Emit(OpCodes.Brtrue, istrue);
			
			this.IfFalse.Emit(state);
			il.Emit(OpCodes.Br, isfalse);
			
			il.MarkLabel(istrue);
			this.IfTrue.Emit(state);
			
			il.MarkLabel(isfalse);
		}
	}
	
	public class NullExpression : Expression {
		public override Type Type {
			get {
				return typeof(object);
			}
			set {
				throw new InvalidOperationException("Cannot set the type of a constant.");
			}
		}
		
		public override void EmitForInvocation(AffeCompilerState state) {
			throw new InvalidOperationException("Cannot invoke on null.");
		}
		
		public override void Emit(AffeCompilerState state) {
			state.ILGenerator.Emit(OpCodes.Ldnull);
		}
	}
	
	public class CastExpression : Expression {
		private Expression mExpression;
		
		public Expression Expression {
			get {
				return this.mExpression;
			}
			set {
				if (value == null)
					throw new ArgumentNullException("value");
				
				this.mExpression = value;
			}
		}
		
		private Identifier mTypeIdentifier = null;
		
		public Identifier TypeIdentifier {
			get {
				return this.mTypeIdentifier;
			}
			set {
				this.mTypeIdentifier = value;
			}
		}
		
		public CastExpression(Expression expression, Type type) {
			this.Expression = expression;
			this.Type = type;
		}
		
		public CastExpression(Expression expression, Identifier type) {
			this.Expression = expression;
			this.TypeIdentifier = type;
		}
		
		public override Node Analyze(AffeCompilerState state) {
			this.mExpression = (Expression) this.mExpression.Analyze(state);
			
			return this.AnalyzeShallow(state);
		}
		
		public Node AnalyzeShallow(AffeCompilerState state) {
			if (this.Type == null) {
				if (this.TypeIdentifier == null)
					throw new AffeException("One of Type and TypeIdentifier must not be null.", this);
				
				TypeSymbol s = state.Scope.GetSymbol(this.TypeIdentifier.Name) as TypeSymbol;
				
				if (s == null)
					throw new AffeException("Destination type could not be resolved.", this.TypeIdentifier);
				
				if (s.Type == null)
					throw new AffeException("Destination type must not be the inferencing type.", this.TypeIdentifier);
				
				this.Type = s.Type;
			}
			
			if (this.mExpression.Type == this.Type)
				return this.mExpression;
			
			if (this.Type == typeof(bool)) {
				// If this is a reference type then test if it's not null.
				if (!this.Expression.Type.IsValueType)
					return new OperatorExpression(Operator.Ne, this.Expression, new NullExpression());
				
				// If we're casting to bool then we are essentially testing if
				// this.Expression != 0.  This fails for non-numeric types.
				return new OperatorExpression(Operator.Ne, this.Expression, new IntegerExpression(0)).Analyze(state);
			}
			
			return this;
		}
		
		public override void Emit(AffeCompilerState state) {
			this.Expression.Emit(state);
			
			ILGenerator il = state.ILGenerator;
			
			// If the types are the same, why are we casting?
			if (this.Expression.Type == this.Type)
				return;
			
			// Check for boxing.
			if (this.Expression.Type.IsValueType && !this.Type.IsValueType) {
				if (!this.Type.IsAssignableFrom(this.Expression.Type))
					throw new AffeException("Cannot perform the required cast to box.", this);
				
				il.Emit(OpCodes.Box, this.Expression.Type);
				return;
			}
			
			// Check for unboxing to primitives.
			if (this.Type.IsPrimitive && this.Expression.Type == typeof(object)) {
				il.Emit(OpCodes.Unbox, this.Type);
				if (this.Type == typeof(bool) || this.Type == typeof(sbyte)) {
					il.Emit(OpCodes.Ldind_I1);
				} else if (this.Type == typeof(byte)) {
					il.Emit(OpCodes.Ldind_U1);
				} else if (this.Type == typeof(short)) {
					il.Emit(OpCodes.Ldind_I2);
				} else if (this.Type == typeof(ushort) || this.Type == typeof(char)) {
					il.Emit(OpCodes.Ldind_U2);
				} else if (this.Type == typeof(int)) {
					il.Emit(OpCodes.Ldind_I4);
				} else if (this.Type == typeof(uint)) {
					il.Emit(OpCodes.Ldind_U4);
				} else if (this.Type == typeof(long) || this.Type == typeof(ulong)) {
					il.Emit(OpCodes.Ldind_I8);
				} else if (this.Type == typeof(float)) {
					il.Emit(OpCodes.Ldind_R4);
				} else if (this.Type == typeof(double)) {
					il.Emit(OpCodes.Ldind_R8);
				} else {
					throw new AffeException("Unable to unbox type.", this);
				}
				return;
			}
			
			// Check for unboxing.
			if (!this.Expression.Type.IsValueType && this.Type.IsValueType) {
				il.Emit(OpCodes.Unbox, this.Type);
				il.Emit(OpCodes.Ldobj, this.Type);
				return;
			}
			
			// Check for object assignment.
			if (!this.Type.IsValueType && !this.Expression.Type.IsValueType) {
				// If the type is assignable then it's free.
				if (this.Type.IsAssignableFrom(this.Expression.Type))
					return;
				
				// Otherwise we must cast.
				il.Emit(OpCodes.Castclass, this.Type);
				return;
			}
			
			// If we got this far, the expression is a primitive.
			// Convert to the required type.
			
			// Bool is handled in the analysis phase by changing the tree.
			if (this.Type == typeof(sbyte)) {
				il.Emit(OpCodes.Conv_I1);
			} else if (this.Type == typeof(byte)) {
				il.Emit(OpCodes.Conv_U1);
			} else if (this.Type == typeof(short)) {
				il.Emit(OpCodes.Conv_I2);
			} else if (this.Type == typeof(ushort) || this.Type == typeof(char)) {
				il.Emit(OpCodes.Conv_U2);
			} else if (this.Type == typeof(int)) {
				il.Emit(OpCodes.Conv_I4);
			} else if (this.Type == typeof(uint)) {
				il.Emit(OpCodes.Conv_U4);
			} else if (this.Type == typeof(long)) {
				il.Emit(OpCodes.Conv_I8);
			} else if (this.Type == typeof(ulong)) {
				il.Emit(OpCodes.Conv_U8);
			} else if (this.Type == typeof(float)) {
				il.Emit(OpCodes.Conv_R4);
			} else if (this.Type == typeof(double)) {
				il.Emit(OpCodes.Conv_R8);
			} else {
				throw new AffeException("Unable to convert type.", this);
			}
		}
	}
	
	public class IdentifierExpression : Lvalue {
		private Identifier mIdentifier;
		
		public Identifier Identifier {
			get {
				return this.mIdentifier;
			}
			set {
				if (value == null)
					throw new ArgumentNullException("value");
				
				this.mIdentifier = value;
			}
		}
		
		public IdentifierExpression(Identifier identifier) {
			this.Identifier = identifier;
		}
		
		public override Node Analyze(AffeCompilerState state) {
			Symbol s = state.Scope.GetSymbol(this.Identifier.Name);
			
			if (s == null)
				s = state.DefineLocal(this.Identifier);
			
			if (s is VariableSymbol) {
				this.Type = ((VariableSymbol) s).Type;
			} else if (s is FieldSymbol) {
				this.Type = ((FieldSymbol) s).Field.FieldType;
			} else {
				throw new AffeException("An identifier used in an expression must be a variable.", this);
			}
			
			return this;
		}
		
		public override void Emit(AffeCompilerState state) {
			string id = this.Identifier.Name;
			Symbol s = state.Scope.GetSymbol(id);
			
			if (s == null) {
				// This should never happen if the analysis was run.
				throw new AffeException("Unknown symbol.", this);
			} else if (s is VariableSymbol) {
				state.ILGenerator.Emit(OpCodes.Ldloc, ((VariableSymbol) s).Local);
			} else if (s is FieldSymbol) {
				FieldInfo fi = ((FieldSymbol) s).Field;
				
				if (fi.IsLiteral) {
					DataInvokationExpression.EmitLiteral(state.ILGenerator, fi, this);
				} else if (fi.IsStatic) {
					state.ILGenerator.Emit(OpCodes.Ldsfld, fi);
				} else {
					state.ILGenerator.Emit(OpCodes.Ldarg_0);
					state.ILGenerator.Emit(OpCodes.Ldfld, fi);
				}
			} else {
				throw new AffeException("Symbol is not a field or variable.", this);
			}
		}
		
		public override void EmitForInvocation(AffeCompilerState state) {
			if (!this.Type.IsValueType) {
				this.Emit(state);
				return;
			}
			
			Symbol s = state.Scope.GetSymbol(this.Identifier.Name);
			
			if (s is VariableSymbol) {
				state.ILGenerator.Emit(OpCodes.Ldloca,
				                       ((VariableSymbol) s).Local);
			} else if (s is FieldSymbol) {
				FieldInfo fi = ((FieldSymbol) s).Field;
				
				if (fi.IsStatic) {
					state.ILGenerator.Emit(OpCodes.Ldsflda, fi);
				} else {
					state.ILGenerator.Emit(OpCodes.Ldarg_0);
					state.ILGenerator.Emit(OpCodes.Ldflda, fi);
				}
			} else {
				throw new Cdh.Affe.AffeException("Symbol is not a field or variable.", this);
			}
		}
		
		public override void EmitStore(AffeCompilerState state, Expression expr) {
			string target = this.Identifier.Name;
			
			Symbol s = state.Scope.GetSymbol(target);
			
			if (s == null) {
				// This should never happen if the analysis was run.
				throw new AffeException("Unknown symbol.", this);
			}
			
			if (!(s is VariableSymbol) && !(s is FieldSymbol))
				throw new AffeException("Assignment target is not a variable or field.", this);
			
			if (s is FieldSymbol) {
				FieldInfo fi = ((FieldSymbol) s).Field;
				
				if (fi.IsInitOnly)
					throw new AffeException("Cannot assign to a read-only field.", this);
				
				if (!fi.IsStatic)
					state.ILGenerator.Emit(OpCodes.Ldarg_0);
			}
			
			expr.Emit(state);
			
			if (s is VariableSymbol) {
				state.ILGenerator.Emit(OpCodes.Stloc, ((VariableSymbol) s).Local);
			} else {
				FieldInfo fi = ((FieldSymbol) s).Field;
				
				state.ILGenerator.Emit(fi.IsStatic ? OpCodes.Stsfld : OpCodes.Stfld, fi);
			}
		}
	}
	
	public class FloatExpression : Expression {
		private float mFloat;
		
		public float Float {
			get {
				return this.mFloat;
			}
			set {
				this.mFloat = value;
			}
		}
		
		public override Type Type {
			get {
				return typeof(float);
			}
			set {
				throw new InvalidOperationException("Cannot set type of a constant.");
			}
		}
		
		public FloatExpression(float @float) {
			this.mFloat = @float;
		}
		
		public override void Emit(AffeCompilerState state) {
			state.ILGenerator.Emit(OpCodes.Ldc_R4, this.mFloat);
		}
	}
	
	public class IntegerExpression : Expression {
		private int mInteger;
		
		public int Integer {
			get {
				return this.mInteger;
			}
			set {
				this.mInteger = value;
			}
		}
		
		public override Type Type {
			get {
				return typeof(int);
			}
			set {
				throw new InvalidOperationException("Cannot set type of a constant.");
			}
		}
		
		public IntegerExpression(int integer) {
			this.mInteger = integer;
		}
		
		public override void Emit(AffeCompilerState state) {
			state.ILGenerator.Emit(OpCodes.Ldc_I4, this.mInteger);
		}
	}
	
	public class BooleanExpression : Expression {
		private bool mBoolean;
		
		public bool Boolean {
			get {
				return this.mBoolean;
			}
			set {
				this.mBoolean = value;
			}
		}
		
		public override Type Type {
			get {
				return typeof(bool);
			}
			set {
				throw new InvalidOperationException("Cannot set type of a constant.");
			}
		}
		
		public BooleanExpression(bool boolean) {
			this.mBoolean = boolean;
		}
		
		public override void Emit(AffeCompilerState state) {
			if (this.mBoolean)
				state.ILGenerator.Emit(OpCodes.Ldc_I4_1);
			else
				state.ILGenerator.Emit(OpCodes.Ldc_I4_0);
		}
	}
	
	public class StringExpression : Expression {
		private string mString;
		
		public string String {
			get {
				return this.mString;
			}
			set {
				// Null allowed here.
				this.mString = value;
			}
		}
		
		public override Type Type {
			get {
				return typeof(string);
			}
			set {
				throw new InvalidOperationException("Cannot set type of a constant.");
			}
		}
		
		public StringExpression(string @string) {
			this.mString = @string;
		}
		
		public override void Emit(AffeCompilerState state) {
			if (this.mString == null)
				state.ILGenerator.Emit(OpCodes.Ldnull);
			else
				state.ILGenerator.Emit(OpCodes.Ldstr, this.mString);
		}
	}
	
	public class CallExpression : Expression {
		private Identifier mName;
		
		public Identifier Name {
			get {
				return this.mName;
			}
			set {
				if (value == null)
					throw new ArgumentNullException("value");
				
				this.mName = value;
			}
		}
		
		private List<Expression> mArguments;
		
		public List<Expression> Arguments {
			get {
				return this.mArguments;
			}
		}
		
		public CallExpression(Identifier name, IEnumerable<Expression> arguments) {
			this.Name = name;
			
			if (arguments == null)
				throw new ArgumentNullException("arguments");
			
			this.mArguments = new List<Expression>(arguments);
		}
		
		public CallExpression(Identifier name, List<Expression> arguments) {
			this.Name = name;
			
			if (arguments == null)
				throw new ArgumentNullException("arguments");
			
			this.mArguments = arguments;
		}
		
		public override Node Analyze(AffeCompilerState state) {
			Symbol s = state.Scope.GetSymbol(this.Name.Name);
			
			if (s == null)
				throw new AffeException("No such method.", this);
			
			if (s is TransformSymbol) {
				this.Type = ((TransformSymbol) s).ResultType;
				
				for (int i = 0; i < this.Arguments.Count; i++) {
					this.Arguments[i] = (Expression) this.Arguments[i].Analyze(state);
				}
			} else if (s is MethodSymbol) {
				MethodSymbol ms = (MethodSymbol) s;
				
				ParameterInfo[] param = ms.Method.GetParameters();
				if (this.Arguments.Count != param.Length)
					throw new AffeException("Incorrect argument count to method.", this);
				
				for (int i = 0; i < param.Length; i++) {
					this.Arguments[i] = state.CastTo((Expression) this.Arguments[i].Analyze(state),
					                                 param[i].ParameterType);
				}
				
				this.Type = ms.Method.ReturnType;
			} else {
				throw new AffeException("Not a method.", this);
			}
			
			return this;
		}
		
		public override void Emit(AffeCompilerState state) {
			Symbol s = state.Scope.GetSymbol(this.Name.Name);
			
			if (s == null)
				// Should never happen if the analysis was run.
				throw new AffeException("Symbol not found.", this);
			
			ILGenerator il = state.ILGenerator;			
			
			if (s is MethodSymbol) {
				MethodInfo mi = ((MethodSymbol) s).Method;
				
				if (!mi.IsStatic)
					il.Emit(OpCodes.Ldarg_0);
				
				foreach (Expression exp in this.Arguments)
					exp.Emit(state);
				
				il.Emit(mi.IsStatic ? OpCodes.Call : OpCodes.Callvirt, mi);
			} else if (s is TransformSymbol) {
				((TransformSymbol) s).Method.Invoke(null, new object[] { state, this.Arguments.ToArray() });
			} else {
				// Should never happen if the analysis was run.
				throw new AffeException("Not a method.", this);
			}
		}
	}
	
	public class OperatorExpression : Expression {
		private static readonly Dictionary<Operator, string> mOpCalls =
			new Dictionary<Operator, string>();
		
		static OperatorExpression() {
			mOpCalls[Operator.Add] = "op_Addition";
			mOpCalls[Operator.Minus] = "op_Subtraction";
			mOpCalls[Operator.Multiply] = "op_Multiply";
			mOpCalls[Operator.Divide] = "op_Division";
			mOpCalls[Operator.Mod] = "op_Modulus";
			mOpCalls[Operator.And] = "op_BitwiseAnd";
			mOpCalls[Operator.Or] = "op_BitwiseOr";
			
			mOpCalls[Operator.Lt] = "op_LessThan";
			mOpCalls[Operator.Gt] = "op_GreaterThan";
			mOpCalls[Operator.Lte] = "op_LessThanOrEqual";
			mOpCalls[Operator.Gte] = "op_GreaterThanOrEqual";
			mOpCalls[Operator.Eq] = "op_Equality";
			mOpCalls[Operator.Ne] = "op_Inequality";
			mOpCalls[Operator.Bor] = "op_LogicalOr";
			mOpCalls[Operator.Band] = "op_LogicalAnd";
		}
		
		private Expression mLeft;
		
		public Expression Left {
			get {
				return this.mLeft;
			}
			set {
				if (value == null)
					throw new ArgumentNullException("value");
				
				this.mLeft = value;
			}
		}
		
		private Expression mRight;
		
		public Expression Right {
			get {
				return this.mRight;
			}
			set {
				if (value == null)
					throw new ArgumentNullException("value");
				
				this.mRight = value;
			}
		}
		
		private Operator mOperator;
		
		public Operator Operator {
			get {
				return this.mOperator;
			}
			set {
				this.mOperator = value;
			}
		}
		
		private MethodInfo mOperatorOverload = null;
		
		public OperatorExpression(Operator @operator, Expression left,
		                          Expression right) {
			this.mOperator = @operator;
			this.Left = left;
			this.Right = right;
		}
		
		private MethodInfo CheckForOperatorCall(Type t) {
			string name;
			
			if (!mOpCalls.TryGetValue(this.Operator, out name))
				return null;
			
			Type[] types = new Type[] { this.Left.Type, this.Right.Type };
			
			return t.GetMethod(name, BindingFlags.Public |
			                   BindingFlags.Static, null,
			                   types, null);
		}
		
		public override Node Analyze(AffeCompilerState state) {
			this.Left = (Expression) this.Left.Analyze(state);
			this.Right = (Expression) this.Right.Analyze(state);
			
			// Look on the left and right types for an operator overload.
			MethodInfo opm = this.CheckForOperatorCall(this.Left.Type);
			if (opm == null)
				opm = this.CheckForOperatorCall(this.Right.Type);
			
			if (opm != null) {
				// There is an operator overload.  No further analysis is required.
				this.mOperatorOverload = opm;
				this.Type = opm.ReturnType;
				
				return this;
			}
			
			if (!this.Left.Type.IsValueType) {
				if (this.Right.Type.IsValueType)
					throw new AffeException("Cannot compare reference and value types.", this);
				
				if (this.Operator != Operator.Eq &&
				    this.Operator != Operator.Ne)
					throw new AffeException("Cannot apply that operator to object references.", this);
			} else if (!AffeCompilerState.IsNumeric(this.Left.Type) ||
			           !AffeCompilerState.IsNumeric(this.Right.Type))
				throw new AffeException("Cannot operate on non-numeric values.", this);
			
			Type opType;
			if (this.Operator == Operator.And || this.Operator == Operator.Or) {
				opType = typeof(int);
			} else if (this.Operator == Operator.Bor || this.Operator == Operator.Band) {
				opType = typeof(bool);
			} else {
				opType = AffeCompilerState.FindCompatibleType(this.Left.Type, this.Right.Type);
				
				// Division forces a cast to floating-point.
				if (this.Operator == Operator.Divide &&
				    opType != typeof(float) &&
				    opType != typeof(double))
					opType = typeof(float);
			}
			
			this.Left = state.CastTo(this.Left, opType);
			this.Right = state.CastTo(this.Right, opType);
			
			if (this.Operator == Operator.Eq ||
			    this.Operator == Operator.Gt ||
			    this.Operator == Operator.Gte ||
			    this.Operator == Operator.Lt ||
			    this.Operator == Operator.Lte ||
			    this.Operator == Operator.Ne ||
			    this.Operator == Operator.Bor ||
			    this.Operator == Operator.Band) {
				this.Type = typeof(bool);
			} else {
				this.Type = opType;
			}
			
			return this;
		}
		
		public override void Emit(AffeCompilerState state) {
			ILGenerator il = state.ILGenerator;
			
			// First check for an operator overload.
			if (this.mOperatorOverload != null) {
				this.Left.Emit(state);
				this.Right.Emit(state);
				il.Emit(OpCodes.Call, this.mOperatorOverload);
				
				return;
			}
			
			// Band and Bor are special cases.
			switch (this.Operator) {
			case Operator.Bor:
			{
				Label istrue = il.DefineLabel();
				Label isfalse = il.DefineLabel();
				
				this.Left.Emit(state);
				il.Emit(OpCodes.Brtrue, istrue);
				
				this.Right.Emit(state);
				il.Emit(OpCodes.Br, isfalse);
				
				il.MarkLabel(istrue);
				il.Emit(OpCodes.Ldc_I4_1);
				
				il.MarkLabel(isfalse);
				
				return;
			}
				
			case Operator.Band:
			{
				Label istrue = il.DefineLabel();
				Label isfalse = il.DefineLabel();
				
				this.Left.Emit(state);
				il.Emit(OpCodes.Brfalse, isfalse);
				
				this.Right.Emit(state);
				il.Emit(OpCodes.Br, istrue);
				
				il.MarkLabel(isfalse);
				il.Emit(OpCodes.Ldc_I4_0);
				
				il.MarkLabel(istrue);
				
				return;
			}
			}
			
			this.Left.Emit(state);
			this.Right.Emit(state);
			
			switch (this.Operator) {
			case Operator.Add:
				il.Emit(OpCodes.Add);
				break;
				
			case Operator.And:
				il.Emit(OpCodes.And);
				break;
				
			case Operator.Divide:
				il.Emit(OpCodes.Div);
				break;
				
			case Operator.Eq:
				il.Emit(OpCodes.Ceq);
				break;
				
			case Operator.Gt:
				il.Emit(OpCodes.Cgt);
				break;
				
			case Operator.Gte:
				il.Emit(OpCodes.Clt);
				il.Emit(OpCodes.Ldc_I4_0);
				il.Emit(OpCodes.Ceq);
				break;
				
			case Operator.Lt:
				il.Emit(OpCodes.Clt);
				break;
				
			case Operator.Lte:
				il.Emit(OpCodes.Cgt);
				il.Emit(OpCodes.Ldc_I4_0);
				il.Emit(OpCodes.Ceq);
				break;
				
			case Operator.Minus:
				il.Emit(OpCodes.Sub);
				break;
				
			case Operator.Mod:
				il.Emit(OpCodes.Rem);
				break;
				
			case Operator.Multiply:
				il.Emit(OpCodes.Mul);
				break;
				
			case Operator.Ne:
				il.Emit(OpCodes.Ceq);
				il.Emit(OpCodes.Ldc_I4_0);
				il.Emit(OpCodes.Ceq);
				break;
				
			case Operator.Or:
				il.Emit(OpCodes.Or);
				break;
				
			default:
				throw new AffeException("Unknown operator.", this);
			}
		}
	}
	
	public class UnaryExpression : Expression {
		private Operator mOperator;
		
		public Operator Operator {
			get {
				return this.mOperator;
			}
			set {
				this.mOperator = value;
			}
		}
		
		private Expression mExpression;
		
		public Expression Expression {
			get {
				return this.mExpression;
			}
			set {
				if (value == null)
					throw new ArgumentNullException("value");
				
				this.mExpression = value;
			}
		}
		
		public UnaryExpression(Operator @operator, Expression expression) {
			this.mOperator = @operator;
			this.Expression = expression;
		}
		
		public override Node Analyze(AffeCompilerState state) {
			this.Expression = (Expression) this.Expression.Analyze(state);
			
			if (!AffeCompilerState.IsNumeric(this.Expression.Type))
				throw new AffeException("Cannot operate on non-numeric values.", this);
			
			switch (this.Operator) {
			case Operator.Neg:
				this.Type = this.Expression.Type;
				break;
				
			case Operator.Not:
				this.Expression = state.CastTo(this.Expression, typeof(bool));
				this.Type = typeof(bool);
				break;
				
			default:
				throw new AffeException("Unknown operator.", this);
			}
			
			return this;
		}
		
		public override void Emit(AffeCompilerState state) {
			this.Expression.Emit(state);
			
			switch (this.Operator) {
			case Operator.Neg:
				state.ILGenerator.Emit(OpCodes.Neg);
				break;
				
			case Operator.Not:
				state.ILGenerator.Emit(OpCodes.Ldc_I4_0);
				state.ILGenerator.Emit(OpCodes.Ceq);
				break;
				
			default:
				throw new AffeException("Unknown operator.", this);
			}
		}
	}
	
	public abstract class Statement : Node {
	}
	
	public abstract class Lvalue : Expression {
		public abstract void EmitStore(AffeCompilerState state, Expression expr);
	}
	
	public class Assignment : Statement {
		private Lvalue mLvalue;
		
		public Lvalue Lvalue {
			get {
				return this.mLvalue;
			}
			set {
				if (value == null)
					throw new ArgumentNullException("value");
				
				this.mLvalue = value;
			}
		}
		
		private Expression mRvalue;
		
		public Expression Rvalue {
			get {
				return this.mRvalue;
			}
			set {
				if (value == null)
					throw new ArgumentNullException("value");
				
				this.mRvalue = value;
			}
		}
		
		public Assignment(Lvalue lvalue, Expression rvalue) {
			this.Lvalue = lvalue;
			this.Rvalue = rvalue;
		}
		
		public override Node Analyze(AffeCompilerState state) {
			this.Rvalue = (Expression) this.Rvalue.Analyze(state);
			
			this.Lvalue = (Lvalue) this.Lvalue.Analyze(state);
			
			this.Rvalue = state.CastTo(this.Rvalue, this.Lvalue.Type);
			
			return this;
		}
		
		public override void Emit(AffeCompilerState state) {
			this.Lvalue.EmitStore(state, this.Rvalue);
		}
	}
	
	public class PersistentVariableDeclaration : Statement {
		private Identifier mType;
		
		public Identifier Type {
			get {
				return this.mType;
			}
			set {
				if (value == null)
					throw new ArgumentNullException("value");
				
				this.mType = value;
			}
		}
		
		private Identifier mName;
		
		public Identifier Name {
			get {
				return this.mName;
			}
			set {
				if (value == null)
					throw new ArgumentNullException("value");
				
				this.mName = value;
			}
		}
		
		public PersistentVariableDeclaration(Identifier type, Identifier name) {
			this.Type = type;
			this.Name = name;
		}
		
		public override Node Analyze(AffeCompilerState state) {
			TypeSymbol ts = state.Scope.GetSymbol(this.Type.Name) as TypeSymbol;
			
			if (ts == null)
				throw new AffeException("Not a type.", this.Type);
			
			if (ts.Type == typeof(void)) {
				// Void TypeSymbol means this is our inferencing type.
				throw new AffeException("Persistent variables cannot be type-inferenced.", this);
			}
			
			Type dtype = ts.Type;
			
			string name = this.Name.Name;
			
			if (state.Scope.GetSymbol(name) != null)
				throw new AffeException("Identifier previously declared.", this.Name);
			
			VariableSymbol vs = new VariableSymbol(name, dtype);
			vs.Local = state.ILGenerator.DeclareLocal(dtype);
			state.Scope.AddSymbol(vs);
			state.MakePersistent(vs);
			
			return this;
		}
	}
	
	public class VariableDeclaration : Assignment {
		private Identifier mType;
		
		public Identifier Type {
			get {
				return this.mType;
			}
			set {
				if (value == null)
					throw new ArgumentNullException("value");
				
				this.mType = value;
			}
		}
		
		public VariableDeclaration(Identifier type, Identifier lvalue,
		                           Expression rvalue)
		: base(new IdentifierExpression(lvalue), rvalue) {
			this.Type = type;
		}
		
		public override Node Analyze(AffeCompilerState state) {
			this.Rvalue = (Expression) this.Rvalue.Analyze(state);
			
			Type dtype = null;
			
			TypeSymbol ts = state.Scope.GetSymbol(this.Type.Name) as TypeSymbol;
			
			if (ts == null)
				throw new AffeException("Not a type.", this.Type);
			
			if (ts.Type == typeof(void)) {
				// Void TypeSymbol means this is our inferencing type.
				dtype = this.Rvalue.Type;
			} else {
				dtype = ts.Type;
			}
			
			string name = ((IdentifierExpression) this.Lvalue).Identifier.Name;
			
			if (state.Scope.GetSymbol(name) != null)
				throw new AffeException("Identifier previously declared.", this.Lvalue);
			
			VariableSymbol vs = new VariableSymbol(name, dtype);
			vs.Local = state.ILGenerator.DeclareLocal(dtype);
			state.Scope.AddSymbol(vs);
			
			this.Rvalue = state.CastTo(this.Rvalue, dtype);
			
			return this;
		}
	}
	
	public class If : Statement {
		private Expression mCondition;
		
		public Expression Condition {
			get {
				return this.mCondition;
			}
			set {
				if (value == null)
					throw new ArgumentNullException("value");
				
				this.mCondition = value;
			}
		}
		
		private Block mIfStatements;
		
		public Block IfStatements {
			get {
				return this.mIfStatements;
			}
			set {
				if (value == null)
					throw new ArgumentNullException("value");
				
				this.mIfStatements = value;
			}
		}
		
		public If(Expression condition, Block ifstatements) {
			this.Condition = condition;
			this.IfStatements = ifstatements;
		}
		
		public override Node Analyze(AffeCompilerState state) {
			this.Condition = state.CastTo((Expression) this.Condition.Analyze(state),
			                              typeof(bool));
			
			this.IfStatements = (Block) this.IfStatements.Analyze(state);
			
			return this;
		}
		
		public override void Emit(AffeCompilerState state) {
			ILGenerator il = state.ILGenerator;		
			
			Label l = il.DefineLabel();
			
			this.Condition.Emit(state);
			il.Emit(OpCodes.Ldc_I4_0);
			il.Emit(OpCodes.Beq, l);
			
			this.IfStatements.Emit(state);
			
			il.MarkLabel(l);
		}
	}
	
	public class IfElse : If {
		private Block mElseStatements;
		
		public Block ElseStatements {
			get {
				return this.mElseStatements;
			}
			set {
				if (value == null)
					throw new ArgumentNullException("value");
				
				this.mElseStatements = value;
			}
		}
		
		public IfElse(Expression condition, Block ifstatements,
		              Block elsestatements)
			: base(condition, ifstatements) {
			this.ElseStatements = elsestatements;
		}
		
		public override Node Analyze(AffeCompilerState state) {
			this.ElseStatements = (Block) this.ElseStatements.Analyze(state);
			
			return base.Analyze(state);
		}
		
		public override void Emit(AffeCompilerState state) {
			ILGenerator il = state.ILGenerator;		
			
			Label l = il.DefineLabel();
			
			this.Condition.Emit(state);
			il.Emit(OpCodes.Ldc_I4_0);
			il.Emit(OpCodes.Beq, l);
			
			this.IfStatements.Emit(state);
			
			Label l2 = il.DefineLabel();
			il.Emit(OpCodes.Br, l2);
			
			il.MarkLabel(l);
			
			this.ElseStatements.Emit(state);
			
			il.MarkLabel(l2);
		}
	}
	
	public abstract class Loop : Statement {
		private Block mBody;
		
		public Block Body {
			get {
				return this.mBody;
			}
			set {
				if (value == null)
					throw new ArgumentNullException("value");
				
				this.mBody = value;
			}
		}
		
		protected Loop(Block body) {
			this.Body = body;
		}
		
		public override Node Analyze(AffeCompilerState state) {
			this.Body = (Block) this.Body.Analyze(state);
			
			return this;
		}
	}
	
	public class WhileLoop : Loop {
		private Expression mCondition;
		
		public Expression Condition {
			get {
				return this.mCondition;
			}
			set {
				if (value == null)
					throw new ArgumentNullException("value");
				
				this.mCondition = value;
			}
		}
		
		public WhileLoop(Expression condition, Block body)
			: base(body) {
			this.Condition = condition;
		}
		
		public override Node Analyze(AffeCompilerState state) {
			this.Condition = state.CastTo((Expression) this.Condition.Analyze(state),
			                              typeof(bool));
			
			return base.Analyze(state);
		}
		
		public override void Emit(AffeCompilerState state) {
			ILGenerator il = state.ILGenerator;
			
			Label start = il.DefineLabel();
			Label end = il.DefineLabel();
			
			state.PushLoopState(new LoopState(start, end));
			
			il.MarkLabel(start);
			
			this.Condition.Emit(state);
			il.Emit(OpCodes.Ldc_I4_0);
			il.Emit(OpCodes.Beq, end);
			
			this.Body.Emit(state);
			
			il.Emit(OpCodes.Br, start);
			il.MarkLabel(end);
			
			state.PopLoopState();
		}
	}
	
	public class BreakStatement : Statement {
		public BreakStatement() {
		}
		
		public override void Emit(AffeCompilerState state) {
			state.ILGenerator.Emit(OpCodes.Br, state.GetLoopState(0).EndLabel);
		}
	}
	
	public class ContinueStatement : Statement {
		public ContinueStatement() {
		}
		
		public override void Emit(AffeCompilerState state) {
			state.ILGenerator.Emit(OpCodes.Br, state.GetLoopState(0).BeginLabel);
		}
	}
	
	public class ReturnStatement : Statement {
		public ReturnStatement() {
		}
		
		public override void Emit(AffeCompilerState state) {
			state.ILGenerator.Emit(OpCodes.Leave, state.ReturnLabel);
		}
	}
	
	public class CallStatement : Statement {
		private CallExpression mCall;
		
		public CallExpression Call {
			get {
				return this.mCall;
			}
			set {
				if (value == null)
					throw new ArgumentNullException("value");
				
				this.mCall = value;
			}
		}
		
		public CallStatement(CallExpression call) {
			this.Call = call;
		}
		
		public override Node Analyze(AffeCompilerState state) {
			this.Call = (CallExpression) this.Call.Analyze(state);
			
			return this;
		}
		
		public override void Emit(AffeCompilerState state) {
			this.mCall.Emit(state);
			
			if (this.mCall.Type != typeof(void))
				state.ILGenerator.Emit(OpCodes.Pop);
		}
	}
	
	public class CallInvokationExpression : Expression {
		private Expression mTarget;
		
		public Expression Target {
			get {
				return this.mTarget;
			}
			set {
				if (value == null)
					throw new ArgumentNullException("value");
				
				this.mTarget = value;
			}
		}
		
		private MethodInfo mMethodInfo = null;
		
		public MethodInfo MethodInfo {
			get {
				return this.mMethodInfo;
			}
			set {
				if (value == null)
					throw new ArgumentNullException("value");
				
				this.mMethodInfo = value;
			}
		}
		
		private Identifier mName;
		
		public Identifier Name {
			get {
				return this.mName;
			}
			set {
				if (value == null)
					throw new ArgumentNullException("value");
				
				this.mName = value;
			}
		}
		
		private List<Expression> mArguments;
		
		public List<Expression> Arguments {
			get {
				return this.mArguments;
			}
		}
		
		public CallInvokationExpression(Expression target, Identifier name,
		                                List<Expression> arguments) {
			this.Target = target;
			this.Name = name;
			
			if (arguments == null)
				throw new ArgumentNullException("arguments");
			
			this.mArguments = arguments;
		}
		
		public CallInvokationExpression(Expression target, Identifier name,
		                                IEnumerable<Expression> arguments) {
			this.Target = target;
			this.Name = name;
			
			if (arguments == null)
				throw new ArgumentNullException("arguments");
			
			this.mArguments = new List<Expression>(arguments);
		}
		
		public override Node Analyze(AffeCompilerState state) {
			BindingFlags isstatic = BindingFlags.Instance;
			
			// Check for static calls.
			Type target = state.CheckForTypeExpression(this.Target);
			
			// If it's not a type, consider it an expression.
			if (target == null) {
				this.Target = (Expression) this.Target.Analyze(state);
				target = this.Target.Type;
			} else {
				isstatic = BindingFlags.Static;
			}
			
			Type[] types = new Type[this.Arguments.Count];
			
			for (int i = 0; i < types.Length; i++) {
				this.Arguments[i] = (Expression) this.Arguments[i].Analyze(state);
				types[i] = this.Arguments[i].Type;
			}
			
			MethodInfo mi;
			try {
				mi = target.GetMethod(this.Name.Name,
				                      isstatic |
				                      BindingFlags.Public |
				                      BindingFlags.FlattenHierarchy,
				                      null, types, null);
			} catch (AmbiguousMatchException) {
				throw new AffeException("Method call is ambiguous.", this);
			} catch (Exception) {
				mi = null;
			}
			
			if (mi == null)
				throw new AffeException("Method not found on class.", this);
			
			// GetMethod will return the closest match it can, not an exact
			// match.  This is nifty.  Now we just have to cast where
			// appropriate.
			
			ParameterInfo[] param = mi.GetParameters();
			for (int i = 0; i < this.Arguments.Count; i++) {
				this.Arguments[i] = state.CastTo(this.Arguments[i],
				                                 param[i].ParameterType);
			}
			
			this.MethodInfo = mi;
			this.Type = mi.ReturnType;
			
			return this;
		}
		
		public override void Emit(AffeCompilerState state) {
			MethodInfo mi = this.mMethodInfo;
			
			ILGenerator il = state.ILGenerator;
			
			// If it's a static call then we don't need to emit the target.
			if (!mi.IsStatic)
				this.Target.EmitForInvocation(state);
			
			foreach (Expression ex in this.Arguments)
				ex.Emit(state);
			
			if (mi.IsStatic)
				il.Emit(OpCodes.Call, mi);
			else
				il.Emit(OpCodes.Callvirt, mi);
		}
	}
	
	public class LateBoundCallInvokationExpression : CallInvokationExpression {
		public LateBoundCallInvokationExpression(Expression target, Identifier name,
		                                         List<Expression> arguments)
		: base(target, name, arguments) {
		}
		
		public LateBoundCallInvokationExpression(Expression target, Identifier name,
		                                         IEnumerable<Expression> arguments)
		: base(target, name, arguments) {
		}
		
		public override Type Type {
			get {
				return typeof(object);
			}
			set {
				throw new InvalidOperationException("Cannot set type of a late-bound expression.");
			}
		}
		
		public override Node Analyze(AffeCompilerState state) {
			this.Target = (Expression) this.Target.Analyze(state);
			
			for (int i = 0; i < this.Arguments.Count; i++) {
				this.Arguments[i] = (Expression)
					state.CastTo((Expression) this.Arguments[i].Analyze(state),
					             typeof(object));
			}
			
			return this;
		}
		
		public override void Emit(AffeCompilerState state) {
			ILGenerator il = state.ILGenerator;
			LocalBuilder array = state.CheckOutLocal(typeof(object[]), true);
			
			il.Emit(OpCodes.Ldc_I4, this.Arguments.Count);
			il.Emit(OpCodes.Newarr, typeof(object));
			il.Emit(OpCodes.Stloc, array);
			
			for (int i = 0; i < this.Arguments.Count; i++) {
				il.Emit(OpCodes.Ldloc, array);
				il.Emit(OpCodes.Ldc_I4, i);
				this.Arguments[i].Emit(state);
				
				il.Emit(OpCodes.Stelem_Ref);
			}
			
			this.Target.Emit(state);
			il.Emit(OpCodes.Ldstr, this.Name.Name);
			il.Emit(OpCodes.Ldloc, array);
			
			il.Emit(OpCodes.Call, PerformInvokeInfo);
		}
		
		private static readonly MethodInfo PerformInvokeInfo =
			typeof(LateBoundCallInvokationExpression).GetMethod("PerformInvoke");
		
		public static object PerformInvoke(object target, string member, object[] param) {
			Type[] types = new Type[param.Length];
			
			for (int i = 0; i < param.Length; i++) {
				if (param[i] == null)
					types[i] = typeof(object);
				else
					types[i] = param[i].GetType();
			}
			
			MethodInfo method;
			try {
				method = target.GetType().GetMethod(member,
				                                    BindingFlags.Instance |
				                                    BindingFlags.Public |
				                                    BindingFlags.FlattenHierarchy,
				                                    null, types, null);
			} catch (AmbiguousMatchException) {
				throw new InvalidOperationException("Late-bound method invokation is overloaded and ambiguous.");
			} catch (Exception) {
				method = null;
			}
			
			if (method == null)
				throw new InvalidOperationException("Late-bound method call cannot be found.");
			
			return method.Invoke(target, param);
		}
	}
	
	public class DataInvokationExpression : Lvalue {
		private Expression mTarget;
		
		public Expression Target {
			get {
				return this.mTarget;
			}
			set {
				if (value == null)
					throw new ArgumentNullException("value");
				
				this.mTarget = value;
			}
		}
		
		private Identifier mMember;
		
		public Identifier Member {
			get {
				return this.mMember;
			}
			set {
				if (value == null)
					throw new ArgumentNullException("value");
				
				this.mMember = value;
			}
		}
		
		private MemberInfo mMemberInfo;
		
		public MemberInfo MemberInfo {
			get {
				return this.mMemberInfo;
			}
			set {
				if (value == null)
					throw new ArgumentNullException("value");
				
				this.mMemberInfo = value;
			}
		}
		
		public bool CanRead {
			get {
				if (this.mMemberInfo == null)
					return false;
				
				if (this.mMemberInfo is PropertyInfo)
					return ((PropertyInfo) this.mMemberInfo).CanRead;
				
				return true;
			}
		}
		
		public bool CanWrite {
			get {
				if (this.mMemberInfo == null)
					return false;
				
				if (this.mMemberInfo is PropertyInfo)
					return ((PropertyInfo) this.mMemberInfo).CanWrite;
				
				return !((FieldInfo) this.mMemberInfo).IsInitOnly;
			}
		}
		
		public DataInvokationExpression(Expression target, Identifier member) {
			this.Target = target;
			this.Member = member;
		}
		
		public override Node Analyze(AffeCompilerState state) {
			BindingFlags isstatic = BindingFlags.Instance;
			
			// Check for static calls.
			Type target = state.CheckForTypeExpression(this.Target);
			
			// If it's not a type, consider it an expression.
			if (target == null) {
				this.Target = (Expression) this.Target.Analyze(state);
				target = this.Target.Type;
			} else {
				isstatic = BindingFlags.Static;
			}
			
			MemberInfo mi;
			try {
				MemberInfo[] mis = target.GetMember(this.Member.Name,
				                                    MemberTypes.Field |
				                                    MemberTypes.Property,
				                                    isstatic |
				                                    BindingFlags.Public |
				                                    BindingFlags.FlattenHierarchy);
				
				if (mis.Length > 1)
					throw new AmbiguousMatchException();
				
				if (mis.Length == 0)
					throw new ArgumentException();
				
				mi = mis[0];
			} catch (Exception) {
				mi = null;
			}
			
			if (mi == null)
				throw new AffeException("Member not found on class.", this);
			
			if (mi is PropertyInfo) {
				this.Type = ((PropertyInfo) mi).PropertyType;
			} else {
				this.Type = ((FieldInfo) mi).FieldType;
			}
			
			this.MemberInfo = mi;
			
			return this;
		}
		
		public static void EmitLiteral(ILGenerator il, FieldInfo field,
		                               Node @this) {
			object literal = field.GetValue(null);
			
			if (AffeCompilerState.IsI4(field.FieldType)) {
				int i4;
				
				if (field.FieldType == typeof(uint))
					// Handle overflow.
					i4 = unchecked((int) (uint) literal);
				else
					i4 = Convert.ToInt32(literal);
				
				il.Emit(OpCodes.Ldc_I4, i4);
			} else if (field.FieldType == typeof(float)) {
				il.Emit(OpCodes.Ldc_R4, (float) literal);
			} else if (field.FieldType == typeof(double)) {
				il.Emit(OpCodes.Ldc_R8, (double) literal);
			} else {
				throw new AffeException("Cannot process literal of type " +
				                        field.FieldType.FullName, @this);
			}
			
			return;
		}
		
		public override void Emit(AffeCompilerState state) {
			PropertyInfo pi = this.mMemberInfo as PropertyInfo;
			FieldInfo fi = this.mMemberInfo as FieldInfo;
			
			// Literal fields are constants, emit the value directly.
			if (fi != null && fi.IsLiteral) {
				EmitLiteral(state.ILGenerator, fi, this);
				return;
			}
			
			// Like CallInvokationExpression, we can skip the target if it's
			// static.
			bool stc = (pi != null) ?
				pi.GetGetMethod().IsStatic :
					fi.IsStatic;
			
			if (!stc)
				this.Target.EmitForInvocation(state);
			
			if (pi != null)
				state.ILGenerator.Emit(stc ? OpCodes.Call : OpCodes.Callvirt,
				                       pi.GetGetMethod());
			else
				state.ILGenerator.Emit(stc ? OpCodes.Ldsfld : OpCodes.Ldfld, fi);
		}
		
		public override void EmitForInvocation(AffeCompilerState state) {
			if (!this.Type.IsValueType) {
				this.Emit(state);
				return;
			}
			
			FieldInfo fi = this.mMemberInfo as FieldInfo;
			
			if (fi != null) {
				if (!fi.IsStatic)
					this.Target.Emit(state);
				
				state.ILGenerator.Emit(fi.IsStatic ? OpCodes.Ldsflda :
				                       OpCodes.Ldflda, fi);
			} else {
				// It's a property, so we can't just ld(s)flda.  We need to
				// fall back to declaring a local for the value.
				base.EmitForInvocation(state);
			}
		}
		
		public override void EmitStore(AffeCompilerState state, Expression expr) {
			if (!this.CanWrite)
				throw new AffeException("Target is read-only.", this);
			
			PropertyInfo pi = this.mMemberInfo as PropertyInfo;
			FieldInfo fi = this.mMemberInfo as FieldInfo;
			
			bool stc = (pi != null) ?
				pi.GetGetMethod().IsStatic :
					fi.IsStatic;
			
			if (!stc)
				this.Target.Emit(state);
			
			expr.Emit(state);
			
			if (pi != null)
				state.ILGenerator.Emit(stc ? OpCodes.Call : OpCodes.Callvirt,
				                       pi.GetSetMethod());
			else
				state.ILGenerator.Emit(stc ? OpCodes.Stsfld : OpCodes.Stfld, fi);
		}		
	}
	
	public class LateBoundDataInvokationExpression : DataInvokationExpression {
		public LateBoundDataInvokationExpression(Expression target, Identifier member)
		: base(target, member) {
		}
		
		public override Type Type {
			get {
				return typeof(object);
			}
			set {
				throw new InvalidOperationException("Cannot set type of a late-bound expression.");
			}
		}
		
		public override Node Analyze(AffeCompilerState state) {
			this.Target = (Expression) this.Target.Analyze(state);
			
			return this;
		}
		
		public override void Emit(AffeCompilerState state) {
			this.Target.Emit(state);
			state.ILGenerator.Emit(OpCodes.Ldstr, this.Member.Name);
			
			state.ILGenerator.Emit(OpCodes.Call, PerformReadInvokeInfo);
		}
		
		private static readonly MethodInfo PerformReadInvokeInfo =
			typeof(LateBoundDataInvokationExpression).GetMethod("PerformReadInvoke");
		
		public static object PerformReadInvoke(object target, string member) {
			MemberInfo[] members = target.GetType().GetMember(member,
			                                                  MemberTypes.Field |
			                                                  MemberTypes.Property,
			                                                  BindingFlags.Instance |
			                                                  BindingFlags.Public |
			                                                  BindingFlags.FlattenHierarchy);
			
			if (members.Length == 0)
				throw new InvalidOperationException("Member not found during late-bound read data invoke.");
			
			if (members.Length > 1)
				throw new InvalidOperationException("Late-bound read data invoke is ambiguous.");
			
			FieldInfo fi = members[0] as FieldInfo;
			
			if (fi != null) {
				return fi.GetValue(target);
			} else {
				PropertyInfo pi = (PropertyInfo) members[0];
				if (!pi.CanRead)
					throw new InvalidOperationException("Late-bound read data invoke is a write-only property.");
				
				return pi.GetValue(target, null);
			}
		}
		
		public override void EmitStore(AffeCompilerState state, Expression expr) {
			this.Target.Emit(state);
			state.ILGenerator.Emit(OpCodes.Ldstr, this.Member.Name);
			expr.Emit(state);
			
			state.ILGenerator.Emit(OpCodes.Call, PerformWriteInvokeInfo);
		}
		
		private static readonly MethodInfo PerformWriteInvokeInfo =
			typeof(LateBoundDataInvokationExpression).GetMethod("PerformWriteInvoke");
		
		public static void PerformWriteInvoke(object target, string member, object value) {
			MemberInfo[] members = target.GetType().GetMember(member,
			                                                  MemberTypes.Field |
			                                                  MemberTypes.Property,
			                                                  BindingFlags.Instance |
			                                                  BindingFlags.Public |
			                                                  BindingFlags.FlattenHierarchy);
			
			if (members.Length == 0)
				throw new InvalidOperationException("Member not found during late-bound write data invoke.");
			
			if (members.Length > 1)
				throw new InvalidOperationException("Late-bound write data invoke is ambiguous.");
			
			FieldInfo fi = members[0] as FieldInfo;
			
			if (fi != null) {
				if (fi.IsInitOnly)
					throw new InvalidOperationException("Late-bound write data invoke is a read-only field.");
				
				fi.SetValue(target, value);
			} else {
				PropertyInfo pi = (PropertyInfo) members[0];
				
				if (!pi.CanWrite)
					throw new InvalidOperationException("Late-bound write data invoke is a read-only property.");
				
				pi.SetValue(target, value, null);
			}
		}
	}
	
	public class CallInvokationStatement : Statement {
		private CallInvokationExpression mCall;
		
		public CallInvokationExpression Call {
			get {
				return this.mCall;
			}
			set {
				if (value == null)
					throw new ArgumentNullException("value");
				
				this.mCall = value;
			}
		}
		
		public CallInvokationStatement(CallInvokationExpression call) {
			this.Call = call;
		}
		
		public override Node Analyze(AffeCompilerState state) {
			this.Call = (CallInvokationExpression) this.Call.Analyze(state);
			
			return this;
		}
		
		public override void Emit(AffeCompilerState state) {
			this.mCall.Emit(state);
			
			if (this.mCall.MethodInfo.ReturnType != typeof(void))
				state.ILGenerator.Emit(OpCodes.Pop);
		}
	}
	
	public class IndexedExpression : Lvalue {
		private Expression mExpression;
		
		public Expression Expression {
			get {
				return this.mExpression;
			}
			set {
				if (value == null)
					throw new ArgumentNullException("value");
				
				this.mExpression = value;
			}
		}
		
		private List<Expression> mIndex;
		
		public List<Expression> Index {
			get {
				return this.mIndex;
			}
		}
		
		private PropertyInfo mIndexer = null;
		
		public IndexedExpression(Expression expression, List<Expression> index) {
			this.Expression = expression;
			this.mIndex = index;
		}
		
		public IndexedExpression(Expression expression, IEnumerable<Expression> index) {
			this.Expression = expression;
			this.mIndex = new List<Expression>(index);
		}
		
		public override Node Analyze(AffeCompilerState state) {
			BindingFlags isstatic = BindingFlags.Instance;
			
			Type etype = state.CheckForTypeExpression(this.Expression);
			
			if (etype == null) {
				this.Expression = (Expression) this.Expression.Analyze(state);
				etype = this.Expression.Type;
			} else {
				isstatic = BindingFlags.Static;
			}
			
			for (int i = 0; i < this.Index.Count; i++)
				this.Index[i] = (Expression) this.Index[i].Analyze(state);
			
			if (etype.IsArray) {
				if (this.Index.Count != this.Expression.Type.GetArrayRank())
					throw new AffeException("Array must be indexed with the correct rank.", this);
				
				if (isstatic == BindingFlags.Static)
					throw new AffeException("Cannot statically index an array.", this);
				
				for (int i = 0; i < this.Index.Count; i++)
					this.Index[i] = state.CastTo(this.Index[i], typeof(int));
				
				this.Type = etype.GetElementType();
			} else {
				object[] attrs =
					etype.GetCustomAttributes(typeof(DefaultMemberAttribute),
					                          true);
				
				if (attrs.Length == 0)
					throw new AffeException("That object type cannot be indexed.", this.Expression);
				
				DefaultMemberAttribute attr = (DefaultMemberAttribute) attrs[0];
				
				PropertyInfo pi = etype.GetProperty(attr.MemberName,
				                                    isstatic |
				                                    BindingFlags.Public |
				                                    BindingFlags.FlattenHierarchy);
				
				if (pi == null)
					throw new AffeException("Cannot find indexer on that type.", this.Expression);
				
				this.Type = pi.PropertyType;
				
				Type[] indexTypes;
				if (pi.GetGetMethod() != null) {
					ParameterInfo[] param = pi.GetGetMethod().GetParameters();
					
					indexTypes = new Type[param.Length];
					for (int i = 0; i < param.Length; i++)
						indexTypes[i] = param[i].ParameterType;
				} else {
					ParameterInfo[] param = pi.GetSetMethod().GetParameters();
					
					indexTypes = new Type[param.Length - 1];
					for (int i = 0; i < param.Length - 1; i++)
						indexTypes[i] = param[i].ParameterType;
				}
				
				if (this.Index.Count != indexTypes.Length)
					throw new AffeException("Incorrect parameter count to indexer.", this);
				
				for (int i = 0; i < indexTypes.Length; i++)
					this.Index[i] = state.CastTo(this.Index[i], indexTypes[i]);
				
				this.mIndexer = pi;
			}
			
			return this;
		}
		
		// EmitStelem and EmitLdelem are hacks pending the fix of a mono bug:
		// https://bugzilla.novell.com/show_bug.cgi?id=333696
		private static void EmitStelem(ILGenerator il, Type t) {
			if (t == typeof(sbyte) || t == typeof(byte))
				il.Emit(OpCodes.Stelem_I1);
			else if (t == typeof(short) || t == typeof(ushort) ||
			         t == typeof(char))
				il.Emit(OpCodes.Stelem_I2);
			else if (t == typeof(int) || t == typeof(uint))
				il.Emit(OpCodes.Stelem_I4);
			else if (t == typeof(long) || t == typeof(ulong))
				il.Emit(OpCodes.Stelem_I8);
			else if (t == typeof(float))
				il.Emit(OpCodes.Stelem_R4);
			else if (t == typeof(double))
				il.Emit(OpCodes.Stelem_R8);
			else
				il.Emit(OpCodes.Stelem_Ref);
		}
		
		private static void EmitLdelem(ILGenerator il, Type t) {
			if (t == typeof(sbyte))
				il.Emit(OpCodes.Ldelem_I1);
			else if (t == typeof(short))
				il.Emit(OpCodes.Ldelem_I2);
			else if (t == typeof(int))
				il.Emit(OpCodes.Ldelem_I4);
			else if (t == typeof(byte))
				il.Emit(OpCodes.Ldelem_U1);
			else if (t == typeof(ushort) || t == typeof(char))
				il.Emit(OpCodes.Ldelem_U2);
			else if (t == typeof(uint))
				il.Emit(OpCodes.Ldelem_U4);
			else if (t == typeof(long) || t == typeof(ulong))
				il.Emit(OpCodes.Ldelem_I8);
			else if (t == typeof(float))
				il.Emit(OpCodes.Ldelem_R4);
			else if (t == typeof(double))
				il.Emit(OpCodes.Ldelem_R8);
			else
				il.Emit(OpCodes.Ldelem_Ref);
		}
		
		public override void Emit(AffeCompilerState state) {
			MethodInfo getter = null;
			if (this.mIndexer != null)
				getter = this.mIndexer.GetGetMethod();
			
			if (getter == null || !getter.IsStatic)
				this.Expression.Emit(state);
			
			foreach (Expression e in this.Index)
				e.Emit(state);
			
			if (this.mIndexer != null) {
				if (getter == null)
					throw new AffeException("Indexer is write-only.", this);
				
				state.ILGenerator.Emit(getter.IsStatic ?
				                       OpCodes.Call :
				                       OpCodes.Callvirt, getter);
			} else {
				//state.ILGenerator.Emit(OpCodes.Ldelem, this.Type);
				EmitLdelem(state.ILGenerator, this.Type);
			}
		}
		
		public override void EmitStore(AffeCompilerState state, Expression expr) {
			MethodInfo setter = null;
			if (this.mIndexer != null)
				setter = this.mIndexer.GetSetMethod();
			
			if (setter == null || !setter.IsStatic)
				this.Expression.Emit(state);
			
			foreach (Expression e in this.Index)
				e.Emit(state);
			expr.Emit(state);
			
			if (this.mIndexer != null) {
				if (setter == null)
					throw new AffeException("Indexer is read-only.", this);
				
				state.ILGenerator.Emit(setter.IsStatic ?
				                       OpCodes.Call :
				                       OpCodes.Callvirt, setter);
			} else {
				//state.ILGenerator.Emit(OpCodes.Stelem, this.Type);
				EmitStelem(state.ILGenerator, this.Type);
			}
		}
		
		public override void EmitForInvocation(AffeCompilerState state) {
			if (!this.Type.IsValueType) {
				this.Emit(state);
				return;
			}
			
			if (this.mIndexer == null) {
				this.Expression.Emit(state);
				foreach (Expression e in this.Index)
					e.Emit(state);
				
				state.ILGenerator.Emit(OpCodes.Ldelema, this.Type);
				
				return;
			}
			
			base.EmitForInvocation(state);
		}
	}
}
