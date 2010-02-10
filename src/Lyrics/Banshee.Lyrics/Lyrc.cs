using System;
using System.Text.RegularExpressions;
using Mono.Unix;
using System.Threading;
using System.IO;
using System.Net;
using System.Text;
namespace Banshee.Plugins.Lyrics
{
public class Lyrc : Banshee.Plugins.Lyrics.LyricBaseSource
{
	private string suggestion=null;
	
	public override string Name { get { return "<Lyrc> www.lyrc.com.ar"; }}
    public override string Url { get { return lyricURL; }}                 
	
	public Lyrc()
	{
		lyricURL = "http://lyrc.com.ar/en/";
		can_add=true;
	}
	
	private string ParseSuggestions(string toparse_html)
	{
			string parsed_html=null;
			Regex r1 = new Regex ("Suggestions :(.*)If none is your song",
                                  RegexOptions.Multiline|RegexOptions.IgnoreCase |
                                  RegexOptions.Singleline);
			if (r1.IsMatch (toparse_html)) {
				Match m = r1.Match(toparse_html);
				parsed_html =m.Groups[1].ToString();	
			}
			return parsed_html;
	}
	
	public override string GetSuggestions(string artist,string title)
	{
		string lyrics=null;
		if (suggestion!=null)
				return suggestion;
		//suggestions not present yet.. download it
		string url = string.Format(
                         "http://lyrc.com.ar/en/tema1en.php" +
                         "?artist={0}&songname={1}",
                         System.Web.HttpUtility.UrlEncode(base.cleanArtistName(artist)),
                         System.Web.HttpUtility.UrlEncode(base.cleanSongTitle(title))
                         );
		string html=ReadPageContent(url);
		lyrics=ParseSuggestions(html);
		return lyrics;
	}
			
    public override string GetLyrics(String url)
	{
        //use always absolute url
        string lyrics=null;
        string html=ReadPageContent(url);
		//regular expression
        Regex r = new Regex ("</table>(.*?)<p>",
                             RegexOptions.IgnorePatternWhitespace |
                             RegexOptions.Multiline|RegexOptions.IgnoreCase |
                             RegexOptions.Singleline);
		suggestion=null;
    	if (r.IsMatch (html)) {
        	Match m = r.Match(html);
        	lyrics = m.Groups[1].ToString();
		} else if (ParseSuggestions(html)!=null){
			suggestion=ParseSuggestions(html);
		}
    	return lyrics;
    }
    
    public override string GetLyrics(string artist, string title)
    {
       	string url = string.Format(
                         "http://lyrc.com.ar/en/tema1en.php" +
                         "?artist={0}&songname={1}",
                         System.Web.HttpUtility.UrlEncode(base.cleanArtistName(artist)),
                         System.Web.HttpUtility.UrlEncode(base.cleanSongTitle(title))
                         );
    	return GetLyrics(url);
    }
	
	public override void AddLyrics(string artist,string title,string album, string year,string lyric)
	{
			// Create a request using a URL that can receive a post. 
            WebRequest request = WebRequest.Create ("http://www.lyrc.com.ar/en/add/add.php");
            // Set the Method property of the request to POST.
            request.Method = "POST";
            // Create POST data and convert it to a byte array.
			
            string postdata ="grupo="+artist;
			postdata=postdata+"&disco="+album;
			postdata=postdata+"&tema="+title;
			postdata=postdata+"&ano="+year;
			postdata=postdata+"&texto="+lyric;			
			postdata=postdata+"&procesado="+"nobody@nobody.org";
            byte[] byteArray = Encoding.UTF8.GetBytes (postdata);
            // Set the ContentType property of the WebRequest.
            request.ContentType = "application/x-www-form-urlencoded";
            // Set the ContentLength property of the WebRequest.
            request.ContentLength = byteArray.Length;
            // Get the request stream.
            Stream dataStream = request.GetRequestStream ();
            // Write the data to the request stream.
            dataStream.Write (byteArray, 0, byteArray.Length);
            // Close the Stream object.
            dataStream.Close ();
            // Get the response.
            WebResponse response = request.GetResponse ();
            // Get the stream containing content returned by the server.
            dataStream = response.GetResponseStream ();
            // Open the stream using a StreamReader for easy access.
            StreamReader reader = new StreamReader (dataStream);
             // Clean up the streams.
            reader.Close ();
            dataStream.Close ();
            response.Close ();
    }
	
    public override string GetCredits ()
    {
        return string.Format("Powered by {0} ({1})","Lyrc",this.Url);
    }
    }
}