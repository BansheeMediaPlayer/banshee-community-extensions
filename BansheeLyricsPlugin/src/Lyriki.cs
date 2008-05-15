using System;
using System.Text.RegularExpressions;
using Mono.Unix;
using System.Threading;
using System.IO;

namespace Banshee.Plugins.Lyrics
{
public class Lyriki : Banshee.Plugins.Lyrics.LyricBaseSource
{
	private string lyricURL="http://www.lyricwiki.org";
	//private string lyric_not_found="Unable to find Lyrics. <br> Suggestions :";
	public Lyriki(){
			can_add=false;
	}
	public override string Name { get { return "<LyricWiki> www.lyricwiki.org"; }}
    public override string Url { get { return lyricURL; }}
        
	private string ReadPageContent(String Url){
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
    public override string GetLyrics(String Url){
		string lyric=null;
        string html=ReadPageContent(Url);
		//regular expression
        Regex r = new Regex ("<div id=\"lyric\">(.*)<div style=\"clear:both;\">",
                             RegexOptions.Multiline|RegexOptions.IgnoreCase |
                             RegexOptions.Singleline);
    	if (r.IsMatch (html)) {
        	Match m = r.Match(html);
        	lyric = m.Groups[1].ToString();
        }
    	return lyric;
    }
    public override string GetSuggestions(string artist,string title){
		
			string url = string.Format(lyricURL + "/{0}:{1}",System.Web.HttpUtility.UrlEncode(artist),
					                            System.Web.HttpUtility.UrlEncode(BansheeWidgets.CurrentTrack.GetAlbum()));
			return GetLyrics(url.Replace("+","_"));
		
	}
    public override string GetLyrics(string artist, string title)
    {
       	string url = string.Format(lyricURL +"/{0}:{1}",System.Web.HttpUtility.UrlEncode(artist),
			                       System.Web.HttpUtility.UrlEncode(title));
		//obtain the lyric from the given url
    	string lyric= GetLyrics(url.Replace("+","_"));
		
		return lyric;
    }

    public override string GetCredits ()
    {
        return string.Format("Powered by {0} (<a href=\"{1}\">{2}</a>)",
                             this.Name,
                             this.Url,
                             this.Url);
    }
    }
}