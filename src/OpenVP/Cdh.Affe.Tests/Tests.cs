using System;
using System.Collections.Generic;
using System.Reflection.Emit;

using Cdh.Affe;
using Cdh.Affe.Tree;

using NUnit.Framework;

namespace Tests {
	[TestFixture]
	public class Tests {
		private delegate void DMCall();
		
		private DMCall MakeDMCall(DynamicMethod dm) {
			return (DMCall) dm.CreateDelegate(typeof(DMCall), this);
		}
		
		private DMCall SharedCompile(string script) {
			return this.MakeDMCall(this.SharedCompiler.Compile(script));
		}
		
		private void RunScript(string script) {
			this.SharedCompile(script)();
		}
		
		public Tests() {
			this.SharedCompiler.SymbolTable.AddSymbol(new TypeSymbol("object", typeof(object)));
			this.SharedCompiler.SymbolTable.AddSymbol(new TypeSymbol("int", typeof(int)));
			this.SharedCompiler.SymbolTable.AddSymbol(new TypeSymbol("IComparable", typeof(IComparable)));
		}
		
		private AffeCompiler SharedCompiler = new AffeCompiler(typeof(Tests));
		
		[AffeBound]
		public float BoundFloat = 0;
		
		[AffeBound]
		public float BoundMethod(float f) {
			Assert.AreEqual(1f, f, 0f, "Bound read");
			return 2;
		}
		
		private List<float> FloatList = new List<float>();
		
		[AffeBound]
		public void Push(float f) {
			this.FloatList.Add(f);
		}
		
		private void AssertList(float[] list, string name) {
			Assert.AreEqual(list.Length, this.FloatList.Count,
			                "Result count (" + name + ")");
			
			for (int i = 0; i < list.Length; i++)
				Assert.AreEqual(list[i], this.FloatList[i],
				                string.Format("Result item {0} ({1})",
				                              i, name));
		}
		
		[Test]
		public void Binding() {
			this.BoundFloat = 0f;
			
			this.RunScript("BoundFloat = 1;");
			
			Assert.AreEqual(1f, this.BoundFloat, 0f);
			
			this.RunScript("BoundFloat = BoundMethod(BoundFloat);");
			
			Assert.AreEqual(2f, this.BoundFloat, 0f);
		}
		
		[Test]
		public void Loop() {
			this.State.Clear();
			this.FloatList.Clear();
			
			this.RunScript("x=0; while (x<10) { Push(x); x = x+1; }");
			
			Assert.AreEqual(10, this.FloatList.Count, "Iterations");
			
			for (int i = 0; i < 10; i++)
				Assert.AreEqual((float) i, this.FloatList[i], 0f,
				                "Loop iteration");
		}
		
		[Test]
		public void Conditional() {
			this.State.Clear();
			this.FloatList.Clear();
			
			this.RunScript("x = 0; if (x == 0) Push(1); if (x) Push(1);" +
			               "x = 1; if (x < 1) Push(0); else Push(1);" +
			               "x = 2; if (x >= 2) Push(1); " +
			               "if (x >= 2 && x == 1) Push(1);");
			
			Assert.AreEqual(3, this.FloatList.Count, "Push count");
			
			for (int i = 0; i < this.FloatList.Count; i++)
				Assert.AreEqual(1f, this.FloatList[i], "Result item " + i);
		}
		
		[AffeTransform(typeof(float))]
		public static void BoundTransform(AffeCompilerState state,
		                                  Expression[] exprs) {
			Assert.AreEqual(2, exprs.Length, "Argument count to BoundTransform");
			
			state.CastTo(exprs[0], typeof(double)).Emit(state);
			state.CastTo(exprs[1], typeof(double)).Emit(state);
			
			state.ILGenerator.Emit(OpCodes.Call, typeof(Math).GetMethod("Pow"));
			state.ILGenerator.Emit(OpCodes.Conv_R4);
		}
		
		[Test]
		public void Transform() {
			this.State.Clear();
			
			this.BoundFloat = 0f;
			
			this.RunScript("x = 2; y = 8; BoundFloat = BoundTransform(x, y);");
			
			Assert.AreEqual(256f, this.BoundFloat, 0f, "Transform result");
		}
		
		[AffeBound]
		public BoundObjectTest BoundObject = new BoundObjectTest();
		
		[AffeBound]
		public BoundObjectTest BoundObject2 = new BoundObjectTest();
		
		[Test]
		public void Invocation() {
			AffeCompiler comp = new AffeCompiler(typeof(Tests));
			comp.SymbolTable.AddSymbol(new TypeSymbol("BoundClass", typeof(BoundObjectTest)));
			DMCall dm = (DMCall) comp.Compile("Push(BoundObject.InstanceCall());" +
			                                  "Push(BoundObject.Property.InstanceCall());" +
			                                  "Push(BoundClass.StaticCall());" +
			                                  "BoundObject.Field = 1;" +
			                                  "BoundObject.FieldProperty = BoundObject.Field + 1;")
				.CreateDelegate(typeof(DMCall), this);
			
			this.FloatList.Clear();
			
			dm();
			
			this.AssertList(new float[] { 1, 1, 2 }, "results");
			
			Assert.AreEqual(1f, this.BoundObject.Field, "BoundObject.Field");
			Assert.AreEqual(2f, this.BoundObject.FieldProperty, "BoundObject.FieldProperty");
		}
		
		[Test]
		public void Declaration() {
			this.State.Clear();
			
			AffeCompiler comp = new AffeCompiler(typeof(Tests));
			comp.SymbolTable.AddSymbol(new TypeSymbol("int", typeof(int)));
			
			DMCall dm = (DMCall) comp.Compile("a = 2.5; int i = a; BoundFloat = i;")
				.CreateDelegate(typeof(DMCall), this);
			
			BoundFloat = 0f;
			
			dm();
			
			Assert.AreEqual(2f, this.BoundFloat, "Result");
		}
		
		[AffeBound]
		public object UntypedObject = new BoundObjectTest();
		
		[Test]
		public void LateBound() {
			BoundObjectTest obj = (BoundObjectTest) this.UntypedObject;
			
			obj.Field = 0f;
			obj.FieldProperty = 0f;
			
			this.FloatList.Clear();
			
			this.RunScript("UntypedObject$Field = 2; UntypedObject$Property$FieldProperty = 3;" +
			               "Push(UntypedObject$Field);Push(UntypedObject$FieldProperty);" +
			               "Push(UntypedObject$InstanceCall());" +
			               "Push(UntypedObject$Mult(2, 4));");
			
			Assert.AreEqual(2f, obj.Field, "UntypedObject.Field");
			Assert.AreEqual(3f, obj.FieldProperty, "UntypedObject.FieldProperty");
			
			this.AssertList(new float[] { 2, 3, 1, 8 }, "results");
		}
		
		[AffeBound]
		public string BoundString = null;
		
		[Test]
		public void Cast() {
			this.BoundString = null;
			this.BoundFloat = 1;
			
			this.RunScript("BoundString=((object) 2.5).GetType().FullName;" +
			               // This makes sure that (identifier) doesn't get
			               // consumed as a cast.
			               "BoundFloat=2 * (BoundFloat);");
			
			Assert.AreEqual("System.Single", this.BoundString, "BoundString");
			Assert.AreEqual(2, this.BoundFloat, "BoundFloat");
		}
		
		[Test]
		public void ValueType() {
			this.BoundString = null;
			this.RunScript("BoundString=(1).ToString();");
			
			Assert.AreEqual("1", this.BoundString, "BoundString constant");
			
			this.BoundFloat = 0;
			this.RunScript("BoundFloat=2.5;BoundString=BoundFloat.ToString();");
			
			Assert.AreEqual("2.5", this.BoundString, "BoundString field");
			
			this.FloatList.Clear();
			this.RunScript("IComparable c = 2.0;Push(c.CompareTo(2.0));Push(c);");
			
			this.AssertList(new float[] { 0, 2 }, "results");
		}
		
		[Test]
		public void Ternary() {
			this.FloatList.Clear();
			
			this.BoundFloat = 2;
			this.RunScript("Push(BoundFloat ? 4 : 5);Push(false ? 6 : 7);");
			
			this.AssertList(new float[] { 4, 7 }, "results");
		}
		
		[Test]
		public void Null() {
			this.FloatList.Clear();
			
			this.RunScript("if (BoundObject) Push(1);" +
			               "if (BoundObject == null) Push(2);" +
			               "if (BoundObject != null) Push(3);");
			
			this.AssertList(new float[] { 1, 3 }, "results");
		}
		
		[Test]
		public void OperatorOverload() {
			this.BoundObject.Field = 0;
			this.BoundObject2.Field = 0;
			this.BoundFloat = 0;
			
			this.RunScript("BoundObject.Field = 5;" +
			               "BoundObject2.Field = 3;" +
			               "BoundFloat = BoundObject - BoundObject2;");
			
			Assert.AreEqual(2, this.BoundFloat, "BoundFloat");
		}
		
		[AffeBound]
		public float[] BoundArray = null;
		
		[Test]
		public void Indexing() {
			this.FloatList.Clear();
			this.BoundArray = new float[] {1, 2, 3};
			
			this.RunScript("Push(BoundArray[0]);" +
			               "Push(BoundArray[1]);" +
			               "Push(BoundArray[2]);" +
			               "BoundArray[1] = 5;");
			
			Assert.AreEqual(1, this.BoundArray[0], "BoundArray[0]");
			Assert.AreEqual(5, this.BoundArray[1], "BoundArray[1]");
			Assert.AreEqual(3, this.BoundArray[2], "BoundArray[2]");
			
			this.AssertList(new float[] { 1, 2, 3 }, "results");
			
			this.BoundFloat = 0;
			this.BoundObject.Field = 0;
			
			this.RunScript("BoundFloat = BoundObject[5, 6];" +
			               "BoundObject[3, 4] = 3;");
			
			Assert.AreEqual(30, this.BoundFloat, "BoundFloat");
			Assert.AreEqual(15, this.BoundObject.Field, "BoundObject.Field");
		}
		
		[Test]
		public void Comment() {
			this.FloatList.Clear();
			
			this.RunScript("Push(1); // Push(2);\n" +
			               "Push(3);\n" +
			               "// test\n" +
			               " // Push(4);\n");
			
			this.AssertList(new float[] { 1, 3 }, "results");
		}
		
		[AffeBound]
		public ScriptState State = new ScriptState();
		
		[Test]
		public void Persistence() {
			this.FloatList.Clear();
			
			this.RunScript("int persist1 = 4; int! persist2; persist2 = 5; x = 6;");
			
			this.RunScript("int! persist1; int! persist2; Push(persist1); Push(persist2); Push(x);");
			
			this.AssertList(new float[] { 0, 5, 6 }, "results");
		}
		
		[AffeBound]
		public const float LiteralPI = (float) Math.PI;
		
		[Test]
		public void Literals() {
			this.BoundFloat = 0;
			
			this.RunScript("BoundFloat = LiteralPI;");
			
			Assert.AreEqual(LiteralPI, this.BoundFloat, "BoundFloat");
		}
	}
	
	public class BoundObjectTest {
		public BoundObjectTest Property {
			get {
				return this;
			}
		}
		
		public float Field = 0f;
		
		public float FieldProperty {
			get {
				return this.mProperty;
			}
			set {
				this.mProperty = value;
			}
		}
		
		public float mProperty = 0f;
		
		public float InstanceCall() {
			return 1f;
		}
		
		public float Mult(float x, float y) {
			return x*y;
		}
		
		public static float StaticCall() {
			return 2f;
		}
		
		public static float operator-(BoundObjectTest a, BoundObjectTest b) {
			return a.Field - b.Field;
		}
		
		public float this[float x, float y] {
			get {
				return x * y;
			}
			set {
				this.Field = x * y + value;
			}
		}
	}
}
