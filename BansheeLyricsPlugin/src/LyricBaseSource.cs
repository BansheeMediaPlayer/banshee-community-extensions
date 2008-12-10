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
		
	
		
	
	public bool CanAdd(){
			return can_add;
	}

}
}
