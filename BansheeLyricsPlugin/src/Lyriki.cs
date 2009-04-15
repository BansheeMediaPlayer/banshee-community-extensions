using System;
using System.Text.RegularExpressions;
using Mono.Unix;
using System.Threading;
using System.IO;

namespace Banshee.Plugins.Lyrics
{
public class Lyriki : Banshee.Plugins.Lyrics.LyricBaseSource
{
	public override string Name { get { return "<LyricWiki> lyricwiki.org"; }}
	public override string Url { get { return lyricURL; }}
	
	public Lyriki(){
		base.lyricURL = "http://lyricwiki.org";
		can_add=false;
	}
		
    public override string GetLyrics(String Url)
	{
		string lyric=null;
        string html=ReadPageContent(Url);
		//regular expression
        Regex r = new Regex ("<div class='lyricbox' >(.*)<p><!--",
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
		string lyricwiki_url = lyricURL + "/";
		
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
			lyricwiki_url += temp[0].ToString().ToUpper() +  splitted_string[i].Substring(1, splitted_string[i].Length - 1) +"_";
		}
		lyricwiki_url = lyricwiki_url.Substring(0,lyricwiki_url.Length - 1);
		
		//obtain the lyric from the given url
		return GetLyrics(lyricwiki_url);
    }

    public override string GetCredits ()
    {
        return string.Format("Powered by {0} ({1})","LyricWiki",this.Url);
    }
    }
}