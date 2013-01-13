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

namespace Banshee.CueSheets
{
	public class CueSheetEntry : TrackInfo
	{
		double 	_offset;
		string 	_performer;
		string 	_title;
		string  _file;
		double  _length;
		string  _art="";
		
		public override string ArtworkId {
      		get { return _art; }
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
		
		public double length() {
			return _length;
		}
		
		public void setLength(double l) {
			_length=l;
			System.Int64 ticks_100nanosecs=(System.Int64) (l*10000000); // 10 miljoen
			this.Duration=new TimeSpan(ticks_100nanosecs);
		}
		
		public override string ToString() {
			return "nr: "+this.TrackNumber+", title: "+this.title ()+", file: "+this.file ();
		}
		
		public CueSheetEntry (string file,String artId,int nr,int cnt,string title,string performer,string album,double offset) {
			_file=file;
			_title=title;
			_performer=performer;
			_offset=offset;
			_length=-1.0;
			
			_art=artId;
			this.AlbumArtist=performer;
			this.TrackTitle=title;
			this.AlbumTitle=album;
			this.ArtistName=performer;
			this.TrackNumber=nr;
			this.TrackCount=cnt;
			this.DiscNumber=1;
			this.CanPlay=true;
			this.CanSaveToDatabase=false;
			this.Duration=new System.TimeSpan(0,0,10,0);
			this.Uri=new Hyena.SafeUri(_file,false);
		}
	}
}

