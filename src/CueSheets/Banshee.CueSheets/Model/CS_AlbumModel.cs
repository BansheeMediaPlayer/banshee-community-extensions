//
// CS_AlbumModel.cs
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
using System.Text.RegularExpressions;

namespace Banshee.CueSheets
{
	public class CS_AlbumModel : BansheeListModel<AlbumInfo>
	{
		private CueSheetsSource MySource;
		private List<CS_AlbumInfo> 	_filteredList;
		private CS_GenreInfo 			_genre;
		private ArtistInfo 			_artist;
		private CS_ComposerInfo 	_composer;
		private string				_album_search;
		private bool				_also_in_tracks;

        public CS_AlbumModel (CueSheetsSource s) {
			MySource=s;
			_filteredList=new List<CS_AlbumInfo>();
			Selection=new Hyena.Collections.Selection();
			_genre=null;
			Reload ();
        }

        public override void Clear () {
			// does nothing 
        }

        public override void Reload () {
			_filteredList.Clear ();
			List<CueSheet> s=MySource.getSheets ();
			int i,N;
			string artist="";
			if (_artist!=null) {
				artist=Loosely.prepare(_artist.Name);
			}
			string composer="";
			if (_composer!=null) {
				composer=Loosely.prepare (_composer.Name);
			}	
			for(i=0,N=s.Count;i<N;i++) {
				bool do_add=true;
				if (_genre!=null) {
					if (s[i].genre ()!=_genre.Genre) { do_add=false; }
				}
				if (_artist!=null && do_add) {
					if (!Loosely.eqp (artist,s[i].performer ())) { do_add=false; }
				}
				if (_composer!=null && do_add) {
					if (!Loosely.eqp (composer,s[i].composer ())) { do_add=false; }
				}
				if (_album_search!=null && do_add) {
					if (!s[i].title ().ToLower().Contains (_album_search) &&
					    !s[i].performer ().ToLower ().Contains (_album_search) &&
					    !s[i].composer ().ToLower ().Contains (_album_search)) {
						if (_also_in_tracks) {
							CueSheet q=s[i];
							int k,M;
							bool can_add=false;
							for(k=0,M=q.nEntries ();k<M && !can_add;k++) {
								CueSheetEntry e=q.entry (k);
								if (e.title ().ToLower ().Contains (_album_search) ||
								    e.performer ().ToLower ().Contains (_album_search) ||
								    e.getComposer ().ToLower ().Contains (_album_search)) {
									can_add=true;
								}
							}
							do_add=can_add;
						} else {
							do_add=false;
						}
					}
				}
				
				if (do_add) {
					_filteredList.Add (new CS_AlbumInfo(s[i]));
				}
			
			}
			_filteredList.Sort (new CS_AlbumInfo.Comparer());
			base.RaiseReloaded ();
        }
	
		public void filterGenre(CS_GenreInfo g) {
			if (g==null) {
				_genre=null;
				Reload ();
			} else if (_genre==null) {
				_genre=g;
				_artist=null;
				_composer=null;
				Reload ();
			} else {
				if (_genre.Genre==g.Genre) {
					// do nothing
				}  else {
					_genre=g;
					_artist=null;
					_composer=null;
					Reload ();
				}
			}
		}
	
		public CS_GenreInfo filterGenre() {
			return _genre;
		}
	
		public void filterArtist(ArtistInfo a) {
			_artist=a;
			Reload();
		}
	
		public ArtistInfo filterArtist() {
			return _artist;
		}
	
		public CS_ComposerInfo filterComposer() {
			return _composer;
		}

		// Filter on all albums of which title/performer/year/etc. match search
		// null means: no search string
		public void filterAlbumOrTracks(string search, bool also_in_tracks) {
			_album_search=search.ToLower ();
			_also_in_tracks=also_in_tracks;
			Reload ();
		} 
		
		public void filterAlbumOrTracks(out string search_string, out bool also_in_tracks) {
			search_string=_album_search;
			also_in_tracks=_also_in_tracks;
		}

		public void filterComposer(CS_ComposerInfo c) {
			_composer=c;
			Reload ();
		}
		
        public override int Count {
            get { 
				//Console.WriteLine ("albumcount="+_filteredList.Count);
				return _filteredList.Count;
			}
        }
	
		public override AlbumInfo this[int index] {
			get {
				if (index>=Count) { return new CS_AlbumInfo(null); }
				if (index<0) { return new CS_AlbumInfo(null); }
				return _filteredList[index];
			} 	
		}
	}
}

