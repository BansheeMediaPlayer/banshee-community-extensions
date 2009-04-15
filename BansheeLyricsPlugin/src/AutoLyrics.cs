using System;
using System.Text.RegularExpressions;
using Mono.Unix;
using System.Threading;
using System.IO;
using System.Net;
using System.Text;
namespace Banshee.Plugins.Lyrics
{
public class AutoLyrics : Banshee.Plugins.Lyrics.LyricBaseSource
{
	private string lyricURL="http://www.autolyrics.com/";
	
	private string suggestion=null;
	public AutoLyrics(){
			can_add=false;
	}
	public override string Name { get { return "<Autolyrics> http://www.autolyrics.com/"; }}
    public override string Url { get { return lyricURL; }}                 
	private string ReadPageContent(String url){
		//use always absolute url
        if (!url.Contains(lyricURL))
            url=lyricURL+url;
        
			string html=null;
        try{
            html = base.GetSource(url);
        }catch(Exception e){
        	Console.WriteLine("unable to contact server!"+e.Message);
            return Catalog.GetString("Unable to contact server!");
        }
		return html;
	}
	
	public override string GetSuggestions(string artist,string title){
			return suggestion;
	}
			
    public override string GetLyrics(String url){
        //use always absolute url
        string lyrics=null;
        string html=ReadPageContent(url);
		//regular expression
		if (html==null)
			return null;
        Regex r = new Regex ("<img src=\"img/pix_discontinua.gif\" width=\"400\" height=\"3\"></td>(.*)TEXTmagenta",
                             RegexOptions.Multiline|RegexOptions.IgnoreCase |
                             RegexOptions.Singleline);
    	if (r.IsMatch (html)) {
        	Match m = r.Match(html);
        	lyrics = m.Groups[1].ToString();
		}
		//fuckin autolyrics
		if (lyrics!=null && (lyrics.Contains("If none is your song")||  lyrics.Contains("Nothing found :")))
			    return null;
    	return lyrics;
    }
    
    public override string GetLyrics(string artist, string title)
    {
       	string url = string.Format(
                         "http://www.autolyrics.com/tema1en.php" +
                         "?artist={0}&songname={1}",
                         System.Web.HttpUtility.UrlEncode(base.cleanArtistName(artist)),
                         System.Web.HttpUtility.UrlEncode(base.cleanSongTitle(title))
                         );
    	return GetLyrics(url);
    }
	public override void AddLyrics(string artist,string title,string album, string year,string lyric){
	
    }
    public override string GetCredits ()
    {
      return string.Format("Powered by {0} ({1})","AutoLyrics",this.Url);
    }
    }
}