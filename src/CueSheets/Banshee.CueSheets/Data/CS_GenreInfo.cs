//
// GenreInfo.cs
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
using System.Collections.Generic;
using System.Collections;

namespace Banshee.CueSheets
{
	public class CS_GenreInfo
	{
		private string _genre;
		
		public class Comparer : IComparer<CS_GenreInfo>  {
			private CaseInsensitiveComparer cmp=new CaseInsensitiveComparer();
		    public int Compare( CS_GenreInfo g1,CS_GenreInfo g2 )  {
				return cmp.Compare (g1._genre,g2._genre);
		    }
		}
		
		public CS_GenreInfo ()
		{
			_genre="";
		}
		
		public CS_GenreInfo(string s) {
			_genre=s;
		}
		
		public string Genre {
			get { return _genre; }
			set { _genre=value; }
		}
	}
}

