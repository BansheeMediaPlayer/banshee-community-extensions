//
// CueSheetEntry.cs
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
using Banshee.Base;
using Hyena;
using Banshee.IO.SystemIO;
using Banshee.Playlists.Formats;
using Banshee.Collection.Database;

namespace Banshee.CueSheets
{
	public class CueSheetEntry : DatabaseTrackInfo
	{
		private string 		_performer;
		private string  	_composer="";
		private string  	_piece="";
		private string  	_file;
		private TimeSpan 	_length;
		private string  	_art="";
		private double		_offset;
		private double      _e_offset=-1.0;
		private string  	_title;
		private CueSheet 	_parent;
		
		public string EntryName {
			get { return _title; }
		}
		
		public CueSheet getCueSheet() {
			return _parent;
		}
		
		public override string ArtworkId {
      		get { return _art; }
		}
		
		public void setArtWorkId(string aaid) {
			_art=aaid;
		}
		
		public string file() {
			return _file;
		}
		
		public string id() {
			return MakeId (this);
		}
		
		public static string MakeId(TrackInfo trk) {
			return "title="+trk.TrackTitle+";offset="+trk.BeginOffset+";end_offset="+trk.EndOffset;
		}
		
		public string title() {
			return _title;
		}
		
		public string performer() {
			return _performer;
		}
		
		public double offset() {
			return _offset;
		}
		
		public double end_offset() {
			return _e_offset;
		}
		
		public void setNrOfTracks(int n) {
			this.TrackCount=n;
		}
		
		
		public void setPiece(string p) {
			_piece=p;
		}
		
		public string getPiece() {
			return _piece;
		}
		
		public string Piece {
			get { return _piece; }
			set { _piece=value; }
		}
		
		public override string Composer {
			get { return _composer; }
			set { _composer=value; }
		}
		
		public void setComposer(string c) {
			_composer=c;
		}
		
		public string getComposer() {
			return _composer;
		}
		
		public string Length {
			get { 
				double l=_e_offset-_offset;
				//Hyena.Log.Information ("Tracklengt="+l);
				int t=(int) (l*100.0);
				int m=t/(60*100);
				int secs=(t/100)%60;
				string ln=String.Format ("{0:00}",m)+":"+String.Format ("{0:00}",secs);
				//Hyena.Log.Information ("Tracklengt="+l+" sec="+secs+" ln="+ln);
				return ln;
			}
		}
		
		public override TimeSpan Duration {
			get {
				return _length;
			}
			set {
				_length=value;
			}
		}

		public double length() {
			return _length.TotalMilliseconds/1000.0;
			//return _length;
		}

		public void setLength(double l) {
			//_length=l;
			_e_offset=_offset+l;
			EndOffset=(int) (_e_offset*1000.0);	// millisecs
			System.Int64 ticks_100nanosecs=(System.Int64) (l*10000000); // 10 miljoen
			_length=new TimeSpan(ticks_100nanosecs);
		}
		
		public override string ToString() {
			return "nr: "+this.TrackNumber+", title: "+this.title ()+", file: "+this.file ();
		}
		
		public CueSheetEntry () {
			_file="";
			_title="";
			_performer="";
			_offset=0.0;
			BeginOffset=0;
			EndOffset=-1;
			_length=new TimeSpan(0);
			_parent=null;
			_art="";
			base.AlbumArtist="";
			base.TrackTitle="";
			base.AlbumTitle="";
			base.ArtistName="";
			base.TrackNumber=0;
			base.TrackCount=0;
			base.DiscNumber=0;
			base.CanPlay=true;
			base.CanSaveToDatabase=false;
			base.Duration=new System.TimeSpan(0,0,10,0);
			base.Uri=new Hyena.SafeUri("",false);
		}

		public CueSheetEntry (CueSheet s,string file,String artId,int nr,int cnt,string title,string performer,string album,double offset) : base() {
			_file=file;
			_title=title;
			_performer=performer;
			_offset=offset;
			BeginOffset=(int) (offset*1000.0); // milliseconds
			EndOffset=-1;
			_length=new TimeSpan(0);
			
			_parent=s;
			
			_art=artId;
			base.AlbumArtist=performer;
			base.TrackTitle=title;
			base.AlbumTitle=album;
			base.ArtistName=performer;
			base.TrackNumber=nr;
			base.TrackCount=cnt;
			base.DiscNumber=1;
			base.CanPlay=true;
			base.CanSaveToDatabase=false;
			base.Duration=new System.TimeSpan(0,0,10,0);
			base.Uri=new Hyena.SafeUri(_file,false);
		}
	}
}

