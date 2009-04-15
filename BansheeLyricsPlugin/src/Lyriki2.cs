// Lyriki2.cs created with MonoDevelop
// User: martellinic at 14:30 11/12/2008
//


// Liryki2.cs created with MonoDevelop
// User: sgrang at 23:20 27/08/2007
//
// To change standard headers go to Edit->Preferences->Coding->Standard Headers
//

using System;
using System.Text.RegularExpressions;
using Mono.Unix;
using System.Threading;
using System.IO;
using System.Net;
using System.Text;

namespace Banshee.Plugins.Lyrics
{
public class Lyriki2 :Banshee.Plugins.Lyrics.LyricBaseSource
{
	private string lyricURL="http://www.lyriki.com";

	public override string Name { get { return "<Lyriki> www.lyriki.com"; }}
    public override string Url { get { return lyricURL; }}
	
    public Lyriki2()
	{
		can_add=false;
	}
	
	private string ReadPageContent(String Url)
	{
		//use always absolute url
        if (!Url.Contains(lyricURL))
            Url=lyricURL+Url;
    
		Console.WriteLine("loading url: "+Url);
        string html=null;
        try{
            html = base.GetSource(Url);
        }catch(Exception e){
        	Console.WriteLine("unable to contact server!"+e.Message);
            return "Unable to contact server!";
        }
		return html;
	}
		
    public override string GetLyrics(String Url)
	{
		string lyric=null;
        string html=ReadPageContent(Url);
		//regular expression
        Regex r = new Regex ("<p>(.*?)</p><!",
                             RegexOptions.Multiline|RegexOptions.IgnoreCase |
                             RegexOptions.Singleline);
    	if (r.IsMatch (html)) {
        	Match m = r.Match(html);
        	lyric = m.Groups[1].ToString();
        }
    	return lyric;
    }
    
	public override string GetSuggestions(string artist,string title)
	{
		string url = string.Format(lyricURL + "/{0}:{1}",System.Web.HttpUtility.UrlEncode(base.cleanArtistName(artist)),
					                            System.Web.HttpUtility.UrlEncode(BansheeWidgets.CurrentTrack.GetAlbum()));
		return GetLyrics(url.Replace("+","_"));
	}
	
	public override string GetLyrics(string artist, string title)
	{
       	string url = string.Format(lyricURL +"/{0}:{1}",System.Web.HttpUtility.UrlEncode(base.cleanArtistName(artist)),
			                       System.Web.HttpUtility.UrlEncode(base.cleanSongTitle(title)));
		string lyriki_url = lyricURL + "/";
		
		/*transform url to match real lyricwiki url form*/
		string   relative_url    = (url.Replace("+","_")).Replace(lyricURL+"/","");
		string[] splitted_string =  relative_url.Split('_');
		/*make first character of each word upper*/
		for (int i = 0 ; i < splitted_string.Length ; i++)
		{
			if (splitted_string[i].Length == 0) {
				continue;
			}
			char[] temp = splitted_string[i].ToCharArray();
			lyriki_url += temp[0].ToString().ToUpper() +  splitted_string[i].Substring(1, splitted_string[i].Length - 1) +"_";
		}
		lyriki_url = lyriki_url.Substring(0,lyriki_url.Length - 1);
		
		//obtain the lyric from the given url
		return GetLyrics(lyriki_url);
	}
	
	public override void AddLyrics(string artist,string title,string album, string year,string lyric)
	{
    }
	
    public override string GetCredits ()
    {
        return string.Format("Powered by {0} ({1})","Lyriki",this.Url);
    }
  }
}