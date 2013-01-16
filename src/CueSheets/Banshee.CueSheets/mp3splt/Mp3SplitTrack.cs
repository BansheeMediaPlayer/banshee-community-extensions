using System;
using System.Runtime.InteropServices;
using Mono.Unix;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace Banshee.CueSheets
{
	[StructLayout(LayoutKind.Sequential)]
	struct SpltPoint {
	  public long val;
	  public IntPtr name;
	  public int type;
	};
	
	[StructLayout(LayoutKind.Sequential)]
	struct Tag {
	  public IntPtr title;
	  public IntPtr artist;
	  public IntPtr album;
	  public IntPtr performer;
	  public IntPtr year;
	  public IntPtr comment;
	  public int track;
	  public IntPtr genre;
	  public int tags_version;
	  public int set_original_tags;
	};

	public class Mp3SplitTrack
	{
		private IntPtr	   _points;
		private SpltPoint  _splitpoint;
		private Tag        _tag;
		private int        _index;
		private string	   _splitname;
		
		public Mp3SplitTrack (IntPtr splitpoints,IntPtr tags,int index) {
			_index=index;
			_points=splitpoints;
			_splitpoint=(SpltPoint) Marshal.PtrToStructure (_points+Marshal.SizeOf(_splitpoint)*index,typeof(SpltPoint));
			_tag=(Tag) Marshal.PtrToStructure(tags+Marshal.SizeOf (_tag)*index,typeof(Tag));
			_splitname=(string) Marshal.PtrToStringAnsi (_splitpoint.name);
		}
		
		public string TrackName {
			get { return _splitname; }
		}
		
		public int TrackIndex {
			get { return _index; }
		}
		
		public double OffsetInSeconds {
			get { return ((double) _splitpoint.val)/100.0; }
		}
		
		public string Album {
			get { return (string) Marshal.PtrToStringAnsi (_tag.album); }
		}
		
		public string Title {
			get { return (string) Marshal.PtrToStringAnsi (_tag.title); }
		}
		
		public string Performer {
			get { return (string) Marshal.PtrToStringAnsi (_tag.performer); }
		}
		
		public string Artist {
			get { return (string) Marshal.PtrToStringAnsi (_tag.artist); }
		}
		
		public int TrackNumber {
			get { return _tag.track; }
		}
		
		public string Genre {
			get { return (string) Marshal.PtrToStringAnsi (_tag.genre); }
		}
		
		public override string ToString() {
			return "nr="+TrackNumber+"album="+Album+",title="+Title+",performer="+Performer+", offset="+OffsetInSeconds+
				   "name="+TrackName;
		}
	}
}