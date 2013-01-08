//
// CueSheet.cs
//
// Authors:
//   Cool Extension Author <hans@oesterholt.net>
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

namespace Banshee.CueSheets
{
	public class CueSheet : MemoryTrackListModel
	{
		private string 				_image_file_name;
		private string				_img_full_path;
		private string				_music_file_name;
		private string 				_title;
		private string 				_performer;
		private CueSheetEntry [] 	_tracks;
		private string				_cuefile;
		
		private void append(CueSheetEntry e) {
			if (_tracks==null) { 
				_tracks=new CueSheetEntry[1];
				_tracks[0]=e;
			} else {
				CueSheetEntry [] es=new CueSheetEntry[_tracks.Length+1];
				int i=0;
				for(i=0;i<_tracks.Length;i++) {
					es[i]=_tracks[i];
				}
				es[i]=e;
				_tracks=es;
			}
			this.Add (e);
		}
		
		public string imageFileName() {
			return _image_file_name;
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
		
		public CueSheetEntry entry(int i) {
			return _tracks[i];
		}
		
		public int nEntries() {
			if (_tracks==null) { return 0; }
			else {
				return _tracks.Length;
			}
		}
		
		public int searchIndex(double offset) {
			int k,N;
			for(k=0,N=nEntries ();k<N && offset>_tracks[k].offset ();k++);
			return k-1;
		}
		
		private string getArtId() {
			string aaid=CoverArtSpec.CreateArtistAlbumId (_performer,_title);
			if (!CoverArtSpec.CoverExists (aaid)) {
				if (File.Exists (_img_full_path)) {
					string path=CoverArtSpec.GetPathForNewFile (aaid,_img_full_path);
					File.Copy (_img_full_path,path);
					Console.WriteLine ("coverartpath="+path);
				}
			}
			string path1=CoverArtSpec.GetPath (aaid);
			Console.WriteLine ("coverartpath1="+path1);
			return aaid;
		}
		
		public CueSheet (string filename,string directory) {
			_cuefile=filename;
			Boolean _in_tracks=false;
			_image_file_name="";
			_img_full_path="";
			_music_file_name="";
			_title="";
			_performer="";
			_tracks=null;
			
			string e_perf="";
			string e_title="";
			double e_offset=-1.0;
			string aaid="";
			int nr=0;
			
			using (System.IO.StreamReader sr = System.IO.File.OpenText(filename)) {
	            string line = "";
            	while ((line = sr.ReadLine()) != null) {
					line=line.Trim ();
					if (line!="") {
						//Console.WriteLine ("it="+_in_tracks+", "+line);
						if (!_in_tracks) {
							if (line.Substring (0,9).ToLower()=="performer") {
								string p=line.Substring (9).Trim ();
								p=Regex.Replace (p,"[\"]","");
								_performer=p;
							} else if (line.Substring (0,5).ToLower ()=="title") {
								_title=Regex.Replace (line.Substring (5).Trim (),"[\"]","");
							} else if (line.Substring (0,4).ToLower ()=="file") {
								_music_file_name=line.Substring (4).Trim ();
								Match m=Regex.Match (_music_file_name,"([\"][^\"]+[\"])");
								Console.WriteLine ("match="+m);
								_music_file_name=m.ToString ();
								Console.WriteLine ("result="+_music_file_name);
								_music_file_name=Regex.Replace (_music_file_name,"[\"]","").Trim ();
								_music_file_name=directory+"/"+_music_file_name;
								Console.WriteLine ("music file="+_music_file_name);
							} else if (line.Substring(0,5).ToLower ()=="track") {
								_in_tracks=true;
							} else if (line.Substring (0,3).ToLower ()=="rem") {
								line=line.Substring (3).Trim ();
								if (line.Substring (0,5).ToLower ()=="image") {
									_image_file_name=line.Substring (5).Trim ();
									_image_file_name=Regex.Replace (_image_file_name,"[\"]","").Trim ();
									_img_full_path=directory+"/"+_image_file_name;
								}
							}
						} 
						
						
						if (_in_tracks) {
							if (aaid=="") { aaid=getArtId (); }
	
							Console.WriteLine ("line="+line);
							if (line.Substring (0,5).ToLower ()=="track") {
								if (e_offset>=0) {
									nr+=1;
									CueSheetEntry e=new CueSheetEntry(_music_file_name,aaid,nr,-1,e_title,e_perf,_title,e_offset);
									append (e);
									if (nr>1) {
										CueSheetEntry ePrev;
										ePrev=this.entry (nr-2);
										ePrev.setLength(e.offset ()-ePrev.offset());
									}
								}
								e_perf=_performer;
								e_title="";
								e_offset=-1.0;
							} else if (line.Substring (0,5).ToLower ()=="title") {
								e_title=Regex.Replace (line.Substring (5).Trim (),"[\"]","");
							} else if (line.Substring (0,9).ToLower ()=="performer") {
								e_perf=Regex.Replace (line.Substring (9).Trim (),"[\"]","");
							} else if (line.Substring (0,5).ToLower ()=="index") {
								string s=line.Substring (5).Trim ();
								s=Regex.Replace (s,"^\\s*[0-9]+\\s*","");
								string []parts=Regex.Split(s,"[:]");
								Console.WriteLine ("parts="+parts[0]+","+parts[1]+","+parts[2]);
								int min=Convert.ToInt32(parts[0]);
								int secs=Convert.ToInt32(parts[1]);
								int hsecs=Convert.ToInt32(parts[2]);
								e_offset=min*60+secs+(hsecs/100.0);
								
							}
						}
					}
    	        }
				if (e_offset>=0) {
					nr+=1;
					CueSheetEntry e=new CueSheetEntry(_music_file_name,aaid,nr,-1,e_title,e_perf,_title,e_offset);
					append (e);
					if (nr>1) {
						CueSheetEntry ePrev;
						ePrev=this.entry (nr-2);
						ePrev.setLength(e.offset ()-ePrev.offset());
					}
				}
				
				{
					int i;
					for(i=0;i<nEntries();i++) {
						entry (i).setNrOfTracks(nr);
					}
				}
        	}
		}
	}
}

