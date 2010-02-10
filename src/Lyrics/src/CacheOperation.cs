using System;
using System.Threading;
using System.IO;

namespace Banshee.Plugins.Lyrics
{	
public class CacheOperation {	
	
	private static string from_dir, to_dir;
    public static void Delete(string artist,string title)
	{
    	string filename=LyricsPlugin.LyricsDefaultDir + artist+"_"+ title+".lyrics";
		Delete(filename);
    
    }
    
	public static void Delete(string filename)
	{
    	try{
    		if (File.Exists(filename)){
				Hyena.Log.Debug("Deleting file " + filename);	
    			File.Delete(filename);
			}
    	}catch(Exception e){
			Hyena.Log.DebugFormat("Unable to delete file {0}: {1} ",filename,e.Message);
        }
    }
	
	public static string Read(string artist,string title)
	{
		string filename=LyricsPlugin.LyricsDefaultDir + artist+"_"+title+".lyrics";
		return Read(filename);
	}
		
    public static string Read(String filename)
	{
    	try{
    		if (File.Exists(filename)){
    			return File.ReadAllText(filename);	
    		}
    	}catch(Exception e){
            Hyena.Log.DebugFormat("Unable to read file {0}: {1} ",filename,e.Message);
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
				Hyena.Log.DebugFormat("problem moving lyrics file {0}: {1}",old_filename,e.Message);	
			}	
		}
		from_dir=null;
		to_dir=null;
	}
	
	public static void MoveLyrics(string from,string to )
	{
		//not good: it was not really useful because i start a thread.	
		lock(typeof(CacheOperation));	
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
	}
	
    public static void Write(string artist,string title,string lyrics) 
	{
        string filename=LyricsPlugin.LyricsDefaultDir + artist+"_"+ title+".lyrics";
        try{
            if (File.Exists(filename))
                return;
            //create a new file
            FileStream stream=File.Create(filename);
            stream.Close();
            //write the lyrics
            File.WriteAllText(filename,lyrics);
            Hyena.Log.DebugFormat("Lyric successfully written " + filename);
        }catch(Exception e){
            Hyena.Log.DebugFormat("Unable to save lyric {0}: {1} ",filename,e.Message);
            //do nothing
        }
        return;
    }
}
}