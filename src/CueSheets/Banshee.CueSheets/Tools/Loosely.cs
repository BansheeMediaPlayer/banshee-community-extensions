//
// Loosely.cs
//
// Authors:
//   Hans Oesterholt <hans@oesterholt.net>
//
// Copyright (C) 2013 Hans Oesterholt
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// 
using System;
using System.Text.RegularExpressions;

namespace Banshee.CueSheets
{
	public class Loosely
	{
		
		public Loosely () {
		}
		
		static Regex r1=new Regex(" and ");
		static Regex r2=new Regex("[_-]+");
		static Regex r3=new Regex("\\s+");
		
		static public string prepare(string a) {
			string aa=a.ToLower ().Trim ();
			aa=r3.Replace (r2.Replace (r1.Replace (aa," & "),"-")," ");
			return aa;
		}
		
		static public bool eq(string a,string b) {
			return prepare (a)==prepare (b);
		}
		
		static public bool eqp(string prepared,string b) {
			return prepared==prepare (b);
		}
	}
}

