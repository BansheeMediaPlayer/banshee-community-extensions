//
// CS_GenreModel.cs
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
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
using System;
using Banshee.Collection;
using System.Collections.Generic;
using Banshee.Collection.Database;

namespace Banshee.CueSheets
{
	public class CS_GenreModel : BansheeListModel<CS_GenreInfo>
	{
			private CueSheetsSource 		MySource;
			private List<CS_GenreInfo>   	_genres;
			private CS_GenreInfo 			_nullGenre;
	
	        public CS_GenreModel (CueSheetsSource s) {
				MySource=s;
				_nullGenre=new CS_GenreInfo("<All Genres>");
				_genres=new List<CS_GenreInfo>();
				Selection=new Hyena.Collections.Selection();
	        }
	
	        public override void Clear () {
				// does nothing 
	        }
		
			private bool exists(string s) {
				int i,N;
				for(i=0,N=_genres.Count;i<N && !Loosely.eq (_genres[i].Genre,s);i++);
				return i<N;
			}
	
	        public override void Reload () {
				HashSet<string> added=new HashSet<string>();
				List<CueSheet> s=MySource.getSheets ();
				_genres.Clear ();
				_genres.Add (_nullGenre);
				for(int i=0;i<s.Count;i++) {
					string gen=Loosely.prepare (s[i].genre ());
					if (!added.Contains (gen)) {
						_genres.Add (new CS_GenreInfo(s[i].genre ()));
						added.Add (gen);
					}
				}
				_genres.Sort(new CS_GenreInfo.Comparer());
				base.RaiseReloaded ();
	        }
		
			public bool isNullGenre(CS_GenreInfo g) {
				return g==_nullGenre;
			}
			
	        public override int Count {
	            get { 
					return _genres.Count;
				}
	        }
		
			public override CS_GenreInfo this[int index] {
				get {
					return _genres[index];
				} 	
			}
	}
}

