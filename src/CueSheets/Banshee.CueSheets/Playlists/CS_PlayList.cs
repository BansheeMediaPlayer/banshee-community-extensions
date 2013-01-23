using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Collections;

namespace Banshee.CueSheets
{
	public class CS_PlayList 
	{
		private List<CS_PlayListEntry> 	_playlist;
		private string					_name;
		private CueSheetCollection		_csc;
		private CS_TrackInfoDb			_db;
		
		public class Comparer : IComparer<CS_PlayList>  {
			private CaseInsensitiveComparer cmp=new CaseInsensitiveComparer();
		    public int Compare( CS_PlayList g1,CS_PlayList g2 )  {
				return cmp.Compare (g1._name,g2._name);
		    }
		}
		
		public string PlsName {
			get { return _name; }
			set { _name=value; }
		}
		
		public CS_PlayList (CS_TrackInfoDb con,string name,CueSheetCollection csc) {
			_playlist=new List<CS_PlayListEntry>();
			_db=con;
			_name=name;
			_csc=csc;
			Load();
		}
		
		public void Add(CueSheetEntry e) {
			_playlist.Add (new CS_PlayListEntry(e));
			//Insert (e,_playlist.Count);
		}
		
		public void Insert(CueSheetEntry e,int index) {
			CS_PlayListEntry ple=new CS_PlayListEntry(e);
			Hyena.Log.Information ("index="+index+", e="+e);
			_playlist.Insert (index,ple);
		}
		
		public int Count {
			get { return _playlist.Count; }
		}
		
		public CueSheetEntry this[int index] {
			get { 
				CS_PlayListEntry ple=_playlist[index]; 
				return ple.GetCueSheetEntry();
			}
		}
		
		public CueSheet GetCueSheet() {
			CueSheet s=new CueSheet();
			s.SetTitle (_name);
			s.SheetKind=CueSheet.Kind.PlayList;
			foreach (CS_PlayListEntry ple in _playlist) {
				s.AddEntry(ple.GetCueSheetEntry ());
			}
			return s;
		}
		
		public void Save() {
			string pls="";
			string sep="";
			Hyena.Log.Information("Playlist="+_playlist);
			foreach (CS_PlayListEntry ple in _playlist) {
				Hyena.Log.Information ("ple="+ple);
				CueSheetEntry e=ple.GetCueSheetEntry ();
				Hyena.Log.Information ("e="+e);
				CueSheet s=e.getCueSheet();
				Hyena.Log.Information("sheet="+s+", entry="+e);
				string id=s.id ();
				string e_id=e.id ();
				string entry="cuesheet="+id+"%%%entry="+e_id;
				pls+=sep;
				pls+=entry;
				sep="#@%@#";
			}
			_db.Set("playlist:"+_name,pls);
		}
		
		public void Load() {
			string pls=null;
			_db.Get("playlist:"+_name,out pls,null);
			if (pls==null) {
				return;
			} else {
				string [] entries=Regex.Split (pls,"#@%@#");
				//Hyena.Log.Information ("playlist name="+pls+", entries="+entries);
				foreach (string e in entries) {
					Hyena.Log.Information ("entry="+e);
					if (e!="") {
						string []ids=Regex.Split (e,"%%%");
						string cs_id=ids[0].Substring ("cuesheet=".Length);
						string cs_e=ids[1].Substring("entry=".Length);
						//Hyena.Log.Information ("finding: cs-id="+cs_id+", e-id="+cs_e);
						CueSheetEntry entry=_csc.FindEntry(cs_id,cs_e);
						if (entry!=null) {
							Hyena.Log.Information("adding entry:"+entry);
							Add (entry);
						}
					}
				}
			}
		}
		
	}
}

