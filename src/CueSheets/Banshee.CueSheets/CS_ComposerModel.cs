using System;
using Banshee.Collection;
using System.Collections.Generic;

namespace Banshee.CueSheets
{
	public class CS_ComposerModel : BansheeListModel<CS_ComposerInfo>
	{
			private CueSheetsSource 		MySource;
			private List<CS_ComposerInfo> 	_composers;
			private CS_ComposerInfo       	_nullComposer;
			private GenreInfo		 		_genre;
			private ArtistInfo				_artist;
	
	        public CS_ComposerModel (CueSheetsSource s) {
				MySource=s;
				_nullComposer=new CS_ComposerInfo (null);
				_composers=new List<CS_ComposerInfo>();
				Selection=new Hyena.Collections.Selection();
	        }
	
	        public override void Clear () {
				// Does nothing
	        }
	
			private bool exists(string artist) {
				int i,N;
				for(i=0,N=_composers.Count;i<N && !Loosely.eq (_composers[i].Name,artist);i++);
				return i<N;
			}
			
	        public override void Reload () {
				HashSet<string> added=new HashSet<String>();
				_composers.Clear ();
				List<CueSheet> s=MySource.getSheets ();
				_composers.Add(_nullComposer);
				string artist="";
				if (_artist!=null) { artist=Loosely.prepare(_artist.Name); }
				for(int i=0;i<s.Count;i++) {
					string comp=Loosely.prepare (s[i].composer ());
					if (!added.Contains (comp)) {
						bool do_add=true;
						if (_genre!=null) {
							if (s[i].genre ()!=_genre.Genre) { do_add=false; }
						}
						if (_artist!=null) {
							if (!Loosely.eqp (artist,s[i].performer ())) { do_add=false; }
						}
						if (do_add) {
							CS_ComposerInfo a=new CS_ComposerInfo (s[i]);
							_composers.Add (a);
							added.Add (comp);
						}
					}
				}
				_composers.Sort (new CS_ComposerInfo.Comparer());
				base.RaiseReloaded ();
	        }
		
			public bool isNullComposer(CS_ComposerInfo a) {
				CS_ComposerInfo aa=(CS_ComposerInfo) a;
				return aa.getCueSheet ()==null;
			}
			
	        public override int Count {
	            get { 
					return _composers.Count;
				}
	        }
		
			public void filterGenre(GenreInfo g) {
				_genre=g;
				Reload ();
			}
		
			public void filterArtist(ArtistInfo g) {
				_artist=g;
				Reload ();
			}

			public override CS_ComposerInfo this[int index] {
				get {
					return _composers[index];
				} 	
			}


	}
}

