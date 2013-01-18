//
// CS_ArtistModelcs
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

namespace Banshee.CueSheets
{
	public class CS_ArtistModel : BansheeListModel<ArtistInfo>
	{
			private CueSheetsSource 		MySource;
			private List<CS_ArtistInfo> 	_artists;
			private CS_ArtistInfo       	_nullArtist;
			private CS_GenreInfo		 		_genre;
			private CS_ComposerInfo			_composer;
	
	        public CS_ArtistModel (CueSheetsSource s) {
				MySource=s;
				_nullArtist=new CS_ArtistInfo (null);
				_artists=new List<CS_ArtistInfo>();
				Selection=new Hyena.Collections.Selection();
	        }
	
	        public override void Clear () {
				// does nothing 
	        }
	
			private bool exists(string artist) {
				int i,N;
				for(i=0,N=_artists.Count;i<N && !Loosely.eq (_artists[i].Name,artist);i++);
				return i<N;
			}
			
	        public override void Reload () {
				HashSet<string> added=new HashSet<String>();
				_artists.Clear ();
				List<CueSheet> s=MySource.getSheets ();
				_artists.Add(_nullArtist);
				string composer="";
				if (_composer!=null) { composer=Loosely.prepare (_composer.Name); }
				for(int i=0;i<s.Count;i++) {
					string perf=Loosely.prepare (s[i].performer ());
					if (!added.Contains (perf)) {
						bool do_add=true;
						if (_genre!=null) {
							if (s[i].genre ()!=_genre.Genre) { do_add=false; }
						}
						if (_composer!=null) {
							if (!Loosely.eqp(composer,s[i].composer())) { do_add=false; }
						}						
						if (do_add) {
							CS_ArtistInfo a=new CS_ArtistInfo (s[i]);
							_artists.Add (a);
							added.Add (perf);
						}
					}
				}
				_artists.Sort (new CS_ArtistInfo.Comparer());
				base.RaiseReloaded ();
	        }
		
			public bool isNullArtist(ArtistInfo a) {
				CS_ArtistInfo aa=(CS_ArtistInfo) a;
				return aa.getCueSheet ()==null;
			}
			
	        public override int Count {
	            get { 
					return _artists.Count;
				}
	        }
		
			public void filterGenre(CS_GenreInfo g) {
				_genre=g;
				Reload ();
			}

			public void filterComposer(CS_ComposerInfo g) {
				_composer=g;
				Reload ();
			}
		
			public override ArtistInfo this[int index] {
				get {
					return _artists[index];
				} 	
			}


	}
}

