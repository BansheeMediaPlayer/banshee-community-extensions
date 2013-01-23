//
// CueSheet.cs
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
using System.IO;
using System.Text.RegularExpressions;
using Banshee.CueSheets;
using Banshee.Collection;
using Banshee.Base;
using Banshee.Playlists.Formats;
using System.Collections.Generic;
using Hyena;
using Banshee.Collection.Database;
using Hyena.Data.Sqlite;
using Hyena.Collections;
using Banshee.Database;

namespace Banshee.CueSheets
{
	public class CueSheet : MemoryTrackListModel
	{
		private string 				_image_file_name;
		private string				_img_full_path;
		private string				_music_file_name;
		private string 				_title;
		private string 				_performer;
		private List<CueSheetEntry> _tracks=new List<CueSheetEntry>();
		private string				_cuefile;
		private string				_directory;
		private string				_basedir;
		
		private string 				_year;
		private string				_composer;
		private string				_subtitle;
		private string 				_cddbId;
		
		private Kind				_kind=Kind.CueSheet;
		
		public enum Kind {
			CueSheet,
			PlayList
		}
		
		public Kind SheetKind {
			get { return _kind; }
			set { _kind=value; }
		}

		public string id() {
			return "title="+_title+";performer="+_performer+
				   ";year="+_year+";composer="+_composer;
		}
		
		public string genre() {
			int n=_basedir.Length;
			string d=_directory.Substring (n);
			string r=Tools.firstpart(d);
			return r;
		}


		private void append(CueSheetEntry e) {
			_tracks.Add(e);
		}
		
		public string imageFileName() {
			return _image_file_name;
		}
		
		public string imageFullFileName() {
			return _img_full_path;
		}
		
		public string cueFile() {
			return _cuefile;
		}
		
		public string musicFileName() {
			return _music_file_name;
		}
		
		public string title() {
			return _title;
		}
		
		public string performer() {
			return _performer;
		}
		
		public string composer() {
			return _composer;
		}
		
		public string subtitle() {
			return _subtitle;
		}
		
		public string year() {
			return _year;
		}
		
		public string cddbId() {
			return _cddbId;
		}
		
		public CueSheetEntry entry(int i) {
			return _tracks[i];
		}
		
		public int nEntries() {
			return _tracks.Count;
		}
		
		public int searchIndex(string _current_entry_id,double _offset) {
			Hyena.Log.Information ("id="+_current_entry_id+", offset="+_offset);
			int k,N;
			if (_current_entry_id==null) {
				for(k=0,N=nEntries ();k<N && _offset>_tracks[k].offset ();k++);
				return k-1;
			} else {
				for(k=0,N=nEntries ();k<N && _current_entry_id!=entry (k).id ();k++);
				if (k==N) {
					return N-1;
				} else {
					CueSheetEntry e=entry (k);
					Hyena.Log.Information ("offset="+e.offset()+", endoffset="+e.end_offset()+" offset="+_offset);
					if (_offset<e.offset ()) {
						return k-1;
					} else if (e.end_offset ()<=0.0) {  // end track, we don't know
						return k;
					} else if (_offset>=e.end_offset ()) {
						return k+1;
					} else {
						return k;
					}
				}
			}
		}
		
		public void  resetArt() {
			if (_img_full_path!=null && _img_full_path!="") {
				if (File.Exists (_img_full_path)) {
					string aaid=CoverArtSpec.CreateArtistAlbumId (_performer,_title);
					string path=CoverArtSpec.GetPathForNewFile(aaid,_img_full_path);
					if (File.Exists (path)) { File.Delete (path); }
					File.Copy (_img_full_path,path);
					int i,N;
					for(i=0,N=nEntries ();i<N;i++) {
						entry (i).setArtWorkId(aaid);
					}
				}
			}
		}
		
		public string getArtId() {
			string aaid=CoverArtSpec.CreateArtistAlbumId (_performer,_title);
			if (!CoverArtSpec.CoverExists (aaid)) {
				if (File.Exists (_img_full_path)) {
					string path=CoverArtSpec.GetPathForNewFile (aaid,_img_full_path);
					if (!File.Exists (path)) {
						File.Copy (_img_full_path,path);
					}
				}
			} else {
				if (File.Exists (_img_full_path)) {
					string path=CoverArtSpec.GetPathForNewFile (aaid,_img_full_path);
					if (File.Exists (path)) { File.Delete (path); }
					if (!File.Exists (path)) {
						File.Copy (_img_full_path,path);
					}
				}
			}
			return aaid;
		}
		
		public bool eq(string s,string begin) {
			if (begin.Length>s.Length) { return false; }
			else {
				if (s.Substring(0,begin.Length).ToLower ()==begin.ToLower ()) {
					return true;
				} else {
					return false;
				}
			}
		}
		
		public override string ToString() {
			return "cuefile: "+this.cueFile();
		}
		
		public void SetPerformer(string p) {
			_performer=p;
		}
		
		public void SetTitle(string t) {
			_title=t;
		}
		
		public void SetComposer(string c) {
			_composer=c;
		}
		
		public void SetSubtitle(string s) {
			_subtitle=s;
		}
		
		public void SetYear(string y) {
			_year=y;
		}
		
		
		public void SetImagePath(string path) {
			Hyena.Log.Information ("SetImagePath: "+path);
			if (path!=null && path!="") {
				if (File.Exists (path)) {
					string fn=Tools.basename(path); 
					string fnp=Tools.makefile(_directory,fn);
					if (!File.Exists (fnp)) {
						File.Copy (path,fnp);
					}
					_image_file_name=fn;
					_img_full_path=fnp;
				}
			}
		}
		
		public void ClearTracks() {
			_tracks.Clear();
		}
		
		public void AddEntry(CueSheetEntry e) {
			append (e);
		}
		
		public CueSheetEntry AddTrack(string e_title,string e_perf,double e_offset) {
			int nr=_tracks.Count;
			string aaid=getArtId ();
			CueSheetEntry e=new CueSheetEntry(this,_music_file_name,aaid,nr,-1,e_title,e_perf,_title,e_offset);
			append (e);
			int i,N;
			for(i=0,N=nEntries ();i<N;i++) {
				_tracks[i].setNrOfTracks(N);
				if (i<N-1) {
					_tracks[i].setLength (_tracks[i+1].offset ()-_tracks[i].offset ());
				} else {
					_tracks[i].setLength (-1.0);
				}
			}
			return e;
		}
		
		private string indent="";
		
		private void wrtl(System.IO.StreamWriter wrt,string key,string val,bool rem=false) {
			string r="";
			if (rem) { r="REM "; }
			wrt.WriteLine (indent+r+key.ToUpper ()+" \""+val+"\"");
		}
		
		private void wrtl_file(System.IO.StreamWriter wrt,string file) {
			wrt.WriteLine (indent+"FILE \""+file+"\" WAVE");
		}
		
		private void wrtl_track(System.IO.StreamWriter wrt,int index) {
			wrt.WriteLine (indent+"TRACK "+String.Format ("{0:00}",index)+" AUDIO");
		}
		
		private void wrtl_index(System.IO.StreamWriter wrt,int inr,double offset) {
			int t=(int) (offset*100.0);
			int m=t/(100*60);
			int s=(t/100)%60;
			int hs=t%100; 
			wrt.WriteLine (indent+"INDEX "+String.Format ("{0:00}",inr)+" "+
			               				   String.Format("{0:00}",m)+":"+
			               				   String.Format ("{0:00}",s)+":"+
				                   		   String.Format ("{0:00}",hs)
				                   );		
		}
		
		public void Save() {
			if (!File.Exists (_cuefile+".bck")) {
				File.Copy (_cuefile,_cuefile+".bck");
			}
			System.IO.StreamWriter wrt=new System.IO.StreamWriter(_cuefile);
			resetArt ();
			indent="";
			wrtl (wrt,"creator","Banshee CueSheets Extension",true);
			wrtl (wrt,"creator-version",CS_Info.Version (),true);
			wrtl (wrt,"banshee-aaid",getArtId (),true);
			wrtl (wrt,"image",_image_file_name,true);
			wrtl (wrt,"composer",_composer,true);
			wrtl (wrt,"subtitle",_subtitle,true);
			wrtl (wrt,"year",_year,true);
			wrtl (wrt,"cddbid",_cddbId,true);
			wrtl (wrt,"performer",_performer);
			wrtl (wrt,"title",_title);
			string mfn=Tools.basename(_music_file_name);
			wrtl_file (wrt,mfn);
			
			int i,N;
			for(i=0,N=nEntries ();i<N;i++) {
				CueSheetEntry e=_tracks[i];
				writeEntry(wrt,e,i);
			}
			
			wrt.Close ();
		}
		
		void writeEntry(StreamWriter wrt,CueSheetEntry e,int i) {
			indent="  ";
			wrtl_track(wrt,i+1);
			indent="    ";
			wrtl (wrt,"title",e.title ());
			wrtl (wrt,"performer",e.performer ());
			wrtl (wrt,"piece",e.getPiece (),true);
			wrtl (wrt,"composer",e.getComposer (),true);
			wrtl_index(wrt,1,e.offset ());
		}
		
		static private Regex unq1=new Regex("^[\"]");
		static private Regex unq2=new Regex("[\"]$");
	
		private string unquote(string s) {
			return unq1.Replace (unq2.Replace (s,""),"");
		}
		
		public void iLoad() {
			string filename=_cuefile;
			using (System.IO.StreamReader sr = System.IO.File.OpenText(filename)) {
				iLoad (sr);
			}
		}
		
		public void iLoad(StreamReader sr) {
		
			_composer="";
			_year="";
			_subtitle="";
			_cddbId="";
			
			Boolean _in_tracks=false;
			string e_perf="";
			string e_title="";
			double e_offset=-1.0;
			string e_piece="";
			string e_composer="";
			string aaid="";
			int nr=0;
			
			//string filename=_cuefile;
			string directory=_directory;
			
            string line = "";
        	while ((line = sr.ReadLine()) != null) {
				line=line.Trim ();
				if (line!="") {
					//Console.WriteLine ("it="+_in_tracks+", "+line);
					if (!_in_tracks) {
						if (eq(line,"performer")) { 
							_performer=unquote(line.Substring (9).Trim ());
						} else if (eq(line,"title")) {
							_title=unquote(line.Substring (5).Trim ());
						} else if (eq(line,"file")) { 
							_music_file_name=line.Substring (4).Trim ();
							Match m=Regex.Match (_music_file_name,"([\"][^\"]+[\"])");
							_music_file_name=m.ToString ();
							_music_file_name=unquote(_music_file_name).Trim ();
							_music_file_name=Tools.makefile(directory,_music_file_name);
						} else if (line.Substring(0,5).ToLower ()=="track") {
							_in_tracks=true;
						} else if (eq(line,"rem")) {
							//Hyena.Log.Information (line);
							line=line.Substring (3).Trim ();
							if (eq(line,"image")) { 
								_image_file_name=line.Substring (5).Trim ();
								_image_file_name=unquote(_image_file_name).Trim ();
								_img_full_path=Tools.makefile(directory,_image_file_name);
							} else if (eq (line,"composer")) {
								_composer=unquote(line.Substring (8).Trim ());
							} else if (eq (line,"subtitle")) {
								_subtitle=unquote(line.Substring (8).Trim ());
							} else if (eq (line,"year")) {
								_year=unquote(line.Substring (4).Trim ());
							} else if (eq (line,"cddbid")) {
								_cddbId=unquote(line.Substring (6).Trim ());
							}
						}
					} 
					
					
					if (_in_tracks) {
						if (aaid=="") { aaid=getArtId (); }

						//Console.WriteLine ("line="+line);
						if (eq(line,"track")) { 
							if (e_offset>=0) {
								nr+=1;
								CueSheetEntry e=new CueSheetEntry(this,_music_file_name,aaid,nr,-1,e_title,e_perf,_title,e_offset);
								e.setComposer (e_composer);
								e.setPiece (e_piece);
								append (e);
								if (nr>1) {
									CueSheetEntry ePrev;
									ePrev=this.entry (nr-2);
									ePrev.setLength(e.offset ()-ePrev.offset());
								}
							}
							e_perf=_performer;
							e_title="";
							e_composer=_composer;
							e_offset=-1.0;
						} else if (eq(line,"title")) {
							e_title=unquote(line.Substring (5).Trim ());
						} else if (eq(line,"performer")) { 
							e_perf=unquote(line.Substring (9).Trim ());
						} else if (eq(line,"rem")) {
							line=line.Substring (3).Trim ();
							if (eq (line,"composer")) {
								e_composer=unquote(line.Substring (8).Trim ());
							} else if (eq(line,"piece")) {
								e_piece=unquote(line.Substring (5).Trim ());
							} 
						} else if (eq(line,"index")) { 
							string s=line.Substring (5).Trim ();
							s=Regex.Replace (s,"^\\s*[0-9]+\\s*","");
							string []parts=Regex.Split(s,"[:]");
							int min=Convert.ToInt32(parts[0]);
							int secs=Convert.ToInt32(parts[1]);
							int hsecs=Convert.ToInt32(parts[2]);
							e_offset=min*60+secs+(hsecs/100.0);
						}
					}
				}
	        }
			//Console.WriteLine ("Last entry adding");
			if (e_offset>=0) {
				nr+=1;
				CueSheetEntry e=new CueSheetEntry(this,_music_file_name,aaid,nr,-1,e_title,e_perf,_title,e_offset);
				e.setComposer (e_composer);
				e.setPiece (e_piece);
				append (e);
				if (nr>1) {
					CueSheetEntry ePrev;
					ePrev=this.entry (nr-2);
					ePrev.setLength(e.offset ()-ePrev.offset());
				}
			}
			//Console.WriteLine ("Last entry added");
			
			{
				int i;
				for(i=0;i<nEntries();i++) {
					entry (i).setNrOfTracks(nr);
				}
				//Console.WriteLine ("Ready");
			}
			
			base.Selection.MaxIndex=nEntries ();
		}		
		
		public void load(CueSheet s) {
			load (s._cuefile,s._directory,s._basedir);
		}
		
		public void load(string filename,string directory,string basedir) {
			Clear ();
			_cuefile=filename;
			_basedir=basedir;
			_directory=directory;
			try {
				iLoad();
			} catch(System.Exception e) {
				Console.WriteLine ("CueSheet: Cannot load "+filename);
				Console.WriteLine (e.ToString ());
			}
		}
		
		private class CacheableDatabaseModel : ICacheableDatabaseModel
        {
            public static CacheableDatabaseModel Instance = new CacheableDatabaseModel ();
            public int FetchCount { get { return 200; } }
            public string ReloadFragment { get { return null; } }
            public string SelectAggregates { get { return null; } }
            public string JoinTable { get { return null; } }
            public string JoinFragment { get { return null; } }
            public string JoinPrimaryKey { get { return null; } }
            public string JoinColumn { get { return null; } }
            public bool CachesJoinTableEntries { get { return false; } }
            public bool CachesValues { get { return false; } }
            public Selection Selection { get { return null; } }
        }
		
		public override void Clear() {
			base.Selection.Clear ();
			_tracks.Clear();
			_cuefile="";
			_image_file_name="";
			_img_full_path="";
			_music_file_name="";
			_title="";
			_performer="";
			_basedir="";
			_directory="";
		}
		
		public CueSheet() {
			Clear ();
		}
		
		public CueSheet(string filename) {
			Clear ();
			load (filename,"","");
		}
		
		public CueSheet (string filename,string directory,string basedir) {
			Clear ();
			load (filename,directory,basedir);
		}

		#region implemented abstract members of MemoryTrackListModel
		public override TrackInfo this [int index] {
			get { Hyena.Log.Information ("get: index="+index); return _tracks[index];  }
		}
		
		public override int Count {
			get { return _tracks.Count; }
		}

		public override void Reload () {
			this.iLoad ();
		}
		
		public override TrackInfo GetRandom (DateTime since) {
			return _tracks[0];			
		}

		public override int IndexOf (TrackInfo track) {
			int i,N;
			for(i=0,N=_tracks.Count;i<N && CueSheetEntry.MakeId(track)!=_tracks[i].id ();i++);
			if (i==N) { return -1; }
			else { return i; }
		}

		#endregion
	}
}

