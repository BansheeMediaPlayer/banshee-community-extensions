//
// CS_AlbumInfo.cs
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
using System.Collections;
using System.Collections.Generic;

namespace Banshee.CueSheets
{
	public class CS_AlbumInfo : AlbumInfo
	{
		private CueSheet _sheet;
		
		public class Comparer : IComparer<CS_AlbumInfo> {
			private CaseInsensitiveComparer cmp=new CaseInsensitiveComparer();
		    public int Compare( CS_AlbumInfo a1,CS_AlbumInfo a2 )  {
				return cmp.Compare (a1.Title+a1.ArtistName,
				                    a2.Title+a2.ArtistName);
		    }
		}
		
		public CS_AlbumInfo (CueSheet s) {
			_sheet=s;
			if (s==null) {
				base.ArtistName="none";
				base.Title="none"; 
			} else {
				base.ArtistName=s.performer ();
				base.Title=s.title ();
				base.ArtworkId=s.getArtId ();
			}
		}
		
		public CueSheet getSheet() {
			return _sheet;
		}
	}
}

