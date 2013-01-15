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

namespace Banshee.CueSheets
{
	public class CueSheetEntry : TrackInfo
	{
		string 	_performer;
		string  _composer="";
		string  _piece="";
		string  _file;
		TimeSpan _length;
		string  _art="";
		double	_offset;
		string  _title;
		
		public override string ArtworkId {
      		get { return _art; }
		}
		
		public void setArtWorkId(string aaid) {
			_art=aaid;
		}
		
		public string file() {
			return _file;
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
		
		public void setNrOfTracks(int n) {
			this.TrackCount=n;
		}
		
		
		public void setComposer(string c) {
			_composer=c;
		}
		
		public void setPiece(string p) {
			_piece=p;
		}
		
		public string getPiece() {
			return _piece;
		}
		
		public string getComposer() {
			return _composer;
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
			System.Int64 ticks_100nanosecs=(System.Int64) (l*10000000); // 10 miljoen
			_length=new TimeSpan(ticks_100nanosecs);
		}
		
		public override string ToString() {
			return "nr: "+this.TrackNumber+", title: "+this.title ()+", file: "+this.file ();
		}

		public CueSheetEntry (string file,String artId,int nr,int cnt,string title,string performer,string album,double offset) : base() {
			_file=file;
			_title=title;
			_performer=performer;
			_offset=offset;
			_length=new TimeSpan(0);
			
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

