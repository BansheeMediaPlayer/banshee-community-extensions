using System;
using System.Reflection.Emit;

using Cdh.Affe;
using Cdh.Affe.Tree;

namespace Tests {
	public delegate void DMCall();
	
	public class MainClass {
		public static void Main(string[] args) {
//			TestOne.Run();
			TestTwo.Run();
//			TestOne.Run();
//			TestTwo.Run();
//			TestOne.Run();
//			TestTwo.Run();
			TestCall.Run();
//			TestPersist.Run();
//			TestCast.Run();
//			TestInvoke.Run();
			TestScope.Run();
		}
	}
	
	public static class Util {
		public static DynamicMethod Compile(string script, Type t) {
			return new AffeCompiler(t).Compile(script);
		}
	}
	
	public class TestOne {
		[AffeBound]
		public float X = 0;
		
		[AffeBound]
		public static readonly float Y = 10;
		
		[AffeBound]
		public static float abs(float a) {
			if (a < 0)
				return -a;
			
			return a;
		}
		
		public static void Run() {
			Console.WriteLine("1)");
			
			DateTime start = DateTime.Now;
			DynamicMethod dm = Util.Compile("X = abs((5 - Y) * 3);", typeof(TestOne));
			Console.WriteLine(DateTime.Now - start);
			
			TestOne obj = new TestOne();
			
			start = DateTime.Now;
			for (int i = 0; i < 100000; i++)
				dm.Invoke(null, new object[] { obj });
			Console.WriteLine(DateTime.Now - start);
			
			DMCall d = (DMCall) dm.CreateDelegate(typeof(DMCall), obj);
			
			start = DateTime.Now;
			for (int i = 0; i < 100000; i++)
				d();
			Console.WriteLine(DateTime.Now - start);
			
			Console.WriteLine(obj.X);
		}
	}
	
	public class TestTwo {
		[AffeBound]
		public float X = 0;
		
		[AffeBound]
		public static readonly float Y = 10;
		
		[AffeBound, AffeTransform(typeof(float))]
		public static void abs(AffeCompilerState comp, Expression[] exp) {
			if (exp.Length != 1)
				throw new ArgumentException("Wrong number of arguments to method.");
			
			ILGenerator il = comp.ILGenerator;
			
			Label l = il.DefineLabel();
			
			exp[0].Emit(comp);
			il.Emit(OpCodes.Dup);
			il.Emit(OpCodes.Ldc_R4, 0f);
			il.Emit(OpCodes.Bge_Un, l);
			
			il.Emit(OpCodes.Neg);
			il.MarkLabel(l);
		}
		
		public static void Run() {
			Console.WriteLine("2)");
			
			DateTime start = DateTime.Now;
			DynamicMethod dm = Util.Compile("X = abs((5 - Y) * 3);", typeof(TestTwo));
			Console.WriteLine(DateTime.Now - start);
			
			TestTwo obj = new TestTwo();
			
			start = DateTime.Now;
			for (int i = 0; i < 100000; i++)
				dm.Invoke(null, new object[] { obj });
			Console.WriteLine(DateTime.Now - start);
			
			DMCall d = (DMCall) dm.CreateDelegate(typeof(DMCall), obj);
			
			start = DateTime.Now;
			for (int i = 0; i < 100000; i++)
				d();
			Console.WriteLine(DateTime.Now - start);
			
			Console.WriteLine(obj.X);
		}
	}
	
	public class TestCall {
		[AffeBound("w")]
		public static float WriteValue(float p) {
			Console.WriteLine("> " + p);
			return 0;
		}
		
		public static void Run() {
			DynamicMethod dm = Util.Compile("a=1+3;a=a*2;while (a > 0) { a = a-1; if (a < 4) continue; w(a); } w(0);", typeof(TestCall));
			
			TestCall call = new TestCall();
			
			DMCall d = (DMCall) dm.CreateDelegate(typeof(DMCall), call);
			
			d();
		}
	}
	
	public class TestPersist {
		[AffeBound]
		public ScriptState State = new ScriptState();
		
		[AffeBound]
		public object nullobj = null;
		
		[AffeBound]
		public void print(float f) {
			Console.WriteLine(f);
		}
		
		public static void Run() {
			DynamicMethod dm = Util.Compile("if (b >= 10) return; a=1;b=b+a;", typeof(TestPersist));
			
			TestPersist obj = new TestPersist();
			
			DMCall d = (DMCall) dm.CreateDelegate(typeof(DMCall), obj);
			
			for (int i = 0; i < 15; i++) {
				d();
				Console.WriteLine(obj.State.GetValue<float>("b"));
			}
			
			dm = Util.Compile("print(x);x=2;nullobj.ToString();", typeof(TestPersist));
			
			d = (DMCall) dm.CreateDelegate(typeof(DMCall), obj);
			
			try {
				d();
			} catch {
			}
			
			try {
				d();
			} catch {
			}
		}
	}
	
	public class TestCast {
		[AffeBound]
		public static void Print(bool i) {
			Console.WriteLine(i);
		}
		
		public static void Run() {
			DynamicMethod dm = Util.Compile("Print(1);Print(0);Print(200);", typeof(TestCast));
			
			DMCall d = (DMCall) dm.CreateDelegate(typeof(DMCall), null);
			
			d();
		}
	}
	
	public class TestInvoke {
		[AffeBound("int")]
		public static readonly object Value = (int) 42;
		
		public static void Run() {
			AffeCompiler compiler = new AffeCompiler(typeof(TestInvoke));
			compiler.SymbolTable.AddSymbol(new TypeSymbol("Console", typeof(Console)));
			compiler.SymbolTable.AddSymbol(new TypeSymbol("string", typeof(string)));
			compiler.SymbolTable.AddSymbol(new TypeSymbol("var", typeof(void)));
			
			DynamicMethod dm = compiler.Compile("var s = \"test\";Console.Out.WriteLine(s.Substring(1));");
			
			DMCall d = (DMCall) dm.CreateDelegate(typeof(DMCall), null);
			
			d();
		}
	}
	
	public class TestScope {
		[AffeBound("i")]
		public static int Num = 0;
		
		[AffeBound]
		public static void print(object o) {
			Console.WriteLine(o.ToString());
		}
		
		public static void Run() {
			AffeCompiler compiler = new AffeCompiler(typeof(TestScope));
			compiler.SymbolTable.AddSymbol(new TypeSymbol("var", typeof(void)));
			
			DynamicMethod dm = compiler.Compile("if(1){var s = \"hi\"; }var s = 1;print(s);");//var s = i;print(s);");
			
			DMCall d = (DMCall) dm.CreateDelegate(typeof(DMCall), null);
			
			d();
		}
	}
}