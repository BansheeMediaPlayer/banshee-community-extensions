using System;
using System.Runtime.InteropServices;
using Mono.Unix;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Runtime.CompilerServices;
using System.IO;

namespace Banshee.CueSheets 
{
	
	enum  Mp3SpltOptions {
	  SPLT_OPT_PRETEND_TO_SPLIT,
	  SPLT_OPT_QUIET_MODE,
	  SPLT_OPT_DEBUG_MODE,
	  SPLT_OPT_SPLIT_MODE,
	  SPLT_OPT_TAGS,
	  SPLT_OPT_XING,
	  SPLT_OPT_CREATE_DIRS_FROM_FILENAMES,
	  SPLT_OPT_OUTPUT_FILENAMES,
	  SPLT_OPT_FRAME_MODE,
	  SPLT_OPT_AUTO_ADJUST,
	  SPLT_OPT_INPUT_NOT_SEEKABLE,
	  SPLT_OPT_PARAM_NUMBER_TRACKS,
	  SPLT_OPT_PARAM_REMOVE_SILENCE,
	  SPLT_OPT_PARAM_GAP,
	  SPLT_OPT_ALL_REMAINING_TAGS_LIKE_X,
	  SPLT_OPT_AUTO_INCREMENT_TRACKNUMBER_TAGS,
	  SPLT_OPT_ENABLE_SILENCE_LOG,
	  SPLT_OPT_FORCE_TAGS_VERSION,
	  SPLT_OPT_LENGTH_SPLIT_FILE_NUMBER,
	  SPLT_OPT_REPLACE_TAGS_IN_TAGS,
	  SPLT_OPT_OVERLAP_TIME,
	  SPLT_OPT_SPLIT_TIME,
	  SPLT_OPT_PARAM_THRESHOLD,
	  SPLT_OPT_PARAM_OFFSET,
	  SPLT_OPT_PARAM_MIN_LENGTH,
	  SPLT_OPT_PARAM_MIN_TRACK_LENGTH,
	  SPLT_OPT_ARTIST_TAG_FORMAT,
	  SPLT_OPT_ALBUM_TAG_FORMAT,
	  SPLT_OPT_TITLE_TAG_FORMAT,
	  SPLT_OPT_COMMENT_TAG_FORMAT,
	  SPLT_OPT_REPLACE_UNDERSCORES_TAG_FORMAT,
	  SPLT_OPT_SET_FILE_FROM_CUE_IF_FILE_TAG_FOUND,
	};
	
	enum SpltOutputFileNamesOptions {
	  SPLT_OUTPUT_FORMAT,
	  SPLT_OUTPUT_DEFAULT,
	  SPLT_OUTPUT_CUSTOM
	};
	
	[StructLayout(LayoutKind.Sequential)]
	struct splt_progres {
	  public int progress_text_max_char;
	  [MarshalAsAttribute(UnmanagedType.ByValTStr, SizeConst = 512)]
	  public string filename_shorted;
	  public float percent_progress;
	  public int current_split;
	  public int max_splits;
	  public int progress_type;
	  public int silence_found_tracks;
	  public float silence_db_level;
	  public int user_data;
	  public IntPtr progress_callback_function; // void (*progress)(struct splt_progres*, void *);
	  public IntPtr progress_cb_data; 			 // public void *progress_cb_data;
	};
	
	public class Mp3Split : IDisposable
	{
		private IntPtr _mp3_state;
		private List<Mp3SplitTrack> _tracks=new List<Mp3SplitTrack>();
		
		private int 	_progress_n_tracks;
		private int 	_progress_current_track;
		private float 	_progress_of_current_track;
		private bool	_finished;
		private bool	_cancelled;
		private string  _file_format;
		
		private bool	_convert_to_latin1;
		private string  _directory;
		
		private CueSheet _sheet;
		
		public delegate void ProgressCallBack(IntPtr progres,IntPtr data);
		
		public Mp3Split (CueSheet s) {
			int error=0;
			_sheet=s;
			
			_mp3_state=mp3splt_new_state (out error);
			error=mp3splt_find_plugins (_mp3_state);
			Hyena.Log.Information ("mp3splt: find_plugins result:"+error);
			
			mp3splt_set_int_option(_mp3_state,Mp3SpltOptions.SPLT_OPT_SET_FILE_FROM_CUE_IF_FILE_TAG_FOUND,1);
			mp3splt_set_int_option(_mp3_state,Mp3SpltOptions.SPLT_OPT_OUTPUT_FILENAMES,(int) SpltOutputFileNamesOptions.SPLT_OUTPUT_FORMAT);
			mp3splt_set_oformat(_mp3_state,"@A - @b - @n - @t",out error);
			_file_format="@A - @b - @n - @t";
			mp3splt_put_cue_splitpoints_from_file(_mp3_state,s.cueFile (),out error);
			mp3splt_set_default_genre_tag(_mp3_state,s.genre ());

			int count,ctags;
			IntPtr pointarray=mp3splt_get_splitpoints(_mp3_state,out count,out error);
			IntPtr tagarray=mp3splt_get_tags (_mp3_state,out ctags,out error);
			Hyena.Log.Information ("count="+count+", ctags="+ctags);

			{ 
				int i;
				_tracks.Clear ();
				int N=(count<ctags) ? count : ctags;
				for(i=0;i<N;i++) {
					_tracks.Add (new Mp3SplitTrack(pointarray,tagarray,i));
				}
			}
		}
		
		public void SplitWithPaths() {
			int error=0;
			mp3splt_set_int_option(_mp3_state,Mp3SpltOptions.SPLT_OPT_CREATE_DIRS_FROM_FILENAMES,1);
			mp3splt_set_oformat(_mp3_state,"@A/@b/@n @t",out error);
			_file_format="@A/@b/@n @t";
		}
		
		public int ProgressCurrentTrack  {
    		[MethodImpl(MethodImplOptions.Synchronized)]
    		get { return _progress_current_track; }
    		[MethodImpl(MethodImplOptions.Synchronized)]
    		set { _progress_current_track = value; }
		}

		public int ProgressNTracks  {
    		[MethodImpl(MethodImplOptions.Synchronized)]
    		get { return _progress_n_tracks; }
    		[MethodImpl(MethodImplOptions.Synchronized)]
    		set { _progress_n_tracks = value; }
		}
		
		public float ProgressOfCurrentTrack {
    		[MethodImpl(MethodImplOptions.Synchronized)]
    		get { return _progress_of_current_track; }
    		[MethodImpl(MethodImplOptions.Synchronized)]
    		set { _progress_of_current_track = value; }
		}
		
		public bool SplitFinished {
    		[MethodImpl(MethodImplOptions.Synchronized)]
    		get { return _finished; }
    		[MethodImpl(MethodImplOptions.Synchronized)]
    		set { _finished = value; }
		}
		
		public bool Cancelled {
			get { return _cancelled; }
			set { _cancelled=value; }
		}
		
		public void Progress(IntPtr progr,IntPtr data) {
			splt_progres pr=(splt_progres) Marshal.PtrToStructure (progr,typeof(splt_progres));
			this.ProgressCurrentTrack=pr.current_split;
			this.ProgressNTracks=pr.max_splits;
			this.ProgressOfCurrentTrack=pr.percent_progress;
		}
		
		private void Splitter() {
			int result=mp3splt_split (_mp3_state);
			if (result>=0) {
				Hyena.Log.Information ("convert to latin1="+_convert_to_latin1);
				if (_convert_to_latin1) {
					convertToLatin1 ();
				}
			}
			SplitFinished=true;
			LogResult ("Splitter",result);
		}
		
		public void CancelSplit() {
			int err=0;
			mp3splt_stop_split (_mp3_state,out err);
			LogResult ("CancelSplit",err);
			Cancelled=true;
		}
		
		public void SplitToDir(string directory,bool convert_to_latin1) {
			mp3splt_set_path_of_split(_mp3_state,directory);
			mp3splt_set_progress_function (_mp3_state,new ProgressCallBack(Progress),IntPtr.Zero);
			SplitFinished=false;
			Cancelled=false;
			_convert_to_latin1=convert_to_latin1;
			_directory=directory;
			Thread split_thread=new Thread(new ThreadStart(Splitter));
			split_thread.Start ();
		}
		
		public void convertToLatin1() {
			setLatinTags (_sheet);
		}
		
		private void setLatinTags(CueSheet s) {
			int i,N;
			for(i=0,N=s.nEntries ();i<N;i++) {
				ProgressCurrentTrack=i+1;
				setLatinTag (i,s,s.entry (i));
			}
		}
		
		private void setLatinTag(int track,CueSheet s,CueSheetEntry e) {
			string fn=_file_format;
			fn=fn.Replace ("@A",s.performer());
			fn=fn.Replace ("@b",s.title ());
			fn=fn.Replace ("@n",(track+1).ToString ());
			fn=fn.Replace ("@t",e.title ());
			fn=_directory+"/"+fn+".mp3";
			Hyena.Log.Information ("file to convert:"+fn);
			if (File.Exists(fn)) {
				TagLib.File file=TagLib.File.Create(fn);
				if (file==null) { 
					Hyena.Log.Error ("Cannot create taglib file for "+fn);
					return;
				} else {
					Hyena.Log.Information("Setting tags for "+fn);
					file.Tag.Album=s.title ();
					file.Tag.AlbumArtists=new string[]{s.performer ()};
					file.Tag.Composers=new string[]{s.composer ()};
					file.Tag.Genres=new string[]{s.genre ()};
					file.Tag.Title=e.title ();
					file.Tag.Track=(uint) track+1;
					file.Tag.Performers=new string[]{e.performer ()};
					file.Save ();
				}
			}
		}
		
		#region IDisposable implementation
		public void Dispose ()
		{
			int error=0;
			mp3splt_free_state(_mp3_state,out error);
			Hyena.Log.Information ("mp3splt-free-state error="+error);
		}
		#endregion
		
		public static bool IsSupported(string file) {
			if (Regex.IsMatch (file,"[.][Mm][Pp][3]$") ||
			    Regex.IsMatch (file,"[.][Oo][Gg][Gg]$")) {
				return true;
			} else {
				return false;
			}
		}
		
		private void LogResult(string s,int res) {
			if (res<0) {
				Hyena.Log.Error ("mp3splt: "+s+" error="+res+", "+mp3splt_get_strerror(_mp3_state,res));
			} else {
				Hyena.Log.Information ("mp3splt: "+s+" result="+res+", "+mp3splt_get_strerror(_mp3_state,res));
			}
		}
		
		private void LogResult(int res) {
			LogResult ("",res);
		}
		
		private static int _dll_present=0;
		
		public static bool DllPresent() {
			if (_dll_present==0) {
				Hyena.Log.Information ("Checking presence of libmp3splt");
				try {
					string version=mp3splt_get_version();
					Hyena.Log.Information ("libmp3splt found. Good thing. Version present: "+version);
					_dll_present=1;
					return true;
				} catch (System.DllNotFoundException ex) {
					Hyena.Log.Error("libmp3splt not present on this system");
					Hyena.Log.Information (ex.ToString ());
					_dll_present=-1;
					return false;
				}
			} else {
				return _dll_present>0;
			}
		}
		
		public static string mp3splt_get_version() {
			StringBuilder version=new StringBuilder(20);
			mp3splt_get_version (version);
			return version.ToString ();
		}
		
		[DllImport ("libmp3splt.dll")]
		private static extern IntPtr mp3splt_get_version(StringBuilder dest);
		
		[DllImport ("libmp3splt.dll")]
		private static extern IntPtr mp3splt_new_state(out int error);

		[DllImport ("libmp3splt.dll")]
		private static extern void mp3splt_free_state(IntPtr state,out int error);
		
		[DllImport ("libmp3splt.dll")]
		private static extern void mp3splt_put_cue_splitpoints_from_file(IntPtr state,string cuefile,out int err);
		
		[DllImport ("libmp3splt.dll")]
		private static extern string mp3splt_get_filename_to_split(IntPtr state);
		
		[DllImport ("libmp3splt.dll")]
		private static extern string mp3splt_get_strerror(IntPtr state, int error_code);
		
		[DllImport ("libmp3splt.dll")]
		private static extern void mp3splt_set_int_option(IntPtr state,Mp3SpltOptions op,int val);
		
		[DllImport ("libmp3splt.dll")]
		private static extern int mp3splt_set_default_genre_tag(IntPtr state, string default_genre_tag);
		
		[DllImport ("libmp3splt.dll")]
		private static extern void mp3splt_export_to_cue(IntPtr state, string out_file, short stop_at_total_time, out int error);
		
		[DllImport ("libmp3splt.dll")]
		private static extern IntPtr mp3splt_get_splitpoints(IntPtr state,out int count,out int err);
		
		[DllImport ("libmp3splt.dll")]
		private static extern IntPtr mp3splt_get_tags(IntPtr state,out int tags_number, out int error);

		[DllImport ("libmp3splt.dll")]
		private static extern void mp3splt_set_oformat(IntPtr state,string format_string,out int error);
		
		[DllImport ("libmp3splt.dll")]
		private static extern int mp3splt_set_path_of_split(IntPtr state, string path);
		
		[DllImport ("libmp3splt.dll")]
		private static extern int mp3splt_set_progress_function(IntPtr state,ProgressCallBack callback,IntPtr cb_data);
		
		[DllImport ("libmp3splt.dll")]
		private static extern int mp3splt_split(IntPtr state);

		[DllImport ("libmp3splt.dll")]
		private static extern void mp3splt_stop_split(IntPtr state,out int error);
		
		[DllImport ("libmp3splt.dll")]
		private static extern int mp3splt_find_plugins(IntPtr state);
		
		[DllImport ("libmp3splt.dll")]
		private static extern int mp3splt_append_tags(IntPtr state,
		                                              string title, 
		                                              string artist,
		                                              string album,  
		                                              string performer,
		                                              string year,
		                                              string comment,
		                                              int track,
		                                              string genre);

	}
	
}

