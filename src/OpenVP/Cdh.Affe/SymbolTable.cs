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

namespace Cdh.Affe {
	public class SymbolTable : IEnumerable<Symbol> {
		private Dictionary<string, Symbol> mTable =
			new Dictionary<string, Symbol>();
		
		public Symbol this[string name] {
			get {
				return (Symbol) this.mTable[name];
			}
		}
		
		public SymbolTable() {
		}
		
		public Symbol GetSymbol(string name) {
			Symbol s;
			if (this.mTable.TryGetValue(name, out s))
				return s;
			
			return null;
		}
		
		public SymbolTable Copy() {
			SymbolTable n = new SymbolTable();
			
			foreach (Symbol i in this)
				if (!(i is VariableSymbol))
					n.AddSymbol(i);
			
			return n;
		}
		
		public void AddSymbol(Symbol s) {
			if (this.mTable.ContainsKey(s.Name))
				throw new ArgumentException("s: Symbol with name \"" + s.Name +
				                            "\" already in table.");
			
			this.mTable[s.Name] = s;
		}
		
		IEnumerator<Symbol> IEnumerable<Symbol>.GetEnumerator() {
			return this.mTable.Values.GetEnumerator();
		}
		
		IEnumerator IEnumerable.GetEnumerator() {
			return this.mTable.Values.GetEnumerator();
		}
	}
	
	public class SymbolTableStack {
		private List<SymbolTable> mStack = new List<SymbolTable>();
		
		public SymbolTableStack() {
		}
		
		public void PushTable(SymbolTable t) {
			this.mStack.Add(t);
		}
		
		public SymbolTable PopTable() {
			if (this.mStack.Count == 0)
				throw new InvalidOperationException("Stack is empty.");
			
			int last = this.mStack.Count - 1;
			
			SymbolTable table = this.mStack[last];
			this.mStack.RemoveAt(last);
			
			return table;
		}
		
		public Symbol GetSymbol(string name) {
			Symbol s = null;
			
			int i = this.mStack.Count - 1;
			while (s == null && i >= 0) {
				s = this.mStack[i].GetSymbol(name);
				--i;
			}
			
			return s;
		}
		
		public void AddSymbol(Symbol s) {
			if (this.mStack.Count == 0)
				throw new InvalidOperationException("Stack is empty.");
			
			this.mStack[this.mStack.Count - 1].AddSymbol(s);
		}
	}
}
