// ScriptingEnvironment.cs
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
using Cdh.Affe;

namespace OpenVP.Scripting {
	public static class ScriptingEnvironment {
		private static readonly Symbol[] mBaseSymbols = new Symbol[] {
			// Integers
			new TypeSymbol("int8", typeof(sbyte)),
			new TypeSymbol("uint8", typeof(byte)),
			new TypeSymbol("int16", typeof(short)),
			new TypeSymbol("uint16", typeof(ushort)),
			new TypeSymbol("int32", typeof(int)),
			new TypeSymbol("uint32", typeof(uint)),
			new TypeSymbol("int64", typeof(long)),
			new TypeSymbol("uint64", typeof(ulong)),
			
			// Floating point
			new TypeSymbol("float", typeof(float)),
			new TypeSymbol("double", typeof(double)),
			
			// Strings, objects, and type-inferencing
			new TypeSymbol("string", typeof(string)),
			new TypeSymbol("object", typeof(object)),
			new TypeSymbol("var", typeof(void))
			
		};
		
		public static void InstallBase(AffeCompiler compiler) {
			Install(compiler.SymbolTable, mBaseSymbols);
		}
		
		private static void Install(SymbolTable table,
		                            IEnumerable<Symbol> symbols) {
			foreach (Symbol i in symbols)
				table.AddSymbol(i);
		}
		
		private static readonly Type[] mTypeFloat = new Type[] {
			typeof(float)
		};
		
		private static readonly Type[] mTypeFloat2 = new Type[] {
			typeof(float), typeof(float)
		};
		
		private static readonly Type[] mTypeDouble = new Type[] {
			typeof(double)
		};
		
		private static readonly Type[] mTypeDouble2 = new Type[] {
			typeof(double), typeof(double)
		};
		
		private static MethodSymbol MakeMethodSymbol(string name, string mname,
		                                             Type type,
		                                             Type[] arguments) {			
			return new MethodSymbol(name, type.GetMethod(mname,
			                                             BindingFlags.Static |
			                                             BindingFlags.Public,
			                                             null, arguments,
			                                             null));
		}
		
		private static readonly Symbol[] mMathSymbols = new Symbol[] {
			MakeMethodSymbol("abs", "Abs", typeof(Math), mTypeFloat),
			
			MakeMethodSymbol("sin", "Sin", typeof(Math), mTypeDouble),
			MakeMethodSymbol("cos", "Cos", typeof(Math), mTypeDouble),
			MakeMethodSymbol("tan", "Tan", typeof(Math), mTypeDouble),
			
			MakeMethodSymbol("asin", "Asin", typeof(Math), mTypeDouble),
			MakeMethodSymbol("acos", "Acos", typeof(Math), mTypeDouble),
			MakeMethodSymbol("atan", "Atan", typeof(Math), mTypeDouble),
			
			MakeMethodSymbol("sqrt", "Sqrt", typeof(Math), mTypeDouble),
			MakeMethodSymbol("pow", "Pow", typeof(Math), mTypeDouble2),
			
			MakeMethodSymbol("log", "Log", typeof(Math), mTypeDouble2),
			MakeMethodSymbol("log10", "Log10", typeof(Math), mTypeDouble),
			
			MakeMethodSymbol("sign", "Sign", typeof(Math), mTypeFloat),
			
			MakeMethodSymbol("min", "Min", typeof(Math), mTypeFloat2),
			MakeMethodSymbol("max", "Max", typeof(Math), mTypeFloat2),
			
			MakeMethodSymbol("rand", "Rand", typeof(MathFunctions), mTypeDouble),
			MakeMethodSymbol("sigmoid", "Sigmoid", typeof(MathFunctions), mTypeDouble2),
			MakeMethodSymbol("sqr", "Square", typeof(MathFunctions), mTypeFloat),
			
			new FieldSymbol("pi", typeof(Math).GetField("PI",
			                                            BindingFlags.Static |
			                                            BindingFlags.Public))
		};
		
		public static void InstallMath(AffeCompiler compiler) {
			Install(compiler.SymbolTable, mMathSymbols);
		}
	}
	
	public static class MathFunctions {
		private static readonly Random mRandom = new Random();
		
		public static double Rand(double ceil) {
			return mRandom.NextDouble() * ceil;
		}
		
		public static double Sigmoid(double x, double c) {
			x = 1 + Math.Exp(-x * c);
			return (x != 0) ? (1.0 / x) : 0;
		}
		
		public static float Square(float f) {
			return f * f;
		}
	}
}
