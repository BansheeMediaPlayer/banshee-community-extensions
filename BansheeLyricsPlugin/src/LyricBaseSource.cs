using System;
using System.IO;
using System.Net;
using Mono.Unix;
using System.Collections;
using System.Text.RegularExpressions;
using System.Threading;
using System.Text;
using GConf;

namespace Banshee.Plugins.Lyrics
{
public abstract class LyricBaseSource
{
	protected string lyricURL;
	
	//constructor
	protected bool can_add=false;
	protected LyricBaseSource(){
	}
		
    public abstract string Name {
        get;
    }
    public abstract string Url {
            get;
    }
    
    public virtual string GetLyrics(string Url)
        {
            return null;
        }
    public virtual string GetLyrics(string artist, string title)
    {
        return null;
    }
    public virtual string GetSuggestions(string artist,string title){
			return null;
    }
	public virtual void AddLyrics(string artist,string title,string album, string year,string lyric){
    }
    
	public virtual string GetCredits () { return ""; }
    
	protected string ReadPageContent(String url)
	{
		//use always absolute url
        if (!url.Contains(lyricURL))
            url=lyricURL+url;
    
        string html=null;
        try{
            html = GetSource(url);
        }catch(Exception e){
        	Hyena.Log.Debug("Unable to contact server " + lyricURL +": " + e.Message);
            return Catalog.GetString("Unable to contact server!");
        }
		return html;
	}
	
    protected string GetSource (string url)
    {
    string source = "";
		HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
		ProxyManager pm = ProxyManager.getInstance();
		if(pm.isHttpProxy()){
			request.Proxy = pm.getProxy(url);
		}

		request.Timeout = 6000;
    HttpWebResponse response = (HttpWebResponse)request.GetResponse();

    if(response.ContentLength == 0) {
            return source;
        }
		StreamReader reader;	
		
		if (response.ContentType.Contains("utf"))
			reader = new StreamReader(response.GetResponseStream());
		else
			reader = new StreamReader(response.GetResponseStream(),Encoding.GetEncoding("iso-8859-1"));
		
		//read all bytes from the stream
    source = reader.ReadToEnd();
    reader.Close();
    return source;
    }
	
	public string cleanArtistName(string artist) {
		if(artist.EndsWith(" ")) {
			artist = artist.Substring(0,artist.Length-2);
		}
		return artist;
	}
		
	public string cleanSongTitle(string songname) {
		if(songname.EndsWith(" ")) {
			songname = songname.Substring(0,songname.Length-1);
		}
		return songname;
	}
	
		
	
	public bool CanAdd(){
			return can_add;
	}

}
}
