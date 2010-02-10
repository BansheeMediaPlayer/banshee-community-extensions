// LyricsManager.cs created with MonoDevelop
// User: sgrang at 19:56 24/08/2007
//
// To change standard headers go to Edit->Preferences->Coding->Standard Headers
//

using System;
using System.Collections;
using System.Threading;
using Mono.Unix;

namespace Banshee.Plugins.Lyrics
{

public class LyricsManager
{
	private string	 	lyric;
	private string		base_url;
	private ArrayList 	sourceList;
	
	private static LyricsManager instance = new LyricsManager();
	
	private LyricsManager():base()
	{
		sourceList	= new ArrayList();
		sourceList.Add(new Lyrc());
		sourceList.Add(new AutoLyrics());
		sourceList.Add(new Lyriki());
		sourceList.Add(new Lyriki2());
	}
	
	/*begin properties*/
	internal string Lyric
	{
		get { return lyric; }
		set { lyric=value; LyricsPlugin.OnLyricEvent(this,new LyricEventArgs(value));}
	}
	
	internal string Base{
		get{return base_url;}
	}
	
	internal static LyricsManager Instance{
		get{return instance;}
	}
	/*end properties*/
	
	/*public methods*/
	public void GetLyrics(Object param)
	{
		LyricParam lParam = (LyricParam)param;
		
		if (lParam.url != null)
			GetLyricsFromLyrc(lParam.url);
		else
			GetLyrics(lParam.artist,lParam.title);
	}
	
	public void  RefreshLyrics(Object param)
	{
		LyricParam lParam= (LyricParam)param;
		RefreshLyrics(lParam.artist,lParam.title);
	}
	
	public void AddLyrics(string artist,string title,string lyric)
	{
		lyric=lyric.Replace("\n","<br>");
		LyricsCache.WriteLyric(artist,title,lyric);
		Lyric = lyric;
	}

	/*private methods*/
	private string GetLyrics(string artist,string title)
	{
	  
	  if (artist ==null || title==null)
	  	return null;
	  	
		//check if the lyric is in cache
		if (LyricsCache.IsCached(artist,title)){
			Lyric=LyricsCache.ReadLyric(artist,title);
			return Lyric;
		}
		//check if the netowrk is up
		if(!BansheeWidgets.GetNetwork().Connected){
			Lyric= Constants.no_network_string;
			return Lyric;
		}
			
		//download the lyrics from lyric sources
		string result=DownloadLyrics(artist,title);			
		
		//write Lyrics in cache if possible
		if (LyricFound(result))
			LyricsCache.WriteLyric(artist,title,result);
		else
			result=GetSuggestions(artist,title);
				
		//when the LyricsManager thread are slower don't update the lyric
		if (LyricOutOfDate(artist,title)){
				return null;
		}
		
		Lyric=result;
		return Lyric;
	}
	
	private string GetLyricsFromLyrc(string Url)
	{
		string result;
		if (Url == null)
			return null;

		/*search for the lyric on lyrc*/
		LyricBaseSource lyrc_server=(LyricBaseSource)sourceList[0];
		result = lyrc_server.GetLyrics(Url);
		
		if (LyricFound(result))
		    result=string.Format ("{0} <br><br>Powered by {1} ({2})",result, lyrc_server.Name, lyrc_server.Url);
		
		Lyric=result;
		return Lyric;
	}
	
	private string RefreshLyrics(string artist,string title)
	{
		//refresh impossible when network is down
		if(!BansheeWidgets.GetNetwork().Connected)
			return null;
		
		LyricsCache.DeleteLyric(artist,title);
		return GetLyrics(artist,title);
	}
	
	private bool LyricFound(string l)
	{
		if (l!=null && !l.Equals(""))
			return true;
		else
			return false;
	}
	
	private bool LyricOutOfDate(string artist,string title)
	{
		if (artist != BansheeWidgets.CurrentTrack.GetArtist() || title!= BansheeWidgets.CurrentTrack.GetTitle())
			return true;
		else
			return false;
	}
	
	private string DownloadLyrics(string artist,string title)
	{
		string result=null;
		LyricBaseSource foundAt=null;
		
		//try to download lyric from a source
		for (int i=0;i<sourceList.Count;i++){
			result=((LyricBaseSource)sourceList[i]).GetLyrics(artist,title);
			if (LyricFound(result)){
				foundAt= (LyricBaseSource)sourceList[i];
				break;
			}
		}
			
		if (foundAt!=null && LyricFound(result)){
			base_url=foundAt.Url;
			result=string.Format ("{0} <br><br>",result) + foundAt.GetCredits();
		}
		
		//ok return
		return result;
	}
	
	private string GetSuggestions(string artist,string title)
	{
		//Obtain suggestions from Lyrc (not from the default source)
		LyricBaseSource lyrc_server=(LyricBaseSource)sourceList[0];
		string suggestions=lyrc_server.GetSuggestions(artist,title);
		if (suggestions==null)
				return null;
		
		//there are some suggestions
		suggestions=string.Format ("{0} <br><br>Powered by {1} ({2})",suggestions, lyrc_server.Name, lyrc_server.Url);
		return Constants.find_error_string + "<br><a href=\""+Constants.add_href_changed+"\">"+ Constants.add_lyric_string +"</a><br><br>" + Catalog.GetString("Suggestions:") + suggestions;
	}
}
/*end of private methods*/
}