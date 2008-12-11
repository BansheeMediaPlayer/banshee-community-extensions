	using System;
	using System.Threading;
	using System.IO;
	
namespace Banshee.Plugins.Lyrics
{	
public class LyricsCache {	
	
	private static string from_dir, to_dir;
	
	private static string GetLyricsFilename(string artist,string title)
	{
		return LyricsPlugin.LyricsDefaultDir + artist+"_"+ title+".lyrics";
	}
	
	public static void DeleteLyric(string artist,string title)
	{
		string filename=GetLyricsFilename(artist,title);
		DeleteLyric(filename);
	}
	
	public static void DeleteLyric(string filename)
	{
		try{
			if (File.Exists(filename)){
				Console.WriteLine("deleting file "+filename);	
				File.Delete(filename);
			}
		}catch(Exception e){
	        Console.WriteLine("Unable to delete file: "+e.Message);
	        //do nothing
	    }
	}
	public static string ReadLyric(string artist,string title)
	{
		string filename=GetLyricsFilename(artist,title);
		return ReadLyric(filename);
	}
		
	public static string ReadLyric(String filename)
	{
		try{
			if (File.Exists(filename))
				return File.ReadAllText(filename);	
		}catch(Exception e){
	        Console.WriteLine("Unable to read file: "+filename+"\n"+e.Message);
	        //do nothing
		}
		return null;
	}
		
	private static void Move()
	{
		string[] all_files=Directory.GetFiles(from_dir,"*.lyrics");
		foreach (string old_filename in all_files){
			string[] splitted_path=old_filename.Split('/');
			string filename=splitted_path[splitted_path.Length-1];
			string new_filename=to_dir+filename;
			try{
				if (!File.Exists(new_filename)){
					File.Move(old_filename,new_filename);
				}else{
					File.Delete(old_filename);
				}
			}catch(Exception e){
				Console.WriteLine("problem moving lyrics file:"+old_filename+"  "+e.Message);	
			}	
		}
		from_dir=null;
		to_dir=null;
	}
	
	public static void MoveLyrics(string from,string to )
	{
		lock(typeof(LyricsCache));	
		from_dir=from;
		to_dir=to;
		if (from_dir==null || to_dir==null)
			return;
		if (!Directory.Exists(from_dir)|| !Directory.Exists(to_dir))
			return ;
		if (from_dir.Equals(to_dir))
			return;		
		ThreadStart getLyricsDelegate = new ThreadStart(Move);
		new Thread(getLyricsDelegate).Start();
		//Move(from,to);
	}
	
	public static void WriteLyric(string artist,string title,string lyrics)
	{
	    string filename=GetLyricsFilename(artist,title);
	    try{
	        if (File.Exists(filename))
	            return;
	        //create a new file
	        FileStream stream=File.Create(filename);
	        stream.Close();
	        //write the lyrics
	        File.WriteAllText(filename,lyrics);
	        Console.WriteLine("lyrics successfull written "+filename);
	    }catch(Exception e){
	        Console.WriteLine("Unable to save lyrics: "+e.Message);
	    }
		
	    return;
	}
	
	public static bool IsCached(string artist,string title)
	{
	    string filename=GetLyricsFilename(artist,title);
	    if (File.Exists(filename))
	        return true;
	    else
	   		return false;
	}
}
}